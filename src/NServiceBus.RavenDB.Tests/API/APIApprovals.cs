namespace NServiceBus.RavenDB.Tests.API
{
    using ApprovalTests;
    using ApprovalTests.Reporters;
    using NUnit.Framework;
    using PublicApiGenerator;
    using System.Runtime.CompilerServices;

    [TestFixture]
    class APIApprovals
    {
        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [UseReporter(typeof(DiffReporter))]
        public void ApproveRavenDbPersistence()
        {
            var publicApi = ApiGenerator.GeneratePublicApi(typeof(RavenDBPersistence).Assembly);

            Approvals.Verify(publicApi);
        }
    }
}
