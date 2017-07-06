This sample demonstrates how to use Cloudera Director to automate the deployment of a Kerberos enabled Cloudera cluster. 

The config files are modified from their [original versions published on Cloudera GitHub](https://github.com/cloudera/director-scripts/tree/master/configs):  
* azure.simple.conf - joins the VMs to Active Directory (AD), but doesn't enable Kerberos on Cloudera automatically.
* azure.simple.kerberos.conf - enable Kerberos on CentOS 6 based cluster
* azure.simplec7.kerberos.conf - enable Kerberos on CentOS 7 based cluster

### Before deploying a Cloudera cluster:
* In the DNS server, manually add a reverse dns lookup zone for the IP range of the VMs that will be created.  This is so that when new VMs join the domain, they will be able to register their forward and reverse DNS automatcially
* If your MySQL uses SSL, turn off SSL
* Go to the [parent folder](/DomainJoinedLinuxVMOnAzure) to deploy a small single VM first to verify the configuration in Active Directory is all correct before deploying a full fleged Cloudera cluster 
* In order to deploy Cloudera Direct in an existing VNet, go to the [parent folder](/DomainJoinedLinuxVMOnAzure) to deploy a single VM that is domain joined, then [manually install Director](https://www.cloudera.com/documentation/director/latest/topics/director_get_started_azure_install_director.html)
  * To install JDK, 
```javascript
  wget --no-check-certificate --no-cookies --header "Cookie: oraclelicense=accept-securebackup-cookie" http://download.oracle.com/otn-pub/java/jdk/8u131-b11/d54c1d3a095b4ff2b6607d096fa80163/jdk-8u131-linux-x64.rpm
```
* Edit the conf file to match your environment
  * VM name prefix can't be longer than 15 chars for AADDS, so prefix in the config file can be 6 chars max
  * Cloudera Managed krb5.conf must be set to true
* To deploy on CentOS 7, create the following file and name it images.conf under /var/lib/cloudera-director-plugins/azure-provider-\<version>/etc, then restart cloudera-director-server. 
```javascript
  # This is the Centos 7 image published by Cloudera
  cloudera-centos-7-latest {
  publisher: cloudera
  offer: cloudera-centos-os
  sku: 7_2
  version: latest
  }
```

### Deploy a Cloudera cluster:
* Use command line instead of Director UI to deploy a cluster
```javascript
  cloudera-director validate ./azure.simplec7.kerberso.conf --lp.remote.username=<user> --lp.remote.password=<password> --lp.remote.hostAndPort=<director host>
  cloudera-director bootstrap-remote ./azure.simplec7.kerberso.conf --lp.remote.username=<user> --lp.remote.password=<password> --lp.remote.hostAndPort=<director host>
```
* If any of the VMs can't successfully register their forward/reverse DNS in the DNS server as part of the domain join, either the deployment will fail or the cluster will not come up healthy.  

### Verify the cluster after deployment and troubleshooting:
* After the cluster starts, 
  * ensure you are no longer able to browse HDFS as the built-in hdfs user, "hdfs dfs ls /" should fail 
  * instead, first enable passwordauthentication in /etc/ssh/sshd_config, and restart sshd service
  * go to Cloudera Manager -> HDFS configuration -> Service Wide -> Security, change supergroup to the name of a group in AD that will have supergroup privileges in HDFS, for example, "hadoopadmin"   
  * in AD, create hadoopadmin group, then create a user in that group, ssh into a cluster node as that user, verify now you can browse hdfs and create a folder under /user
  * in AD, create another user that doesn't belong to the hadoopadmin group, ssh into a cluster node as that user, verify you can't write to /user folder
* If you see Kerberos keytab renewer error in Hue with Permission denied: "/var/log/hue/kt_renewer.log", run
```javascript
  chown -R hue:hue /var/log/hue
  // restart Hue Service
```
* If you see a Hive SQL_SELECT_LIMIT=DEFAULT related error, fix it by going to the Hive Server and do the following:
```javascript
  wget https://dev.mysql.com/get/Downloads/Connector-J/mysql-connector-java-5.1.42.tar.gz
  tar xvfz mysql-connector-java-5.1.42.tar.gz
  cd mysql-connector-java-5.1.42
  sudo cp mysql-connector-java-5.1.42-bin.jar /usr/share/java
  cd /usr/share/java
  sudo rm mysql-connector-java.jar
  ln -s mysql-connector-java-5.1.42-bin.jar mysql-connector-java.jar
  restart Hive
```

### Clean up a deployment:
* To terminate a cluster
```javascript
  cloudera-director terminate-remote ./azure.simplec7.kerberso.conf --lp.remote.username=<user> --lp.remote.password=<password> --lp.remote.hostAndPort=<director host>
```
* If database is not cleaned up, go to Director UI, and remove the databases for the environment
* Go to the Director UI, and delete the environment
* Go to the DNS server, and delete the DNS entries for the cluster from both the forward and reverse DNS zones
* Go to Active Directory, and remove the VMs that joined to the domain
* If the cluster can not be cleanly terminated
  * stop cloudera-director-server service
  * delete the file /var/lib/cloudera-director-server/*
  * optionally delete log files /var/log/cloudera-director-server/*
  * manually delete all the databases created by Cloudera 
  * start cloudera-director-server service 
