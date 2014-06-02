namespace NServiceBus.RavenDB.Internal
{
    using System.Configuration;
    using System.Linq;

    class Helpers
    {
        public static string GetFirstNonEmptyConnectionString(params string[] connectionStringNames)
        {
            try
            {
                return connectionStringNames.FirstOrDefault(cstr => ConfigurationManager.ConnectionStrings[cstr] != null);
            }
            catch (ConfigurationErrorsException)
            {
                return null;
            }
        }
    }
}
