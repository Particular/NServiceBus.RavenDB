NServiceBus.RavenDB
======================

Persistence support for NServiceBus RavenDB

NOT READY FOR PRODUCTION
=======================

## Wait I thought RavenDB was embedded in NServiceBus?

So a little history

### RavenDB used to be ILMerged into NServiceBus

In version 3 of NSerivceBus the default persistence was changed from NHibernate to RavenDB. The required RavenDB assemblies were [ILMerged](http://research.microsoft.com/en-us/people/mbarnett/ilmerge.aspx) into NServiceBus.Core.dll to give users a seamless OOTB experience.

This worked but had several negative side effects

 * The RavenDB classes had to be internalized to avoid namespace conflicts when people also reference the actual Raven assemblies. This meant a strong typed configuration API, that takes a `DocumentStore`, was not possible.
 * If consumers of upgraded to newer versions of Raven assemblies, for bug fixes or performance improvements, it was not possible for NServiceBus to leverage these newer assemblies. NServiceBus was hard coded to use the ILMerged versions.
 * Any changes in the compatibility of the Raven Client and Server would require a new version of NServiceBus be release with a new ILMerged version of Raven.

### RavenDB is now resource merged into NServiceBus

In version 4 of the approach to embedding Raven in NServiceBus.Core.dll changed from ILMerge to [Costura](https://github.com/Fody/Costura) 

This allowed us, at runtime, to chose which version of the Raven assemblies to load. So if a consumer of NServiceBus has updated to newer raven assemblies NServiceBus would use those instead of the merged versions. 

This resolved all the issue with ILMerged but raised a different one:  **Compatibility between different versions of the Raven client assemblies**. NServiceBus need to use a small subset of the Raven client APIs. At any one time we need to choose one version of those APIs to reference. This means that any incompatibilities between different versions of the Raven client API require a new version of NServiceBus to be release that copes with that incompatibility using reflection.  

## So what is the intent of this library

The idea with this library it to test the feasibility of shipping the RavenDB functionality for NServiceBus as a separate assembly. This will allow us to evolve the implementation more closely instep with the RavenDB release schedule. It should also reduce the need for version compatibility hacks.

### But isnt RavenDB still embedded in NServiceBus?

Yes this is true however since Costura only loads on demand usage of this library will effectively suppress usage of the merged version 

## How to use this library.

```
var documentStore = new DocumentStore 
{ 
    Url = "http://myravendb.mydomain.com/" 
};
config.RavenDBPersistence(store, true);
documentStore.Initialize();
```

## Where did the connection string config overloads go?

The previous Raven configuration API supported several approaches to passing in a connection string. This API had several issue.

 * Suffered from too many choices.
 * Minor typos in an App.config file could cause connection string to be ignored
 * The strong typed `DocumentStore` overload does not apply the NServiceBus conventions and hence force a user to have internal knowledge of NServiceBus
 * NServiceBus took ownership of the client-server version compatibility checking. This should be a concern of the developer consuming the API
 * NServiceBus took ownership of verifying the connectivity to the server. This should be a concern of the developer consuming the API.
 
So now there is one configuration API that takes a `DosumentStore`.