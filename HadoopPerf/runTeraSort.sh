#!/bin/bash

tsize=1000
replication=1
nnodes=10
tdisks=10
nentries=$[$tsize*1000*1000*1000/100]
reducersPerNode=2
mappers=$[nnodes*$tdisks]
reducers=$[nnodes*$reducersPerNode]
mapMB=2048
redMB=4096
TGTDIRIN=/benchmarks/tera/in
TGTDIROUT=/benchmarks/tera/out
hadoop fs -rm -r -skipTrash $TGTDIROUT

#supply your own teragen test script or comment this line and uncomment the following block to run a general teragen cmd
${1} -p terasort -x $replication -m $mappers -M $mapMB -r $reducers -R $redMB -d $nentries -s regular -i $TGTDIRIN -o $TGTDIROUT

#TJAR=$(ls /opt/cloudera/parcels/CDH/lib/hadoop-0.20-mapreduce/hadoop-examples.jar)
#hadoop jar $TJAR terasort \
#    -Ddfs.replication=$replication \
#    -Dmapreduce.job.maps=$mappers \
#    -Dmapreduce.job.reduces=$reducers \
#    -Dmapreduce.reduce.memory.mb=$redMB \
#    -Dmapreduce.map.memory.mb=$mapMB \
#    $TGTDIRIN $TGTDIR 2>&1 | tee -a logfile
