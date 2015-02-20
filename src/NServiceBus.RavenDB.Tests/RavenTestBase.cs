using System;

namespace NServiceBus.RavenDB.Tests
{
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using Raven.Abstractions.Data;
    using Raven.Client;
    using Raven.Client.Connection;
    using Raven.Client.Document;
    using Raven.Client.Embedded;
    using Raven.Database.Server;
    using Raven.Json.Linq;
    using NUnit.Framework;

    public class RavenTestBase
    {
        protected IDocumentStore store;

        [SetUp]
        public void SetUp()
        {
            store = NewDocumentStore();
        }

        [TearDown]
        public void TearDown()
        {
            store.Dispose();
        }

        protected static EmbeddableDocumentStore NewDocumentStore()
        {
            var store = new EmbeddableDocumentStore
            {
                DefaultDatabase = Guid.NewGuid().ToString("N").Substring(0, 8),
                RunInMemory = true,
//                Conventions =
//                {
//                    DefaultQueryingConsistency = ConsistencyOptions.AlwaysWaitForNonStaleResultsAsOfLastWrite,
//                },
                Configuration =
                {
                    RunInUnreliableYetFastModeThatIsNotSuitableForProduction = true,
                    CompiledIndexCacheDirectory = Path.GetTempPath()
                }
            };

            store.Initialize();
            return store;
        }

        public static void WaitForIndexing(IDocumentStore store, string db = null, TimeSpan? timeout = null)
        {
            var databaseCommands = store.DatabaseCommands;
            if (db != null)
                databaseCommands = databaseCommands.ForDatabase(db);
            var spinUntil = SpinWait.SpinUntil(() => databaseCommands.GetStatistics().StaleIndexes.Length == 0, timeout ?? TimeSpan.FromSeconds(20));
            if (spinUntil == false)
                WaitForUserToContinueTheTest(store);
            Assert.True(spinUntil);
        }

        protected static void WaitForIndexing(IDocumentStore store)
        {
            Assert.IsTrue(SpinWait.SpinUntil(() => store.DatabaseCommands.GetStatistics().StaleIndexes.Length == 0, TimeSpan.FromMinutes(5)));
        }

        static void WaitForUserToContinueTheTest(IDocumentStore documentStore, bool debug = true, int port = 8079)
        {
            if (debug && Debugger.IsAttached == false)
                return;

            var databaseName = Constants.SystemDatabase;

            var embeddableDocumentStore = documentStore as EmbeddableDocumentStore;
            OwinHttpServer server = null;
            var url = documentStore.Url;
            if (embeddableDocumentStore != null)
            {
                databaseName = embeddableDocumentStore.DefaultDatabase;
                embeddableDocumentStore.Configuration.Port = port;
                SetStudioConfigToAllowSingleDb(embeddableDocumentStore);
                embeddableDocumentStore.Configuration.AnonymousUserAccessMode = AnonymousUserAccessMode.Admin;
                NonAdminHttp.EnsureCanListenToWhenInNonAdminContext(port);
                server = new OwinHttpServer(embeddableDocumentStore.Configuration, embeddableDocumentStore.SystemDatabase);
                url = embeddableDocumentStore.Configuration.ServerUrl;
            }

            var remoteDocumentStore = documentStore as DocumentStore;
            if (remoteDocumentStore != null)
            {
                databaseName = remoteDocumentStore.DefaultDatabase;
            }

            using (server)
            {
                try
                {
                    var databaseNameEncoded = Uri.EscapeDataString(databaseName ?? Constants.SystemDatabase);
                    var documentsPage = url + "studio/index.html#databases/documents?&database=" + databaseNameEncoded + "&withStop=true";
                    Process.Start(documentsPage); // start the server
                }
                catch (Win32Exception e)
                {
                    Console.WriteLine("Failed to open the browser. Please open it manually at {0}. {1}", url, e);
                }

                do
                {
                    Thread.Sleep(100);
                } while (documentStore.DatabaseCommands.Head("Debug/Done") == null && (debug == false || Debugger.IsAttached));
            }
        }

        protected void WaitForUserToContinueTheTest(bool debug = true, string url = null)
        {
            if (debug && Debugger.IsAttached == false)
                return;

            using (var documentStore = new DocumentStore
            {
                Url = url ?? "http://localhost:8079"
            })
            {
                documentStore.Initialize();
                documentStore.DatabaseCommands.Put("Pls Delete Me", null,
                                                   RavenJObject.FromObject(new { StackTrace = new StackTrace(true) }), new RavenJObject());

                Process.Start(documentStore.Url); // start the server

                do
                {
                    Thread.Sleep(100);
                } while (documentStore.DatabaseCommands.Get("Pls Delete Me") != null && (debug == false || Debugger.IsAttached));
            }
        }

        protected void WaitForDocument(IDatabaseCommands databaseCommands, string id)
        {
            var done = SpinWait.SpinUntil(() =>
            {
                // We expect to get the doc from the <system> database
                var doc = databaseCommands.Get(id);
                return doc != null;
            }, TimeSpan.FromMinutes(5));

            Assert.True(done);
        }

        /// <summary>
        ///     Let the studio knows that it shouldn't display the warning about sys db access
        /// </summary>
        static void SetStudioConfigToAllowSingleDb(IDocumentStore documentDatabase)
        {
            var jsonDocument = documentDatabase.DatabaseCommands.Get("Raven/StudioConfig");
            RavenJObject doc;
            RavenJObject metadata;
            if (jsonDocument == null)
            {
                doc = new RavenJObject();
                metadata = new RavenJObject();
            }
            else
            {
                doc = jsonDocument.DataAsJson;
                metadata = jsonDocument.Metadata;
            }

            doc["WarnWhenUsingSystemDatabase"] = false;

            documentDatabase.DatabaseCommands.Put("Raven/StudioConfig", null, doc, metadata);
        }
    }
}
