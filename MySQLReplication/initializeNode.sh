#/bin/bash

ADMINUSER=$1

#disable apparmor
/etc/init.d/apparmor teardown
update-rc.d -f apparmor remove

#set up ssh public key for root and ADMINUSER
mkdir ~/.ssh
chmod 700 ~/.ssh

ssh-keygen -y -f /var/lib/waagent/*.prv > ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys

mkdir /home/$ADMINUSER/.ssh
chown $ADMINUSER /home/$ADMINUSER/.ssh
chmod 700 /home/$ADMINUSER/.ssh

ssh-keygen -y -f /var/lib/waagent/*.prv > /home/$ADMINUSER/.ssh/authorized_keys
chown $ADMINUSER /home/$ADMINUSER/.ssh/authorized_keys
chmod 600 /home/$ADMINUSER/.ssh/authorized_keys

#set up ssh private key for root and ADMINUSER
openssl rsa -in /var/lib/waagent/*.prv -out ~/.ssh/id_rsa
chmod 600 ~/.ssh/id_rsa

openssl rsa -in /var/lib/waagent/*.prv -out /home/$ADMINUSER/.ssh/id_rsa
chown $ADMINUSER /home/$ADMINUSER/.ssh/id_rsa
chmod 600 /home/$ADMINUSER/.ssh/id_rsa

#attach disk
bash prepareDisks.sh