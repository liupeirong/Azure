# Cloudera deployment designed for Active Directory integration

This template refactors the original [Cloudera Enterprise Data Hub template](https://github.com/Azure/azure-quickstart-templates/tree/master/cloudera-on-centos) into 2 parts - VM deployment and Cloudera installation, such that we can integrate the cluster with Active Directory.  For details of how to integrate Cloudera with Active Directory to enable Kerberos and Single Sing On, please see [this blog](http://blogs.msdn.com/b/paigeliu/archive/2015/12/28/integrating-cloudera-cluster-with-active-directory.aspx).  
