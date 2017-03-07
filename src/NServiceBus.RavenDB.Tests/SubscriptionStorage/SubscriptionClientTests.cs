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
            var list = new List<SubscriptionClient>();

            // Different casings of TransportAddress
            list.Add(new SubscriptionClient
            {
                Endpoint = "MyEndpoint",
                TransportAddress = "My.Transport.Address@servername"
            });
            list.Add(new SubscriptionClient
            {
                Endpoint = "MyEndpoint",
                TransportAddress = "My.Transport.Address@SERVERNAME"
            });
            list.Add(new SubscriptionClient
            {
                Endpoint = "MyEndpoint",
                TransportAddress = "MY.TRANSPORT.ADDRESS@SERVERNAME"
            });
            list.Add(new SubscriptionClient
            {
                Endpoint = "MyEndpoint",
                TransportAddress = "my.transport.address@servername"
            });

            // Distinct on different casings should result in 1 item
            var distinctList = list.Distinct().ToList();
            Assert.AreEqual(1, distinctList.Count);
        }
    }
}
