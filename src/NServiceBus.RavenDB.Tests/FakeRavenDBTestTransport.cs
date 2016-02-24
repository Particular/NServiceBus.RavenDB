namespace NServiceBus.RavenDB.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Routing;
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    /// <summary>
    /// This is a copy from NServiceBus.AcceptanceTests
    /// </summary>
    public class FakeRavenDBTransportInfrastructure : TransportInfrastructure
    {
        public FakeRavenDBTransportInfrastructure(TransportTransactionMode transactionMode)
        {
            this.transactionMode = transactionMode;
        }

        public override EndpointInstance BindToLocalEndpoint(EndpointInstance instance)
        {
            throw new NotImplementedException();
        }

        public override string ToTransportAddress(LogicalAddress logicalAddress)
        {
            throw new NotImplementedException();
        }

        public override TransportReceiveInfrastructure ConfigureReceiveInfrastructure()
        {
            throw new NotImplementedException();
        }

        public override TransportSendInfrastructure ConfigureSendInfrastructure()
        {
            throw new NotImplementedException();
        }

        public override TransportSubscriptionInfrastructure ConfigureSubscriptionInfrastructure()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<Type> DeliveryConstraints { get; } = Enumerable.Empty<Type>();

        private TransportTransactionMode transactionMode;

        public override TransportTransactionMode TransactionMode => transactionMode;

        public override OutboundRoutingPolicy OutboundRoutingPolicy { get; } = new OutboundRoutingPolicy(OutboundRoutingType.Unicast, OutboundRoutingType.Unicast, OutboundRoutingType.Unicast);
    }

    /// <summary>
    /// This is a copy from NServiceBus.AcceptanceTests
    /// </summary>
    public class FakeRavenDBTestTransport : TransportDefinition
    {
        private TransportTransactionMode transactionMode;

        public FakeRavenDBTestTransport(TransportTransactionMode transactionMode)
        {
            this.transactionMode = transactionMode;
        }

        protected override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
        {
            return new FakeRavenDBTransportInfrastructure(transactionMode);
        }

        public override bool RequiresConnectionString => false;

        public override string ExampleConnectionStringForErrorMessage => null;
    }
}
