<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fliupeirong%2FAzure%2Fmaster%2FAzureClusterDeploy%2Fazuredeploy.json" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png"/>
</a>
<a href="http://armviz.io/#/?load=https%3A%2F%2Fraw.githubusercontent.com%2Fliupeirong%2FAzure%2Fmaster%2FAzureClusterDeploy%2Fazuredeploy.json" target="_blank">
  <img src="http://armviz.io/visualizebutton.png"/>
</a>

# A Sample Template for deploying a number of MySQL VMs in Azure

Use this template as a sample for deploying a number of Ubuntu VMs on Azure.  It uses cloud-init user data to set up the VMs. Run convert_cloud_init.sh to convert your script into a gzip+base64 string and set it as CustomData in the template. 
