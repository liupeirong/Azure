[![Deploy to Azure](http://azuredeploy.net/deploybutton.png)](https://azuredeploy.net/?repository=https://github.com/liupeirong/Azure/tree/master/ARMPXC/ARMPXC.Deployment/Templates)

This template lets you create a 3 node Percona XtraDB Cluster 5.6 on Azure.  It's tested on Ubuntu 12.04 LTS and CentOS 6.5.  To verify the cluster, type in "mysql -h <dnsname> -u test -p".  MySQL queries will be load balanced to the cluster nodes. 
