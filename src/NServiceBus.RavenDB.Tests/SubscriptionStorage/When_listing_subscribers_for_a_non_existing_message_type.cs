using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.RavenDB.Tests;
using NServiceBus.Unicast.Subscriptions.RavenDB;
using NUnit.Framework;

[TestFixture]
public class When_listing_subscribers_for_a_non_existing_message_type : RavenDBPersistenceTestBase
{
    [Test]
    public async Task No_subscribers_should_be_returned()
    {
        var persister = new SubscriptionPersister(store);
        var subscriptionsForMessageType = await persister.GetSubscriberAddressesForMessage(MessageTypes.MessageA, new ContextBag());

        Assert.AreEqual(0, subscriptionsForMessageType.Count());
    }
}