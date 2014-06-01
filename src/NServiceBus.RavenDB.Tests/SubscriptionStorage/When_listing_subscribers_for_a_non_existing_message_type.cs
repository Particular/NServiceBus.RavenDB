using System.Linq;
using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
using NUnit.Framework;

[TestFixture]
public class When_listing_subscribers_for_a_non_existing_message_type
{
    [Test]
    public void No_subscribers_should_be_returned()
    {
        using (var store = DocumentStoreBuilder.Build())
        {
            var storage = new RavenSubscriptionStorage(store);

            storage.Init();
            var subscriptionsForMessageType = storage.GetSubscriberAddressesForMessage(MessageTypes.MessageA);

            Assert.AreEqual(0, subscriptionsForMessageType.Count());
        }
    }
}