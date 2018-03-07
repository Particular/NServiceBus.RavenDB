namespace NServiceBus.RavenDB.Tests
{
    using System;

    class TestConstants
    {
        public static string RavenUrl => Environment.GetEnvironmentVariable("RavenDbUrl") ?? "http://localhost:8084";

        public static string RavenApiKey => Environment.GetEnvironmentVariable("RavenDbApiKey");
    }
}
