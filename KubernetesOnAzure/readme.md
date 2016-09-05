# Azure templates and tools to deploy a Kubernetes cluster on CoreOS 

This folder contains the necessary templates and scripts to automate the deployment of a Kubernetes cluster to Azure.  By default it deploys a cluster with a single Kubernetes controller and two workers. You can customize cloud init configuration to meet your needs.  All the certificates used by Kubernetes are stored in Azure Key Vault.

### Create or customize cloud init configuration
* Customize controller and worker clouder init files in the cloud_init folder to meet your needs. The default file included here deploys a generic cluster
* Run conver_cloud_init.sh to convert your cloud init files into the format for Azure template. The script will automatically splits a file into 2 output strings if it is more than 500 lines
* Replace the CustomData variables at the end of the azuredeploy.json with the output from the above step

### Generate certificates and upload to Azure Key Vault
* Run generateKeys.sh in keys_scripts folder to generate all the certificates for Kubernetes nodes to communicate with each other
* Run uploadKeys to upload the certificates to Azure Key Vault, use uploadKeys.ps1 on Windows and uploadKeys.sh on Linux

### Optionally set up Azure File Service (AFS) as shared persistent storage for Kubernetes containers
* If your containers require persistent storage, run setupAFS.sh to set up AFS. You may need to go back to your worker cloud init configuration file to review if mount.cifs is downloaded and AFS is mounted

### Deploy the cluster
* Review azuredeploy.json to make sure VNet and subnet addresses, Kubernetes pod network address and service address are all correct
* Provide parameters in azuredeploy.parameters.json 
* Deploy azuredeploy.json to create the cluster, it should take only a minute or so for the cluster to come up
* Optionally run setupKubectl.sh to set up kubectl
