namespace NServiceBus.RavenDB.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class SagaAndOutbox : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_work()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithSagaAndOutbox>(b =>
                {
                    b.DoNotFailOnErrorMessages();
                    b.CustomConfig(cfg =>
                    {
                        cfg.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                        cfg.EnableOutbox();
                        cfg.Recoverability().Immediate(x => x.NumberOfRetries(5));
                    });
                    b.When((session, ctx) => session.SendLocal(new StartMsg { OrderId = "12345" }));

                    var timeout = DateTime.UtcNow.AddSeconds(15);

                    b.When(c => DateTime.UtcNow > timeout, (session, ctx) => session.SendLocal(new FinishMsg { OrderId = "12345" }));
                })
                .Done(c => c.SagaData != null)
                .Run();

            Assert.That(context.SagaData, Is.Not.Null);
            Assert.That(context.SagaData.ContinueCount, Is.EqualTo(3));
            Assert.That(context.SagaData.CollectedIndexes, Does.Contain(1));
            Assert.That(context.SagaData.CollectedIndexes, Does.Contain(2));
            Assert.That(context.SagaData.CollectedIndexes, Does.Contain(3));
        }

        public class Context : ScenarioContext
        {
            public EndpointWithSagaAndOutbox.OrderSagaData SagaData { get; set; }
        }

        public class EndpointWithSagaAndOutbox : EndpointConfigurationBuilder
        {
            public EndpointWithSagaAndOutbox()
            {
                EndpointSetup<DefaultServer>();
            }

            class OrderSaga : Saga<OrderSagaData>,
                IAmStartedByMessages<StartMsg>,
                IHandleMessages<ContinueMsg>,
                IHandleMessages<FinishMsg>
            {
                Context testContext;

                public OrderSaga(Context testContext)
                {
                    this.testContext = testContext;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartMsg>(m => m.OrderId).ToSaga(s => s.OrderId);
                    mapper.ConfigureMapping<ContinueMsg>(m => m.OrderId).ToSaga(s => s.OrderId);
                    mapper.ConfigureMapping<FinishMsg>(m => m.OrderId).ToSaga(s => s.OrderId);
                }

                public async Task Handle(StartMsg message, IMessageHandlerContext context)
                {
                    await context.SendLocal(new ContinueMsg { OrderId = message.OrderId, Index = 1 });
                    await context.SendLocal(new ContinueMsg { OrderId = message.OrderId, Index = 2 });
                    await context.SendLocal(new ContinueMsg { OrderId = message.OrderId, Index = 3 });
                }

                public Task Handle(ContinueMsg message, IMessageHandlerContext context)
                {
                    Data.ContinueCount++;
                    Data.CollectedIndexes.Add(message.Index);

                    if (Data.ContinueCount == 3)
                    {
                        return context.SendLocal(new FinishMsg { OrderId = message.OrderId });
                    }

                    return Task.CompletedTask;
                }

                public Task Handle(FinishMsg message, IMessageHandlerContext context)
                {
                    MarkAsComplete();
                    testContext.SagaData = Data;
                    return Task.CompletedTask;
                }
            }

            public class OrderSagaData : ContainSagaData
            {
                public string OrderId { get; set; }
                public int ContinueCount { get; set; }
                public List<int> CollectedIndexes { get; set; } = [];
            }
        }

        public class StartMsg : ICommand
        {
            public string OrderId { get; set; }
        }

        public class ContinueMsg : ICommand
        {
            public string OrderId { get; set; }
            public int Index { get; set; }
        }

        public class FinishMsg : ICommand
        {
            public string OrderId { get; set; }
        }
    }
}
