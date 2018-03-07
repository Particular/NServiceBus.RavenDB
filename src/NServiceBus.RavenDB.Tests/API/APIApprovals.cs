namespace NServiceBus.RavenDB.Tests.API
{
    using System;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using NUnit.Framework;
    using PublicApiGenerator;

    [TestFixture]
    class APIApprovals
    {
        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ApproveRavenDBPersistence()
        {
            var publicApi = Filter(ApiGenerator.GeneratePublicApi(typeof(RavenDBPersistence).Assembly));
            TestApprover.Verify(publicApi);
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