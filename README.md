
# AutoCrane: Shipping container services safely on Kubernetes

AutoCrane is a Kubernetes operator that helps you safely ship container services. The concepts used by AutoCrane are borrowed from a Microsoft-internal orchestrator technology called Autopilot. Autopilot uses feedback from watchdogs--status reported from external applications or the application itself. Watchdogs in an error state can trigger automatic deployment rollbacks and will cause applications to be restarted up to the configured failing limit.

Another important feature is the concept of a data deployment. Applications often depend on data or configuration files. For applications that take a long time to start, we would prefer to update this data in a safe manner without restarting the application. Updating one of the data sources is called a data deployment. One might expect to see a new data deployment happening every couple of minutes, and turning these into application deployments would not be desirable due to extra resources consumed during application startup.

# AutoCrane Components

AutoCrane is configured with Kubernetes Custom Resource Definitions (CRDs). The CRDs are:
  - AutoCraneDeployment: A deployment spec with data sources, rollout config, and failing limit config

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
