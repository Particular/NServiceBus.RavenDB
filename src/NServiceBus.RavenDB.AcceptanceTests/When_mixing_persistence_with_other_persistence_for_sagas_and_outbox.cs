namespace NServiceBus.RavenDB.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_mixing_persistence_with_other_persistence_for_sagas_and_outbox : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_work()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithMixedPersistence>(
                    b => b.When(session => session.SendLocal(new StartSaga
                    {
                        DataId = Guid.NewGuid()
                    })))
                .Done(c => c.Done)
                .Run();

            Assert.True(context.Done);
        }

        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
        }

        public class EndpointWithMixedPersistence : EndpointConfigurationBuilder
        {
            public EndpointWithMixedPersistence()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.UsePersistence<LearningPersistence, StorageType.Sagas>();
                    config.UsePersistence<LearningPersistence, StorageType.Outbox>();

                    config.EnableOutbox();
                });
            }

            public class MySaga : Saga<MySaga.MySagaData>,
                IAmStartedByMessages<StartSaga>
            {
                Context testContext;

                public MySaga(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(StartSaga message, IMessageHandlerContext context)
                {
                    Data.DataId = message.DataId;
                    testContext.Done = true;

                    return Task.CompletedTask;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSaga>(m => m.DataId).ToSaga(s => s.DataId);
                }

                public class MySagaData : ContainSagaData
                {
                    public virtual Guid DataId { get; set; }
                }
            }
        }

        public class StartSaga : IMessage
        {
            public Guid DataId { get; set; }
        }
    }
}
