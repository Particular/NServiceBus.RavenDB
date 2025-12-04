namespace NServiceBus.Persistence.RavenDB;

class SagaDataContainer
{
    public string Id { get; set; }
    public string IdentityDocId { get; set; }
    public IContainSagaData Data { get; set; }

    internal static readonly string SchemaVersion = "1.0.0";
}