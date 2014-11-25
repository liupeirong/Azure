#
# DeployPXC.ps1
#

# the following resources must already exist
$SubscirptionName="pliu@ted"
$StorageAccountName="portalvhds57np6vhvvd1mq"
$VNetName="peivm"
$SubnetName="pxcsubnet"
# the following resources will be created
$CloudServiceName="contosopxc"
$VMSize="Large"
$VMNamePrefix="azpxc"
$DataDiskSize=128
# the script is only tested on the following OS, if you change it, you may need to change some parts of the script
$OSName="CentOS 6.5"

Select-AzureSubscription -SubscriptionName $SubscirptionName
Set-AzureSubscription -SubscriptionName $SubscirptionName -CurrentStorageAccountName $StorageAccountName
$VNet = get-azurevnetsite | where-object {$_.Name -eq $VNetName}
if (!$VNet) {
	Write-Host "You must create a VNet and its subnet for the cluster VMs." -ForegroundColor  Red
	Exit
}
$StorageLocation = Get-AzureStorageAccount -StorageAccountName "portalvhds57np6vhvvd1mq" | Select -ExpandProperty "Location"
$VNetLocation = $VNet.Location
if ($StorageLocation -ne $VNetLocation) {
	Write-Host "Storage account should be in the same region as the cluster VMs." -ForegroundColor  Red
	Exit
}

# check if cloud service already exists
$CloudService = Get-AzureService -ServiceName  $CloudServiceName -ErrorAction silentlycontinue
if (!$?) {
	New-AzureService -ServiceName  $CloudServiceName -Location $VNetLocation
} elseif ($CloudService.Location -ne $VNetLocation) {
	Write-Host "Cloud Service already exists but is in a different region from VNet." -ForegroundColor  Red	
	Exit
}

