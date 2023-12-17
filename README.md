# NServiceBus.RavenDB

NServiceBus.RavenDB is the official [NServiceBus](https://github.com/Particular/NServiceBus) persistence implementation for [RavenDB](https://ravendb.net/).

It is part of the [Particular Service Platform](https://particular.net/service-platform), which includes [NServiceBus](https://particular.net/nservicebus) and tools to build, monitor, and debug distributed systems.

See the [RavenDB Persistence documentation](http://docs.particular.net/nservicebus/ravendb/) for more details on how to use it.

## Running tests locally

Running the tests requires RavenDB 5.2 and two environment variables: 

1. `CommaSeparatedRavenClusterUrls` containing the URLs, separated by commas, to connect to a RavenDB cluster to run cluster-wide transaction tests
1. `RavenSingleNodeUrl` containing the URL of a single node RavenDB instance to run non-cluster-wide tests

The tests can be run with RavenDB servers hosted on a Docker container.

## CI Workflow

The [CI workflow](/.github/workflows/ci.yml) requires the following secret, unique to RavenDB, to be defined both as an Actions and Dependabot secret:

1. RAVENDB_LICENSE: A RavenDB development license, expressed as JSON, but all on one line, with escaped quotes `\"`

The value used by Particular is stored in a secure note called "RavenDB CI Secrets".

### Spinning up the necessary infrastructure

This assumes docker and docker-compose are properly setup. It currently works on Windows with Docker Desktop but not on docker hosted in WSL2 only.

1. [Acquire a developer license](https://ravendb.net/license/request/dev)
1. Convert the multi-line license JSON to a single line JSON and set the `LICENSE` variable. Alternatively the license can be set using [an `.env` file](https://docs.docker.com/compose/environment-variables/).
1. Inside the root directory of the repository issue the following command: `docker-compose up -d`.

The single node server is reachable under [`http://localhost:8080`](http://localhost:8080). The cluster leader is reachable under [`http://localhost:8081`](http://localhost:8081).
