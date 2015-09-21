﻿namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Saga;
    using NUnit.Framework;

    public class When_message_has_a_saga_id : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_start_a_new_saga_if_not_found()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SagaEndpoint>(b => b.Given(bus =>
                {
                    var message = new MessageWithSagaId();
                    var options = new SendOptions();

                    options.SetHeader(Headers.SagaId, Guid.NewGuid().ToString());
                    options.SetHeader(Headers.SagaType, typeof(MessageWithSagaIdSaga).AssemblyQualifiedName);
                    options.RouteToLocalEndpointInstance();
                    bus.Send(message, options);
                    return Task.FromResult(0);
                }))
                .Done(c => c.OtherSagaStarted)
                .Run();

            Assert.False(context.NotFoundHandlerCalled);
            Assert.True(context.OtherSagaStarted);
            Assert.False(context.MessageHandlerCalled);
            Assert.False(context.TimeoutHandlerCalled);
        }

        class MessageWithSagaIdSaga : Saga<MessageWithSagaIdSaga.MessageWithSagaIdSagaData>, IAmStartedByMessages<MessageWithSagaId>,
            IHandleTimeouts<MessageWithSagaId>,
            IHandleSagaNotFound
        {
            public Context Context { get; set; }

            public class MessageWithSagaIdSagaData : ContainSagaData
            {
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MessageWithSagaIdSagaData> mapper)
            {
            }

            public void Handle(MessageWithSagaId message)
            {
                Context.MessageHandlerCalled = true;
            }

            public void Handle(object message)
            {
                Context.NotFoundHandlerCalled = true;
            }

            public void Timeout(MessageWithSagaId state)
            {
                Context.TimeoutHandlerCalled = true;
            }
        }

        class MyOtherSaga : Saga<MyOtherSaga.SagaData>, IAmStartedByMessages<MessageWithSagaId>
        {
            public Context Context { get; set; }

            public void Handle(MessageWithSagaId message)
            {
                Data.DataId = message.DataId;

                Context.OtherSagaStarted = true;
            }
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<MessageWithSagaId>(m => m.DataId).ToSaga(s => s.DataId);
            }

            public class SagaData : ContainSagaData
            {
                public virtual Guid DataId { get; set; }
            }
        }


        class Context : ScenarioContext
        {
            public bool NotFoundHandlerCalled { get; set; }
            public bool MessageHandlerCalled { get; set; }
            public bool TimeoutHandlerCalled { get; set; }
            public bool OtherSagaStarted { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }
        }

        public class MessageWithSagaId : IMessage
        {
            public Guid DataId { get; set; }
        }
    }
}