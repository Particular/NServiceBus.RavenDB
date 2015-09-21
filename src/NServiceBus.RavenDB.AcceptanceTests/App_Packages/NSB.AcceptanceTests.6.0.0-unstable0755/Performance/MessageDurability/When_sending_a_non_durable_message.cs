﻿namespace NServiceBus.AcceptanceTests.Performance.MessageDurability
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_sending_a_non_durable_message : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_available_as_a_header_on_receiver()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(b => b.Given((bus, c) =>
                    {
                        bus.SendLocal(new MyMessage());
                        return Task.FromResult(0);
                    }))
                    .Done(c => c.WasCalled)
                    .Run(TimeSpan.FromSeconds(10));

            Assert.IsTrue(context.NonDurabilityHeader, "Message should be flagged as non durable");
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public bool NonDurabilityHeader { get; set; }
        }
        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }
            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MyMessage message)
                {
                    Context.NonDurabilityHeader = bool.Parse(Bus.CurrentMessageContext.Headers[Headers.NonDurableMessage]);
                    Context.WasCalled = true;
                }
            }
        }


        [Express]
        public class MyMessage : IMessage
        {
        }
    }
}
