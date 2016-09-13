#!/bin/bash
# assumes the disks you want to run against are named as /data*

runEndLoop=9
dataDisks=($(ls -d /data*))
logdir=/home/$(whoami)

#clean log file
sudo rm -rf ${logdir}/ddout*

#write
for x in "${dataDisks[@]}"
do
  sudo rm -rf ${x}/dd*  
done 

for y in `seq 0 ${runEndLoop}`
do 
  #run on all data disks, at the same time
  for x in "${dataDisks[@]}"
  do
    sudo dd if=/dev/zero of=${x}/ddout bs=1M count=1000 oflag=direct >> ${logdir}/ddout${x:5}.txt 2>&1 & 
  done 
  sleep 30s
done 

sudo rm -rf ${logdir}/ddwrite*
vmName=$(hostname -s)
for x in "${dataDisks[@]}"
do 
  sudo cat ${logdir}/ddout${x:5}.txt | awk -v id=${x:5} -v vm=${vmName} '/copied/ { print vm ",data" id ",write," $8}' >> ${logdir}/ddwrite.txt
done

