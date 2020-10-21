
# AutoCrane: Shipping container services safely on Kubernetes

AutoCrane is a Kubernetes operator that helps you safely ship container services. The concepts used by AutoCrane are borrowed from a Microsoft-internal orchestrator technology called Autopilot. Autopilot uses feedback from watchdogs--status reported from external applications or the application itself. Watchdogs in an error state can trigger automatic deployment rollbacks and will cause applications to be restarted up to the configured failing limit.

Another important feature is the concept of a data deployment. Applications often depend on data or configuration files. For applications that take a long time to start, we would prefer to update this data in a safe manner without restarting the application. Updating one of the data sources is called a data deployment. One might expect to see a new data deployment happening every couple of minutes, and turning these into application deployments would not be desirable due to extra resources consumed during application startup.

# AutoCrane Components

AutoCrane is configured with Kubernetes Custom Resource Definitions (CRDs). The CRDs are:
  - AutoCraneDeployment: A replicaSetSpec with data sources, rollout config, and failing limit config

Components:
  - AutoCrane: Watches for AutoCraneDeployment CRDs, creates Kubernetes pods for apps.
    Asks VersionWatcher for latest app version and manages rollouts/rollbacks.
    Asks DataRepository for latest synced data and tells DataDeployer what version of data to sync.
    Kills failing pods up to specified limits.
  - DataDeployer: an init and sidecar container for downloading data from DataRepository
  - DataRepository: Asks VersionWatcher for new data versions, downloads locally, serves to cluster.
  - Get/Post Watchdog: A utility to get or post watchdog information.
  - VersionWatcher: Probes upstream sources for new app and data versions.
  - WatchdogListener: Web service that lists for posting of watchdog status. Updates annotations/labels


# Setup

## Developer

If you are running Kubernetes locally with [minikube](https://github.com/kubernetes/minikube/releases), use this command to set up docker to build images:

`minikube docker-env`

Build an image like this:

`docker build . -t autocrane`

Install it like this:

`dotnet run --project src/AutoCrane/AutoCrane.csproj -- yaml | kubectl apply -f -`

or

`docker run -e AUTOCRANE_ARGS=yaml -it autocrane | kubectl apply -f -`

## Running Tools

Post a watchdog status (using kubernetes config credentials)

`dotnet run -- postwatchdog --Pod:Namespace=autocrane --Pod:Name=watchdoglistener-6957bbc9cf-w979c --Watchdog:Name=test --Watchdog:Level=Warning --Watchdog:Message=hi`

Get watchdog status (using kubernetes config credentials)

`dotnet run -- getwatchdog --Pod:Namespace=autocrane --Pod:Name=watchdoglistener-6957bbc9cf-w979c`

View rollup status:

`kubectl get pods -L status.autocrane.io/health`

Post watchdog status (inside the watchdog listener pod):

`curl http://localhost:8080/watchdogs/autocrane/watchdoglistener-6957bbc9cf-wgpzb -X PUT -H "Content-Type: application/json" -d "{\"Name\": \"test1\", \"Level\": \"Error\", \"Message\": \"failure\"}"`

View individual watchdogs (inside the watchdog listener pod):

`curl http://localhost:8080/watchdogs/autocrane/watchdoglistener-6957bbc9cf-wgpzb`



# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
