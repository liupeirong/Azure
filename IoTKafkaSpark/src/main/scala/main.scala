import org.apache.spark.sql.SparkSession
import org.apache.spark.sql.functions._
import org.apache.spark.sql.streaming.Trigger
import scala.concurrent.duration._
import com.typesafe.config._

object IoTStreaming {
  def main(args: Array[String]): Unit = {
    val appconf = ConfigFactory.load("iotconf")
    val kafkaBrokers = appconf.getString("iotsim.kafkaBrokers")
    val kafkaTopic = appconf.getString("iotsim.kafkaTopic")
    val maxOffsetsPerTrigger = appconf.getString("iotsim.maxOffsetPerTrigger")
    val workingDir = appconf.getString("iotsim.devicelogWorkingDir")
    val devicelogDir = appconf.getString("iotsim.devicelogDir")
    val devicelogCheckpointDir = appconf.getString("iotsim.devicelogCheckpointDir")
    val messageFormat = appconf.getString("iotsim.messageFormat")
    
    val spark = SparkSession
      .builder
      .appName("iotsim")
      .getOrCreate
    import spark.implicits._
    
    // executors and executor cores vs partition
    val dfraw = spark.readStream.
      format("kafka").
      option("kafka.bootstrap.servers", kafkaBrokers).
      option("subscribe", kafkaTopic). 
      //no need to set consumer group, a unique group will be auto created
      //each partition will be assigned to one consumer in the group, each consumer can consume multiple partitions
      //when the number of consumers equals to the number of kafka partitions, you achieve max parallelism without idle
      //stick with about 5 cores per executor
      option("startingOffsets", "earliest").
      option("maxOffsetsPerTrigger", maxOffsetsPerTrigger).  //this controls how many messages to read per trigger
      load()
      
    //for testing, use file source instead of Kafka
    //val dfraw = spark.readStream.schema(devicelogSchema).option("header", "true").csv("/user/pliu/iotinput")

    val dftyped = if (messageFormat == "csv") toTypedDF.fromCSV(dfraw, spark) else toTypedDF.fromJSON(dfraw, spark)

    // if the events include a timestamp field
    //val df = dftyped.withColumn("ts", from_unixtime($"ts" /1000, "YYYY-MM-dd HH:mm:ss").cast(TimestampType))
    // else we add a timestamp field just to show how to use the windowing functions below    
    val df = dftyped.withColumn("ts", current_timestamp)
    
    /* aggregation */
    val dfagg = df.
      withWatermark("ts", "1 minute").
      groupBy(window($"ts", "1 minute"), $"deviceid").
      agg(avg($"sensor9")).
      select($"window.start", $"window.end", $"deviceid", $"count").
      withColumn("year", year($"ts")).
      withColumn("month", month($"ts"))
    
    /* alerting - not used in this example */
    //spark.conf.get("spark.sql.caseSensitive") by default its false
    val dfalert = df.
    	filter($"tag" === "temperature" && $"sensor11" > 600).
    	withColumn("message", concat(lit("temperature too high"),$"sensor11"))

    /* storing output */
    // the working folder is partitioned by year and month, so after the month ends, 
    // run the compaction job to compact the files in that month of the year to its destination folder
    val query = dfagg.
      writeStream.
      format("parquet").
      partitionBy("year", "month").
      trigger(Trigger.ProcessingTime(1.minutes)). //trigger controls how often to read from Kafka
      option("path", workingDir).
      option("checkpointLocation",devicelogCheckpointDir).  //checkpoint controls offset to read from
      start()
     
    // for testing, use console sink. Note that only Append mode is supported for file sink. Append mode only works 
    // with watermark, and will only produce outputs when the next trigger kicks in AND
    // max seen event time - watermark > evaluated time window, so by default it's append mode
    //    val query = dfagg.writeStream.format("console").option("truncate", false).start 
    // if you don't see result, try Update or Complete mode
    //    val query = dfagg.writeStream.format("console").outputMode(OutputMode.Update).option("truncate", false).start

    query.awaitTermination

    /* compacting small files in working dir to larger files*/
    /*
    spark.read.parquet(workingDir + "/year=2017/month=10").
      repartition($"deviceid").
      write.partitionBy("deviceid").
      mode(SaveMode.Overwrite).
      parquet(devicelogDir + "/year=2017/month=10")
    */
            
    //TODO use Kafka console producer to simulate device
    //TODO create hive tables on parquet, all partitioned fields must be lower case
  }  
}  
