using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Persistence.RavenDB;
using NServiceBus.RavenDB.Tests;
using NUnit.Framework;
using NServiceBus.RavenDB.Persistence.SubscriptionStorage;

[TestFixture]
public class When_listing_subscribers_for_a_non_existing_message_type : RavenDBPersistenceTestBase
{
    [Test]
    public async Task No_subscribers_should_be_returned()
    {
        var idFormatter = new SubscriptionIdFormatter(useMessageVersionToGenerateSubscriptionId: true);
        var persister = new SubscriptionPersister(store, idFormatter);
        var subscriptionsForMessageType = await persister.GetSubscriberAddressesForMessage(new []{ MessageTypes.MessageA }, new ContextBag());

        Assert.AreEqual(0, subscriptionsForMessageType.Count());
    }
}