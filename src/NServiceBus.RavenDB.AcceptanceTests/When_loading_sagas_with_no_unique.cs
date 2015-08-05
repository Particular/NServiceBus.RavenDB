namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Persistence;
    using NUnit.Framework;
    using Saga;

    public class When_loading_sagas_with_no_unique : NServiceBusAcceptanceTest
    {
        [Test,Ignore("Flaky, issue raised")]
        public async Task Should_blow_up()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SagaEndpoint>(b => b.Given(bus =>
                {
                    bus.SendLocal(new StartSagaMessage
                    {
                        SomeId = Guid.NewGuid()
                    });
                    return Task.FromResult(0);
                }))
                .Done(c =>
                {
                    if(c.Exceptions.Any())
                    {
                        c.AddTrace("Exceptions found: " + c.Exceptions);

                        return true;
                    }

                    return false;
                    
                })
                .AllowExceptions(ex => ex.Message.Contains("Please add a [Unique]"))
                .Run();

            Assert.False(context.SagaStarted, "Saga should not have started");
            Assert.NotNull(context.Exceptions,"An exception should have been thrown");
            Assert.True(context.Exceptions.Any(e => e.Message == " Please add a [Unique] attribute to the 'SomeId' property on your 'NonUniqueSagaData'"));
        }

        [Test]
        public async Task Should_not_blow_up_if_there_is_no_mapping()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SagaEndpoint>(b=>b.Given(bus =>
                {
                    bus.SendLocal(new StartSagaMessageWithNoMapping());
                    return Task.FromResult(0);
                }))
                .Done(c => c.SagaStarted)
                .Run();

            Assert.True(context.SagaStarted);
        }

        [Test]
        public async Task Should_not_blow_up_if_user_opts_in()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SagaEndpointWithOptIn>(b=>b.Given(bus => {
                    bus.SendLocal(new StartSagaMessage
                    {
                        SomeId = Guid.NewGuid()
                    });
                    return Task.FromResult(0);
                }))
                .Done(c => c.SagaStarted)
                .Run();

            Assert.True(context.SagaStarted);
        }

        public class Context : ScenarioContext
        {
            public bool SagaStarted { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class NonUniqueSaga : Saga<NonUniqueSagaData>, IAmStartedByMessages<StartSagaMessage>, IAmStartedByMessages<StartSagaMessageWithNoMapping>
            {
                public Context Context { get; set; }
                public void Handle(StartSagaMessage message)
                {
                    Context.AddTrace("Saga started by StartSagaMessage");
                    Context.SagaStarted = true;
                }

                public void Handle(StartSagaMessageWithNoMapping message)
                {
                    Context.AddTrace("Saga started by StartSagaMessageWithNoMapping");
                    Context.SagaStarted = true;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<NonUniqueSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s=>s.SomeId);
                }

            }

            public class NonUniqueSagaData : IContainSagaData
            {
                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }

                public virtual Guid SomeId { get; set; }
            }
        }


        public class SagaEndpointWithOptIn : EndpointConfigurationBuilder
        {
            public SagaEndpointWithOptIn()
            {
                EndpointSetup<DefaultServer>(c => c.UsePersistence<RavenDBPersistence>().AllowStaleSagaReads());
            }

            public class OptInSaga : Saga<OptInSagaData>, IAmStartedByMessages<StartSagaMessage>
            {
                public Context Context { get; set; }
                public void Handle(StartSagaMessage message)
                {
                    Context.SagaStarted = true;
                }

         
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OptInSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }

            }

            public class OptInSagaData : IContainSagaData
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


        public class StartSagaMessageWithNoMapping:ICommand
        {
        }
    }

}