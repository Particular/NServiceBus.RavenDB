﻿namespace NServiceBus.AcceptanceTests.Audit
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using MessageMutator;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_a_replymessage_is_audited : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_audit_the_message()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<Server>()
                    .WithEndpoint<EndpointWithAuditOn>(b => b.Given(bus =>
                    {
                        bus.Send(new Request());
                        return Task.FromResult(0);
                    }))
                    .WithEndpoint<AuditSpyEndpoint>()
                    .Done(c => c.MessageAudited)
                    .Run();

            Assert.True(context.MessageProcessed);
            Assert.True(context.MessageAudited);
        }


        public class Context : ScenarioContext
        {
            public bool MessageAudited { get; set; }
            public bool MessageProcessed { get; set; }
        }

        public class Server : EndpointConfigurationBuilder
        {
            public Server()
            {
                EndpointSetup<DefaultServer>();
            }

            class RequestHandler : IHandleMessages<Request>
            {
                public IBus Bus { get; set; }
                public void Handle(Request message)
                {
                    var replyOptions = new ReplyOptions();
                   
                    replyOptions.SetHeader("MyHeader", "SomeValue");

                    Bus.Reply(new ResponseToBeAudited(), replyOptions);
                }
            }
        }

        public class EndpointWithAuditOn : EndpointConfigurationBuilder
        {
            public EndpointWithAuditOn()
            {
                EndpointSetup<DefaultServer>(c => c.DisableFeature<Outbox>())
                    .AddMapping<Request>(typeof(Server))
                    .AuditTo<AuditSpyEndpoint>();
            }


            public class MessageToBeAuditedHandler : IHandleMessages<ResponseToBeAudited>
            {
                public IBus Bus { get; set; }

                public Context Context { get; set; }

                public void Handle(ResponseToBeAudited message)
                {
                    Assert.AreEqual(Bus.CurrentMessageContext.Headers["MyHeader"], "SomeValue");
                    Context.MessageProcessed = true;
                }
            }
        }

        class AuditSpyEndpoint : EndpointConfigurationBuilder
        {
            public AuditSpyEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class BodySpy : IMutateIncomingTransportMessages, INeedInitialization
            {
                public Context Context { get; set; }

                public void MutateIncoming(MutateIncomingTransportMessageContext transportMessage)
                {
                    Context.MessageAudited = true;
                }

                public void Customize(BusConfiguration configuration)
                {
                    configuration.RegisterComponents(c => c.ConfigureComponent<BodySpy>(DependencyLifecycle.InstancePerCall));
                }
            }

            public class MessageToBeAuditedHandler : IHandleMessages<ResponseToBeAudited>
            {
                public void Handle(ResponseToBeAudited message)
                {
                }
            }
        }

        public static byte Checksum(byte[] data)
        {
            var longSum = data.Sum(x => (long)x);
            return unchecked((byte)longSum);
        }

        [Serializable]
        public class ResponseToBeAudited : IMessage
        {
        }


        class Request : IMessage
        {
        }
    }

}
