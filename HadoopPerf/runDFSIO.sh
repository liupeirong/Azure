#!/bin/bash

#file size in MB
fsize=10000
tdisks=16
filesPerDisk=2

TJAR=$(ls /opt/cloudera/parcels/CDH/lib/hadoop-0.20-mapreduce/hadoop-test.jar)

hadoop jar $TJAR TestDFSIO \
      -Dmapreduce.job.name=DFSIO-write \
      -Dmapreduce.map.memory.mb=768 \
      -Dmapreduce.map.disk=0.5 \
      -Dmapreduce.map.speculative=false \
      -Dmapreduce.reduce.speculative=false \
      -write -nrFiles $[tdisks*$filesPerDisk] \
         -fileSize $[fsize / $filesPerDisk]  -bufferSize 65536

hadoop jar $TJAR TestDFSIO \
      -Dmapreduce.job.name=DFSIO-read \
      -Dmapreduce.map.memory.mb=768 \
      -Dmapreduce.map.disk=0.5 \
      -Dmapreduce.map.speculative=false \
      -Dmapreduce.reduce.speculative=false \
      -read -nrFiles $[tdisks*$filesPerDisk] \
         -fileSize $[fsize / $filesPerDisk]  -bufferSize 65536
