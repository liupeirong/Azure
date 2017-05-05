Once you provisioned the VMs using the azuredeploy_multivm template, you get a set of domain joined kerberos enabled VMs. You can then follow these steps to create a Cloudera cluster on the provisioned VMs. 

* Run "initialize-cloudera-node.sh masternode" on the master node.  Note that I haven't tested HDFS name node HA yet, so for now, use the first VM (vmName0) as the master node.
* Run "initialize-cloudera-node.sh datanode" on the data nodes. 
* Fill in the parameter section of bootstrap-cloudera.sh, and then run that script to install Cloudera.
* Follow this Cloudera doc (https://www.cloudera.com/documentation/enterprise/latest/topics/cm_sg_intro_kerb.html) to enable Kerberos on Cloudera. 
