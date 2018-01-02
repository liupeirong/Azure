# Real time data pipeline with Kafka, Spark on HDInsight and Power BI #

This lab demonstrates an end-to-end example to ingest data into Kafka, process data with Spark, and finally visualize data in Power BI.

![Alt text](/KafkaSparkBILab/diagram.png?raw=true "Data Pipeline")

This lab is inspired by the [Azure Predictive Maintenance Prediction sample](https://gallery.cortanaintelligence.com/Collection/Predictive-Maintenance-Template-3).  The sample data is based on the "Turbofan Engine Degradation Simulation Data Set" in the [NASA Ames Prognostics Data Repository](http://ti.arc.nasa.gov/tech/dash/pcoe/prognostic-data-repository/). The lab environment can be automatically provisioned with the [Azure Resource Manager template]().  The lab environment currently consists of the following resources:

* A virtual network in which everything will be provisioned
* An HDInsight cluster for Kafka with 3 workers, at the time of this writing, the latest HDInsight version is 3.6, and Kafka is 0.10
* An HDInsight cluster for Spark with 2 workers, at the time of this writing, the latest HDInsight version is 3.6, and Spark is 2.1
* Optionally a Windows machine from which you can ssh into the clusters, and run Power BI Desktop.  If you don't have a Windows machine, you can use Azure Cloud Shell to ssh into the cluster. You can also log on to [Power BI](https://powerbi.com) to get data from HDInsight Spark cluster.  

### Ingest data to Kafka ###
ssh into your Kafka cluster {{Kafka cluster name}}-ssh.azurehdinsight.com. The sample data simulated_device_data.csv is located in the home directory. We will ingest this data into Kafka. This data has the following format:
```csv
DeviceId,Cycle,Counter,EndOfCycle,Sensor9,Sensor11,Sensor14,Sensor15
N1172FJ-2,1,1,1,9048.49521305798,47.6290124202935,8127.91067233134,8.2332657671585
N1172FJ-1,1,1,1,9043.35372177262,47.1369259061993,8126.71445988376,8.17990143908579
N1172FJ-2,1,2,0,9052.71816833555,46.8470457503592,8123.23052632809,8.37387280454549
N1172FJ-1,1,2,0,9040.48238011606,47.7821727021101,8128.94564507941,8.26440855195816
N1172FJ-2,1,3,0,9053.00339110528,47.3061069800384,8125.71228068237,8.68102656244265
```

__Step 1__ Obtain Kafka broker and Zookeeper hostnames by running the following commands in your ssh session:
```bash
sudo apt -y install jq
CLUSTERNAME='{{Kafka cluster name}}'
PASSWORD='{{Kafka cluster password}}'
export KAFKAZKHOSTS=`curl -sS -u admin:$PASSWORD -G https://$CLUSTERNAME.azurehdinsight.net/api/v1/clusters/$CLUSTERNAME/services/ZOOKEEPER/components/ZOOKEEPER_SERVER | jq -r '["\(.host_components[].HostRoles.host_name):2181"] | join(",")' | cut -d',' -f1,2`
export KAFKABROKERS=`curl -sS -u admin:$PASSWORD -G https://$CLUSTERNAME.azurehdinsight.net/api/v1/clusters/$CLUSTERNAME/services/KAFKA/components/KAFKA_BROKER | jq -r '["\(.host_components[].HostRoles.host_name):9092"] | join(",")' | cut -d',' -f1,2`
echo '$KAFKAZKHOSTS='$KAFKAZKHOSTS
echo '$KAFKABROKERS='$KAFKABROKERS
export PATH=$PATH:/usr/hdp/current/kafka-broker/bin
```

__Step 2__ Create a topic and randomly distribute data into partitions
```bash
# The sample data contains 8 devices, create 8 partitions with the intention to partition by device
kafka-topics.sh --create --replication-factor 1 --partitions 8 --topic test --zookeeper $KAFKAZKHOSTS
# Check the topic created
kafka-topics.sh --describe --topic test --zookeeper $KAFKAZKHOSTS
# Remove the csv header row, ingest to Kafka topic
awk '{if (NR>1) {print}}' simulated_device_data.csv | kafka-console-producer.sh --broker-list $KAFKABROKERS --topic test
# Observe the data ingested in each partition
kafka-console-consumer.sh --bootstrap-server $KAFKABROKERS --topic test --partition 0 --from-beginning --max-messages 10
kafka-console-consumer.sh --bootstrap-server $KAFKABROKERS --topic test --partition 1 --from-beginning --max-messages 10
```
Note that data from the same device could end up in different partitions. Data is treated as key-value pairs in Kafka. When key is null, data is randomly placed in partitions. 

__Step 3__ Partition data by device id
```
# delete the topic
kafka-topics.sh --delete --topic test --zookeeper $KAFKAZKHOSTS
# make sure it's deleted
kafka-topics.sh --list --zookeeper $KAFKAZKHOSTS
# recreate the topic
kafka-topics.sh --create --replication-factor 1 --partitions 8 --topic test --zookeeper $KAFKAZKHOSTS
# ingest the data, this time specify the first field is the partition key and the separator between the key and the rest of the data
awk '{if (NR>1) {print}}' simulated_device_data.csv | kafka-console-producer.sh --broker-list $KAFKABROKERS --topic test --property parse.key=true --property key.separator=,
# observe the data in each partition
kafka-console-consumer.sh --bootstrap-server $KAFKABROKERS --topic test --partition 0 --from-beginning --max-messages 10 --property print.key=true --property key.separator=:
kafka-console-consumer.sh --bootstrap-server $KAFKABROKERS --topic test --partition 1 --from-beginning --max-messages 10 --property print.key=true --property key.separator=:
```
Note that this time data from the same device will always go to the same partition, however, multiple devices could still go to the same partition. Kafka uses murmur2 hash on the partition key mod by the number of partitions to determine the partition. In order to distribute data with different partition keys to go to different partitions, you will need to write your own partitioner. Custom partitioner is not supported in Kafka console producer. 

Partitioning data in Kafka allows Spark to run tasks to process multiple partitions simultaneously. 

In the next section, we will process the data that we just ingested into the Kafka topic. Note down the value of KAFKABROKERS and Kafka topic, which we will use in the following Spark job.

### Process data in Spark ###
In this section, we will process the data ingested into Kafka in the previous section with Spark Structured Streaming, and learn how to control the streaming behaviour with windowing functions, output mode, and watermark. I've found that spark-shell is a better tool to use when learning about streaming than notebooks because console output is very helpful but doesn't work in notebooks. To run the following code, ssh into your spark cluster {{Spark cluster name}}-ssh.azurehdinsight.com, and start spark-shell with this command and paste the code into the spark-shell console. 
```bash
spark-shell --packages org.apache.spark:spark-sql-kafka-0-10_2.11:2.1.0
```

__Step 1__ Simple pass through streaming job that outputs Kafka messages to console
Replace the value of KafkaBrokers and KafkaTopic, then paste the code into spark-shell to run
```scala
import org.apache.spark.sql.types._
import org.apache.spark.sql.functions._
import spark.implicits._ 

val kafkaBrokers = "{{your KAFKABROKERS}}"
val kafkaTopic = "{{your Kafka topic}}"

val dfraw = spark.readStream.
      format("kafka").
      option("kafka.bootstrap.servers", kafkaBrokers).
      option("subscribe", kafkaTopic). 
      option("startingOffsets", "earliest"). 
      load

val query = dfraw.writeStream.
      format("console").  
      option("truncate", false).
      start
```
This simply consumes the messages from Kafka and outputs them in the console. You can run the following commands to examine the query status and stop the query
```scala
query.lastProgress
query.status
query.stop
```

__Step 2__ Aggregate with a tumbling window and a key. Note that the job finishes in single batch.
```scala
val tumblingWindow = "5 seconds"

val df = dfraw.
      select($"key".cast(StringType), $"value".cast(StringType)).
      withColumn("ts", current_timestamp).
      groupBy(window($"ts", tumblingWindow), $"key"). // aggregate by tumbling window and key
      count.
      select($"window.end".alias("windowend"), lower($"key").alias("deviceid"), $"count")

val query = df.writeStream.
      format("console").  
      option("truncate", false).
      outputMode("update").
      start
```

### Visualize data in Power BI ###
__Demo 3__ Output to persistent table for BI queries, or push to Power BI in real time