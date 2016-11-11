#!/bin/bash

replication=${1}
nnodes=10
tdisks=10
reducersPerNode=2
mapMB=2048
redMB=4096
TJAR=$(ls /opt/cloudera/parcels/CDH/lib/hadoop-0.20-mapreduce/hadoop-examples.jar)

mappers=$[nnodes*$tdisks]
reducers=$[nnodes*$reducersPerNode]
TGTDIRIN=/benchmarks/tera/gen${replication}
TGTDIROUT=/benchmarks/tera/sort${replication}

hadoop fs -rm -r -skipTrash $TGTDIROUT

hadoop jar $TJAR terasort \
    -Ddfs.replication=$replication \
    -Ddfs.client.block_write.locateFollowingBlock.retries=30 \
    -Dmapred.reduce.child.log.level=WARN \
    -Dyarn.app.mapreduce.am.job.cbd-mode.enable=false \
    -Dmapreduce.job.maps=$mappers \
    -Dmapreduce.job.reduces=$reducers \
    -Dmapreduce.reduce.memory.mb=$redMB \
    -Dmapreduce.map.memory.mb=$mapMB \
    -Dmapreduce.job.reduce.shuffle.consumer.plugin.class=org.apache.hadoop.mapreduce.task.reduce.Shuffle \
    -Dyarn.app.mapreduce.am.job.map.pushdown=false \
    $TGTDIRIN $TGTDIROUT
