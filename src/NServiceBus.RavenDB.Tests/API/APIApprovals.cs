namespace NServiceBus.RavenDB.Tests.API
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using ApprovalTests;
    using NUnit.Framework;
    using PublicApiGenerator;
    using System.Runtime.CompilerServices;

    [TestFixture]
    class APIApprovals
    {
        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ApproveRavenDbPersistence()
        {
            var combine = Path.Combine(TestContext.CurrentContext.TestDirectory, "NServiceBus.RavenDB.dll");
            var assembly = Assembly.LoadFile(combine);
            var publicApi = Filter(ApiGenerator.GeneratePublicApi(assembly));
            Approvals.Verify(publicApi);
        }

        string Filter(string text)
        {
            return string.Join(Environment.NewLine, text.Split(new[]
                {
                    Environment.NewLine
                }, StringSplitOptions.RemoveEmptyEntries)
                .Where(l => !l.StartsWith("[assembly: ReleaseDateAttribute("))
                .Where(l => !l.StartsWith("[assembly: System.Runtime.Versioning.TargetFrameworkAttribute("))
                .Where(l => !string.IsNullOrWhiteSpace(l))
            );
        }
    }
}