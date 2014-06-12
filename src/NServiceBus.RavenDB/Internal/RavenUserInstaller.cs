namespace NServiceBus.RavenDB.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Raven.Abstractions.Data;
    using Raven.Abstractions.Extensions;
    using Raven.Client;
    using Raven.Client.Connection;
    using Raven.Client.Document;
    using Raven.Json.Linq;
    using Installation;
    using Logging;

    /// <summary>
    /// Add the identity to the Raven users group 
    /// </summary>
    class RavenUserInstaller : INeedToInstallSomething
    {
        static readonly ILog logger = LogManager.GetLogger(typeof(RavenUserInstaller));
        static List<DocumentStore> StoresToInstall { get; set; }
        static RavenUserInstaller()
        {
            StoresToInstall = new List<DocumentStore>();
        }
        public static void AddDocumentStore(IDocumentStore documentStore)
        {
            var store = documentStore as DocumentStore;
            if (store != null)
                StoresToInstall.Add(store);
        }

        public void Install(string identity, Configure config)
        {
            foreach (var store in StoresToInstall)
            {
                try
                {
                    AddUserToDatabase(identity, store);
                }
                catch (Exception exception)
                {
                    logger.Warn("Failed to add user to raven. Processing will continue", exception);
                }
            }
        }

        internal static void AddUserToDatabase(string identity, DocumentStore documentStore)
        {
            var database = documentStore.DefaultDatabase ?? "<system>";

            logger.InfoFormat(string.Format("Adding user '{0}' to raven. Instance:'{1}', Database:'{2}'.", identity, documentStore.Url, database));

            var systemCommands = documentStore
                .DatabaseCommands
                .ForSystemDatabase();
            var existing = systemCommands.Get("Raven/Authorization/WindowsSettings");

            WindowsAuthDocument windowsAuthDocument;
            if (existing == null)
            {
                windowsAuthDocument = new WindowsAuthDocument();
            }
            else
            {
                windowsAuthDocument = existing
                    .DataAsJson
                    .JsonDeserialization<WindowsAuthDocument>();
            }
            AddOrUpdateAuthUser(windowsAuthDocument, identity, database);

            var ravenJObject = RavenJObject.FromObject(windowsAuthDocument);

            InvokePut(systemCommands, ravenJObject);
        }

        static void InvokePut(IDatabaseCommands systemCommands, RavenJObject ravenJObject)
        {
            try
            {
                systemCommands.Put("Raven/Authorization/WindowsSettings", null, ravenJObject, new RavenJObject());
            }
            catch (TargetInvocationException exception)
            {
                //need to catch OperationVetoedException here but cant do it in a strong typed way since the namespace of OperationVetoedException changed in 2.5
                if (exception.InnerException.Message.Contains("Cannot setup Windows Authentication without a valid commercial license."))
                {
                    throw new Exception("RavenDB requires a Commercial license to configure windows authentication. Please either install your RavenDB license or contact support@particular.net if you need a copy of the RavenDB license.");
                }
                throw;
            }
        }

        static void AddOrUpdateAuthUser(WindowsAuthDocument windowsAuthDocument, string identity, string tenantId)
        {
            var windowsAuthForUser = windowsAuthDocument
                .RequiredUsers
                .FirstOrDefault(x => x.Name == identity);
            if (windowsAuthForUser == null)
            {
                windowsAuthForUser = new WindowsAuthData
                    {
                        Name = identity
                    };
                windowsAuthDocument.RequiredUsers.Add(windowsAuthForUser);
            }
            windowsAuthForUser.Enabled = true;

            AddOrUpdateDataAccess(windowsAuthForUser, tenantId);
        }

        static void AddOrUpdateDataAccess(WindowsAuthData windowsAuthForUser, string tenantId)
        {
            var dataAccess = windowsAuthForUser
                .Databases
                .FirstOrDefault(x => x.TenantId == tenantId);
            if (dataAccess == null)
            {
                dataAccess = new DatabaseAccess
                    {
                        TenantId = tenantId
                    };
                windowsAuthForUser.Databases.Add(dataAccess);
            }
            dataAccess.ReadOnly = false;
            dataAccess.Admin = true;
        }

        class WindowsAuthDocument
        {
            public List<WindowsAuthData> RequiredGroups = new List<WindowsAuthData>();
            public List<WindowsAuthData> RequiredUsers = new List<WindowsAuthData>();
        }

        class WindowsAuthData
        {
            public string Name;
// ReSharper disable once NotAccessedField.Local
            public bool Enabled;
            public List<DatabaseAccess> Databases = new List<DatabaseAccess>();
        }
    }

}