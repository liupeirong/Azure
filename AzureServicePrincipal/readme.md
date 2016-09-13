# Create Service Prinpical with Azure Cli to Manage Azure Resources

In a scenario where you use an application to automate the creation of Azure resources, you will need to create a Service Principal in Azure Active Directory (AAD), and assign a role to the Principal so that it has proper access rights to your Azure subscription. The following steps outline how you can do this using Azure cross-platform command line tool. 

* Login in to Azure cli, this also decides which Azure Active Directory you are logging in: 
```sh
azure login
```
* Set the current Azure subscription 
```sh
azure account list  # list of ubscriptions
azure account set <my subscription id>
```
* Create a service principal, the urls don't have to be live
```sh
azure ad sp create -n <my service principal name> -m http://<my url> -p <my password> -r http://<my url> -i http://<unique url in my directory>
# output:
# info:    Executing command ad sp create
# + Creating application <my service principal name>
# + Creating service principal for application 12345678-9012-3456-7890-123456789012
# data:    Object Id:               00000000-5689-4f09-958d-b0ae2c5d9dbc
# data:    Display Name:            <my service principal name>
# data:    Service Principal Names:
# data:                             12345678-9012-3456-7890-123456789012
# data:                             http://<my unique url>
```
* Assign the service principal a role to access Azure subscription 
```sh
# add the --scope parameter to limit access to a specific resource, for example, a resource group instead of the entire subscription
azure role assignment create --objectId 00000000-5689-4f09-958d-b0ae2c5d9dbc --roleName contributor

# get the tenant id for later use
azure account show
```

* In another session, use the service principal to log in to Azure Cli and start manage resources
```sh
azure login --service-principal -u 12345678-9012-3456-7890-123456789012 -p <my password> --tenant <my tenant id>
```