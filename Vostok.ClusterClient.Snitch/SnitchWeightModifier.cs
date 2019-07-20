using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Ordering.Storage;
using Vostok.Clusterclient.Core.Ordering.Weighed;
using Vostok.Commons.Collections;
using Vostok.Logging.Abstractions;
using Vostok.ServiceDiscovery.Abstractions;
using Vostok.ServiceDiscovery.Extensions;

namespace Vostok.Clusterclient.Snitch
{
    /// <summary>
    /// <para>A weight modifier that applies weights computed by Snitch and stored in Service Discovery nodes.</para>
    /// </summary>
    [PublicAPI]
    public class SnitchWeightModifier : IReplicaWeightModifier
    {
        private readonly IServiceLocator serviceLocator;
        private readonly string environment;
        private readonly string application;
        private readonly ILog log;

        private readonly CachingTransform<IServiceTopology, ReplicaWeights> transform;

        public SnitchWeightModifier(
            [NotNull] IServiceLocator serviceLocator,
            [NotNull] string environment,
            [NotNull] string application,
            [CanBeNull] ILog log)
        {
            this.serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
            this.application = application ?? throw new ArgumentNullException(nameof(application));
            this.log = log ?? LogProvider.Get();

            transform = new CachingTransform<IServiceTopology, ReplicaWeights>(ExtractWeights);
        }

        public void Modify(Uri replica, IList<Uri> allReplicas, IReplicaStorageProvider storageProvider, Request request, RequestParameters parameters, ref double weight)
        {
            var weights = transform.Get(serviceLocator.Locate(environment, application));
            if (weights == null || !weights.TryGetValue(replica, out var snitchWeight))
                return;

            weight *= snitchWeight.Value;
        }

        public void Learn(ReplicaResult result, IReplicaStorageProvider storageProvider)
        {
        }

        [CanBeNull]
        private ReplicaWeights ExtractWeights([CanBeNull] IServiceTopology topology)
        {
            if (topology?.Properties == null)
                return null;

            var weights = topology.Properties.GetReplicaWeights();
            if (weights != null)
                LogWeights(weights);

            return weights;
        }

        #region Logging

        private void LogWeights([NotNull] ReplicaWeights weights)
        {
            if (!log.IsEnabledForDebug())
                return;

            var builder = new StringBuilder();

            if (weights.Count == 0)
            {
                builder.Append("[empty]");
            }
            else
            {
                foreach (var pair in weights)
                {
                    builder.Append("\n\t");
                    builder.Append(pair.Key.DnsSafeHost);
                    builder.Append(":");
                    builder.Append(pair.Key.Port);
                    builder.Append(" - ");
                    builder.Append(pair.Value.Value.ToString("F4"));
                }
            }

            log.Debug(
                "Updated weights for topology '{Topology}' in environment '{Environment}': {Weights}",
                application,
                environment,
                builder.ToString());
        }

        #endregion
    }
}
