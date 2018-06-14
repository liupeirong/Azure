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
java -cp "/path/to/dependencies/*" com.paige.azure.App [my_storage_account]  [MSI_client_id]
# to get the client id 
az identity show -g [my_resource_group] -n [my_msi_name]
```

Below steps illustrate end-to-end how to use Azure Cli to access storage from a VM with MSI instead of access keys or shared access tokens. More details can be found [here](https://docs.microsoft.com/en-us/azure/active-directory/managed-service-identity/tutorial-linux-vm-access-storage).

## Using System Assigned MSI 
1. Enable system assigned MSI on an existing VM
```bash
# this command also prints out the principal id of the msi which will be used below
az vm identity assign -g [my_resource_group] -n [my_vm]
# or you can display the info for an existing vm msi
az vm identity show -g [my_resource_group] -n [my_vm]
```
2. Grant the MSI access to storage
```bash
# principal id comes from the output of the above command
az role assignment create --assignee-object-id [msi_principal_id] --role 'Storage Blob Data Reader (Preview)' --scope "/subscriptions/[my_subscription]/resourcegroups/[my_resource_group]/providers/Microsoft.Storage/storageAccounts/[my_storage_account]"
```
3. Acquire auth token from MSI to access storage
```bash
curl 'http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource=https%3A%2F%2Fstorage.azure.com%2F' -H Metadata:true
```
4. Use the token to access storage
```bash
curl https://[my_storage_account].blob.core.windows.net/[my_container]/[my_file] -H "x-ms-version: 2017-11-09" -H "Authorization: Bearer [token]"
```
5. You can also log in to Azure Cli using MSI by running the following command on the MSI enabled VM, the MSI must have been granted access to at least one Azure service.
```bash
az login --identity
```

## Using User Assigned MSI
1. Create a user assigned MSI
```bash
# this will display the msi client id and principal id which will be used later
az identity create -g [my_resource_group] -n [my_msi_name]
# or you can display the info for an existing user assigned msi
az identity show -g [my_resource_group] -n [my_msi_name]
```
2. Assign the MSI to an existing VM
```bash
az vm identity assign -g [my_resource_group] -n [my_vm] --identities "/subscriptions/[my_subscription]/resourcegroups/[my_resource_group]/providers/Microsoft.ManagedIdentity/userAssignedIdentities/[my_msi_name]"
```
3. Grant user assigned  MSI access to storage
```bash
# principal id comes from the output of the commands in step 1
az role assignment create --assignee [msi_principal_id] --role 'Storage Blob Data Reader (Preview)' --scope "/subscriptions/[my_subscription]/resourcegroups/[my_resource_group]/providers/Microsoft.Storage/storageAccounts/[my_storage_account]"
```
4. acquire token for the user assigned MSI from the VM
```bash
# client id comes from the output of the commands in step 1
curl "http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource=https%3A%2F%2Fstorage.azure.com%2F&client_id=[msi_client_id]" -H Metadata:true
```
5. Use the token to access storage
```bash
curl https://[my_storage_account].blob.core.windows.net/[my_container]/[my_file] -H "x-ms-version: 2017-11-09" -H "Authorization: Bearer [token]"
```