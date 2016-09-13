#!/bin/bash

nnodes=10
tsize=1000
nentries=$[$tsize*1000*1000*1000/100]
SRCDIR=/benchmarks/tera/in
TGTDIR=/benchmarks/tera/out
tdisks=10
reducersPerNode=2
mappers=$[nnodes*$tdisks]
reducers=$[nnodes*$reducersPerNode]
replication=1
mapMB=2048
redMB=4096

hadoop fs -rm -r $TGTDIR

#supply your own teragen test script or comment this line and uncomment the following block to run a general teragen cmd
${1} -p terasort -x $replication -m $mappers -M $mapMB -r $reducers -R $redMB -d $nentries -s regular -i $SRCDIR -o $TGTDIR

#TJAR=$(ls /opt/cloudera/parcels/CDH/lib/hadoop-0.20-mapreduce/hadoop-examples.jar)
#hadoop jar $TJAR terasort \
#    -Ddfs.replication=$replication \
#    -Dmapreduce.job.maps=$mappers \
#    -Dmapreduce.job.reduces=$reducers \
#    -Dmapreduce.reduce.memory.mb=$redMB \
#    -Dmapreduce.map.memory.mb=$mapMB \
#    $SRCDIR $TGTDIR 2>&1 | tee -a logfile
