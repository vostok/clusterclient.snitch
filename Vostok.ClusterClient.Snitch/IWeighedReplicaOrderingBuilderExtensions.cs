using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Ordering.Weighed;
using Vostok.ServiceDiscovery.Abstractions;

namespace Vostok.Clusterclient.Snitch
{
    [PublicAPI]
    public static class IWeighedReplicaOrderingBuilderExtensions
    {
        /// <summary>
        /// Adds a <see cref="SnitchWeightModifier"/> that will apply external weights stored in Service Discovery nodes for given <paramref name="application"/> from given <paramref name="environment"/>.
        /// </summary>
        public static void SetupSnitchWeightModifier(
            [NotNull] this IWeighedReplicaOrderingBuilder self,
            [NotNull] IServiceLocator serviceLocator,
            [NotNull] string environment,
            [NotNull] string application)
        {
            self.AddModifier(new SnitchWeightModifier(serviceLocator, environment, application, self.Log));
        }
    }
}