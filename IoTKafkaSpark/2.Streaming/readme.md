# Spark structured streaming from Kafka

* Logging
  * Logging level WARN or above is usually sufficient for normal runs of Spark jobs.  With Spark structured streaming, there's no longer the "Streaming" tab in the Spark Web UI. You can either call an API to get the streaming progress and metrics, or set the logging level to INFO for `org.apache.spark.sql.execution.streaming.StreamExecution`, in which case it will output the progress info for each batch.
  * Create log4j.properties and use it in spark-submit by specifying `--driver-java-options='-Dlog4j.configuration=file:log4j.properties'`


* Configuration
  * A type safe config file can be included in the jar.  It can also be placed in _spark.driver.extraClassPath_ and _spark.executor.extraClassPath_.  The one in the class path takes higher priority.

  
* Control Kafka processing
  * Each Kafka partition is assigned to a Spark executor core, and the driver also needs a core.  As a general guideline, each executor should have no more than 5 cores.  With this in mind, you can specify the number of executors and executor cores to achieve max parallelism to process Kafka partitions without wasting idle cores. 
  * No need to create a Kafka consumer group in structured streaming.  A unique consumer group will be auto created.
  * `startingOffsets` controls where to read Kafka message from.  It takes effect only when there's no checkpointing.  When `writeStream` has `checkpointLocation` specified, offset stored in the checkpoint will take effect when reading from Kafka.
  * `Trigger` controls how often Spark goes to read a batch of events from Kafka, and `maxOffsetsPerTrigger` controls how many events to read per batch.  `watermark` controls how long to wait for late events to arrive.  
  * When `OutputMode` is set to `Append`, Spark will only output aggregations for a time window after `maxSeenEventTime - watermark > endOfTimeWindow`.  So to test, you can use `trigger`, `maxOffsetsPerTrigger`, and `watermark` to control the processing timing in order to verify if you are getting the expected results.  For example, if you are computing the sum of a series of values between 9:00 and 9:05, and watermark is 7 minutes, there will be no output until Spark sees an event with a timestamp of 9:12 or later.  `Append` mode is the default for console sink.  If you don't see any results in console when testing, try `Update` or `Complete` mode.
  * For testing, use file source instead of Kafka makes it easy to control what data gets processed, at what rate, and what's the expected results.  All you need to do is to drop file into the target directory.

* Many small files
  * If the output sink is a file, you will see many small files.  If you have a Hive or Impala table on this file, query will become very slow even with small amount of data.  Partition the output by, for example, year/month/day, and run a separate batch job periodically to [compact](/IoTKafkaSpark/3.compact) the files. 
  
To see the console output of the driver, run this Spark job in YARN in Client mode:
```bash
spark2-submit --master yarn --deploy-mode client --num-executors 3 --executor-cores 3 --jars /opt/libs/config-1.3.1.jar --class org.pliu.iot.sim.streaming ./original-sim-streaming-0.0.1.jar
```