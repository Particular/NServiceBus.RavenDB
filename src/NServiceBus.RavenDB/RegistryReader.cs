namespace NServiceBus.Utils
{
    using System;
    using Logging;
    using Microsoft.Win32;

    class RegistryReader<T>
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(RegistryReader<T>));

        public static T Read(string name, T defaultValue = default(T))
        {
            try
            {              
                return ReadRegistryKeyValue(name, defaultValue);
            }
            catch (Exception ex)
            {
                var message = string.Format(@"We couldn't read the registry to retrieve the {0}, from 'HKEY_LOCAL_MACHINE\SOFTWARE\ParticularSoftware\ServiceBus'.", name);
                Logger.Warn(message, ex);
            }

            return defaultValue;
        }

        static T ReadRegistryKeyValue(string keyName, object defaultValue)
        {
            if (Environment.Is64BitOperatingSystem)
            {
                if (ReadRegistry(RegistryView.Registry32, keyName, defaultValue) != null)
                {
                    return (T) ReadRegistry(RegistryView.Registry32, keyName, defaultValue);
                }
                return (T) ReadRegistry(RegistryView.Registry64, keyName, defaultValue);
            }
            return (T) ReadRegistry(RegistryView.Default, keyName, defaultValue);
        }

        static object ReadRegistry(RegistryView view, string keyName, object defaultValue)
        {
            using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view).OpenSubKey(@"SOFTWARE\ParticularSoftware\ServiceBus"))
            {
                if (key == null)
                {
                    return defaultValue;
                }
                return key.GetValue(keyName, defaultValue);
            }
        }
    }
}