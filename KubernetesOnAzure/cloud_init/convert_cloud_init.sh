#!/bin/bash
# convert cloud_init.yml into the format for Azure ARM template CustomData
# An ARM template variable can have a max length of 16KB. This script will split a file into two if it's over 500 lines 

inputFile=$1
outputFile=$2

function convert_tmp {
    inFile=$1
    tFile=$2
    sed "s/'/''/g" $inFile > $tFile
    sed -i "s/STRIPPED_CAPEM/', parameters('capem'), '/g" $tFile
    sed -i "s/STRIPPED_APISERVERPEM/', parameters('apiserverpem'), '/g" $tFile
    sed -i "s/STRIPPED_APISERVERKEYPEM/', parameters('apiserverkeypem'), '/g" $tFile
    sed -i "s/STRIPPED_WORKERPEM/', parameters('workerpem'), '/g" $tFile
    sed -i "s/STRIPPED_WORKERKEYPEM/', parameters('workerkeypem'), '/g" $tFile
    sed -i "s/REPLACE_CONTROLLERIP/', variables('controllerIP'), '/g" $tFile
    sed -i "s/REPLACE_KUBEDNSIP/', variables('kubeDNSIP'), '/g" $tFile
    sed -i "s/REPLACE_KUBESERVICECIDR/', variables('kubeServiceCidr'), '/g" $tFile
    sed -i "s/REPLACE_KUBEPODNETWORKCIDR/', variables('kubePodNetworkCidr'), '/g" $tFile
    sed -i "s/REPLACE_AFSACCOUNT/', parameters('azurefsAccount'), '/g" $tFile
    sed -i "s/REPLACE_AFSSHARE/', parameters('azurefsShare'), '/g" $tFile
    sed -i 's/\\/\\\\/g' $tFile
    sed -i "1s/^/[concat('/" $tFile
    sed -i "\$a')]" $tFile
    sed -i ':a;N;$!ba;s/\n/\\n/g' $tFile
    sed -i 's/"/\\"/g' $tFile
    sed -i "s/REPLACE_AFSKEY/', parameters('azurefsKey'), '/g" $tFile
}

total_lines=$(wc -l <${inputFile})
if [ ${total_lines} -gt 500 ]
then 
  ((n1 = $total_lines / 2))
  ((n2 = $total_lines - $n1 + 1))
  head -n $n1 ${inputFile} > ${inputFile}.1
  tail -n $n2 ${inputFile} > ${inputFile}.2

  for i in 1 2 
  do
    input=${inputFile}.${i}
    tmp=${input}.tmp
    convert_tmp ${input} ${tmp}
    mv $tmp ${outputFile}.${i}
  done
else
  tmp=${inputFile}.tmp
  convert_tmp ${inputFile} ${tmp}
  mv $tmp ${outputFile}
fi
