#!/bin/sh
#

HOSTS=/etc/hosts
PREFIX=pliu

# greps the prefix from the hosts file and extracts the host name. iterates over the result.
#
for h in `cat $HOSTS | grep $PREFIX | awk '{print $2}'`; do

  # copy the file to the remote host. turn off host key checking so host is automatically added to "known_hosts" file.
  #
  scp -o "StrictHostKeyChecking=no" $HOSTS $h:./hosts
  
  # copy the hosts file into place
  #
  ssh $h 'sudo cp ./hosts /etc/hosts'
  
  # restart the networking service so the eth0 device is bound to the right host name.
  #
  ssh $h 'sudo service network restart'
done
