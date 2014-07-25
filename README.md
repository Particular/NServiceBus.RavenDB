NServiceBus.RavenDB
======================

Persistence support for NServiceBus RavenDB

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

The idea is to be able to ship upgrades to this library without having to ship the core. This will allow us to evolve the implementation more closely instep with the RavenDB release schedule. It should also reduce the need for version compatibility hacks.

### But isn't RavenDB still embedded in NServiceBus?

Yes, but with this library is now possible to load a different implementation of the RavenDB storages.

## How to use this library.

```c#
Configure.With()
    .DefaultBuilder()
    .RavenDBStorage() // Need to call this method
    .UseRavenDBSubscriptionStorage() // Call this method to use Raven subscriptiion storage
    .UseRavenDBTimeoutStorage() // Call this method to use Raven saga storage
    .UseRavenDBGatewayDeduplicationStorage() // Call this method to use Raven dedupplication storage for the Gateway
    .UseRavenDBGatewayStorage(); // Call this method to use the old Raven Gateway storage method

```
