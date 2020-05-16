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
        // if the schema is changed make sure to increase the schema version
        Approver.Verify(new Subscription
        {
            Id = nameof(Subscription.Id),
            MessageType = new MessageType("System.Object", "4.0.0"),
            Subscribers = new List<SubscriptionClient>
            {
                new SubscriptionClient
                {
                    Endpoint = nameof(SubscriptionClient.Endpoint),
                    TransportAddress = nameof(SubscriptionClient.TransportAddress)
                }
            },
        });
    }
}