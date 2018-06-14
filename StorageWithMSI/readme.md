# Example for Azure VM to access Azure Blob Storage using MSI

Managed Service Identity (MSI) automatically provides Azure services identity to be used to authenticate against other Azure services, so you don't have to store password or secret keys in your code. 

There are two types of MSI: 
* System assigned MSI - exists with the service, for example a VM, that assumes the MSI
* User assigned MSI - exists as a standalone resource, other services can assume the MSI

The Java code uses the [Azure Java SDK](https://github.com/Azure/autorest-clientruntime-for-java) to implement a simple example of using system or user assigned MSI to list the containers in a given Azure blob storage account.  To run it, make sure you have all the dependency jars in your class folder, then run the following command: 

```bash
# using system assigned MSI
java -cp "/path/to/dependencies/*" com.paige.azure.App [my_storage_account]  
# using user assigned MSI
java -cp "/path/to/dependencies/*" com.paige.azure.App [my_storage_account]  [optional user_assigned_MSI client_id]
```

Below examples illustrate how MSI works with Azure Cli.

## Using System Assigned MSI 
1. Enable system assigned MSI to an existing VM
```bash
# this command also prints out the principal id of the msi which will be used below
az vm identity assign -g [my_resource_group] -n [my_vm]
# or you can display the info for an existing vm msi using the following command
az vm identity show -g [my_resource_group] -n [my_vm]
```
2. Grant the MSI access to storage
```bash
az role assignment create --assignee-object-id [principal id of msi] --role 'Storage Blob Data Reader (Preview)' --scope "/subscriptions/[my_subscription]/resourcegroups/[my_resource_group]/providers/Microsoft.Storage/storageAccounts/[my_storage_account]"
```
3. Acquire auth token for the MSI
```bash
curl 'http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource=https%3A%2F%2Fstorage.azure.com%2F' -H Metadata:true
```
4. Use the token to access storage
```bash
curl https://[my_storage_account].blob.core.windows.net/[my_container]/[my_file] -H "x-ms-version: 2017-11-09" -H "Authorization: Bearer [token]"
```
5. You can also log in to Azure Cli using MSI by running the following command on the MSI enabled VM
```bash
az login --identity
```