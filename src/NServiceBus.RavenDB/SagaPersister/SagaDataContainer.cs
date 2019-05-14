namespace NServiceBus.Persistence.RavenDB
{
    // TODO: Would making this SagaDataContainer<T> where T : IContainSagaData make things easier for determining type?
    class SagaDataContainer
    {
        public string Id { get; set; }
        public string IdentityDocId { get; set; }
        public IContainSagaData Data { get; set; }
    }
}