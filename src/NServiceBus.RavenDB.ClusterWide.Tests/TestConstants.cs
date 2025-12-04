namespace NServiceBus.RavenDB.Tests;

using System;

class TestConstants
{
    public static string[] RavenUrls
    {
        get
        {
            var urls = Environment.GetEnvironmentVariable("CommaSeparatedRavenClusterUrls") ?? "http://localhost:8081,http://localhost:8082,http://localhost:8083";
            return urls.Split(',');
        }
    }
}