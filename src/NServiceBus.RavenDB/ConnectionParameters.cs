namespace NServiceBus.RavenDB
{
    /// <summary>
    /// Connection parameters to be used when connecting to RavenDB
    /// </summary>
    public class ConnectionParameters
    {
        /// <summary>
        /// The url of the RavenDB server
        /// </summary>
        public string Url { get; set; }
        
        /// <summary>
        /// The name of the database to use on the specified RavenDB server
        /// </summary>
        public string DatabaseName { get; set; }
        
        /// <summary>
        /// The RavenDB API key if needed
        /// </summary>
        public string ApiKey { get; set; }
    }
}
