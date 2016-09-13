#!/bin/bash

vmprefix=sf4ds10
datanodeEndloop=9
namenodeEndloop=2

user=$(whoami)
resultdir=/home/${user}

iostatcmd="iostat"
iostatssh="nohup ${iostatcmd} -x -t -m 1 > /home/${user}/${iostatcmd}.txt 2>&1 < /dev/null &"
iostatheader="vmname,disk,r/s,w/s,rMB/s,wMB/s,avgqu-sz,await"
iostatawk='/sd[c-z]/ { print vmname "," $1 "," $4 "," $5 "," $6 "," $7 "," $9 "," $10 }'
iostatsed='s/1/1/'

iftopcmd="iftop"
iftopssh="nohup ${iftopcmd} -i eth0 -t > /home/${user}/${iftopcmd}.txt 2>&1 < /dev/null &"
iftopheader="vmname,srRate2s,srRate10s,srRate40s"
iftopawk='/Total send and receive rate/ {print vmname "," $6 "," $7 "," $8}'
iftopsed='s/Kb//g'

iperf3cmd="iperf3"
iperf3ssh="nohup ${iperf3cmd} -c ${vmprefix}-mn0 -i 1 -t 10 > /home/${user}/${iperf3cmd}.txt 2>&1 < /dev/null"
iperf3header="vmname,interval,bandwidth,unit,direction"
iperf3awk='/0.00-10./ {print vmname "," $3 "," $7 "," $8 "," $NF }'
iperf3sed='s/1/1/g'

# sshcmd=$iostatssh
# shortcmd=$iostatcmd
# header=$iostatheader
# awkopt=$iostatawk
# sedopt=$iostatsed

# sshcmd=$iftopssh
# shortcmd=$iftopcmd
# header=$iftopheader
# awkopt=$iftopawk
# sedopt=$iftopsed

sshcmd=$iperf3ssh
shortcmd=$iperf3cmd
header=$iperf3header
awkopt=$iperf3awk
sedopt=$iperf3sed

#run cmd on data nodes
for x in `seq 0 ${datanodeEndloop}`
do
  echo "run ${shortcmd} on node dn${x}"
  ssh -o StrictHostKeyChecking=no ${vmprefix}-dn${x} "sudo ${sshcmd}"
done

#run cmd on name nodes
for x in `seq 1 ${namenodeEndloop}`
do
  echo "run ${shortcmd} on node mn${x}"
  ssh -o StrictHostKeyChecking=no ${vmprefix}-mn${x} "sudo ${sshcmd}"
done

#kill cmd on data nodes
for x in `seq 0 ${datanodeEndloop}`
do
  echo "kill ${shortcmd} on node dn${x}"
  ssh -o StrictHostKeyChecking=no ${vmprefix}-dn${x} "sudo pkill ${shortcmd}"
done

#kill cmd on name nodes
for x in `seq 1 ${namenodeEndloop}`
do
  echo "kill ${shortcmd} on node mn${x}"
  ssh -o StrictHostKeyChecking=no ${vmprefix}-mn${x} "sudo pkill ${shortcmd}"
done

#collect results
rm -rf ${resultdir}/${shortcmd}_*.txt
echo ${header} > ${resultdir}/${shortcmd}results.txt
for x in `seq 0 ${datanodeEndloop}`
do 
  echo "collect ${shortcmd} on node dn${x}"
  scp ${vmprefix}-dn${x}:/home/${user}/${shortcmd}.txt ${resultdir}/${shortcmd}_dn${x}.txt  
  cat ${resultdir}/${shortcmd}_dn${x}.txt | awk -v vmname=${vmprefix}-dn${x} "${awkopt}" | sed "${sedopt}" >> ${resultdir}/${shortcmd}results.txt
done

for x in `seq 1 ${namenodeEndloop}`
do
  echo "collect ${shortcmd} on node mn${x}"
  scp ${vmprefix}-mn${x}:/home/${user}/${shortcmd}.txt ${resultdir}/${shortcmd}_mn${x}.txt  
  cat ${resultdir}/${shortcmd}_mn${x}.txt | awk -v vmname=${vmprefix}-mn${x} "${awkopt}" | sed "${sedopt}" >> ${resultdir}/${shortcmd}results.txt
done
