#!/bin/bash
# CSV to JSON converter using BASH
# original script from http://blog.secaserver.com/2013/12/convert-csv-json-bash/
# thanks SecaGuy!
# Usage ./csv2json.sh input.csv > output.json
#      if [[ $each_element == [a-zA-Z]* ]] && [ $each_element != "NULL" ]; then

input=$1
delim=","
null_element="NULL"
 
[ -z $1 ] && echo "No CSV input file specified" && exit 1
[ ! -e $input ] && echo "Unable to locate $1" && exit 1
 
read first_line < $input
a=0
headings=`echo $first_line | awk -F"${delim}" {'print NF'}`
lines=`cat $input | wc -l`
while [ $a -lt $headings ]
do
  head_array[$a]=$(echo $first_line | awk -v x=$(($a + 1)) -F"${delim}" '{print $x}')
  a=$(($a+1))
done
 
c=0
while [ $c -lt $lines ]
do
  read each_line
  if [ $c -ne 0 ]; then
    d=0
    echo -n "{"
    while [ $d -lt $headings ]
    do
      each_element=$(echo $each_line | awk -v y=$(($d + 1)) -F"${delim}" '{print $y}')
      if [[ $each_element != $null_element ]]; then
        each_element="\"${each_element}\""                     
      else
        each_element="null"
      fi
      if [ $d -ne $(($headings-1)) ]; then
        echo -n "\"${head_array[$d]}\":$each_element,"
      else
        echo -n "\"${head_array[$d]}\":$each_element"
      fi
      d=$(($d+1))
    done
    echo "}"
  fi
c=$(($c+1))
done < $input
