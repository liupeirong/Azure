<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fliupeirong%2FAzure%2Fmaster%2FAzureClusterDeploy%2Fazuredeploy.json" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png"/>
</a>
<a href="http://armviz.io/#/?load=https%3A%2F%2Fraw.githubusercontent.com%2Fliupeirong%2FAzure%2Fmaster%2FAzureClusterDeploy%2Fazuredeploy.json" target="_blank">
  <img src="http://armviz.io/visualizebutton.png"/>
</a>

# A Generic Template for deploying a cluster of VMs in Azure

Use this template as the basis for your cluster deployment if your deployment has the following characteristics:
* You need to deploy a number of VMs in parallel
* After the VMs are up, you need to run install scripts from one VM, and from there, ssh into other nodes to set them up
* You need to handle the ssh keys securely
* Disks attached to the VMs will be automatically formatted and mounted to /data0, /data1...
* You may want to put your scripts and config files in a secred Azure storage account than public github

```

License
----

MIT

