namespace NServiceBus.RavenDB.Tests.API
{
    using NUnit.Framework;
    using Particular.Approvals;
    using PublicApiGenerator;

    [TestFixture]
    class APIApprovals
    {
        [Test]
        public void ApproveRavenDbPersistence()
        {
            var publicApi = ApiGenerator.GeneratePublicApi(typeof(RavenDBPersistence).Assembly, excludeAttributes: new[] { "System.Runtime.Versioning.TargetFrameworkAttribute" });
            Approver.Verify(publicApi);
        }
    }
}