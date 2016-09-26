#!/bin/bash
# convert cloud_init.yml into the format for Azure ARM template CustomData
# An ARM template variable can have a max length of 16KB. This script will split a file into two if it's over 500 lines 

inputFile=$1
outputFile=$2

gzip -c ${inputFile} > ${inputFile}.gz 
openssl enc -base64 -in ${inputFile}.gz | tr -d '\n' > ${outputFile}
cat ${outputFile}
echo ' '
