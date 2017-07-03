This example demonstrates how to create domain joined Linux VMs on Azure using Azure AD Domain Service or Active Directory on VMs. 
* azuredeploy.json deploys a single CentOS 6 VM that is domain joined and Kerberos enabled
* azuredeploy_multivm.json deploys a set of CentOS 6 VMs that are domain joined and Kerberos enabled
* azuredeploy_centos7.json deploys a single CentOS 7.2 Cloudera VM image that is domain joined and Kerberos enabled

To deploy a Cloudera cluster with Kerberos enabled, there are two options:
* Option 1: Once you have a set of VMs, you can follow the instructions in [setupcloudera folder](/DomainJoinedLinuxVMOnAzure/setupcloudera) to set up a domain joined Kerberos enabled cloudera cluster. Provision at least 4 Standard_DS13 VMs.  Cloudera may not run on smaller VMs. 
* Option 2: Create a single VM, [manually install Cloudera Director](https://www.cloudera.com/documentation/director/2-2-x/topics/director_get_started_azure_install_director.html) on it, then go to the [ClouderaDirector folder](/DomainJoinedLinuxVMOnAzure/ClouderaDirector) and followed the instructions to automatically deploy a Kerberos enabled cluster. 