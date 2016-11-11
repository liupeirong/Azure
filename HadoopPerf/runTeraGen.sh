#!/bin/bash

tsize=1000
replication=${1}
nnodes=10
tdisks=10
mapMB=2048
TJAR=$(ls /opt/cloudera/parcels/CDH/lib/hadoop-0.20-mapreduce/hadoop-examples.jar)

nentries=$[$tsize*1000*1000*1000/100]
mappers=$[$tdisks*$nnodes]
TGTDIROUT=/benchmarks/tera/gen${replication}

hadoop fs -rm -r -skipTrash $TGTDIROUT

hadoop jar $TJAR teragen \
     -Ddfs.replication=$replication \
     -Dmapreduce.job.maps=$mappers \
     -Dmapreduce.map.memory.mb=$mapMB \
     -Ddfs.client.block.write.locateFollowingBlock.retries=15 \
     -Dyarn.app.mapreduce.am.job.cbd-mode.enable=false \
     -Dyarn.app.mapreduce.am.job.map.pushdown=false \
     $nentries \
     $TGTDIROUT
