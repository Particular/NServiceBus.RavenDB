﻿using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.RavenDB.Tests;
using NServiceBus.SagaPersisters.RavenDB;
using NUnit.Framework;
using Raven.Client;

[TestFixture]
public class Saga_with_unique_property_set_to_null : RavenDBPersistenceTestBase
{
    [Test]
    public async Task should_throw_a_ArgumentNullException()
    {
        var saga1 = new SagaData
        {
            Id = Guid.NewGuid(),
            UniqueString = null
        };

        IDocumentSession session;
        var context = this.CreateContextWithAsyncSessionPresent(out session);
        var ravenSession = new RavenDBSynchronizedStorageSession(session, true);
        var persister = new SagaPersister();

        var exception = await Catch<ArgumentNullException>(async () =>
        {
            await persister.Save(saga1, this.CreateMetadata<SomeSaga>(saga1), ravenSession, context);
            await session.SaveChangesAsync().ConfigureAwait(false);
        });

        Assert.IsNotNull(exception);
    }

    class StartSaga
    {
    }

    class SomeSaga : Saga<SagaData>, IAmStartedByMessages<StartSaga>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<Message>(m => m.UniqueString).ToSaga(s => s.UniqueString);
        }

        class Message
        {
            public string UniqueString { get; set; }
        }

        public Task Handle(StartSaga message, IMessageHandlerContext context)
        {
            return Task.FromResult(0);
        }
    }

    class SagaData : IContainSagaData
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public string UniqueString { get; set; }
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }
}