namespace NServiceBus.RavenDB.Tests
{
    using System;
    using NUnit.Framework;
    using Raven.Client;
    using Raven.Tests.Helpers;

    public class RavenDBPersistenceTestBase : RavenTestBase
    {
        protected IDocumentStore store;

        [SetUp]
        public virtual void SetUp()
        {
            Console.WriteLine($"Execution Environment: 64-bit OS:{Environment.Is64BitOperatingSystem}, 64-bit Process:{Environment.Is64BitProcess}");
            store = NewDocumentStore();
        }

        [TearDown]
        public virtual void TearDown()
        {
            store.Dispose();
        }
    }
}
