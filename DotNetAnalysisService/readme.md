## Simple example to add a role member to Azure Analysis Service

This C# example demonstrates how to automate management tasks of Azure Analysis Service by calling PowerShell commands to run a TMSL (Tabular Model Scripting Language) command. You can use either a user credential or an Azure Active Directory Service Principal to perform the automation tasks. 

### Pre-requisite
* [Upgrade to the latest version of PowerShell](https://docs.microsoft.com/en-us/powershell/scripting/setup/installing-windows-powershell?view=powershell-6#upgrading-existing-windows-powershell)
* Install the SqlServer PowerShell module by running ```Install-Module SqlServer``` as an administrator

__If you use a Service Principal to add a member__
* Verify Azure PowerShell version is at least 5.5.0:

```
Get-Module -Name AzureRM -ListAvailable | Select-Object -Property Name,Version

Name    Version
----    -------
AzureRM 5.5.0
```

If not, [Install the latest version of Azure PowerShell](https://docs.microsoft.com/en-us/powershell/azure/install-azurerm-ps?view=azurermps-5.5.0)

* Verify SqlServer module version is at least 21.0.17224:

```
Get-Module -Name SqlServer -ListAvailable | Select-Object -Property Name,Version

Name      Version
----      -------
SqlServer 21.0.17224

```

If not, Update to the latest version and delete the older version:

```
Install-Module SqlServer -Force
Uninstall-Module SqlServer -MaximumVersion {max version to delete}
```

* Add the Service Principal as an admin of the Azure Analysis Service server
    * Connect to Azure Analysis Service (AAS) using Sql Server Management Studio
    * Right-click on the AAS instance, __Properties__, __Security__, __Add...__![Alt text](/DotNetAnalysisService/Images/aasAdmin.png?raw=true "AAS Admin")
    * a dialog pops up for you to choose a user, close the dialog without choosing a user, another dialog pops up, in __Manual Entry__, enter the service principal in the format of app:appid@tenantid, click __Add__![Alt text](/DotNetAnalysisService/Images/addServicePrincipal.png?raw=true "add service principal")
