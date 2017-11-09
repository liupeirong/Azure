# Ingest simulated data into Kafka

In this example, we will ingest simulated_device_data.csv into a Kafka topic.  Rather than building a simulator to stream data into Kafka, kafka-console-producer is an easy-to-use and powerful tool that can be used to stream sample data into Kafka.  Often times, the sample data is in CSV or JSON format.  Depending on how you want the data to land in Kafka, simple data transformation may be needed.  Here are a few examples:

* If the sample data is in CSV format, but the streaming consumer expects JSON format - use the csv2json.sh in this folder to convert CSV to JSON. Set delimiter and NULL value at the beginning of the script.  Note that if you put capital "NULL" in parquet files and then build a Hive table on the files, Hive won't be able to read the data.  **NULL value in Hive must be in lower case**.


* If each file belongs to a single Kafka partition, you can add a field at the beginning of each line to serve as a partition key.  For example,
```bash
$ awk -F, '{$1=$key FS $1;}1' OFS=, data_file.json | kafka-console-producer ... --property parse.key=true --property key.separator=,
```


* The way Kafka partitions data by default is to compute a hash on the partition key, then mod it by the number of partitions.  So while the hash key is likely unique, when mod by a small number of partitions, data could end up skewed in one or just a few partitions.  To evenly distribute the data into all partitions, you may use a numeric value 0..#partitions-1 as the key.


* If the sample data contains data values for all keys, as shown in this example - deviceid is the key and the simulated_device_data file contains multiple devices, create a file mapDevice2Partition.csv to map between deviceid and 0..#partitions-1 keys. Then use AWK to add the desired key to each line.  Below is the full sequence of commands needed to ingest simulated_device_data.csv to a Kafka topic, with each device going to its own partition.
```bash
$ export BROKERS=worker1:9092,worker2:9092,worker3:9092
$ export ZOOKEEPER=zookeeper:2181
$ kafka-topics --zookeeper $ZOOKEEPER --create --topic devicelog --partitions 8 --replication-factor 3
$ awk '
     BEGIN { FS = OFS = "," }
     FNR == NR {
         split($0, f, /:/)
         map[f[1]] = f[2]
         next
     }
     {
         if ($1 in map) { $1=map[$1] FS $1 }
     }
     FNR > 1 {
         print 
     }
' mapDevice2Partition.csv simulated_device_data.csv | /usr/bin/kafka-console-producer --topic devicelog --broker-list $BROKERS --property parse.key=true --property key.separator=,
```


* With the data ingested to Kafka, move on to Spark to [stream and process the data] (/IoTKafkaSpark/2.Streaming)