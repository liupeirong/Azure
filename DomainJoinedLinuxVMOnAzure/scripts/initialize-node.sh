#!/bin/bash
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#   http://www.apache.org/licenses/LICENSE-2.0
# 
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# 
# See the License for the specific language governing permissions and
# limitations under the License.

ADDNS=$1
PDC=$2
BDC=$3
PDCIP=$4
BDCIP=$5
ADMINUSER=$6
DOMAINADMINUSER=$7
DOMAINADMINPWD=$8
ADOUPATH=$9

findOsVersion() {
    # if it's there, use lsb_release
    rpm -q redhat-lsb
    if [ $? -eq 0 ]; then
        os=$(lsb_release -si)
        major_release=$(lsb_release -sr | cut -d '.' -f 1)

    # if lsb_release isn't installed, use /etc/redhat-release
    else
        grep  "CentOS.* 6\." /etc/redhat-release
        if [ $? -eq 0 ]; then
            os="CentOS"
            major_release="6"
        fi
	    grep  "CentOS.* 7\." /etc/redhat-release
        if [ $? -eq 0 ]; then
            os="CentOS"
            major_release="7"
        fi
    fi

    echo "OS: $os $major_release"

    # select the OS and run the appropriate setup script
    not_supported_msg="OS $os $release is not supported."
    if [ "$os" != "CentOS" ]; then
        echo "$not_supported_msg"
        exit 1
	fi
    if [ "$major_release" != "6" ] && [ "$major_release" != "7" ]; then
        echo "$not_supported_msg"
        exit 1
    fi
}

replace_ad_params() {
    target=${1}
    shortdomain=`echo ${ADDNS} | sed 's/\..*$//'`
    sed -i "s/REPLACEADDOMAIN/${ADDNS}/g" ${target}
    sed -i "s/REPLACEUPADDOMAIN/${ADDNS^^}/g" ${target}
    sed -i "s/REPLACESHORTADDOMAIN/${shortdomain}/g" ${target}
    sed -i "s/REPLACEPDC/${PDC}/g" ${target}
    sed -i "s/REPLACEBDC/${BDC}/g" ${target}
    sed -i "s/REPLACEIPPDC/${PDCIP}/g" ${target}
    sed -i "s/REPLACEIPBDC/${BDCIP}/g" ${target}
}

# Disable the need for a tty when running sudo and allow passwordless sudo for the admin user
sed -i '/Defaults[[:space:]]\+!*requiretty/s/^/#/' /etc/sudoers
echo "$ADMINUSER ALL=(ALL) NOPASSWD: ALL" >> /etc/sudoers

# Disable SELinux
setenforce 0 
sed -i 's^SELINUX=enforcing^SELINUX=disabled^g' /etc/selinux/config

# Install and configure domain join packages
yum install -y ntp
yum -y remove samba-client
yum -y remove samba-common
yum -y install sssd
yum -y install sssd-client
yum -y install krb5-workstation
yum -y install samba4
yum -y install openldap-clients
yum -y install policycoreutils-python

os=""
release=""
findOsVersion

# in CentOS7, tell NetworkManager not to overwrite /etc/resolv.conf
if [ "$major_release" = "7" ]; then
    cp -f nwnodns.conf /etc/NetworkManager/conf.d/
    systemctl restart NetworkManager.service
fi

cp -f resolv.conf /etc/resolv.conf
replace_ad_params /etc/resolv.conf
cp -f krb5.conf /etc/krb5.conf
replace_ad_params /etc/krb5.conf
cp -f smb.conf /etc/samba/smb.conf
replace_ad_params /etc/samba/smb.conf
cp -f sssd.conf /etc/sssd/sssd.conf
replace_ad_params /etc/sssd/sssd.conf
cp -f ntp.conf /etc/ntp.conf
replace_ad_params /etc/ntp.conf
sed -i "s/session.*pam_oddjob_mkhomedir.so.*/session     optional      pam_oddjob_mkhomedir.so skel=\/etc\/skel umask=0077/" /etc/pam.d/system-auth

# in CentOS6, prevent /etc/resolv.conf from being overwritten
cat > /etc/dhcp/dhclient-enter-hooks << EOF
#!/bin/sh
make_resolv_conf() {
echo "do not change resolv.conf"
}
EOF
chmod a+x /etc/dhcp/dhclient-enter-hooks

chmod 600 /etc/sssd/sssd.conf
service ntpd start
chkconfig ntpd on
service smb start
chkconfig smb on

# Join domain, must join domain first, otherwise sssd won't start
shortHostName=`hostname -s`
hostname ${shortHostName}.${ADDNS}
n=0
until [ $n -ge 40 ]
do
  if [ ! -z "$ADOUPATH" ]; then
    net ads join createcomputer="$ADOUPATH" -U${DOMAINADMINUSER}@${ADDNS}%${DOMAINADMINPWD}  
  else
    net ads join -U${DOMAINADMINUSER}@${ADDNS}%${DOMAINADMINPWD}  
  fi
  result=$?
  [ $result -eq 0 ] && break
  n=$[$n+1]
  if [ $n -ge 10 ]; then
     sleep 120
  else
     sleep 30
  fi
done
if [ $result -eq 0 ]; then
  klist -k
  authconfig --enablesssd --enablemkhomedir --enablesssdauth --update
  service sssd restart
  chkconfig sssd on
  hostname ${shortHostName}
  exit 0
else
  hostname ${shortHostName}
  exit 1
fi
