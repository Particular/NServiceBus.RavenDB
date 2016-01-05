namespace NServiceBus.RavenDB.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
    using Raven.Imports.Newtonsoft.Json.Linq;
    using Raven.Json.Linq;

    static class LegacyAddress
    {
        public static List<SubscriptionClient> ParseMultipleToSubscriptionClient(RavenJArray array) => array.Select(ParseToSubscriptionClient).ToList();

        public static SubscriptionClient ParseToSubscriptionClient(RavenJToken token)
        {
            var queue = token.Value<string>("Queue");
            var machine = token.Value<string>("Machine");

            // Previously known as IgnoreMachineName (for brokers)
            if (string.IsNullOrEmpty(machine))
            {
                return new SubscriptionClient { TransportAddress = queue, Endpoint = queue };
            }

            return new SubscriptionClient { TransportAddress = queue + "@" + machine, Endpoint = queue };
        }

        public static string ParseToString(Func<RavenJToken> tokenSelector)
        {
            var token = tokenSelector();

            // When we have the new timeout data we just return the value
            if (token.Type == JTokenType.String)
            {
                return token.Value<string>();
            }

            var queue = token.Value<string>("Queue");
            var machine = token.Value<string>("Machine");

            // Previously known as IgnoreMachineName (for brokers)
            if (string.IsNullOrEmpty(machine))
            {
                return queue;
            }

            return queue + "@" + machine;
        }
    }
}