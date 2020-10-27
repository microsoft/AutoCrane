
# AutoCrane: Shipping container services safely on Kubernetes

AutoCrane is a Kubernetes operator that helps you safely ship container services. The concepts used by AutoCrane are borrowed from a Microsoft-internal orchestrator technology called Autopilot. Autopilot uses feedback from watchdogs--status reported from external applications or the application itself. Watchdogs in an error state can trigger automatic deployment rollbacks and will cause applications to be restarted up to the configured failing limit.

Another important feature is the concept of a data deployment. Applications often depend on data or configuration files. For applications that take a long time to start, we would prefer to update this data in a safe manner without restarting the application. Updating one of the data sources is called a data deployment. One might expect to see a new data deployment happening every couple of minutes, and turning these into application deployments would not be desirable due to extra resources consumed during application startup.

# AutoCrane

## Components
  - AutoCrane:
    - Asks DataRepository for data versions and tells pods/DataDeployer what version of data to sync.
    - Calls Eviction API on pods with watchdog failures.
  - DataDeployer: an init and sidecar container for downloading data from DataRepository
  - DataRepository: Probes for new data versions, downloads locally, serves version info and data to cluster.
  - Get/Post Watchdog: A utility to get or post watchdog information.
  - TestWorkload: A program with a GET `/watchdog` endpoint which fails after you post to `/fail`.
  - WatchdogProber: Finds watchdog probe URLs by scanning pod annotations. Probes and updates watchdog status annotations/labels
  - WatchdogHealthz: Reads pod's watchdog annotations and provides a probe that succeeds/fails based on how long the pod has been in a healthy watchdog state.

## Annotations and Labels

AutoCrane components consumes the following pod annotations set by you:
  - `probe.autocrane.io/NAME: POD_IP:1234/url` For WatchdogProber: Sets up a watchdog called `NAME` by probing `http://POD_IP:1234/url`
  - `store.autocrane.io/url: http://datarepository` For DataDeployer: Sets the url for the data repository.
  - `store.autocrane.io/location: /data` For DataDeployer: Sets where data is downloaded to.
  - `data.autocrane.io/NAME: srcname` For DataDeployer: Sets up a pod-local data deployment called `NAME`.
  - `data.autocrane.io/srcname: git:https://github.com/microsoft/AutoCrane.git` For DataRepository: Sets up a source and the spec for where it comes from.

AutoCrane components set the following annotations for storing state:
  - `status.autocrane.io/NAME: <level>/timestamp/message` A watchdog status annotation
  - `request.data.autocrane.io/NAME: data` Stores AutoCrane's request for data deployment `NAME`. Read by DataDeployer.
  - `status.data.autocrane.io/NAME: data` Stores DataDeployer status for data deployment `NAME`. Read by AutoCrane.

AutoCrane components set the following pod labels for convenient querying:
  - `status.autocrane.io/health: error`: Updated when watchdogs are written to make it easier to find failing pods

# Patterns

## Watchdogs

For steady-state errors in applications which you would not want to cause a global outage (e.g. data or cache refresh failures):
  - Create an endpoint in one of the containers in a pod to signal such a failure
  - Create a PodDisruptionBudget for your failing limits
  - Set a an annotation: `probe.autocrane.io/watchdog1: POD_IP:8080/watchdog` (watchdog1 is the watchdog name)
  - WatchdogProber will scan for that annotation and update annotations/labels on your failing pods
  - AutoCrane will call the eviction API on pods with a watchdog error (it scans periodically and if it hasn't cleared after a few scans)


## Using Watchdogs to Stop Deployment Rollout

To block a deployment on watchdog failures we'll create a dummy sidecar pod that maps watchdog errors to a readiness probe.
The catch is that we don't want to fail the readiness probe after the deployment is complete, because that will hurt availability.
So the approach is to have a canary deployment with this sidecar and a regular deployment without the sidecar.
That way if the canary's readiness probes all fail, you can track it with an alert but won't have a large outage at once.

  - Sample sidecar for canary deployment:
```
      - name: watchdoghealthz
        image: <autocrane>
        env:
          - name: AUTOCRANE_ARGS
            value: watchdoghealthz
          - name: LISTEN_PORT
            value: "8081"
          - name: Watchdogs__MinReadySeconds
            value: <MinReadySeconds>
          - name: Pod__Name
            valueFrom:
              fieldRef:
                fieldPath: metadata.name
          - name: Pod__Namespace
            valueFrom:
              fieldRef:
                fieldPath: metadata.namespace
        readinessProbe:
          httpGet:
            path: /healthz
            port: 8081
          failureThreshold: 1
```

Values
  - MinReadySeconds: how long it would take for watchdog failures to be posted--keep the pod in non-ready state until then.

# Setup

## Developer

If you are running Kubernetes locally with [minikube](https://github.com/kubernetes/minikube/releases), use this command to set up docker to build images:

`minikube docker-env`

Build an image like this:

`docker build . -t autocrane`

Install it like this:

`docker run -e AUTOCRANE_ARGS="yaml use_testworkload=1" -it autocrane | kubectl apply -f -`

## Running Tools

Post a watchdog status (using kubernetes config credentials)

`dotnet run -- postwatchdog --Pod:Namespace=autocrane --Pod:Name=watchdoglistener-6957bbc9cf-w979c --Watchdog:Name=test --Watchdog:Level=Warning --Watchdog:Message=hi`

Get watchdog status (using kubernetes config credentials)

`dotnet run -- getwatchdog --Pod:Namespace=autocrane --Pod:Name=watchdoglistener-6957bbc9cf-w979c`

View rollup status:

`kubectl get pods -L status.autocrane.io/health`





## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
