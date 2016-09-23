#!/bin/bash

echo $@
DNSPREFIX=${1}
NODECOUNT=${2}

NODENAME=$(hostname)
NODEIP=`hostname -i`

# identify this node and peer nodes in the cluster
cp cluster.conf /tmp
cd /tmp
sed -i "s/^myip=.*/myip=$NODEIP/I" ./cluster.conf
sed -i "s/^myname=.*/myname=$NODENAME/I" ./cluster.conf

let "NODEEND=NODECOUNT-1"
for i in $(seq 0 $NODEEND)
do
    IP=`host $DNSPREFIX$i | awk '/has address/ { print $4 }'`
    HOST=$DNSPREFIX$i
    echo "clusernode$i=$IP;$HOST" >> ./cluster.conf
done

# go to peer nodes to set them up
for i in $(seq 1 $NODEEND)
do
    IP=`host $DNSPREFIX$i | awk '/has address/ { print $4 }'`
    HOST=$DNSPREFIX$i
	scp -o StrictHostKeyChecking=no -r /tmp/cluster.conf $IP:/tmp 
    ssh -o StrictHostKeyChecking=no root@$IP "echo $IP,$HOST,$i > /tmp/hello"  
done 

#bash ./any_additional_scripts.sh
