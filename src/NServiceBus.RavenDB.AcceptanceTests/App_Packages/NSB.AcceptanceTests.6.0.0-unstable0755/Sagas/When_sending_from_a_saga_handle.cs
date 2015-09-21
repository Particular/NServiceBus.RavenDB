﻿
namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Features;
    using NUnit.Framework;
    using Saga;
    using ScenarioDescriptors;

    public class When_sending_from_a_saga_handle : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_match_different_saga()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.Given(bus =>
                {
                    bus.SendLocal(new StartSaga1 { DataId = Guid.NewGuid() });
                    return Task.FromResult(0);
                }))
                .Done(c => c.DidSaga2ReceiveMessage)
                .Repeat(r => r.For(Transports.Default))
                .Should(c => Assert.True(c.DidSaga2ReceiveMessage))
                .Run();
        }

        public class Context : ScenarioContext
        {
            public bool DidSaga2ReceiveMessage { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {

            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config => config.EnableFeature<TimeoutManager>());
            }

            public class TwoSaga1Saga1 : Saga<TwoSaga1Saga1Data>, IAmStartedByMessages<StartSaga1>, IHandleMessages<MessageSaga1WillHandle>
            {
                public Context Context { get; set; }

                public void Handle(StartSaga1 message)
                {
                    var dataId = Guid.NewGuid();
                    Data.DataId = dataId;
                    Bus.SendLocal(new MessageSaga1WillHandle
                             {
                                 DataId = dataId
                             });
                }

                public void Handle(MessageSaga1WillHandle message)
                {
                    Bus.SendLocal(new StartSaga2());
                    MarkAsComplete();
                }
                
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TwoSaga1Saga1Data> mapper)
                {
                    mapper.ConfigureMapping<MessageSaga1WillHandle>(m => m.DataId).ToSaga(s => s.DataId);
                    mapper.ConfigureMapping<StartSaga1>(m => m.DataId).ToSaga(s => s.DataId);
                }

            }

            public class TwoSaga1Saga1Data : ContainSagaData
            {
                public virtual Guid DataId { get; set; }
            }


            public class TwoSaga1Saga2 : Saga<TwoSaga1Saga2.TwoSaga1Saga2Data>, IAmStartedByMessages<StartSaga2>
            {
                public Context Context { get; set; }

                public void Handle(StartSaga2 message)
                {
                    Context.DidSaga2ReceiveMessage = true;
                }

                public class TwoSaga1Saga2Data : ContainSagaData
                {
                    public virtual Guid DataId { get; set; }
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TwoSaga1Saga2Data> mapper)
                {
                    mapper.ConfigureMapping<StartSaga2>(m => m.DataId).ToSaga(s => s.DataId);
                }
            }

        }

        [Serializable]
        public class StartSaga1 : ICommand
        {
            public Guid DataId { get; set; }
        }


        [Serializable]
        public class StartSaga2 : ICommand
        {
            public Guid DataId { get; set; }
        }
        public class MessageSaga1WillHandle : IMessage
        {
            public Guid DataId { get; set; }
        }
    }
}
