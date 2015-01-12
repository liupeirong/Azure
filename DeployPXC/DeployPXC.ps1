<#
.SYNOPSIS
    This script/runbook provides a way to deploy a MySQL Percona XtraDB Cluster (PXC) in the specified Azure environment. 
	It provisions network, storage and compute resources for the cluster, and leverages Azure Linux custom script
	VM extension to run a bash script to install and configure MySQL on each node.  It also optionally stripes the disks and 
    configures a second NIC for each node. 
    
    After the cluster is deployed, access the internal load balancer with the user 'test' and the password specified in 
    my.cnf template by default. For example, "mysql -h 10.0.0.7 -u test --password=s3cret". Then inside the MySQL environment, 
    run "show status like 'wsrep%';" to make sure wsrep_cluster_size shows the number of
    cluster nodes specified, and wsrep_ready is "ON". 
    
    You should customize MySQL configuration file to your needs, especially the account for SST, checkcluster, and the account that
    can access the database from the load balancer or other IPs.  See the CUSTOMIZATION section below for details.
	
.SUPPORTED ENVIRONMENT
    Percona XtraDB Cluster 5.6
	CentOS 6.5
	
.DEPLOYMENT SPECIFICATION
    Network
	    You must create an Azure virtual network and subnet that the cluster should be deployed to.  The virtual network, 
		the storage account, and the cloud service must belong to the same region.  You can also specify the use of a second NIC 
		for the cluster nodes.  If you use a second NIC, you must create the subnet for the second NIC.  The IP addresses 
		you specify for the cluster nodes must be available in their respective subnet. Only PowerShell SDK versions 0.8.12 
		and above support the creation of a second NIC.
		
		You also need to specify the IP address of an Azure internal load balancer for the cluster, so that your application
		can access MySQL using the load balancer instead of individual nodes.  The load balancer IP should be available in 
		the virtual network.
		
	Storage
	    The storage account you specify will host the disks of the cluster nodes.  Therefore it must be in the same region
		as the virtual network to ensure good performance.  You may specify the number of disks and disk size to be attached
		to each cluster node.  If the number of disks is greater than 1, the disks will be stripped into a raid0 array.  
		
	Virtual Machines
	    You may specify the number of nodes to be deployed to the cluster, the size of each node, and the prefix of the node's
		host name.  A number, starting from 1, will be attached to the prefix to form the full host name, for example if the prefix
		is azpxc, then the node names will be azpxc1, azpxc2 and so on.
		
.CUSTOMIZATION
    In addition to providing the parameters for this script/runbook, you can customize MySQL my.cnf file to your needs. You can 
	leave cluster address, node address and node name empty in your template as these will be replaced by the VM extension script, 
	everything else can be customized.  By default, the VM extension script is located at 
	https://raw.githubusercontent.com/liupeirong/Azure/master/DeployPXC/azurepxc.sh, and the my.cnf template is located at 
	https://raw.githubusercontent.com/liupeirong/Azure/master/DeployPXC/my.cnf.template.  You can copy them to another location,
	provide your own customization, and then specify their location in the parameters of this script/runbook. 

.EXAMPLE
    To deploy a 3 node cluster without disk striping or 2nd NIC: 

	.\DeployPXC.ps1 -SubscriptionName "mysubscription" -StorageAccountName "mystorage" -ServiceName "myservice" -VNetName "myvnet" `
	                -DBSubnet "dbsubnet" -DBNodeIPs "10.0.0.1,10.0.0.2,10.0.0.3" -LoadBalancerIP "10.0.0.4" `
					-VMSize "Large" -NumOfDisks 1 -DiskSizeInGB 10 -VMNamePrefix "azpxc" -VMUser "azureuser" -VMPassword "s3cret#" 
					
	To deploy a 3 node cluster with disk striping and 2nd NIC:

    .\DeployPXC.ps1 -SubscriptionName "mysubscription" -StorageAccountName "mystorage" -ServiceName "myservice" -VNetName "myvnet" `
	                -DBSubnet "dbsubnet" -DBNodeIPs "10.0.0.1,10.0.0.2,10.0.0.3" -LoadBalancerIP "10.0.0.4" `
					-VMSize "Large" -NumOfDisks 4 -DiskSizeInGB 10 -VMNamePrefix "azpxc" -VMUser "azureuser" -VMPassword "s3cret#" 
				    -SecondNICName "eth1" -SecondNICSubnet "clustersubnet" -SecondNICIPs "10.1.0.4,10.1.0.5,10.1.0.6"
#>

param (
        [parameter(Mandatory=$false)]
        [String]$CredentialName, #only used with Azure automation, leave empty in standalone Powershell script
        [parameter(Mandatory=$true)]
        [String]$SubscriptionName,
        [parameter(Mandatory=$true)]
        [String]$StorageAccountName,
        [parameter(Mandatory=$true)]
        [String]$ServiceName, #Azure cloud service that the cluster nodes will be deployed to, will create if not already exist
        [parameter(Mandatory=$true)]
        [String]$VNetName, #Azure vnet that the cluster nodes will be deployed to, must already exist
        [parameter(Mandatory=$true)]
        [String]$DBSubnet, #the subnet inside the vnet that the primary NIC of the cluster nodes belong to
        [parameter(Mandatory=$true)]
        [String]$DBNodeIPs, #the IPs of the primary NIC of the cluster nodes, comma separated, ex: "10.0.0.1,10.0.0.2,10.0.0.3"
        [parameter(Mandatory=$true)]
        [String]$LoadBalancerIP, #the IP of the load balancer for the cluster nodes, must be in the VNet
        [parameter(Mandatory=$true)]
        [String]$VMSize = "Large", #Azure VM size for the cluster nodes
        [parameter(Mandatory=$true)]
        [Int]$NumOfDisks, #data disks attached to each cluster node, if greater than 1, then the disks will be provisioned as raid0
        [parameter(Mandatory=$true)]
        [Int]$DiskSizeInGB, #size of each data disk attached to cluster node
        [parameter(Mandatory=$true)]
        [String]$VMNamePrefix, #the prefix of the host name for each cluster node, a number will be appended to the prefix to distiguish each node, for example,azpxc1,azpxc2...
        [parameter(Mandatory=$true)]
        [String]$VMUser, #the user name that can be used to ssh into the cluster node
        [parameter(Mandatory=$true)]
        [String]$VMPassword, #the password for the ssh user
        [parameter(Mandatory=$false)]
        [String]$VMExtLocation = "https://raw.githubusercontent.com/liupeirong/Azure/master/DeployPXC/azurepxc.sh", #the location of the VM extension script that will run as part of the cluster node creation
        [parameter(Mandatory=$false)]
        [String]$MyCnfLocation = "https://raw.githubusercontent.com/liupeirong/Azure/master/DeployPXC/my.cnf.template", #the location of MySQL my.cnf template, IP and hostname will be substituted by the script, you configure everything else to your liking
        [parameter(Mandatory=$false)]
        [String]$SecondNICName,  #this indicates the need for 2nd NIC. leave this empty if you don't have 2nd NIC, all other 2nd NIC settings will have no effect if this is empty
        [parameter(Mandatory=$false)]
        [String]$SecondNICSubnet, #the subnet inside the vnet that the second NIC of the cluster nodes belong to
        [parameter(Mandatory=$false)]
        [String]$SecondNICIPs, #the IPs of the second NIC of the cluster nodes, comma separated, ex: "10.1.0.1,10.1.0.2,10.1.0.3"
        [parameter(Mandatory=$false)]
        [String]$PrivateVMExtConfig  #leave this empty if your VM extension script is publicly accessible
)

$ErrorActionPreference = "Stop"
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
    if (!((Test-AzureStaticVNetIP -VNetName $VNetName -IPAddress $LoadBalancerIP).IsAvailable))
    {
	    Write-Error ("Load Balancer IP {0} is not available in VNet {1}." -f $LoadBalancerIP, $VNetName)	
	    Exit
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
		    Exit
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
		    Exit
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
        $extParams += $MyCnfLocation + " "
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
	Write-Verbose -Verbose ("Run this command in a node to verify the cluster is working, user is 'test' and password is 's3cret' by default, or use your customized user:")
	Write-Verbose -Verbose ("    mysql -h {0} -u test -p", $LoadBalancerIP)
	Write-Verbose -Verbose ("then in MySQL, run:")
	Write-Verbose -Verbose ("    show status like 'wsrep%';")
	Write-Verbose -Verbose ("verify wsrep_cluster_size is {0}, and wsrep_ready is ON", $script:NodeIPs.Count)
}

$AzureVersion = (Get-Module -Name Azure).Version
Select-AzureSubscription -SubscriptionName $SubscriptionName
Set-AzureSubscription -SubscriptionName $SubscriptionName -CurrentStorageAccountName $StorageAccountName
validateInput
deployCluster


