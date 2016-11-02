using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Persistence.RavenDB;
using NServiceBus.RavenDB.Tests;
using NUnit.Framework;

[TestFixture]
public class When_listing_subscribers_for_a_non_existing_message_type : RavenDBPersistenceTestBase
{
    [Test]
    public async Task No_subscribers_should_be_returned()
    {
        await SubscriptionIndex.Create(store);

        var persister = new SubscriptionPersister(store);
        var subscriptionsForMessageType = await persister.GetSubscriberAddressesForMessage(new []{ MessageTypes.MessageA }, new ContextBag());

        Assert.AreEqual(0, subscriptionsForMessageType.Count());
    }
}