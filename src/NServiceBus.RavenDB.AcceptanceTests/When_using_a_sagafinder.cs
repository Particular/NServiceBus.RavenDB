namespace NServiceBus.RavenDB.AcceptanceTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    public class When_using_a_sagafinder : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw()
        {
            var exception = Assert.ThrowsAsync<Exception>(async () =>
            {
                await Scenario.Define<Context>()
                     .WithEndpoint<SagaFinderEndpoint>(b => b
                         .When(bus => bus.SendLocal(new StartSagaMessage()))
                         .When(c => c.SagaId != Guid.Empty, bus => bus.SendLocal(new StartSagaMessage())))
                     .Done(c => c.SecondMessageProcessed)
                     .Run();
            });

            Assert.IsTrue(exception.Message.Contains("does not support custom saga finders"), "Exception message did not contain expected phrase");
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

                public Task<SagaFinderSagaData> FindBy(StartSagaMessage message, SynchronizedStorageSession session, ReadOnlyContextBag options, CancellationToken cancellationToken)
                {
                    if (Context.SagaId == Guid.Empty)
                    {
                        return Task.FromResult(default(SagaFinderSagaData));
                    }

                    return session.RavenSession().LoadAsync<SagaFinderSagaData>(Context.SagaId.ToString(), cancellationToken);
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