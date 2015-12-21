namespace NServiceBus.RavenDB.Persistence.SubscriptionStorage
{
    /// <summary>
    /// This is an anti-corruption layer against changes in NServiceBus Core
    /// </summary>
    class SubscriptionClient
    {
        public string TransportAddress { get; set; }

        public string Endpoint { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj is SubscriptionClient && Equals((SubscriptionClient)obj);
        }

        bool Equals(SubscriptionClient obj) => string.Equals(TransportAddress, obj.TransportAddress) && Equals(Endpoint, obj.Endpoint);

        public override int GetHashCode() => (TransportAddress + Endpoint).GetHashCode();
    }
}
