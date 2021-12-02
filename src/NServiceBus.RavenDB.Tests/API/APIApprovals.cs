namespace NServiceBus.RavenDB.Tests.API
{
    using NUnit.Framework;
    using Particular.Approvals;
    using PublicApiGenerator;

    [TestFixture]
    class APIApprovals
    {
        [Test]
        public void ApproveRavenDBPersistence()
        {
            var publicApi = typeof(RavenDBPersistence).Assembly.GeneratePublicApi(new ApiGeneratorOptions
            {
                ExcludeAttributes = new[] { "System.Runtime.Versioning.TargetFrameworkAttribute", "System.Reflection.AssemblyMetadataAttribute" }
            });
            Approver.Verify(publicApi);
        }
    }
}