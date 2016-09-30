namespace NServiceBus.RavenDB.Persistence.SubscriptionStorage
{
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

        bool Equals(SubscriptionClient obj) => string.Equals(TransportAddress, obj.TransportAddress);

        public override int GetHashCode() => TransportAddress.GetHashCode();
    }
}
