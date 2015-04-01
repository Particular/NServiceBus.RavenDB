namespace NServiceBus.RavenDB.Internal
{
    using System;
    using System.Linq;

    class RavenBuildInfo
    {
        public string ProductVersion { get; set; }
        public string BuildVersion { get; set; }

        public Version ConvertToVersion()
        {
            Version productVersion;
            if (!Version.TryParse(ProductVersion.Split(' ').First(), out productVersion))
            {
                throw new Exception($"Could not convert RavenDB server product version '{ProductVersion}' to a .net version.");
            }
            if (productVersion.Revision > 0)
            {
                throw new Exception($"Could not convert RavenDB server product version '{ProductVersion}' contained a revision which was unexpected.");
            }
            if (productVersion.Build > 0)
            {
                throw new Exception($"Could not convert RavenDB server product version '{ProductVersion}' contained a build which was unexpected.");
            }
            int buildVersion;
            if (!int.TryParse(BuildVersion, out buildVersion))
            {
                throw new Exception($"Could not convert RavenDB server build version '{BuildVersion}' to an int.");
            }
            return new Version(productVersion.Major, productVersion.Minor, buildVersion);
        }

    }
}