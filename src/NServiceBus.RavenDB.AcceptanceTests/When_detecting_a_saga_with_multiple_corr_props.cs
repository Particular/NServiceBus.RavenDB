namespace NServiceBus.RavenDB.AcceptanceTests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_detecting_a_saga_with_multiple_corr_props : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_blow_up()
        {
            var ex = Assert.Throws<AggregateException>(async () => await Scenario.Define<Context>()
                .WithEndpoint<MultiPropEndpoint>()
                .AllowExceptions()
                .Done(c => c.Exceptions.Any() || c.EndpointsStarted)
                .Run());


            Assert.True(ex.InnerException.InnerException.Message.Contains("Sagas that are correlated on multiple properties are not supported by the RavenDB saga persister"), "Should blow up telling the user multi props isn't supported for this persister");
        }

        public class Context : ScenarioContext
        {
        }

        public class MultiPropEndpoint : EndpointConfigurationBuilder
        {
            public MultiPropEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MultiPropSaga : Saga<MultiPropSagaData>, IAmStartedByMessages<StartSagaMessage>
            {
                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MultiPropSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId).ToSaga(s => s.SomeId);
                    mapper.ConfigureMapping<StartSagaMessage2>(m => m.SomeOtherId).ToSaga(s => s.SomeOtherId);
                }
            }

            public class MultiPropSagaData : IContainSagaData
            {
                public Guid SomeOtherId { get; set; }
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

        public class StartSagaMessage2 : ICommand
        {
            public Guid SomeOtherId { get; set; }
        }
    }
}