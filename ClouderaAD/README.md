# Cloudera deployment designed for Active Directory integration

This template refactors the original [Cloudera Enterprise Data Hub template](https://github.com/Azure/azure-quickstart-templates/tree/master/cloudera-on-centos) into 2 parts - VM deployment and Cloudera installation, such that we can integrate the cluster with Active Directory.  For details of how to integrate Cloudera with Active Directory to enable Kerberos and Single Sing On, please see [this blog](http://blogs.msdn.com/b/pliu/archive/2016/01/02/integrating-cloudera-cluster-with-active-directory-part-1-3.aspx).  

Deploy VMs:
<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fliupeirong%2FAzure%2Fmaster%2FClouderaAD%2Fazuredeploy.json" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png" />
</a>

Install Cloudera on VMs:
<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fliupeirong%2FAzure%2Fmaster%2FClouderaAD%2Fazuredeploy_postad.json" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png" />
</a>
