namespace NServiceBus.RavenDB.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using NServiceBus.Installation;
    using NServiceBus.Logging;
    using NServiceBus.Settings;
    using Raven.Abstractions.Data;
    using Raven.Abstractions.Extensions;
    using Raven.Client.Connection;
    using Raven.Client.Document;
    using Raven.Json.Linq;

    /// <summary>
    ///     Add the identity to the Raven users group
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

        internal static void AddUserToDatabase(string identity, DocumentStore documentStore)
        {
            var database = documentStore.DefaultDatabase ?? "<system>";

            var credentials = documentStore.Credentials as NetworkCredential;
            if (credentials != null && !string.IsNullOrWhiteSpace(credentials.UserName) && !string.IsNullOrWhiteSpace(credentials.Password))
            {
                logger.InfoFormat("Skipping adding user '{0}' to RavenDB, because credentials were provided via connection string", identity);
                return;
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
#pragma warning disable 414
            public bool Enabled;
#pragma warning restore 414
            public List<DatabaseAccess> Databases = new List<DatabaseAccess>();
        }
    }
}