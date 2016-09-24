#!/bin/bash
# convert cloud_init.yml into the format for Azure ARM template CustomData
# An ARM template variable can have a max length of 16KB. This script will split a file into two if it's over 500 lines 

inputFile=$1
outputFile=$2

function convert_tmp {
    inFile=$1
    tFile=$2
    sed "s/'/''/g" $inFile > $tFile
    sed -i 's/\\/\\\\/g' $tFile
    sed -i ':a;N;$!ba;s/\n/\\n/g' $tFile
    sed -i 's/"/\\"/g' $tFile
}

convert_tmp ${inputFile} ${outputFile}
gzip ${outputFile} 
openssl enc -base64 -in ${outputFile}.gz | tr -d '\n' > ${outputFile}
cat ${outputFile}
echo ' '
