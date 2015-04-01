namespace NServiceBus.RavenDB.Internal
{
    using System;
    using System.Linq;
    using Raven.Imports.Newtonsoft.Json.Linq;
    using Raven.Json.Linq;

    static class LegacyAddress
    {
        public static RavenJToken ParseMultiple(Func<RavenJArray> tokenSelector)
        {
            var array = tokenSelector() ;

            var clients = array.Select(token => Parse(() => token)).ToList();

            return new RavenJArray(clients);
        }

        public static string Parse(Func<RavenJToken> tokenSelector)
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