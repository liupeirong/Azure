# Automatically deploy MySQL Percona XtraDB Cluster in Azure

This PowerShell script and the corresponding Azure automation runbook deploys a fully customizable MySQL Percona XtraDB Cluster (PXC) in the Microsoft Azure cloud.  It provides the following capabilities:

  - supports provisioning of PXC 5.6 on Cent OS 6.5.  Ubuntu support is coming soon.  If you use a different Linux flavor/version, chances are you can make some small modifications to the custom script that runs at VM provision time to work for you
  - automatically provision PXC nodes with specified VM size, region, virtual network, IP and hostname in Azure
  - automatically add data disks to the VM for MySQL data folder.  If multiple data disks are specified, the script will automatically create a raid 0 volume
  - automatically provision Azure internal load balancer for the cluster nodes with a probe that checks the health of each node
  - optionally provision a second NIC for each node so that the intra-cluster traffic can be separated from application traffic
  - automatically place the cluster nodes in an availability set to ensure high availability
  - you can fully customize MySQL configuration file my.cnf, and just leave IP addresses and host names for the script to automatically fill
### Usage
You can either run DeployPXC.ps1 as a standalone PowerShell script in an Azure PowerShell environment, or you can go to the Azure portal, create an Automation account, and import this runbook from the automation gallery.  This runbook is in the category "Provisioning".  The benefit of running it as automation is that you don't need another machine to run PowerShell.  If you run it as automation, your will need to create a user to execute the runbook.  If you have never done that before, you can refer to this [blog](http://azure.microsoft.com/blog/2014/08/27/azure-automation-authenticating-to-azure-using-azure-active-directory/) for details.  Other than this difference, both use cases share the same basic steps.

Here's the step-by-step guide to deploy PXC:

 - Plan your environment, including: 
 - the region the cluster should be deployed to
 - the virtual network the cluster should be deployed to
 - subnet and IPs for the cluster nodes
 - IP for the load balancer 
 - cluster node VM size 
 - number of data disks to attach to each node, note that the number of disks that can be attached to a VM    
 - vary based on VM size, see [Azure VM    sizes](http://msdn.microsoft.com/en-us/library/azure/dn197896.aspx)    for details 
 - size of data disks 
 - whether the cluster nodes should have a second NIC, and if so, the subnet and IP addresses for the second  NIC
 - Provision the virtual network and subnets in Azure if they don't already exist (this step is not automated as many times you already have network configured and only need to deploy the cluster to your existing network).  Ensure the VMs don't already exist, and the IP addresses are available in the specified subnets.
 -  Download the my.cnf.template and customize it to your needs, especially, wsrep_sst_auth and innodb_buffer_pool_size.  Leave wsrep_cluster_address, wsrep_node_name and wsrep_node_address empty as they will be filled by the script.  Note that the sst user will be automatically created, and a 'test' user with the same password as the sst user will be created that can access the cluster from the load balancer.  Modify the azurepxc.sh script if you need to modify this behavior.
 - Upload modified my.cnf.template to a location that azurepxc.sh can download with "wget" from the deployed VM. 
 - Azurepxc.sh configures network, disks, and MySQL.  If you need to make any changes, for example, customize it to work for another Linux version or flavor, just upload your copy to either a GitHub location or Azure blob storage, and provide the URL and access info as parameters for the script. 
 - Provide the parameters and run the script or runbook.  The script will exit and the runbook will stop if an error occurred.  A lot of sanity checks are done before any resources are provisioned.  So likely you can rerun it after fixing any issues.  If resources are already provisioned and you want to clean up and restart, the easiest way is to delete the entire cloud service if the cloud service is created by this script or doesn't have any other resources in it.  Otherwise, you will need to delete the VMs. 
 - Once the deployment finishes, access the cluster from the IP of the load balancer with the 'test' account:
 - mysql -h <load balancer ip> -u test --password=<same as sst by default>
 - mysql> show status like 'wsrep%' 
 make sure wsrep_cluster_size is the number of the nodes provisioned.
### Known Issues
These are the known issues at the time of this writing - 

 - Azure Automation runbook is running on a version of Azure Powershell
   SDK that doesn't support the provision of multiple NICs yet.  To
   provision a 2nd NIC, you will need to run the script in PowerShell
   right now.  Once the SDK is updated in Azure Automation, this problem
   will go away.
 - Provisioning of second NIC may fail in some regions. 
   North Europe is the first region to support multiple NICs.  As this
   feature is rolled out to other regions, this problem will go away.
### Todo's
 - Support for Ubuntu is coming soon
License
----
MIT
