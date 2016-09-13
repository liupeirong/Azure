#!/bin/bash

tsize=1000
nentries=$[$tsize*1000*1000*1000/100]
tdisks=10
nnodes=10
mappers=$[$tdisks*$nnodes]
mapMB=2048
replication=1
TGTDIRIN=/benchmarks/tera/in
hadoop fs -rm -r $TGTDIRIN
hadoop fs -rm -r $TGTDIROUT

#supply your own teragen test script or comment this line and uncomment the following block to run a general teragen cmd
${1} -p teragen -x $replication -m $mappers -M $mapMB -d $nentries -s regular -i $TGTDIRIN -o $TGTDIROUT

#TJAR=$(ls /opt/cloudera/parcels/CDH/lib/hadoop-0.20-mapreduce/hadoop-examples.jar)
#hadoop jar $TJAR teragen \
#     -Dmapreduce.job.maps=$mappers \
#     -Dmapreduce.map.speculative=false \
#     -Dmapreduce.reduce.speculative=false \
#     -Ddfs.replication=$replication \
#     -Dmapreduce.map.memory.mb=$mapMB \
#     $nentries $TGTDIRIN
