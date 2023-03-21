namespace NServiceBus.Persistence.RavenDB;

using System;
using NuGet.Versioning;

static class MinimumRequiredRavenDbVersion
{
    static readonly string Version = "5.2.0";

    public static void Validate(string ravenDbVersionString, string minimumRequiredVersion)
    {
        var minimumRavenDbVersion = new NuGetVersion(minimumRequiredVersion);

        var ravenDbVersion = new NuGetVersion(ravenDbVersionString);

        if (ravenDbVersion.CompareTo(minimumRavenDbVersion) < 0)
        {
            throw new Exception($"We detected that the server is running on version {ravenDbVersionString}. RavenDB persistence requires RavenDB server {Version} or higher");
        }
    }

    public static void Validate(string ravenDbVersionString) => Validate(ravenDbVersionString, Version);
}