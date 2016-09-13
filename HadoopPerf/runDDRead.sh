#!/bin/bash
# assumes the disks you want to run against are named as /data*

runEndLoop=9
dataDisks=($(ls -d /data*))
logdir=/home/$(whoami)

sudo rm -rf ${logdir}/ddin*
for y in `seq 0 ${runEndLoop}`
do
  for x in "${dataDisks[@]}"
  do 
    sudo dd if=${x}/ddout of=/dev/null bs=1M count=1000 iflag=direct >> ${logdir}/ddin${x:5}.txt 2>&1 & 
  done
  sleep 30s
done
echo done

sudo rm -rf ${logdir}/ddread*
vmName=$(hostname -s)
for x in "${dataDisks[@]}"
do 
  sudo cat ${logdir}/ddin${x:5}.txt | awk -v id=${x:5} -v vm=${vmName} '/copied/ { print vm ",data" id ",read," $8}' >> ${logdir}/ddread.txt
done

