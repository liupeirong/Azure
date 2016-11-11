# Run Hadoop perf tests on Azure

* Set up ssh key for cluster nodes so that from one node where you want to run the scripts, let's call it the driver node, you can ssh into other nodes without password
* Copy the scripts to the driver node, modify the parameters in the top section of warmupDisks.sh to match your cluster
* Run warmupDisks.sh. This is necessary if your nodes are running standard storage in Azure. It's not necessary for premium storage. This script will run several loops of dd read and dd write, and collect the output from all nodes to the driver node's ~/ddresults.txt, a csv file that you can load to Excel and view charts
* Adjust the parameters at the top of runTeraGen.sh, runTeraSort.sh, and runTeraValidate.sh as needed
* Run runTeraGen.sh, runTeraSort.sh, and runTeraValidate.sh one by one, each command takes replication factor as a parameter. Verify everything works
* Run runTeraAll.sh which repeatedly run teragen, terasort, and teravalidate with different replication factors in parallel
* Optionally create an Impala table from the teragen/terasort output using impalaTera.sql, and run queries against Impala
