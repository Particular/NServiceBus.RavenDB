namespace NServiceBus.RavenDB.Persistence.SagaPersister
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text;

    class SagaUniqueIdentity
    {
        public string Id { get; set; }
        public Guid SagaId { get; set; }
        public object UniqueValue { get; set; }
        public string SagaDocId { get; set; }

        public static string FormatId(Type sagaType, KeyValuePair<string, object> uniqueProperty)
        {
            if (uniqueProperty.Value == null)
            {
                throw new ArgumentNullException("uniqueProperty", string.Format("Property {0} is marked with the [Unique] attribute on {1} but contains a null value. Please make sure that all unique properties are set on your SagaData and/or that you have marked the correct properties as unique.", uniqueProperty.Key, sagaType.Name));
            }

            // use MD5 hash to get a 16-byte hash of the string
            using (var provider = new MD5CryptoServiceProvider())
            {
                var inputBytes = Encoding.Default.GetBytes(uniqueProperty.Value.ToString());
                var hashBytes = provider.ComputeHash(inputBytes);

                // generate a guid from the hash:
                var value = new Guid(hashBytes);

                var id = string.Format("{0}/{1}/{2}", sagaType.FullName.Replace('+', '-'), uniqueProperty.Key, value);

                // raven has a size limit of 255 bytes == 127 unicode chars
                if (id.Length > 127)
                {
                    // generate a guid from the hash:
                    var hash = provider.ComputeHash(Encoding.Default.GetBytes(sagaType.FullName + uniqueProperty.Key));
                    var key = new Guid(hash);

                    id = string.Format("MoreThan127/{0}/{1}", key, value);
                }

                return id;
            }
        }
    }
}