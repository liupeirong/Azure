#!/bin/bash

vmprefix=pliustdnc1
datanodeEndloop=9
namenodeEndloop=2

scriptdir=/tmp/perfscripts
user=$(whoami)

#copy the scripts to the nodes
#ssh -o StrictHostKeyChecking=no ${vmprefix}-dn0 "ls ${scriptdir}"
#if [ $? -ne 0 ]; then
  for x in `seq 0 ${datanodeEndloop}`
  do 
    ssh -o StrictHostKeyChecking=no ${vmprefix}-dn${x} "mkdir -p ${scriptdir}"
    scp /opt/perfscripts/*.sh ${vmprefix}-dn${x}:${scriptdir} 
  done
  for x in `seq 1 ${namenodeEndloop}`
  do 
    ssh -o StrictHostKeyChecking=no ${vmprefix}-mn${x} "mkdir -p ${scriptdir}"
    scp /opt/perfscripts/*.sh ${vmprefix}-mn${x}:${scriptdir} 
  done
#fi

#run dd write on data nodes
for x in `seq 0 ${datanodeEndloop}`
do
  echo "dd write node dn${x}"
  ssh -o StrictHostKeyChecking=no ${vmprefix}-dn${x} "nohup ${scriptdir}/runDDWrite.sh 1>foo 2>&1 < /dev/null &"
done

#run dd write on name nodes
for x in `seq 1 ${namenodeEndloop}`
do
  echo "dd write node mn${x}"
  ssh -o StrictHostKeyChecking=no ${vmprefix}-mn${x} "nohup ${scriptdir}/runDDWrite.sh 1>foo 2>&1 < /dev/null &"
done

#wait until writes done, then verify we have something in the folder
echo write sleeping
sleep 10m

for x in `seq 0 ${datanodeEndloop}`
do 
  ssh ${vmprefix}-dn${x} "ls /home/${user}"
done

#run dd read on data nodes
for x in `seq 0 ${datanodeEndloop}`
do
  echo "dd read node dn${x}"
  ssh -o StrictHostKeyChecking=no ${vmprefix}-dn${x} "nohup ${scriptdir}/runDDRead.sh 1>foo 2>&1 < /dev/null &"
done

#run dd read on name nodes
for x in `seq 1 ${namenodeEndloop}`
do
  echo "dd read node mn${x}"
  ssh -o StrictHostKeyChecking=no ${vmprefix}-mn${x} "nohup ${scriptdir}/runDDRead.sh 1>foo 2>&1 < /dev/null &"
done

echo read sleeping
sleep 10m

#wait until reads done, then verify we have something in the folder
for x in `seq 0 ${datanodeEndloop}`
do 
  ssh ${vmprefix}-dn${x} "ls /home/${user}"
done

#collect results from all nodes into a single file of format (vmname,diskid,read/write,MB/s)
rm -rf /home/${user}/ddread_*.txt
rm -rf /home/${user}/ddwrite_*.txt
rm -rf /home/${user}/ddresults.txt
for x in `seq 0 ${datanodeEndloop}`
do 
  echo "cp dd write node dn${x}"
  scp ${vmprefix}-dn${x}:/home/${user}/ddread.txt /home/${user}/ddread_dn${x}.txt  
  scp ${vmprefix}-dn${x}:/home/${user}/ddwrite.txt /home/${user}/ddwrite_dn${x}.txt  
done
for x in `seq 1 ${namenodeEndloop}`
do
  echo "cp dd read node mn${x}"
  scp ${vmprefix}-mn${x}:/home/${user}/ddread.txt /home/${user}/ddread_mn${x}.txt  
  scp ${vmprefix}-mn${x}:/home/${user}/ddwrite.txt /home/${user}/ddwrite_mn${x}.txt  
done
echo "vmname,disk,op,mb/s" > /home/${user}/ddresults.txt
cat /home/${user}/ddread_*.txt >> /home/${user}/ddresults.txt
cat /home/${user}/ddwrite_*.txt >> /home/${user}/ddresults.txt

