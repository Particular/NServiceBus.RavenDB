using System.Collections.Generic;
using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
using NServiceBus.Unicast.Subscriptions;
using NUnit.Framework;
using Particular.Approvals;

[TestFixture]
public class SubscriptionApprovals
{
    [Test]
    public void ApproveSubscriptionSchema()
    {
        // if the schema is changed, the schema version must be incremented
        Approver.Verify(new Subscription
        {
            Id = nameof(Subscription.Id),
            MessageType = new MessageType("System.Object", "4.0.0"),
            Subscribers =
            [
                new SubscriptionClient
                {
                    Endpoint = nameof(SubscriptionClient.Endpoint),
                    TransportAddress = nameof(SubscriptionClient.TransportAddress),
                },
            ],
        });
    }
}
