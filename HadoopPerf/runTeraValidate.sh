#!/bin/bash

replication=${1}
nnodes=10
tdisks=10
mapMB=2048
TJAR=$(ls /opt/cloudera/parcels/CDH/lib/hadoop-0.20-mapreduce/hadoop-examples.jar)

mappers=$[nnodes*$tdisks]
TGTDIRIN=/benchmarks/tera/sort${replication}
TGTDIROUT=/benchmarks/tera/validate${replication}

hadoop fs -rm -r -skipTrash $TGTDIROUT

hadoop jar $TJAR teravalidate \
    -Ddfs.replication=$replication \
    -Dmapreduce.job.maps=$mappers \
    -Dmapreduce.map.memory.mb=$mapMB \
    -Dyarn.app.mapreduce.am.job.map.pushdown=false \
    $TGTDIRIN $TGTDIROUT
