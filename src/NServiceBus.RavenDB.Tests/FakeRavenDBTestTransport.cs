namespace NServiceBus.RavenDB.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Routing;
    using NServiceBus.Transport;

    /// <summary>
    /// This is a copy from NServiceBus.AcceptanceTests to allow mocking TransportTransactionMode
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

        TransportTransactionMode transactionMode;

        public override TransportTransactionMode TransactionMode => transactionMode;

        public override OutboundRoutingPolicy OutboundRoutingPolicy { get; } = new OutboundRoutingPolicy(OutboundRoutingType.Unicast, OutboundRoutingType.Unicast, OutboundRoutingType.Unicast);
    }
}
