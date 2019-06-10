namespace NServiceBus.RavenDB.Tests
{
    using System;

    class TestConstants
    {
        public static string[] RavenUrls
        {
            get
            {
                var urls = Environment.GetEnvironmentVariable("CommaSeparatedRavenClusterUrls");
                if (urls == null)
                {
                    throw new Exception("RavenDB cluster URLs must be specified in an environment variable named CommaSeparatedRavenClusterUrls.");
                }

                return urls.Split(',');
            }
        }
    }
}
