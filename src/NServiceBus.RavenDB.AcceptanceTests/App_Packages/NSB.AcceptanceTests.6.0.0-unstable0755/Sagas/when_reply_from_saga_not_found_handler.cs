﻿namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Saga;

    public class when_reply_from_saga_not_found_handler : NServiceBusAcceptanceTest
    {
        // related to NSB issue #2044
        [Test]
        public async Task It_should_invoke_message_handler()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<Sender>(b => b.Given((bus, c) =>
                    {
                        bus.Send(new MessageToSaga());
                        return Task.FromResult(0);
                    }))
                    .WithEndpoint<ReceiverWithSaga>()
                    .Done(c => c.ReplyReceived)
                    .Run();

            Assert.IsTrue(context.ReplyReceived);
        }

        public class Context : ScenarioContext
        {
            public bool ReplyReceived { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>()
                    .AddMapping<MessageToSaga>(typeof(ReceiverWithSaga));
            }

            public class ReplyHandler : IHandleMessages<Reply>
            {
                public Context Context { get; set; }


                public void Handle(Reply message)
                {
                    Context.ReplyReceived = true;
                }
            }
        }

        public class ReceiverWithSaga : EndpointConfigurationBuilder
        {
            public ReceiverWithSaga()
            {
                EndpointSetup<DefaultServer>();
            }

            public class NotFoundHandlerSaga1 : Saga<NotFoundHandlerSaga1.NotFoundHandlerSaga1Data>, IAmStartedByMessages<StartSaga1>, IHandleMessages<MessageToSaga>
            {

                public void Handle(StartSaga1 message)
                {
                    Data.ContextId = message.ContextId;
                }

                public void Handle(MessageToSaga message)
                {
                }

                public class NotFoundHandlerSaga1Data : ContainSagaData
                {
                    public virtual Guid ContextId { get; set; }
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<NotFoundHandlerSaga1Data> mapper)
                {
                    mapper.ConfigureMapping<StartSaga1>(m => m.ContextId)
                        .ToSaga(s => s.ContextId);
                    mapper.ConfigureMapping<MessageToSaga>(m => m.ContextId)
                        .ToSaga(s => s.ContextId);
                }
            }

            public class SagaNotFound : IHandleSagaNotFound
            {
                public IBus Bus { get; set; }

                public void Handle(object message)
                {
                    Bus.Reply(new Reply());
                }
            }
        }

        [Serializable]
        public class StartSaga1 : ICommand
        {
            public Guid ContextId { get; set; }
        }

        [Serializable]
        public class MessageToSaga : ICommand
        {
            public Guid ContextId { get; set; }
        }

        [Serializable]
        public class Reply : IMessage
        {
        }
    }
}