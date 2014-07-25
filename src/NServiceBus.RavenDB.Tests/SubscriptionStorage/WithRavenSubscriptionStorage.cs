namespace NServiceBus.Core.Tests.Persistence.RavenDB.SubscriptionStorage
{
    using NServiceBus.RavenDB.Persistence;
    using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
    using NUnit.Framework;
    using Raven.Client;
    using Raven.Client.Document;
    using Raven.Client.Embedded;

    public class WithRavenSubscriptionStorage
    {
        protected ISubscriptionStorage storage;
        protected IDocumentStore store;

        [SetUp]
        public void SetupContext()
        {
            store = new EmbeddableDocumentStore { RunInMemory = true};
            store.Conventions.DefaultQueryingConsistency = ConsistencyOptions.AlwaysWaitForNonStaleResultsAsOfLastWrite;
           
            store.Initialize();

            storage = new RavenSubscriptionStorage(new StoreAccessor(store));
            storage.Init();
        }

        [TearDown]
        public void Cleanup()
        {
            if (store != null)
            {
                store.Dispose();
            }
        }
    }
}