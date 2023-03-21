namespace NServiceBus;

using System;
using NuGet.Versioning;

static class RavenDbVersionValidator
{
    static bool IsLessThan(this NuGetVersion version, NuGetVersion otherVersion)
    {
        var majorIsLessThan = version.Major < otherVersion.Major;

        var majorIsEqualTo = version.Major == otherVersion.Major;

        var minorIsLessThan =
            majorIsEqualTo
            && version.Minor < otherVersion.Minor;

        var minorIsEqualTo =
            majorIsEqualTo
            && version.Minor == otherVersion.Minor;

        var patchIsLessThan =
            minorIsEqualTo
            && version.Patch < otherVersion.Patch;

        var patchIsEqualTo =
            minorIsEqualTo
            && version.Patch == otherVersion.Patch;

        var releaseIsEqual = version.IsPrerelease == otherVersion.IsPrerelease;

        var releaseIsLessThan = version.Release.CompareTo(otherVersion.Release) < 0;

        var prereleaseIsLessThan =
            patchIsEqualTo
            && (releaseIsEqual
                || releaseIsLessThan);

        return majorIsLessThan
               || minorIsLessThan
               || patchIsLessThan
               || prereleaseIsLessThan;
    }

    public static void ValidateVersion(string ravenDbVersionString, NuGetVersion minimumRequiredVersion)
    {
        var ravenDbVersion = new NuGetVersion(ravenDbVersionString);

        if (ravenDbVersion.IsLessThan(minimumRequiredVersion))
        {
            throw new Exception($"We detected that the server is running on version {ravenDbVersion}. RavenDB persistence requires RavenDB server 5.2 or higher");
        }
    }
}