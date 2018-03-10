Many ISVs (Independent Software Vendors) developing multi-tenant applications with Power BI isolate tenants by providing separate data sources for each tenant. They can use [Power BI REST API](https://msdn.microsoft.com/en-us/library/mt147898.aspx) to clone reporting assets and update data source connection info for each tenant.  This doc illustrates how you can programmatically update the connection info for Azure Blob Storage Account.
 
## Programmatically update Azure Blob Storage Account in Power BI

1. In Power BI Desktop, parameterize Account Blob Storage Account Name for your report.
  - Create a new parameter

![Alt text](/PowerBIISV/Docs/Images/NewParameter.png?raw=true "Create a new parameter")
  
  - Define the parameter

![Alt text](/PowerBIISV/Docs/Images/BlobAccountParameter.png?raw=true "Define the parameter")

  - Reference the parameter

![Alt text](/PowerBIISV/Docs/Images/UseParameter.png?raw=true "Reference the parameter")
 
2. Assuming you know how to get workspace id, and the dataset id inside that workspace which contains Azure Blob Storage data source, call REST API to set the parameter
```
   POST https://api.powerbi.com/v1.0/myorg/groups/{{group_id}}/datasets/{{dataset_id}}/UpdateParameters
   Headers
        Authorization: Bearer {{bearer_token}}
        Content-Type: application/json
   Body
       { 
            "updateDetails": [ 
                { 
                    "name": "StorageAccountName", 
                    "newValue": "{{target_storage_account}}" 
                }
            ] 
        } 
```
   
3. Optinally call REST API to get the parameter to verify it's set correctly
```
    GET https://api.powerbi.com/v1.0/myorg/groups/{{group_id}}/datasets/{{dataset_id}}/parameters
    Headers
        Authorization: Bearer {{bearer_token}}
```

4. Call REST API to get the internal gateway for the Azure Blob Storage data source
```
    GET https://api.powerbi.com/v1.0/myorg/groups/{{group_id}}/datasets/{{dataset_id}}/Default.GetBoundGatewayDataSources
    Headers
        Authorization: Bearer {{bearer_token}}
```    
Response will look like this:
```json
    {
        "@odata.context": "http://some-redirect.analysis.windows.net/v1.0/myorg/groups/{{group_id}}/$metadata#gatewayDatasources",
        "value": [
            {
                "id": "{{gateway_datasource_id}}",
                "gatewayId": "{{gateway_id}}",
                "datasourceType": "AzureBlobs",
                "connectionDetails": "{\"account\":\"{{storage_account_name}}\",\"domain\":\"blob.core.windows.net\"}"
            }
        ]
    }
```
5. Call REST API to set the Storage Account key
```
    PATCH https://api.powerbi.com/v1.0/myorg/gateways/{{gateway_id}}/datasources/{{gateway_datasource_id}}
    Headers
        Authorization: Bearer {{bearer_token}}
        Content-Type: application/json
    Body
        {
            "credentialDetails": {
                "credentials": "{\"credentialData\":[{\"name\":\"key\",\"value\":\"{{storage_account_key}}\"}]}",
                "credentialType": "Key",
                "encryptedConnection": "Encrypted",
                "encryptionAlgorithm": "None",
                "privacyLevel": "None"
            }
        }
        
