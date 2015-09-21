﻿namespace NServiceBus.AcceptanceTests.Volatile
{
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class When_sending_to_non_durable_endpoint: NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_the_message()
        {
            await Scenario.Define<Context>()
                    .WithEndpoint<Sender>(b => b.Given((bus, c) =>
                    {
                        bus.Send(new MyMessage());
                        return Task.FromResult(0);
                    }))
                    .WithEndpoint<Receiver>()
                    .Done(c => c.WasCalled)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(c => Assert.True(c.WasCalled, "The message handler should be called"))
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(builder => builder.DisableDurableMessages())
                    .AddMapping<MyMessage>(typeof(Receiver));
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(builder => builder.DisableDurableMessages());
            }
        }

        public class MyMessage : IMessage
        {
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public Context Context { get; set; }

            public IBus Bus { get; set; }

            public void Handle(MyMessage message)
            {
                Context.WasCalled = true;
            }
        }
    }
}