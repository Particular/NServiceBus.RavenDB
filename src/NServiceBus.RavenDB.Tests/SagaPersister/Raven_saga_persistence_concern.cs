namespace NServiceBus.Core.Tests.Persistence.RavenDB.SagaPersister
{
    using System;
    using NServiceBus.RavenDB.Persistence;
    using NServiceBus.RavenDB.Persistence.SagaPersister;
    using NUnit.Framework;
    using Raven.Client.Document;
    using Raven.Client.Embedded;
    using Saga;

    abstract class Raven_saga_persistence_concern
    {
        protected DocumentStore store;

        [TestFixtureSetUp]
        public virtual void Setup()
        {
            store = new EmbeddableDocumentStore { RunInMemory = true };
            
            store.Initialize();
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            store.Dispose();
        }

        public void WithASagaPersistenceUnitOfWork(Action<RavenSagaPersister> action)
        {
            var sessionFactory = new RavenSessionFactory(new StoreAccessor(store));

            try
            {
                var sagaPersister = new RavenSagaPersister(sessionFactory);
                action(sagaPersister);

                sessionFactory.SaveChanges();
            }
            finally 
            {
                sessionFactory.ReleaseSession();
                
            }           
        }

        protected void SaveSaga<T>(T saga) where T : IContainSagaData
        {
            WithASagaPersistenceUnitOfWork(p => p.Save(saga));
        }

        protected void CompleteSaga<T>(Guid sagaId) where T : IContainSagaData
        {
            WithASagaPersistenceUnitOfWork(p =>
                                           {
                                               var saga = p.Get<T>(sagaId);
                                               Assert.NotNull(saga, "Could not complete saga. Saga not found");
                                               p.Complete(saga);
                                           });
        }

        protected void UpdateSaga<T>(Guid sagaId, Action<T> update) where T : IContainSagaData
        {
            WithASagaPersistenceUnitOfWork(p =>
            {
                var saga = p.Get<T>(sagaId);
                Assert.NotNull(saga, "Could not update saga. Saga not found");
                update(saga);
                p.Update(saga);
            });
        }
    }
}
