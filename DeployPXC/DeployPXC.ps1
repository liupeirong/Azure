#
# DeployPXC.ps1
#
param (
        [parameter(Mandatory=$true)]
        [String]$CredentialName = "pliu@microsoft.com",
        [parameter(Mandatory=$true)]
        [String]$SubscriptionName = "pliu@ted",
        [parameter(Mandatory=$true)]
        [String]$StorageAccountName = "pliueuro",
        [parameter(Mandatory=$true)]
        [String]$ServiceName = "azurepxc",
        [parameter(Mandatory=$true)]
        [String]$VNetName = "peieuro",
        [parameter(Mandatory=$true)]
        [String]$DBSubnet = "dbsubnet",
        [parameter(Mandatory=$true)]
        [String]$DBNodeIPs = "10.34.0.4,10.34.0.5,10.34.0.6",
        [parameter(Mandatory=$true)]
        [String]$LoadBalancerIP = "10.34.0.7",
        [parameter(Mandatory=$true)]
        [String]$VMSize = "Large",
        [parameter(Mandatory=$true)]
        [Int]$NumOfDisks = 4, #if greater than 1, then the disks will be provisioned as raid0
        [parameter(Mandatory=$true)]
        [Int]$DiskSizeInGB = 10,
        [parameter(Mandatory=$true)]
        [String]$VMNamePrefix = "azpxc",
        [parameter(Mandatory=$true)]
        [String]$VMUser = "pliu",
        [parameter(Mandatory=$true)]
        [String]$VMPassword = "MicrosoftDx1",
        [parameter(Mandatory=$true)]
        [String]$VMExtLocation = "https://pliueuro.blob.core.windows.net/scripts/azurepxc.sh",
        [parameter(Mandatory=$false)]
        [String]$SecondNICName = "",  #this indicates the need for 2nd NIC. leave this empty if you don't have 2nd NIC
        [parameter(Mandatory=$false)]
        [String]$SecondNICSubnet = "clustersubnet", #leave this empty if you don't need 2nd NIC
        [parameter(Mandatory=$false)]
        [String]$SecondNICIPs = "10.35.0.4,10.35.0.5,10.35.0.6", #leave this empty if you don't have 2nd NIC
        [parameter(Mandatory=$false)]
        [String]$PrivateVMExtConfig = ""  #leave this empty if your VM extension script is publicly accessible
)

#poo$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest 

$OSName="OpenLogic 6.5" # this script is only tested on this OS
$LoadBalancerName=$VMNamePrefix + "ilb"
$LoadBalancedSetName=$VMNamePrefix + "lbset"
$AvailabilitySetName=$VMNamePrefix + "set"
$MySQLPort=3306
$MySQLProbePort=9200
$ExtensionName="CustomScriptForLinux"
$ExtensionPublisher="Microsoft.OSTCExtensions"
$ExtensionVersion="1.1"

$CloudService=$null
$VNetLocation=$null
$NodeIPs=$null
$SecondIPs=$null

function checkSubnet ([string]$cidr, [string]$ip)
{
    $network, [int]$subnetlen = $cidr.Split('/')
    $a = [uint32[]]$network.split('.')
    [uint32] $unetwork = ($a[0] -shl 24) + ($a[1] -shl 16) + ($a[2] -shl 8) + $a[3]

    $mask = (-bnot [uint32]0) -shl (32 - $subnetlen)

    $a = [uint32[]]$ip.split('.')
    [uint32] $uip = ($a[0] -shl 24) + ($a[1] -shl 16) + ($a[2] -shl 8) + $a[3]

    return ($unetwork -eq ($mask -band $uip))
}

function validateInput
{
    # check VNet and subnet exist
    $VNet = get-azurevnetsite | where-object {$_.Name -eq $VNetName}
    if (!$VNet) 
    {
	    Write-Error ("You must create the VNet {0} and its subnet(s) for the cluster VMs." -f $VNetName)
	    Exit
    }
    $DBSubnetObj = $VNet.Subnets | where-object {$_.Name -eq $DBSubnet}
    if (!$DBSubnetObj) 
    {
	    Write-Error ("You must create the subnet {0} in {1} for the cluster nodes." -f $DBSubnet, $VNetName)
	    Exit
    }
    # check storage account is in the same region as the VNet where nodes will be located
    $StorageAccount = Get-AzureStorageAccount -StorageAccountName $StorageAccountName
    $StorageLocation = $StorageAccount | Select -ExpandProperty "Location"
    [xml]$VNetConfig = (Get-AzureVNetConfig).XMLConfiguration
    $script:VNetLocation = ($VNetConfig.NetworkConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites.VirtualNetworkSite | where {$_.name -eq $VNetName}).Location
    # this only works with Azure SDk 0.8.12
    # $script:VNetLocation = $VNet.Location 
    if ($StorageLocation -ne $script:VNetLocation) 
    {
	    Write-Error ("Storage account in {0} should be in the same region as the VNet {1}." -f $StorageLocation, $script:VNetLocation)
	    Exit
    }
    # check if storage account has global replication disabled
    if ($StorageAccount | where-object {$_.AccountType -ne "Standard_LRS"}) 
    {
	    Write-Error ("Storage account {0} should be configured to use local instead of geo replication." -f $StorageAccountName)
        Exit
    }
    # check if cloud service already exists in the same region or create one
    $script:CloudService = Get-AzureService -ServiceName  $ServiceName -ErrorAction silentlycontinue
    if (!$?)
    {
        if (Test-AzureName -Service $ServiceName) 
        {
    	    Write-Error ("Cloud Service name {0} is not available." -f $ServiceName)	
	        Exit
        }
    }
    elseif ($script:CloudService.Location -ne $script:VNetLocation)
    {
	    Write-Error ("Cloud Service {0} exists in {1}, not in VNet's region {2}."  `
	                -f $ServiceName, $script:CloudService.Location, $script:VNetLocation)	
	    Exit
    }
    # check if load balancer IP is available in DBSubnet
    if (!(checkSubnet $DBSubnetObj.AddressPrefix $LoadBalancerIP) -or 
       !((Test-AzureStaticVNetIP -VNetName $VNetName -IPAddress $LoadBalancerIP).IsAvailable))
    {
	    Write-Error ("Load Balancer IP {0} is not available in subnet {1}." -f $LoadBalancerIP, $DBSubnet)	
	    #pooExit
    }
    # check if DBNodeIPs are available in DBSubnet
    $script:NodeIPs = $DBNodeIPs.Split(',')
    if ($script:NodeIPs.Count -lt 3)
    {
	    Write-Error ("Need at least 3 nodes in DBNodeIPs.")	
	    Exit
    }
    foreach ($NodeIP in $script:NodeIPs)
    {
        $NodeIP = $NodeIP.trim(' ')
	    if (!(checkSubnet $DBSubnetObj.AddressPrefix $NodeIP) -or 
		    !((Test-AzureStaticVNetIP -VNetName $VNetName -IPAddress $NodeIP).IsAvailable))
	    {
		    Write-Error ("Node IP {0} is not available in subnet {1}." -f $NodeIP, $DBSubnet)	
		    #pooExit
	    }
    }
    # check VMs with the same name don't already exist in the same cloud service
    for ($i=1; $i -le $script:NodeIPs.Count; $i++) 
    {
        $vmName = $VMNamePrefix + $i
        if ($vmName.Length > 15)
        {
		    Write-Error ("Node Name {0} must not be longer than 15 characters." -f $vmName)	
		    Exit
        }
        $vmObj = Get-AzureVM -ServiceName $ServiceName -Name $vmName -ErrorAction SilentlyContinue
        if ($vmObj)
        {
		    Write-Error ("VM {0} already exist." -f $vmName)	
		    #pooExit
        }
    }

    # check if 2nd NIC is needed
    if ($SecondNICName -ne "") 
    {
        if ($AzureVersion.Minor -eq 8 -and $AzureVersion.Build -lt 12)
        {
	        Write-Error ("Multiple NIC is not supported in this version of Azure SDK. Clear SecondNICName or upgrade to 0.8.12")
	        Exit
        }
        # check if SecondNICSubnet exits
        $SecondNICSubnetObj = $VNet.Subnets | where-object {$_.Name -eq $SecondNICSubnet}
        if (!$SecondNICSubnetObj) 
        {
	        Write-Error ("You must create the subnet {0} in {1} for the 2nd NIC of cluster nodes." -f $SecondNICSubnet, $VNetName)
	        Exit
        }
        # check if SecondNICIPs are avilable in SecondNICSubnet
        $script:SecondIPs = $SecondNICIPs.Split(',')
        if ($script:SecondIPs.Count -ne $script:NodeIPs.Count)
        {
	        Write-Error ("Must specify the same number of nodes in SecondNICIPs and DBNodeIPs.")	
	        Exit
        }
        foreach ($NodeIP in $script:SecondIPs)
        {
            $NodeIP = $NodeIP.trim(' ')
	        if (!(checkSubnet $SecondNICSubnetObj.AddressPrefix $NodeIP) -or 
		        !((Test-AzureStaticVNetIP -VNetName $VNetName -IPAddress $NodeIP).IsAvailable))
	        {
		        Write-Error ("Node IP {0} is not available in subnet {1}." -f $NodeIP, $SecondNICSubnet)	
		        Exit
	        }
        }
    }
    Write-Verbose -Verbose ("Passed input validation.")
}

function deployCluster
{
    #create cloud service if it doesn't exist
    if (!$script:CloudService)
    {
        New-AzureService -ServiceName $ServiceName -Location $script:VNetLocation
        sleep 1
        $svc = Get-AzureService -ServiceName $ServiceName
        while ($svc.Status -ne "Created")
        {
            sleep 1
            $svc = Get-AzureService -ServiceName $ServiceName
        }
    }

    #create internal load balancer in the cloud service
    $ilbConfig = New-AzureInternalLoadBalancerConfig -InternalLoadBalancerName $LoadBalancerName -StaticVNetIPAddress $LoadBalancerIP -SubnetName $DBSubnet

    #create VMs
    $imageName=@(Get-AzureVMImage |where-object {$_.Label -like $OSName}).ImageName
    for ($i=0; $i -lt $script:NodeIPs.Count; $i++)
    {
        $vmName = $VMNamePrefix + ($i + 1)
        $vmIP = $script:NodeIPs[$i].trim(' ')
        $vm = New-AzureVMConfig -Name $vmName -InstanceSize $VMSize -ImageName $imageName -AvailabilitySetName $AvailabilitySetName

        Add-AzureProvisioningConfig -VM $vm -Linux -LinuxUser $VMUser -Password $VMPassword
        Set-AzureSubnet -SubnetNames $DBSubnet -VM $vm
        Set-AzureStaticVNetIP -IPAddress $vmIP -VM $vm
        Add-AzureEndpoint -LBSetName $LoadBalancedSetName -Name "MySQL" -Protocol "tcp" -LocalPort $MySQLPort -PublicPort $MySQLPort -ProbePort $MySQLProbePort -ProbeProtocol http -ProbePath '/' -InternalLoadBalancerName $LoadBalancerName -VM $vm
        if ($SecondNICName -and $SecondNICName -ne "") 
        {
            $vmIP2 = $script:SecondIPs[$i].trim(' ')
            Add-AzureNetworkInterfaceConfig -Name $SecondNICName -SubnetName $SecondNICSubnet -StaticVNetIPAddress $vmIP2 -VM $vm
            $extParams = $SecondNICIPs + ' ' + $vmIP2 + ' '
        }
        else
        {
            $extParams = $DBNodeIPs + ' ' + $vmIP + ' '
        }
        for ($j=0; $j -lt $NumOfDisks; $j++)
        {
            $diskLabel = $vmName + "-data" + ($j+1)
            Add-AzureDataDisk -CreateNew -DiskSizeInGB $DiskSizeInGB -DiskLabel $diskLabel -LUN $j -VM $vm
        }
        if ($i -eq 0)
        {
            $extParams += "bootstrap-pxc "
        }
        else
        {
            $extParams += "start "
        }
        $extParams += $SECONDNICName
        $PublicVMExtConfig = '{"fileUris":["' + $VMExtLocation +'"], "commandToExecute": "bash azurepxc.sh ' + $extParams + '" }' 
        if ($PrivateVMExtConfig -and $PrivateVMExtConfig -ne "")
        {
            Set-AzureVMExtension -ExtensionName $ExtensionName -Publisher $ExtensionPublisher -Version $ExtensionVersion -PublicConfiguration $PublicVMExtConfig -PrivateConfiguration $PrivateVMExtConfig -VM $vm -Verbose
        }
        else
        {
            Set-AzureVMExtension -ExtensionName $ExtensionName -Publisher $ExtensionPublisher -Version $ExtensionVersion -PublicConfiguration $PublicVMExtConfig -VM $vm -Verbose
        }
        Write-Verbose -Verbose ("Creating VM {0}" -f $vmName)
        New-AzureVM -ServiceName $ServiceName -VNetName $VNetName -VM $vm -InternalLoadBalancerConfig $ilbConfig
        sleep 5
        $vmCreated = Get-AzureVM -ServiceName $ServiceName -Name $vmName
        Write-Verbose -Verbose ("VM {0} is {1}" -f $vmName, $vmCreated.PowerState)
        while (($vmCreated.PowerState -ne "Started") -or ($vmCreated.InstanceStatus -ne "ReadyRole"))
        {
            sleep 5
            $vmCreated = Get-AzureVM -ServiceName $ServiceName -Name $vmName
        }
        Write-Verbose -Verbose ("VM {0} {1}" -f $vmName, $vmCreated.PowerState)
    }

    Write-Verbose -Verbose ("Done deploying cluster")
}

$AzureVersion = (Get-Module -Name Azure).Version
Select-AzureSubscription -SubscriptionName $SubscriptionName
Set-AzureSubscription -SubscriptionName $SubscriptionName -CurrentStorageAccountName $StorageAccountName
validateInput
deployCluster


