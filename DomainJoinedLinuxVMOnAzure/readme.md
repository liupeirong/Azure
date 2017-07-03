This example demonstrates how to create domain joined Linux VMs on Azure using Azure AD Domain Service. 

* azuredeploy.json deploys a single CentOS 6 VM that is domain joined and Kerberos enabled
* azuredeploy_multivm.json deploys a set of CentOS 6 VMs that are domain joined and Kerberos enabled
* azuredeploy_centos7.json deploys a single CentOS 7.2 Cloudera VM image that is domain joined and Kerberos enabled

To deploy a Cloudera cluster with Kerberos enabled, there are two options:
* once you have a set of VMs, you can follow the instructions in setupcloudera folder to set up a domain joined Kerberos enabled cloudera cluster. provision at least 4 Standard_DS13 VMs.  Cloudera may not run on smaller VMs. 
* create a single VM, manually install Cloudera Director on it, then go to the ClouderaDirector folder and followed the instructions to automatically deploy a Kerberos enabled cluster. 