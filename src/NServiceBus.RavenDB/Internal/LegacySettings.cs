namespace NServiceBus.RavenDB.Internal
{
    using System;
    using NServiceBus.Saga;

    /// <summary>
    /// RavenDB conventions which were used in previous versions of NServiceBus
    /// </summary>
    class LegacySettings
    {     
        /// <summary>
        /// NServiceBus default RavenDB FindTypeTagName convention
        /// </summary>
        /// <param name="t">The type to apply convention.</param>
        /// <returns>The name of the find type tag.</returns>
        internal static string LegacyFindTypeTagName(Type t)
        {
            var tagName = t.Name;

            if (IsASagaEntity(t))
            {
                tagName = tagName.Replace("Data", String.Empty);
            }

            return tagName;
        }

        static bool IsASagaEntity(Type t)
        {
            return t != null && typeof(IContainSagaData).IsAssignableFrom(t);
        }
    }
}
