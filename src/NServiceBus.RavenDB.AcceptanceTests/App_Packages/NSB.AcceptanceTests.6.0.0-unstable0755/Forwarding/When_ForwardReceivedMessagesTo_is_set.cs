﻿namespace NServiceBus.AcceptanceTests.Forwarding
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class When_ForwardReceivedMessagesTo_is_set : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_forward_message()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointThatForwards>(b => b.Given((bus, c) =>
                {
                    bus.SendLocal(new MessageToForward());
                    return Task.FromResult(0);
                }))
                .WithEndpoint<ForwardReceiver>()
                .Done(c => c.GotForwardedMessage)
                .Run();

            Assert.IsTrue(context.GotForwardedMessage);
        }

        public class Context : ScenarioContext
        {
            public bool GotForwardedMessage { get; set; }
        }

        public class ForwardReceiver : EndpointConfigurationBuilder
        {
            public ForwardReceiver()
            {
                EndpointSetup<DefaultServer>(c => c.EndpointName("forward_receiver"));
            }

            public class MessageToForwardHandler : IHandleMessages<MessageToForward>
            {
                public Context Context { get; set; }

                public void Handle(MessageToForward message)
                {
                    Context.GotForwardedMessage = true;
                }
            }
        }

        public class EndpointThatForwards : EndpointConfigurationBuilder
        {
            public EndpointThatForwards()
            {
                EndpointSetup<DefaultServer>()
                    .WithConfig<UnicastBusConfig>(c => c.ForwardReceivedMessagesTo = "forward_receiver");
            }

            public class MessageToForwardHandler : IHandleMessages<MessageToForward>
            {
                public void Handle(MessageToForward message)
                {
                }
            }
        }

        [Serializable]
        public class MessageToForward : IMessage
        {
        }
    }
}
