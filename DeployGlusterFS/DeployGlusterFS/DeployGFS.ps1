param (
	[parameter(Mandatory=$false)]
	[String]$UserName = "azureuser",
	[parameter(Mandatory=$false)]
	[String]$Password = "password",
	[parameter(Mandatory=$false)]
	[String]$SubscriptionId = "12345...",
	[parameter(Mandatory=$false)]
	[String]$Location = "East US",
	[parameter(Mandatory=$false)]
	[String]$GlusterVolumeName = "gfsvol",
	[parameter(Mandatory=$false)]
	[String]$ServiceName = "randomservicename",
	[parameter(Mandatory=$false)]
	[String]$StorageAccountName1 = "storageaccount1",
	[parameter(Mandatory=$false)]
	[String]$StorageAccountName2 = "storageaccount2",
	[parameter(Mandatory=$false)]
	[String]$VNetName = "vnet",
	[parameter(Mandatory=$false)]
	[String]$SubnetName = "Subnet-1",
	[parameter(Mandatory=$false)]
	[String]$AvailabilitySet = "glusterset",
	[parameter(Mandatory=$false)]
	[String]$VMName1 = "gluster1",
	[parameter(Mandatory=$false)]
	[String]$VMName2 = "gluster2",
	[parameter(Mandatory=$false)]
	[String]$InstanceSize = "Standard_D2",
	[parameter(Mandatory=$false)]
	[String]$VMip1 = "10.0.0.4",
	[parameter(Mandatory=$false)]
	[String]$VMip2 = "10.0.0.5",
	[parameter(Mandatory=$false)]
	[Int]$NumOfDisks = 2,
	[parameter(Mandatory=$false)]
	[Int]$DiskSizeInGB = 1000,
	[parameter(Mandatory=$false)]
    [String]$VMExtLocation = "https://raw.githubusercontent.com/liupeirong/Azure/master/DeployGlusterFS/DeployGlusterFS/azuregfs.sh", 
	[parameter(Mandatory=$false)]
    [String]$PrivateVMExtConfig 
)        

$OSName = "OpenLogic 6.5"
$ExtensionName="CustomScriptForLinux"
$ExtensionPublisher="Microsoft.OSTCExtensions"
$ExtensionVersion="1.1"

function CreateVM($vmName, $vmIP, $peerNodeName, $peerNodeIP, $storageAccount, $isLastNode)
{
	$vm = Get-AzureVM -ServiceName $ServiceName -Name $vmName
	if ($vm) 
	{
		Write-Verbose -Verbose ("vm {0} already exists, not creating" -f $vmName)
		return
	}
	Set-AzureSubscription -SubscriptionId $SubscriptionId -CurrentStorageAccountName $storageAccount
	$vm = New-AzureVMConfig -Name $vmName -InstanceSize $InstanceSize -Image $imageName -AvailabilitySetName $AvailabilitySet
	Add-AzureProvisioningConfig –VM $vm -Linux -LinuxUser $UserName -Password $Password
	Set-AzureSubnet -SubnetNames $SubnetName -VM $vm
	Set-AzureStaticVNetIP -IPAddress $vmIP -VM $vm
	#Add-AzureNetworkInterfaceConfig -Name eth1 -SubnetName "Subnet-2" -StaticVNetIPAddress 10.11.111.29 -VM $vm
	for ($j=0; $j -lt $NumOfDisks; $j++)
	{
		$diskLabel = $vmName + "-data" + ($j+1)
		Add-AzureDataDisk -CreateNew -DiskSizeInGB $DiskSizeInGB -DiskLabel $diskLabel -LUN $j -VM $vm
	}
	$extParams = $peerNodeName + ' ' + $peerNodeIP + ' ' + $GlusterVolumeName + ' ' + $isLastNode
	$PublicVMExtConfig = '{"fileUris":["' + $VMExtLocation +'"], "commandToExecute": "bash azuregfs.sh ' + $extParams + '" }' 
	if ($PrivateVMExtConfig -and $PrivateVMExtConfig -ne "")
	{
		Set-AzureVMExtension -ExtensionName $ExtensionName -Publisher $ExtensionPublisher -Version $ExtensionVersion -PublicConfiguration $PublicVMExtConfig -PrivateConfiguration $PrivateVMExtConfig -VM $vm -Verbose
	}
	else
	{
		Set-AzureVMExtension -ExtensionName $ExtensionName -Publisher $ExtensionPublisher -Version $ExtensionVersion -PublicConfiguration $PublicVMExtConfig -VM $vm -Verbose
	}
	New-AzureVM -ServiceName $ServiceName –VNetName $VNetName –VM $vm
}

Select-AzureSubscription -SubscriptionId $SubscriptionId -Current
$cs = Get-AzureService -ServiceName $ServiceName
if (!$cs)
{ 
	New-AzureService -ServiceName $ServiceName -Location $Location
}
if ($InstanceSize.StartsWith("Standard_DS", "CurrentCultureIgnoreCase")) {
    $storageType = "Premium_LRS"
}
else {
    $storageType = "Standard_LRS"
}
$premiumStore1 = Get-AzureStorageAccount -StorageAccountName $StorageAccountName1
if (!$premiumStore1)
{
	New-AzureStorageAccount -StorageAccountName $StorageAccountName1 -Type $storageType -Location $Location 
}
$premiumStore2 = Get-AzureStorageAccount -StorageAccountName $StorageAccountName2
if (!$premiumStore2)
{
	New-AzureStorageAccount -StorageAccountName $StorageAccountName2 -Type $storageType -Location $Location 
}

$images = @(Get-AzureVMImage |where-object {$_.Label -like $OSName})
$imageName = $images[$images.Length - 1].ImageName
CreateVM $VMName1 $VMip1 $VMName2 $VMip2 $StorageAccountName1
CreateVM $VMName2 $VMip2 $VMName1 $VMip1 $StorageAccountName2 y
