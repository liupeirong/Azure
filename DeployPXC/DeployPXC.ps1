#
# DeployPXC.ps1
#

# the following resources must already exist
$SubscirptionName="pliu@ted"
$StorageAccountName="portalvhds57np6vhvvd1mq"
$VNetName="peivm"
$DBSubnet="Subnet-1" #this is the subnet where the nodes will expose their MySQL endpoint (3306) to clients
$EnableDualNIC=0 #will attach 2nd NIC for inter-cluster communication if set to non-zero
$2ndNICName="" #something like "eth1" if EnableDualNIC is non-zero
$ClusterSubnet="" #this is the subnet that the nodes communicate state with each other if EnableDualNIC is not 0
# the following resources will be created
$LoadBalancerIP="10.11.0.5" #static IP, make sure it's available in DBSubnet
$NodeDBIPs=@("10.11.0.6", "10.11.0.7", "10.11.0.8") #static IP, make sure they are available in DBSubnet
$NodeClusterIPs=@() #static IP, make sure they are available in ClusterSubnet if EnableDualNIC=1
$CloudServiceName="contosopxc"
$VMSize="Large"
$VMNamePrefix="azpxc"
$EnableRaid0=1 #will stripe the attached disks if set to non-zero and $NumberOfDisks > 1
$NumberOfDisks=1
$DataDiskSize=128
$OSName="OpenLogic 6.5" # the script is only tested on this OS, if you change it, you may need to change some parts of the script
$MyCnfTemplate="c:\temp\my.conf" # this is the template you want to use for my.conf, leave Node names and IPs empty
# you can leave these parameters as default
$DataDriveName="/datadrive" #this is the folder name of mounted disk
$LoadBalancerName="pxcilb"
$AvailabilitySetName="pxcset"

# internal variables
$ExitError=1
$ExitSuccess=0

# validate the configuration as much as possbile before we create anything
Select-AzureSubscription -SubscriptionName $SubscirptionName
Set-AzureSubscription -SubscriptionName $SubscirptionName -CurrentStorageAccountName $StorageAccountName
# check VNet and subnet exist
$VNet = get-azurevnetsite | where-object {$_.Name -eq $VNetName}
if (!$VNet) {
	Write-Host ("You must create the VNet {0} and its subnet(s) for the cluster VMs." -f $VNetName) -ForegroundColor  Red
	Exit $ExitError
}
$DBSubnetObj = $VNet.Subnets | where-object {$_.Name -eq $DBSubnet}
if (!$DBSubnetObj) {
	Write-Host ("You must create the subnet {0} for {1} for the cluster VMs." -f $DBSubnet, $VNetName) -ForegroundColor  Red
	Exit $ExitError
}
# check storage account is in the same region as the VNet where nodes will be located
$StorageLocation = Get-AzureStorageAccount -StorageAccountName "portalvhds57np6vhvvd1mq" | Select -ExpandProperty "Location"
$VNetLocation = $VNet.Location
if ($StorageLocation -ne $VNetLocation) {
	Write-Host "Storage account should be in the same region as the cluster VMs." -ForegroundColor  Red
	Exit $ExitError
}
# check if cloud service already exists in the same region or create one
$CloudService = Get-AzureService -ServiceName  $CloudServiceName -ErrorAction silentlycontinue
if (!$?) {
	New-AzureService -ServiceName  $CloudServiceName -Location $VNetLocation
} elseif ($CloudService.Location -ne $VNetLocation) {
	Write-Host ("Cloud Service {0} exists in {1}, not in VNet's region {2}."  `
	            -f $CloudServiceName, $CloudService.Location, $VNetLocation) -ForegroundColor  Red	
	Exit $ExitError
}
# check if load balancer IP is available in DBSubnet
if (!(checkSubnet $DBSubnetObj.AddressPrefix $LoadBalancerIP) -or 
   !((Test-AzureStaticVNetIP -VNetName $VNetName -IPAddress $LoadBalancerIP).IsAvailable))
{
	Write-Host ("Load Balancer IP {0} is not available in subnet {1}." -f $LoadBalancerIP, $DBSubnet) -ForegroundColor  Red	
	Exit $ExitError
}
# check if NodeDBIPs are available in DBSubnet
foreach ($NodeIP in $NodeDBIPs)
{
	if (!(checkSubnet $DBSubnetObj.AddressPrefix $NodeIP) -or 
		!((Test-AzureStaticVNetIP -VNetName $VNetName -IPAddress $NodeIP).IsAvailable))
	{
		Write-Host ("Node IP {0} is not available in subnet {1}." -f $NodeIP, $DBSubnet) -ForegroundColor  Red	
		Exit $ExitError
	}
}
# check if EnableRaid0 is non-zero, then NumberOfDisks > 1
if ($EnableRaid0 -and $NumberOfDisks -le 1)
{
	Write-Host ("Raid is enabled but you specified only one disk to be attached.") -ForegroundColor  Red	
	Exit $ExitError
}
# TODO: if EnableDualNIC is non-zero, check if 2ndNICName is non-zero and NodeClusterIPs are available in DBSubnet

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


