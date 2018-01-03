To create the lab:

1. Install the cross-platform [Azure Cli](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest).
2. Have an Azure subscription with at least one region that has 50+ cores for Standard A SKUs.
3. On Windows, run the azurecreategroup.bat with the first parameter as the Azure region with sufficient cores, and the second parameter a unique string no longer than 6 characters to identify the clusters. For example
```sh
azurecreategroup eastus2 xr301
```
4. On Linux, simply run the Azure Cli command inside azurecreategroup.bat, providing the Azure region and a unique string to identify the clusters. 
