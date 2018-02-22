This example demonstrates 
1. how to query when a VM create/delete request is received by Azure
2. how to send custom logs on VM to Azure Storage

If you are an ISV building solutions on Azure infrastructure, you'll likely need a robust way to manage and monitor the VMs you create on your users' behalf. If a VM creation/deletion takes longer than expected, you'd want to isolate whether the problem is in your code or in the cloud fabric controller. Also, if something inside the VM fails, but the VM itself is successfully created, you'd probably want to automatically send the log to an outside storage before deleting the VM.  

## Given a VM name and approximate starting time, find out when Azure received the requests to create/delete the VM

The following example uses Azure Cli to query the Azure activity log for a given VM. In this case, the VM went through being created, stopped/deallocted, started, and finally deleted. For deallocation and start, it appears to be that the requests were made asynchronously, so there are one BeginRequest and two EndRequest events associated with each operation. The initial VM creation and final deletion are only associated with a single event respectively.

```bash
>az monitor activity-log list --caller me@contoso.com --resource-provider Microsoft.Compute --start-time 2018-02-01T00:00:00Z --query "[?contains(resourceId, 'mytestvm')].[resourceGroup, operationName.value, eventName.value, eventTimestamp, status.value, correlationId]" --out table

ResourceGroup  OperationName                                 EventName     EventTimestamp                    Status      CorrelationId
-------------  --------------------------------------------- ----------------------------------------------  ---------- ------------------------------------  
testrg  Microsoft.Compute/virtualMachines/delete             EndRequest    2018-02-16T22:33:32.802538+00:00  Succeeded  4ee19ab9-2321-4730-acd9-9f973adaf149
testrg  Microsoft.Compute/virtualMachines/start/action       EndRequest    2018-02-16T19:35:03.329812+00:00  Succeeded  3ee664b9-3a9b-4d51-94d1-4b8aa097462d
testrg  Microsoft.Compute/virtualMachines/start/action       EndRequest    2018-02-16T19:26:27.291853+00:00  Accepted   3ee664b9-3a9b-4d51-94d1-4b8aa097462d
testrg  Microsoft.Compute/virtualMachines/start/action       BeginRequest  2018-02-16T19:26:26.885549+00:00  Started    3ee664b9-3a9b-4d51-94d1-4b8aa097462d
testrg  Microsoft.Compute/virtualMachines/deallocate/action  EndRequest    2018-02-16T07:16:22.856966+00:00  Succeeded  44d84eca-b40b-4a75-9e38-163583110c04
testrg  Microsoft.Compute/virtualMachines/deallocate/action  EndRequest    2018-02-16T07:13:26.853594+00:00  Accepted   44d84eca-b40b-4a75-9e38-163583110c04
testrg  Microsoft.Compute/virtualMachines/deallocate/action  BeginRequest  2018-02-16T07:13:26.478608+00:00  Started    44d84eca-b40b-4a75-9e38-163583110c04
testrg  Microsoft.Compute/virtualMachines/start/action       EndRequest    2018-02-15T17:41:20.223935+00:00  Succeeded  61e2ccfd-a099-4b8c-a8ff-68d0a64e228b
```

Given a correlationId, you can also query all the events related to that operation. 

```bash
>az monitor activity-log list --resource-provider Microsoft.Compute --start-time 2018-02-01T00:00:00Z --query "[?correlationId == '44d84eca-b40b-4a75-9e38-163583110c04'].[resourceGroup, resourceId, operationName.value, eventName.value, eventTimestamp, status.value]" --out table

ResourceGroup  ResourceId                                                                                    OperationName                                        EventName     EventTimestamp                    Status                           
-------------  --------------------------------------------------------------------------------------------  ---------------------------------------------------  ------------  --------------------------------  ---------
testrg  /subscriptions/xyz/resourceGroups/testrg/providers/Microsoft.Compute/virtualMachines/mytestvm  Microsoft.Compute/virtualMachines/deallocate/action  EndRequest    2018-02-16T07:16:22.856966+00:00  Succeeded
testrg  /subscriptions/xyz/resourceGroups/testrg/providers/Microsoft.Compute/virtualMachines/mytestvm  Microsoft.Compute/virtualMachines/deallocate/action  EndRequest    2018-02-16T07:13:26.853594+00:00  Accepted
testrg  /subscriptions/xyz/resourceGroups/testrg/providers/Microsoft.Compute/virtualMachines/mytestvm  Microsoft.Compute/virtualMachines/deallocate/action  BeginRequest  2018-02-16T07:13:26.478608+00:00  Started
```

## Automatically send VM extension log to Azure blob storage

In this scenario, we want to automatically preserve logs from an Azure Linux VM extension to Blob storage shortly after the VM is created, so that we can delete the VM without losing the logs. There are a few options for collecting in-VM logs:
* __Azure Log Analytics__ using [Operations Management Suit(OMS) agent](https://docs.microsoft.com/en-us/azure/virtual-machines/linux/extensions-oms) - the custom log collection capability is still in preview and can't get it to work for this case at the moment (Feb 2018).
* __Azure diagnostics logs__ using [Azure diagnostics extension](https://docs.microsoft.com/en-us/azure/virtual-machines/linux/diagnostic-extension) - this works and there's no need to install and configure additional components. But the logs are only sent to storage every hour, and it's about an hour after the VM is created. This is true for both blob and table storage. So you'll need to keep the VM for an hour which is inconvenient.  [diagnostics.azuredeploy.json](/VMMonitoring/diagnostics.azuredeploy.json) calls the nested template [diagnostics.extension.json](/VMMonitoring/diagnostics.extension.json) to set up Azure diagnostics extension during VM creation. 
* __Fluentd__ with [Azure Blob Storage output plugin](https://github.com/htgc/fluent-plugin-azurestorage) - this is implemented in [fluentd.azuredeploy.json](/VMMonitoring/fluentd.azuredeploy.json), which calls the nested template [fluentd.extension.json](/VMMonitoring/fluentd.extension.json), which calls a [script](/VMMonitoring/setup-fluentd.sh) to install and configure fluentd. It's configured to send logs immediately to storage. There's one caveat - the tail type of source doesn't seem to work when the log is first created, even though we already configured fluentd to be installed and running before the target log is generated. If the log is later appended, it does work. To solve this problem, we set ```read_from_head true```, but this does mean if the log is later appended, fluentd will send the entire log file to storage again. 
 
