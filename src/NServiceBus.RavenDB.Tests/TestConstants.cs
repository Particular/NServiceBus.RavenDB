namespace NServiceBus.RavenDB.Tests
{
    using System;

    class TestConstants
    {
        public static string[] RavenUrls
        {
            get
            {
                var ravenUrl = Environment.GetEnvironmentVariable("RavenDbUrl") ?? "http://localhost:8084";
                return new[] {ravenUrl};
            }
        }
    }
}
