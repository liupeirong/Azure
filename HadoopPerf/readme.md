# Run Hadoop perf tests on Azure

* Set up ssh key for cluster nodes so that from one node where you want to run the scripts, let's call it driver node, you can ssh into other nodes
* Copy the scripts to the driver node, modify the parameters in the top section of warmupDisks.sh to match your cluster
* Run warmupDisks.sh. This is necessary if your nodes are running standard storage in Azure. It's not necessary for premium storage. This script will run several loops of dd read and dd write, and collect the output from all nodes to the driver node's ~/ddresults.txt, a csv file that you can load to Excel and view charts
* Run runTeraGen.sh, configure the parameters in the script, especially the data size and replication factor. You can run a generic teragen job, or pass in a vendor specific script 
* Create a loop to repeatedly run runTeraSort.sh, or run multiple terasorts in the background in parallel. Configure the parameters in the script
* Optionally create an Impala table from the teragen/terasort output using impalaTera.sql, and run queries against Impala
