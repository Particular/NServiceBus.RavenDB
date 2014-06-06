using System;

namespace NServiceBus.RavenDB.Tests
{
    using System.Diagnostics;
    using System.Threading;
    using Raven.Client;
    using Raven.Client.Connection;
    using Raven.Client.Document;
    using Raven.Client.Embedded;
    using Raven.Database;
    using Raven.Database.Server;
    using Raven.Json.Linq;
    using NUnit.Framework;

    public class RavenTestBase
    {
        public static void WaitForIndexing(IDocumentStore store, string db = null, TimeSpan? timeout = null)
        {
            var databaseCommands = store.DatabaseCommands;
            if (db != null)
                databaseCommands = databaseCommands.ForDatabase(db);
            var spinUntil = SpinWait.SpinUntil(() => databaseCommands.GetStatistics().StaleIndexes.Length == 0, timeout ?? TimeSpan.FromSeconds(20));
            if (spinUntil == false)
                WaitForUserToContinueTheTest((EmbeddableDocumentStore)store);
            Assert.True(spinUntil);
        }

        public static void WaitForIndexing(DocumentDatabase db)
        {
            Assert.IsTrue(SpinWait.SpinUntil(() => db.Statistics.StaleIndexes.Length == 0, TimeSpan.FromMinutes(5)));
        }

        public static void WaitForUserToContinueTheTest(EmbeddableDocumentStore documentStore, bool debug = true)
        {
            if (debug && Debugger.IsAttached == false)
                return;

            documentStore.SetStudioConfigToAllowSingleDb();

            documentStore.DatabaseCommands.Put("Pls Delete Me", null,

                                               RavenJObject.FromObject(new { StackTrace = new StackTrace(true) }),
                                               new RavenJObject());

            documentStore.Configuration.AnonymousUserAccessMode = AnonymousUserAccessMode.Admin;
            using (var server = new HttpServer(documentStore.Configuration, documentStore.DocumentDatabase))
            {
                server.StartListening();
                Process.Start(documentStore.Configuration.ServerUrl); // start the server

                do
                {
                    Thread.Sleep(100);
                } while (documentStore.DatabaseCommands.Get("Pls Delete Me") != null && (debug == false || Debugger.IsAttached));
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
    }
}
