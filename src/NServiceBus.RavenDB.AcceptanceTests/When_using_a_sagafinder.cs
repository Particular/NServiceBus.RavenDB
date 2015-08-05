namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Saga;
    using NUnit.Framework;
    using Raven.Client;

    public class When_using_a_sagafinder : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_able_to_access_session()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SagaFinderEndpoint>(b => b.Given(bus =>
                {
                    bus.SendLocal(new StartSagaMessage());
                    return Task.FromResult(0);
                })
                    .When(c => c.SagaId != Guid.Empty, bus =>
                    {
                        bus.SendLocal(new StartSagaMessage());
                        return Task.FromResult(0);
                    }))
                .Done(c =>c.SecondMessageProcessed)
                .Run();

            Assert.True(context.SameSagaInstanceFound,"If the finder is used the same sagas instance should be found");
        }

        public class Context : ScenarioContext
        {
            public Guid SagaId { get; set; }
            public bool SameSagaInstanceFound{ get; set; }

            public bool SecondMessageProcessed { get; set; }
        }

        public class SagaFinderEndpoint : EndpointConfigurationBuilder
        {
            public SagaFinderEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }


            class MySagaFinder: IFindSagas<SagaFinderSagaData>.Using<StartSagaMessage>
            {
                public Context Context { get; set; }
                public SagaFinderSagaData FindBy(StartSagaMessage message, SagaPersistenceOptions options)
                {
                    if (Context.SagaId == Guid.Empty)
                    {
                        return null;
                    }

                    var session = options.Context.Get<IDocumentSession>();
                    return session.Load<SagaFinderSagaData>(Context.SagaId);
                }
            }

            public class SagaFinderSaga : Saga<SagaFinderSagaData>, IAmStartedByMessages<StartSagaMessage>
            {
                public Context Context { get; set; }
                public void Handle(StartSagaMessage message)
                {
                    if (Context.SagaId != Guid.Empty)
                    {
                        Context.SameSagaInstanceFound = Context.SagaId == Data.Id;

                        Context.SecondMessageProcessed = true;

                        return;
                    }
                    Context.SagaId = Data.Id;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaFinderSagaData> mapper)
                {
                }

            }

            public class SagaFinderSagaData : IContainSagaData
            {
                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }

                public virtual Guid SomeId { get; set; }
            }
        }

        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }

    }
}