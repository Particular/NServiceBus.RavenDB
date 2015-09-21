﻿namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_aborting_the_behavior_chain : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Subsequent_handlers_will_not_be_invoked()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<MyEndpoint>(b => b.Given(bus =>
                {
                    bus.SendLocal(new SomeMessage());
                    return Task.FromResult(0);
                }))
                .Done(c => c.FirstHandlerInvoked)
                .Run();

            Assert.That(context.FirstHandlerInvoked, Is.True);
            Assert.That(context.SecondHandlerInvoked, Is.False);
        }

        public class Context : ScenarioContext
        {
            public bool FirstHandlerInvoked { get; set; }
            public bool SecondHandlerInvoked { get; set; }
        }

        [Serializable]
        public class SomeMessage : IMessage { }

        public class MyEndpoint : EndpointConfigurationBuilder
        {
            public MyEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.ExecuteTheseHandlersFirst(typeof(FirstHandler), typeof(SecondHandler)));
            }

            class FirstHandler : IHandleMessages<SomeMessage>
            {
                public Context Context { get; set; }
                
                public IBus Bus { get; set; }
                
                public void Handle(SomeMessage message)
                {
                    Context.FirstHandlerInvoked = true;

                    Bus.DoNotContinueDispatchingCurrentMessageToHandlers();
                }
            }

            class SecondHandler : IHandleMessages<SomeMessage>
            {
                public Context Context { get; set; }
                
                public void Handle(SomeMessage message)
                {
                    Context.SecondHandlerInvoked = true;
                }
            }
        }
    }
}