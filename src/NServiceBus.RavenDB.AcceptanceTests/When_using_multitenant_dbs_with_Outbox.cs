namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Session;

    public class When_using_multitenant_dbs_with_Outbox : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_honor_SetMessageToDatabaseMappingConvention()
        {
            await RunTest(cfg =>
            {
                cfg.PersistenceExtensions.SetMessageToDatabaseMappingConvention(headers => headers.TryGetValue("RavenDatabaseName", out var dbName) ? dbName : cfg.DefaultStore.Database);
            });
        }

        [Test]
        public async Task Should_honor_UseSharedAsyncSession()
        {
            await RunTest(cfg =>
            {
                cfg.PersistenceExtensions.UseSharedAsyncSession(headers => cfg.DefaultStore.OpenAsyncSession(headers["RavenDatabaseName"]));
            });
        }

        async Task RunTest(Action<ContextDbConfig> configureMultiTenant)
        {
            var context = await Scenario.Define<Context>(c =>
                {
                    c.Db1 = "Tenant1-" + Guid.NewGuid().ToString("n").Substring(16);
                    c.Db2 = "Tenant2-" + Guid.NewGuid().ToString("n").Substring(16);
                })
                .WithEndpoint<MultiTenantEndpoint>(b =>
                {
                    b.CustomConfig((cfg, c) =>
                    {
                        cfg.EnableOutbox();
                        cfg.LimitMessageProcessingConcurrencyTo(1);
                        cfg.Pipeline.Register(new MessageCountingBehavior(c), "Counts all messages processed");

                        var settings = cfg.GetSettings();

                        var defaultStore = ConfigureEndpointRavenDBPersistence.GetDefaultDocumentStore(settings);
                        c.DefaultDb = defaultStore.Database;
                        c.DbConfig.DefaultStore = defaultStore;

                        ConfigureEndpointRavenDBPersistence.CreateDatabase(defaultStore, c.Db1);
                        ConfigureEndpointRavenDBPersistence.CreateDatabase(defaultStore, c.Db2);

                        c.DbConfig.PersistenceExtensions = ConfigureEndpointRavenDBPersistence.GetDefaultPersistenceExtensions(settings);
                        configureMultiTenant(c.DbConfig);
                    });

                    async Task SendMessage(IMessageSession session, string messageId, string orderId, string dbName)
                    {
                        var msg = new TestMsg
                        {
                            OrderId = orderId
                        };
                        var opts = new SendOptions();
                        opts.RouteToThisEndpoint();
                        opts.SetHeader("RavenDatabaseName", dbName);
                        opts.SetMessageId(messageId);
                        await session.Send(msg, opts);
                    }

                    b.When(async (session, ctx) =>
                    {
                        var msgId1 = Guid.NewGuid().ToString();
                        var msgId2 = Guid.NewGuid().ToString();

                        await SendMessage(session, msgId1, "OrderA", ctx.Db1);
                        await SendMessage(session, msgId1, "OrderA", ctx.Db1);
                        await SendMessage(session, msgId2, "OrderB", ctx.Db2);
                        await SendMessage(session, msgId2, "OrderB", ctx.Db2);
                    });
                })
                .Done(c => c.MessagesObserved >= 4)
                .Run();

            await ConfigureEndpointRavenDBPersistence.DeleteDatabase(context.Db1);
            await ConfigureEndpointRavenDBPersistence.DeleteDatabase(context.Db2);

            Assert.AreEqual(4, context.MessagesObserved);
            Assert.AreEqual(2, context.ObservedDbs.Count);
            Assert.IsFalse(context.ObservedDbs.Any(db => db == context.DefaultDb));
            Assert.Contains(context.Db1, context.ObservedDbs);
            Assert.Contains(context.Db2, context.ObservedDbs);
        }

        public class Context : ScenarioContext
        {
            public int MessagesReceived;
            public string DefaultDb { get; set; }
            public string Db1 { get; set; }
            public string Db2 { get; set; }
            public List<string> ObservedDbs { get; } = new List<string>();
            public string ObservedDbsOutput => string.Join(", ", ObservedDbs);
            public ContextDbConfig DbConfig { get; } = new ContextDbConfig();
            public int MessagesObserved;
        }

        public class ContextDbConfig
        {
            public DocumentStore DefaultStore { get; set; }
            public DocumentStore Tenant1 { get; set; }
            public DocumentStore Tenant2 { get; set; }
            public PersistenceExtensions<RavenDBPersistence> PersistenceExtensions { get; set; }
        }

        public class MultiTenantEndpoint : EndpointConfigurationBuilder
        {
            public MultiTenantEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MTSaga : Saga<MTSagaData>,
                IAmStartedByMessages<TestMsg>
            {
                Context testCtx;

                public MTSaga(Context testCtx)
                {
                    this.testCtx = testCtx;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MTSagaData> mapper)
                {
                    mapper.ConfigureMapping<TestMsg>(m => m.OrderId).ToSaga(s => s.OrderId);
                }

                public Task Handle(TestMsg message, IMessageHandlerContext context)
                {
                    var ravenSession = context.SynchronizedStorageSession.RavenSession();
                    if (ravenSession is InMemoryDocumentSessionOperations ravenSessionOps)
                    {
                        var dbName = ravenSessionOps.DatabaseName;
                        testCtx.ObservedDbs.Add(dbName);
                    }
                    Interlocked.Increment(ref testCtx.MessagesReceived);
                    return Task.FromResult(0);
                }
            }

            public class MTSagaData : ContainSagaData
            {
                public virtual string OrderId { get; set; }
            }
        }

        public class TestMsg : ICommand
        {
            public string OrderId { get; set; }
        }

        public class MessageCountingBehavior : IBehavior<ITransportReceiveContext, ITransportReceiveContext>
        {
            Context testContext;

            public MessageCountingBehavior(Context testContext)
            {
                this.testContext = testContext;
            }

            public async Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, Task> next)
            {
                await next(context);

                Interlocked.Increment(ref testContext.MessagesObserved);
            }
        }
    }
}