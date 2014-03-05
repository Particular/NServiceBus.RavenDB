using System;
using NServiceBus.RavenDB.Persistence;
using NUnit.Framework;
using Raven.Client.Document;
using Raven.Client.Embedded;

[TestFixture]
[Explicit]
public class RavenUserInstallerTests
{
    [Test]
    public void Integration()
    {
        using (var documentStore = new DocumentStore
            {
                Url = "http://localhost:8080",
                DefaultDatabase = "Test"
            })
        {
            documentStore.Initialize();

            var identity = Environment.MachineName + @"\Test";
            RavenUserInstaller.AddUserToDatabase(identity, documentStore);
        }
    }

    [Test]
    public void EnsureUserIsAddedToWindowsSettings()
    {
        using (var documentStore = new EmbeddableDocumentStore
            {
                RunInMemory = true,
            })
        {
            documentStore.Initialize();
            RavenUserInstaller.AddUserToDatabase(@"domain\user", documentStore);
            var systemCommands = documentStore
                .DatabaseCommands
                .ForSystemDatabase();
            var existing = systemCommands.Get("Raven/Authorization/WindowsSettings");

            var expected = @"{
  ""RequiredGroups"": [],
  ""RequiredUsers"": [
    {
      ""Name"": ""domain\\user"",
      ""Enabled"": true,
      ""Databases"": [
        {
          ""Admin"": true,
          ""ReadOnly"": false,
          ""TenantId"": ""<system>""
        }
      ]
    }
  ]
}".Replace("\r", String.Empty);

            var actual = existing.DataAsJson.ToString().Replace("\r", String.Empty);
            Assert.AreEqual(expected, actual);
        }
    }
}