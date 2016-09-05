#!/bin/bash

MASTER_DNS_PREFIX=$1
LOCATION=${2}.cloudapp.azure.com
CA_CERT=keys/ca.pem
ADMIN_KEY=keys/admin-key.pem
ADMIN_CERT=keys/admin.pem

curl -O https://storage.googleapis.com/kubernetes-release/release/v1.3.4/bin/linux/amd64/kubectl 
chmod +x kubectl
export PATH=./:$PATH

kubectl config set-cluster default-cluster --server=https://${MASTER_DNS_PREFIX}.${LOCATION} --certificate-authority=${CA_CERT}
kubectl config set-credentials default-admin --certificate-authority=${CA_CERT} --client-key=${ADMIN_KEY} --client-certificate=${ADMIN_CERT}
kubectl config set-context default-system --cluster=default-cluster --user=default-admin
kubectl config use-context default-system
kubectl get namespaces
