namespace NServiceBus.RavenDB.Persistence.SagaPersister;

using System;
using System.Security.Cryptography;
using System.Text;

class SagaUniqueIdentity
{
    public string Id { get; set; }
    public Guid SagaId { get; set; }
    public object UniqueValue { get; set; }
    public string SagaDocId { get; set; }

    public static string FormatId(Type sagaType, string propertyName, object propertyValue)
    {
        if (propertyValue == null)
        {
            throw new ArgumentNullException(nameof(propertyValue), $"Property {propertyName} is a correlation property on {sagaType.Name} but contains a null value. Make sure that all correlation properties on the SagaData have a defined value.");
        }

        // use MD5 hash to get a 16-byte hash of the string
        using (var provider = MD5.Create())
        {
            var inputBytes = Encoding.Default.GetBytes(propertyValue.ToString());
            var hashBytes = provider.ComputeHash(inputBytes);

            // generate a guid from the hash:
            var value = new Guid(hashBytes);

            var id = $"{sagaType.FullName.Replace('+', '-')}/{propertyName}/{value}";

            // raven has a size limit of 255 bytes == 127 unicode chars
            if (id.Length > 127)
            {
                // generate a guid from the hash:
                var hash = provider.ComputeHash(Encoding.Default.GetBytes(sagaType.FullName + propertyName));
                var key = new Guid(hash);

                id = $"MoreThan127/{key}/{value}";
            }

            return id;
        }
    }

    internal static readonly string SchemaVersion = "1.0.0";
}