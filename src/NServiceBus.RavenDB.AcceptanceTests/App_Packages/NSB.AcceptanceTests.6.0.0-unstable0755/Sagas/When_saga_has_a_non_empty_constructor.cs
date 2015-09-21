﻿namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Saga;
    using ScenarioDescriptors;

    public class When_saga_has_a_non_empty_constructor : NServiceBusAcceptanceTest
    {
        static Guid IdThatSagaIsCorrelatedOn = Guid.NewGuid();

        [Test]
        public async Task Should_hydrate_and_invoke_the_existing_instance()
        {
            await Scenario.Define<Context>()
                    .WithEndpoint<SagaEndpoint>(b => b.Given(bus =>
                        {
                            bus.SendLocal(new StartSagaMessage { SomeId = IdThatSagaIsCorrelatedOn });
                            bus.SendLocal(new OtherMessage { SomeId = IdThatSagaIsCorrelatedOn });
                            return Task.FromResult(0);
                        }))
                    .Done(c => c.SecondMessageReceived)
                    .Repeat(r => r.For(Persistence.Default))
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool SecondMessageReceived { get; set; }

        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>(

                    builder => builder.Transactions().DoNotWrapHandlersExecutionInATransactionScope());
            }

            public class TestSaga11 : Saga<TestSagaData11>,
                IAmStartedByMessages<StartSagaMessage>, IHandleMessages<OtherMessage>
            {
                Context context;

                // ReSharper disable once UnusedParameter.Local
                public TestSaga11(IBus bus, Context context)
                {
                    this.context = context;
                }

                public void Handle(StartSagaMessage message)
                {
                    Data.SomeId = message.SomeId;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData11> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                    mapper.ConfigureMapping<OtherMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }

                public void Handle(OtherMessage message)
                {
                    context.SecondMessageReceived = true;
                }
            }

            public class TestSagaData11 : IContainSagaData
            {
                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }
                public virtual Guid SomeId { get; set; }
            }
        }

        [Serializable]
        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }

        }
        [Serializable]
        public class OtherMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }
    }
}