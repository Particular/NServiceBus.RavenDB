namespace NServiceBus.RavenDB.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using Microsoft.CSharp.RuntimeBinder;
    using Raven.Abstractions.Data;
    using Raven.Client;
    using Raven.Client.Connection;
    using Raven.Client.Document;
    using Raven.Json.Linq;
    using Installation;
    using Logging;
    using Settings;

    /// <summary>
    /// Add the identity to the Raven users group 
    /// </summary>
    abstract class RavenUserInstaller : INeedToInstallSomething
    {
        static readonly ILog logger = LogManager.GetLogger(typeof(RavenUserInstaller));
        
        public DocumentStore StoreToInstall { get; set; }

        public ReadOnlySettings Settings { get; set; }

        public void Install(string identity, Configure config)
        {
            if (StoreToInstall == null)
            {
                return;
            }

            if (Settings.GetOrDefault<bool>("RavenDB.DoNotSetupPermissions"))
            {
                logger.Info("User permissions setup has been disabled. Please make sure the correct access rights has been granted in RavenDB manually");
                return;
            }

            try
            {
                AddUserToDatabase(identity, StoreToInstall);
            }
            catch (Exception exception)
            {
                logger.Warn("Failed to add user to raven. Processing will continue", exception);
            }
        }

        internal static bool AddUserToDatabase(string identity, dynamic documentStore)
        {
            // We are using dynamic because the DefaultDatabase and Credentials properties aren't available on the IDocumentStore interface,
            // and EmbeddableDocumentStore (which isn't available without referencing additional assemblies) needs to supported as well.
            if (!(documentStore is IDocumentStore))
            {
                logger.ErrorFormat("Skipping adding user '{0}' to RavenDB, documentStore object passed wasn't of a recognized type", identity);
                return false;
            }

            try
            {
                var database = documentStore.DefaultDatabase ?? "<system>";

                var credentials = documentStore.Credentials as NetworkCredential;
                if (credentials != null && !string.IsNullOrWhiteSpace(credentials.UserName) && !string.IsNullOrWhiteSpace(credentials.Password))
                {
                    logger.InfoFormat("Skipping adding user '{0}' to RavenDB, because credentials were provided via connection string", identity);
                    return false;
                }

                logger.Info(string.Format("Adding user '{0}' to raven. Instance:'{1}', Database:'{2}'.", identity, documentStore.Url, database));

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

                return InvokePut(systemCommands, ravenJObject);
            }
            catch (RuntimeBinderException e) // to make sure nothing breaks in the future, due to our use of dynamic
            {
                logger.Error(string.Format("Skipping adding user '{0}' to RavenDB, because credentials were provided via connection string", identity), e);
                return false;
            }
        }

        static bool InvokePut(IDatabaseCommands systemCommands, RavenJObject ravenJObject)
        {
            try
            {
                systemCommands.Put("Raven/Authorization/WindowsSettings", null, ravenJObject, new RavenJObject());
                return true;
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
                dataAccess = new ResourceAccess
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
            public List<ResourceAccess> Databases = new List<ResourceAccess>();
        }
    }

}
