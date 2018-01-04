# Real time data pipeline with Kafka, Spark on HDInsight and Power BI #

This lab demonstrates an end-to-end example to ingest data into Kafka, process data with Spark, and finally visualize data in Power BI.

![Alt text](/KafkaSparkBILab/diagram.png?raw=true "Data Pipeline")

This lab is inspired by the [Azure Predictive Maintenance Prediction sample](https://gallery.cortanaintelligence.com/Collection/Predictive-Maintenance-Template-3).  The sample data is based on the "Turbofan Engine Degradation Simulation Data Set" in the [NASA Ames Prognostics Data Repository](http://ti.arc.nasa.gov/tech/dash/pcoe/prognostic-data-repository/). The lab environment can be automatically provisioned with the [Azure Resource Manager template]().  

You can [create the lab](/KafkaSparkBILab/createlab) yourself. The lab environment currently consists of the following resources:

* A virtual network in which everything will be provisioned
* An HDInsight cluster for Kafka with 3 workers, at the time of this writing, the lakafkalab HDInsight version is 3.6, and Kafka is 0.10
* An HDInsight cluster for Spark with 2 workers, at the time of this writing, the lakafkalab HDInsight version is 3.6, and Spark is 2.1
* Optionally a Windows machine from which you can ssh into the clusters, and run Power BI Desktop.  If you don't have a Windows machine, you can use Azure Cloud Shell to ssh into the cluster. You can also log on to [Power BI](https://powerbi.com) to get data from HDInsight Spark cluster.  

### 1 Ingest data to Kafka ###
ssh into your Kafka cluster {{Kafka cluster name}}-ssh.azurehdinsight.com. The sample data simulated_device_data.csv is located in the "/" directory. We will ingest this data into Kafka. This data has the following format:
```csv
DeviceId,Cycle,Counter,EndOfCycle,Sensor9,Sensor11,Sensor14,Sensor15
N1172FJ-2,1,1,1,9048.49521305798,47.6290124202935,8127.91067233134,8.2332657671585
N1172FJ-1,1,1,1,9043.35372177262,47.1369259061993,8126.71445988376,8.17990143908579
N1172FJ-2,1,2,0,9052.71816833555,46.8470457503592,8123.23052632809,8.37387280454549
N1172FJ-1,1,2,0,9040.48238011606,47.7821727021101,8128.94564507941,8.26440855195816
N1172FJ-2,1,3,0,9053.00339110528,47.3061069800384,8125.71228068237,8.68102656244265
```

__Step 1.1__ Obtain Kafka broker and Zookeeper hostnames by running the following commands in your ssh session:
```sh
CLUSTERNAME='{{Kafka cluster name}}'
PASSWORD='{{Kafka cluster password}}'
export KAFKAZKHOSTS=`curl -sS -u admin:$PASSWORD -G https://$CLUSTERNAME.azurehdinsight.net/api/v1/clusters/$CLUSTERNAME/services/ZOOKEEPER/components/ZOOKEEPER_SERVER | jq -r '["\(.host_components[].HostRoles.host_name):2181"] | join(",")' | cut -d',' -f1,2`
export KAFKABROKERS=`curl -sS -u admin:$PASSWORD -G https://$CLUSTERNAME.azurehdinsight.net/api/v1/clusters/$CLUSTERNAME/services/KAFKA/components/KAFKA_BROKER | jq -r '["\(.host_components[].HostRoles.host_name):9092"] | join(",")' | cut -d',' -f1,2`
echo '$KAFKAZKHOSTS='$KAFKAZKHOSTS
echo '$KAFKABROKERS='$KAFKABROKERS
export PATH=$PATH:/usr/hdp/current/kafka-broker/bin
```

__Step 1.2__ Create a topic and randomly distribute data into partitions.
```sh
# The sample data contains 8 devices, create 8 partitions with the intention to partition by device
kafka-topics.sh --create --replication-factor 1 --partitions 8 --topic kafkalab --zookeeper $KAFKAZKHOSTS
# Check the topic created
kafka-topics.sh --describe --topic kafkalab --zookeeper $KAFKAZKHOSTS
# Remove the csv header row, ingest to Kafka topic
awk '{if (NR>1) {print}}' /simulated_device_data.csv | kafka-console-producer.sh --broker-list $KAFKABROKERS --topic kafkalab
# Observe the data ingested in each partition
kafka-console-consumer.sh --bootstrap-server $KAFKABROKERS --topic kafkalab --partition 0 --from-beginning --max-messages 10
kafka-console-consumer.sh --bootstrap-server $KAFKABROKERS --topic kafkalab --partition 1 --from-beginning --max-messages 10
```
Note that every partition has data.  But data from the same device could end up in different partitions. This is because data is treated as key-value pairs in Kafka. When key is null, data is randomly placed in partitions. 

__Step 1.3__ Partition data by device id.
```
# delete the topic
kafka-topics.sh --delete --topic kafkalab --zookeeper $KAFKAZKHOSTS
# make sure it's deleted
kafka-topics.sh --list --zookeeper $KAFKAZKHOSTS
# recreate the topic
kafka-topics.sh --create --replication-factor 1 --partitions 8 --topic kafkalab --zookeeper $KAFKAZKHOSTS
# ingest the data, this time specify the first field is the partition key and the separator between the key and the rest of the data
awk '{if (NR>1) {print}}' /simulated_device_data.csv | kafka-console-producer.sh --broker-list $KAFKABROKERS --topic kafkalab --property parse.key=true --property key.separator=,
# observe the data in each partition
kafka-console-consumer.sh --bootstrap-server $KAFKABROKERS --topic kafkalab --partition 0 --from-beginning --max-messages 10 --property print.key=true --property key.separator=:
kafka-console-consumer.sh --bootstrap-server $KAFKABROKERS --topic kafkalab --partition 1 --from-beginning --max-messages 10 --property print.key=true --property key.separator=:
```
Note that this time data from the same device will always go to the same partition, however, multiple devices could still go to the same partition, and certain partitions don't have data. This is because Kafka uses murmur2 hash on the partition key mod by the number of partitions to determine the partition. In order to distribute data to different partitions with different partition keys, you will need to write your own partitioner. Custom partitioner is not supported in Kafka console producer. 

Partitioning data in Kafka allows Spark to run tasks to process multiple partitions simultaneously. 

In the next section, we will process the data that we just ingested into the Kafka topic. Note down the value of KAFKABROKERS and Kafka topic, which we will use in the following Spark job.

### 2 Process data in Spark ###
In this section, we will process the data ingested into Kafka in the previous section with Spark Structured Streaming, and learn how to control the streaming behaviour with windowing functions, output mode, and watermark. I've found that spark-shell is a better tool to use when learning about streaming than notebooks because console output is very helpful but doesn't work in notebooks. To run the following code, ssh into your spark cluster {{Spark cluster name}}-ssh.azurehdinsight.com, and start spark-shell with the following command and paste the code blocks into the spark-shell console as you move along the following steps.
```sh
spark-shell --packages org.apache.spark:spark-sql-kafka-0-10_2.11:2.1.0
```

__Step 2.1__ Simple pass through streaming job that outputs Kafka messages to console.
Replace the value of KafkaBrokers, then paste the code into spark-shell to run:
```scala
import org.apache.spark.sql.types._
import org.apache.spark.sql.functions._
import spark.implicits._ 

val kafkaBrokers = "{{your KAFKABROKERS}}"
val kafkaTopic = "kafkalab"

val dfraw = spark.readStream.
      format("kafka").
      option("kafka.bootstrap.servers", kafkaBrokers).
      option("subscribe", kafkaTopic). 
      option("startingOffsets", "earliest"). 
      load

val df = dfraw.
      select($"key".cast(StringType), $"value".cast(StringType))
      
val query = df.writeStream.
      format("console").  
      option("truncate", false).
      start
```
This simply consumes the messages from Kafka and outputs them in the console. You can run the following commands to examine the query status and stop the query:
```scala
query.lastProgress
query.status
query.stop
```

__Step 2.2__ Aggregate with a tumbling window and a key. This sample dataset doesn't have a time stamp in its payload, so we add the current time stamp. Note that the job finishes in single batch.
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

__Step 2.3__ Set maxOffsetPerBatch to 2000 and see how the behaviour changes. 
```scala
query.stop

val maxOffsetPerTrigger = 2000

val dfraw = spark.readStream.
      format("kafka").
      option("kafka.bootstrap.servers", kafkaBrokers).
      option("subscribe", kafkaTopic). 
      option("startingOffsets", "earliest"). 
      option("maxOffsetsPerTrigger", maxOffsetPerTrigger).  //maxOffsetPerTrigger controls how many messages to read per trigger
      load

val df = dfraw.
      select($"key".cast(StringType), $"value".cast(StringType)).
      withColumn("ts", current_timestamp).
      groupBy(window($"ts", tumblingWindow), $"key").
      count.
      select($"window.end".alias("windowend"), lower($"key").alias("deviceid"), $"count")

val query = df.writeStream.
      format("console").  
      option("truncate", false).
      outputMode("update").
      start
```
Note that the job now finishes in about 6 batches because there are about 12000 rows in Kafka.

__Step 2.4__ Change the outputMode to "complete" to see how the behaviour changes.
```scala
query.stop

val query = df.writeStream.
      format("console").  
      option("truncate", false).
      outputMode("complete"). // outputs complete results aggregated so far instead of the changed results from last batch
      start
```

__Step 2.5__ Change the outputMode to "append" (default). 

Watermark is required for aggregation in "append" mode. Watermark controls how late an event could arrive to be calculated in aggregation. For a given time window ending T, if the last seen event time is more current than T + watermark, then aggregation up to T is completed for output. Any events that happened before T and arrive later will be ignored. Note the first few batches will be empty, waiting for the late arrival events.

```scala
query.stop

val watermark = "10 seconds"

val df = dfraw.
      select($"key".cast(StringType), $"value".cast(StringType)).
      withColumn("ts", current_timestamp).
      withWatermark("ts", watermark). // watermark controls late arrival events
      groupBy(window($"ts", tumblingWindow), $"key").
      count.
      select($"window.end".alias("windowend"), lower($"key").alias("deviceid"), $"count")

val query = df.writeStream.
      format("console").  
      option("truncate", false). // default is "append" mode
      start
```

__Step 2.6__ Save results to a file by updating workingDir and checkpointDir below:

```scala
query.stop

val workingDir = "/user/sshuser/lab"
val checkpointDir = "/user/sshuser/checkpoint"

val query = df.writeStream.
      format("parquet").  
      option("path", workingDir).
      option("checkpointLocation",checkpointDir).
      start
```

Observe the files by running the following shell commands:
```sh
hdfs dfs -ls /user/sshuser/lab
hdfs dfs -ls /user/sshuser/checkpoint
```

In spark-shell, if you run the query again, you will see it doesn't reprocess the messages, this is because checkpointing records the offset of the Kafka messages processed. 
```
query.stop

val query = df.writeStream.
      format("parquet").  
      option("path", workingDir).
      option("checkpointLocation",checkpointDir).
      start

query.status
query.lastProgress
query.stop
```

### 3 Visualize data in Power BI ###
Data can be persisted for historical analysis in Power BI, or it can be pushed to Power BI for real time analysis. 

__Step 3.1__ Save the output to a table for historical analysis. 

Note that the output from the previous section consists of many small parquet files. To improve query efficiency we will repartition the data by device id when we save the table. 
```scala
val dftable = spark.
      read.parquet(workingDir).
      write.
      partitionBy("deviceid"). // repartition, otherwise many small files
      saveAsTable("labbi")  //table saved in /hive/warehouse by default
```

Open Power BI Desktop, __Get Data__, __Azure HDInsight Spark__, input your HDInsight server name {{Spark cluster name}}.azurehdinsight.net and credential. You should see the "labbi" table  saved above.

__Step 3.2__ Push streaming results directly to Power BI. 

* Login to powerbi.com.
* In __My Workspace__, __Datasets__, __Create__, __Streaming Dataset__, __API__, give it a name, and a structure like the following:
    * *windowend: datetime*
    * *count: number*
* Click __Create__, __cURL__ tab, copy and paste the API endpoint, including API key, to *pbiUrl* in the below code.
* Create a new Dashboard in Power BI, add a Tile with a line chart and the newly created streaming dataset, with __axis__ set to __windowend__, __value__ set to __count__ of the dataset, and display last 10 minutes of data. 
* Run the following code into spark-shell, and watch the line chart changing as new data streaming into Power BI.
* You can troubleshoot using [requestbin](https://requestb.in) if data is not flowing into Power BI.

```scala
import org.apache.spark.sql.ForeachWriter
import org.apache.http.client.methods.HttpPost
import org.apache.http.impl.client.HttpClientBuilder
import org.apache.http.entity.StringEntity

val pbiUrl = "{{your Power BI API endpoint}}"
//val pbiUrl = "https://requestb.in/{{your bin}}"

// make each batch smaller so that we can see the line chart moves as each batch of aggregation flows into Power BI
val maxOffsetPerTrigger = 50

val dfraw = spark.readStream.
      format("kafka").
      option("kafka.bootstrap.servers", kafkaBrokers).
      option("subscribe", kafkaTopic). 
      option("startingOffsets", "earliest"). 
      option("maxOffsetsPerTrigger", maxOffsetPerTrigger). 
      load

val df = dfraw.
      select($"key".cast(StringType), $"value".cast(StringType)).
      withColumn("ts", current_timestamp).
      groupBy(window($"ts", tumblingWindow), $"key").
      count.
      select($"window.end".alias("windowend"), lower($"key").alias("deviceid"), $"count")

// Power BI is a custom sink
val powerbiForeachSink = new ForeachWriter[String] {
     override def open(partitionId: Long, version: Long): Boolean = { 
       true 
     }
     override def process(record: String) = {
       if (record != null && !record.isEmpty)
         {
           val body = new StringEntity("[" + record + "]")
           val post = new HttpPost(pbiUrl)
           post.addHeader("Content-Type", "application/json")
           post.setEntity(body)
           val httpClient = HttpClientBuilder.create.build
           val resp = httpClient.execute(post)
         }
     }
     override def close(errorOrNull: Throwable): Unit = {} 
    }

val query = df.select(to_json(struct($"windowend", $"count"))).as[(String)].
      writeStream.
      outputMode("update").
      foreach(powerbiForeachSink).
      start

```
