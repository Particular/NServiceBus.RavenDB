namespace NServiceBus.RavenDB.Tests.SubscriptionStorage
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
    using NUnit.Framework;

    public class SubscriptionClientTests
    {
        [Test]
        public void ShouldCompareNoCase()
        {
            var list = new List<SubscriptionClient>
            {
                // Different casings of TransportAddress
                new SubscriptionClient
                {
                    Endpoint = "MyEndpoint",
                    TransportAddress = "My.Transport.Address@servername"
                },
                new SubscriptionClient
                {
                    Endpoint = "MyEndpoint",
                    TransportAddress = "My.Transport.Address@SERVERNAME"
                },
                new SubscriptionClient
                {
                    Endpoint = "MyEndpoint",
                    TransportAddress = "MY.TRANSPORT.ADDRESS@SERVERNAME"
                },
                new SubscriptionClient
                {
                    Endpoint = "MyEndpoint",
                    TransportAddress = "my.transport.address@servername"
                }
            };

            // Distinct on different casings should result in 1 item
            var distinctList = list.Distinct().ToList();
            Assert.That(distinctList, Has.Count.EqualTo(1));
        }
    }
}
