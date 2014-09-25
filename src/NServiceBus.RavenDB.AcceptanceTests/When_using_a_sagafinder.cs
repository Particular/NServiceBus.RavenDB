namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.RavenDB.Persistence;
    using NServiceBus.Saga;
    using NUnit.Framework;

    public class When_using_a_sagafinder : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_be_able_to_access_session()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<SagaFinderEndpoint>(b => b.Given(bus => bus.SendLocal(new StartSagaMessage()))
                    .When(c => c.SagaId != Guid.Empty, bus => bus.SendLocal(new StartSagaMessage())))
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


            class MySagaFinder:IFindSagas<TestSagaData>.Using<StartSagaMessage>
            {
                public ISessionProvider SessionProvider { get; set; }

                public Context Context { get; set; }
                public TestSagaData FindBy(StartSagaMessage message)
                {
                    if (Context.SagaId == Guid.Empty)
                    {
                        return null;
                    }

                    return SessionProvider.Session.Load<TestSagaData>(Context.SagaId);
                }
            }

            public class TestSaga : Saga<TestSagaData>, IAmStartedByMessages<StartSagaMessage>
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


                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper)
                {
                }

            }

            public class TestSagaData : IContainSagaData
            {
                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }

                [Unique]
                public virtual Guid SomeId { get; set; }
            }
        }



        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }

    }
}