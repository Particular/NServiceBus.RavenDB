namespace NServiceBus.RavenDB.Tests.API
{
    using System.Runtime.CompilerServices;
    using NUnit.Framework;
    using PublicApiGenerator;

    [TestFixture]
    class APIApprovals
    {
        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ApproveRavenDbPersistence()
        {
            var publicApi = ApiGenerator.GeneratePublicApi(typeof(RavenDBPersistence).Assembly, excludeAttributes: new[] { "System.Runtime.Versioning.TargetFrameworkAttribute" });
            TestApprover.Verify(publicApi);
        }
    }
}