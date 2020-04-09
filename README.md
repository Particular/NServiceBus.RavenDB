# NServiceBus.RavenDB

The official [NServiceBus](https://github.com/Particular/NServiceBus) persistence implementation for [RavenDB.](https://ravendb.net/)

Learn more about NServiceBus.RavenDB through our [documentation.](http://docs.particular.net/nservicebus/ravendb/)

If you are interested in contributing, please follow the instructions [here.](https://github.com/Particular/NServiceBus/blob/develop/CONTRIBUTING.md)

## Running the tests

Running the tests requires RavenDB 4.2 available on `localhost:8080` or an environment variable named `CommaSeparatedRavenClusterUrls` containing the connection URLs, separated by commas if testing a cluster. The tests can be run with a RavenDB server hosted on a Docker container.
