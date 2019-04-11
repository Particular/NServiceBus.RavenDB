// TODO: No IDocumentConversionListener?

//namespace NServiceBus.Persistence.RavenDB
//{
//    using System.Linq;
//    using Newtonsoft.Json.Linq;
//    using NServiceBus.RavenDB.Persistence.SubscriptionStorage;

//    class SubscriptionV1toV2Converter : IDocumentConversionListener
//    {
//        public void BeforeConversionToDocument(string key, object entity, JObject metadata)
//        {
//            var subscription = entity as Subscription;

//            if (subscription == null)
//            {
//                return;
//            }

//            var converted = LegacyAddress.ConvertMultipleToLegacyAddress(subscription.Subscribers);
//            subscription.LegacySubscriptions.Clear();
//            subscription.LegacySubscriptions.AddRange(converted);
//        }

//        public void AfterConversionToDocument(string key, object entity, JObject document, JObject metadata)
//        {

//        }

//        public void BeforeConversionToEntity(string key, JObject document, JObject metadata)
//        {
//        }

//        public void AfterConversionToEntity(string key, JObject document, JObject metadata, object entity)
//        {
//            var subscription = entity as Subscription;

//            if (subscription == null)
//            {
//                return;
//            }

//            var clients = document["Clients"];

//            if (clients != null)
//            {
//                var converted = LegacyAddress.ParseMultipleToSubscriptionClient(subscription.LegacySubscriptions);

//                var legacySubscriptions = converted.Except(subscription.Subscribers).ToArray();
//                foreach (var legacySubscription in legacySubscriptions)
//                {
//                    subscription.Subscribers.Add(legacySubscription);
//                }
//            }
//        }
//    }
//}