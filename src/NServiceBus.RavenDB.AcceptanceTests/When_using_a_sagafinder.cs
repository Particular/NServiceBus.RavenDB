namespace NServiceBus.RavenDB.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Raven.Client;

    public class When_using_a_sagafinder : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_able_to_access_session()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SagaFinderEndpoint>(b => b
                    .When(bus => bus.SendLocal(new StartSagaMessage()))
                    .When(c => c.SagaId != Guid.Empty, bus => bus.SendLocal(new StartSagaMessage())))
                .Done(c => c.SecondMessageProcessed)
                .Run();

            Assert.True(context.SameSagaInstanceFound, "If the finder is used the same sagas instance should be found");
        }

        public class Context : ScenarioContext
        {
            public Guid SagaId { get; set; }
            public bool SameSagaInstanceFound { get; set; }

            public bool SecondMessageProcessed { get; set; }
        }

        public class SagaFinderEndpoint : EndpointConfigurationBuilder
        {
            public SagaFinderEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.LimitMessageProcessingConcurrencyTo(1));
            }


            class MySagaFinder : IFindSagas<SagaFinderSagaData>.Using<StartSagaMessage>
            {
                public Context Context { get; set; }

                public async Task<SagaFinderSagaData> FindBy(StartSagaMessage message, SynchronizedStorageSession session, ReadOnlyContextBag options)
                {
                    if (Context.SagaId == Guid.Empty)
                    {
                        return await Task.FromResult(default(SagaFinderSagaData));
                    }

                    var ravenSession = options.Get<IAsyncDocumentSession>();
                    return await ravenSession.LoadAsync<SagaFinderSagaData>(Context.SagaId).ConfigureAwait(false);
                }
            }

            public class SagaFinderSaga : Saga<SagaFinderSagaData>, IAmStartedByMessages<StartSagaMessage>
            {
                public Context Context { get; set; }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    if (Context.SagaId != Guid.Empty)
                    {
                        Context.SameSagaInstanceFound = Context.SagaId == Data.Id;

                        Context.SecondMessageProcessed = true;

                        return Task.FromResult(0);
                    }
                    Context.SagaId = Data.Id;
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaFinderSagaData> mapper)
                {
                }
            }

            public class SagaFinderSagaData : IContainSagaData
            {
                public Guid SomeId { get; set; }
                public Guid Id { get; set; }
                public string Originator { get; set; }
                public string OriginalMessageId { get; set; }
            }
        }

        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }
    }
}