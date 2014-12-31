#
# DeployPXC.ps1
#
param (
        [parameter(Mandatory=$true)]
        [String]$CredentialName = "pliu@microsoft.com",
        [parameter(Mandatory=$true)]
        [String]$SubscriptionName = "pliu@ted",
        [parameter(Mandatory=$true)]
        [String]$StoragenAccountName = "pliueuro",
        [parameter(Mandatory=$true)]
        [String]$ServiceName = "azurepxc",
        [parameter(Mandatory=$true)]
        [String]$VNetName = "peieuro",
        [parameter(Mandatory=$true)]
        [String]$DBSubnet = "dbsubnet",
        [parameter(Mandatory=$true)]
        [String]$2ndNICName = "",  #this indicates the need for 2nd NIC. leave this empty if you don't have 2nd NIC
        [parameter(Mandatory=$true)]
        [String]$ClusterSubnet = "", #leave this empty if you don't need 2nd NIC
        [parameter(Mandatory=$true)]
        [String[]]$NodeDBIPs = @("10.11.0.4", "10.11.0.5", "10.11.0.6"),
        [parameter(Mandatory=$true)]
        [String[]]$NodeClusterIPs = @(), #leave this empty if you don't have 2nd NIC
        [parameter(Mandatory=$true)]
        [String]$LoadBalancerIP = "10.11.0.8",
        [parameter(Mandatory=$true)]
        [String]$VMSize = "Standard_A3",
        [parameter(Mandatory=$true)]
        [Int]$NumOfDisks = 4, #if greater than 1, then the disks will be provisioned as raid0
        [parameter(Mandatory=$true)]
        [Int]$DiskSizeInGB = 10,
        [parameter(Mandatory=$true)]
        [String]$VMNamePrefix = "azpxc",
        [parameter(Mandatory=$true)]
        [String]$DataDriveName = "/datadrive",
        [parameter(Mandatory=$true)]
        [String]$ConfigTemplate = "c:\temp\my.cnf"
)

Set-StrictMode -Version Latest 

$OSName="OpenLogic 6.5" # this script is only tested on this OS
$LoadBalancerName=$VMNamePrefix + "ilb"
$AvailabilitySetName=$VMNamePrefix + "set"

Select-AzureSubscription -SubscriptionName $SubscirptionName
Set-AzureSubscription -SubscriptionName $SubscirptionName -CurrentStorageAccountName $StorageAccountName

# check VNet and subnet exist
$VNet = get-azurevnetsite | where-object {$_.Name -eq $VNetName}
if (!$VNet) {
	Write-Output ("You must create the VNet {0} and its subnet(s) for the cluster VMs." -f $VNetName)
	Exit
}
$DBSubnetObj = $VNet.Subnets | where-object {$_.Name -eq $DBSubnet}
if (!$DBSubnetObj) {
	Write-Output ("You must create the subnet {0} in {1} for the cluster nodes." -f $DBSubnet, $VNetName)
	Exit
}
# check storage account is in the same region as the VNet where nodes will be located
$StorageAccount = Get-AzureStorageAccount -StorageAccountName $StoragenAccountName
$StorageLocation = $StorageAccount | Select -ExpandProperty "Location"
$VNetLocation = $VNet.Location
if ($StorageLocation -ne $VNetLocation) {
	Write-Output ("Storage account in {0} should be in the same region as the VNet {1}." -f $StorageLocation, $VNetLocation)
	Exit
}
# check if storage account has global replication disabled
if ($StorageAccount | where-object {$_.AccountType -ne "Standard_LRS"}) {
	Write-Output ("Storage account {0} should be configured to use local instead of geo replication." -f $StorageAccountName)
    Exit
}
# check if cloud service already exists in the same region or create one
$CloudService = Get-AzureService -ServiceName  $CloudServiceName -ErrorAction silentlycontinue
if ($? -and ($CloudService.Location -ne $VNetLocation)) {
	Write-Output ("Cloud Service {0} exists in {1}, not in VNet's region {2}."  `
	            -f $CloudServiceName, $CloudService.Location, $VNetLocation)	
	Exit
}
# check if load balancer IP is available in DBSubnet
if (!(checkSubnet $DBSubnetObj.AddressPrefix $LoadBalancerIP) -or 
   !((Test-AzureStaticVNetIP -VNetName $VNetName -IPAddress $LoadBalancerIP).IsAvailable))
{
	Write-Output ("Load Balancer IP {0} is not available in subnet {1}." -f $LoadBalancerIP, $DBSubnet)	
	Exit
}
# check if NodeDBIPs are available in DBSubnet
if ($NodeDBIPs.Count -lt 3)
{
	Write-Output ("Need at least 3 nodes in NodeDBIPs.")	
	Exit
}
foreach ($NodeIP in $NodeDBIPs)
{
	if (!(checkSubnet $DBSubnetObj.AddressPrefix $NodeIP) -or 
		!((Test-AzureStaticVNetIP -VNetName $VNetName -IPAddress $NodeIP).IsAvailable))
	{
		Write-Output ("Node IP {0} is not available in subnet {1}." -f $NodeIP, $DBSubnet)	
		Exit
	}
}
# check if 2nd NIC is needed
if ($2ndNICName -ne "") {
    # check if ClusterSubnet exits
    $ClusterSubnetObj = $VNet.Subnets | where-object {$_.Name -eq $ClusterSubnet}
    if (!$ClusterSubnetObj) {
	    Write-Output ("You must create the subnet {0} in {1} for the 2nd NIC of cluster nodes." -f $ClusterSubnet, $VNetName)
	    Exit
    }
    # check if ClusterNodeIPs are avilable in ClusterSubnet
    if ($NodeClusterIPs.Count -lt 3)
    {
	    Write-Output ("Need at least 3 nodes in NodeClusterIPs.")	
	    Exit
    }
    foreach ($NodeIP in $NodeClusterIPs)
    {
	    if (!(checkSubnet $ClusterSubnetObj.AddressPrefix $NodeIP) -or 
		    !((Test-AzureStaticVNetIP -VNetName $VNetName -IPAddress $NodeIP).IsAvailable))
	    {
		    Write-Output ("Node IP {0} is not available in subnet {1}." -f $NodeIP, $ClusterSubnet)	
		    Exit
	    }
    }
}

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
