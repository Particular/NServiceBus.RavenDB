# NServiceBus.RavenDB

The official [NServiceBus](https://github.com/Particular/NServiceBus) persistence implementation for [RavenDB.](https://ravendb.net/)

Learn more about NServiceBus.RavenDB through our [documentation.](http://docs.particular.net/nservicebus/ravendb/)

If you are interested in contributing, please follow the instructions [here.](https://github.com/Particular/NServiceBus/blob/develop/CONTRIBUTING.md)

## Running the tests

Running the tests requires RavenDB 5.2 and two environment variables. One named `CommaSeparatedRavenClusterUrls` containing the URLs, separated by commas, to connect to a RavenDB cluster to run cluster-wide transaction tests. The second one named `RavenSingleNodeUrl` containing the URL of a single node RavenDB instance to run non-cluster-wide tests. The tests can be run with RavenDB servers hosted on a Docker container.

## CI Workflow

The [CI workflow](/.github/workflows/ci.yml) requires two secrets unique to RavenDB to be defined both as Actions and Dependabot secrets:

* RAVENDB_LICENSE: A RavenDB development license, expressed as JSON, but all on one line, with escaped quotes `\"`
* PASSPHRASE: Any phrase used to encrypt the connection information between steps in the CI

The values used by Particular are both stored in a secure note called "RavenDB CI Secrets".

### Spinning up the necessary infrastructure

This assumes docker and docker-compose are properly setup. It works currently on Windows with Docker Desktop but not on docker hosted in WSL2 only.

1. [Acquire a developer license](https://ravendb.net/license/request/dev)
1. Convert the multi-line license JSON to a single line JSON and set the `LICENSE` variable. Alternatively the license can be set using [an `.env` file](https://docs.docker.com/compose/environment-variables/).
1. Inside the root directory of the repository issue the following command: `docker-compose up -d`.

The single node server is reachable under [`http://localhost:8080`](http://localhost:8080). The cluster leader is reachable under [`http://localhost:8081`](http://localhost:8081).
