namespace NServiceBus.RavenDB.Tests.API
{
    using System.IO;
    using System.Runtime.CompilerServices;
    using ApiApprover;
    using ApprovalTests;
    using ApprovalTests.Reporters;
    using Mono.Cecil;
    using NUnit.Framework;

    [TestFixture]
    class APIApprovals
    {
        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [UseReporter(typeof(DiffReporter))]
        public void ApproveRavenDbPersistence()
        {
            Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);
            var assemblyPath = Path.GetFullPath(typeof(RavenDBPersistence).Assembly.Location);
            var asm = AssemblyDefinition.ReadAssembly(assemblyPath);
            var publicApi = PublicApiGenerator.CreatePublicApiForAssembly(asm, definition => true, false);

            Approvals.Verify(publicApi);
        }
    }
}
