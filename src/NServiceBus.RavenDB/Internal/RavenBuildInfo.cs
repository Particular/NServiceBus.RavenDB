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
                throw new Exception(string.Format("Could not convert RavenDB server product version '{0}' to a .net version.", ProductVersion));
            }
            if (productVersion.Revision > 0)
            {
                throw new Exception(string.Format("Could not convert RavenDB server product version '{0}' conained a revision which was unexpected.", ProductVersion));
            }
            if (productVersion.Build > 0)
            {
                throw new Exception(string.Format("Could not convert RavenDB server product version '{0}' conained a build which was unexpected.", ProductVersion));
            }
            int buildVersion;
            if (!int.TryParse(BuildVersion, out buildVersion))
            {
                throw new Exception(string.Format("Could not convert RavenDB server build version '{0}' to a int.", BuildVersion));
            }
            return new Version(productVersion.Major,productVersion.Minor,buildVersion);
        }

    }
}