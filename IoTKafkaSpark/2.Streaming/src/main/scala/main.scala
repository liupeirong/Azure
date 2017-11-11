package org.pliu.iot.sim

import org.apache.spark.sql.SparkSession
import org.apache.spark.sql.functions._
import org.apache.spark.sql.streaming.{Trigger, OutputMode}
import scala.concurrent.duration._
import com.typesafe.config._

object streaming {
  def main(args: Array[String]): Unit = {
    val appconf = ConfigFactory.load("iotsim")
    val kafkaBrokers = appconf.getString("iotsim.kafkaBrokers")
    val kafkaTopic = appconf.getString("iotsim.kafkaTopic")
    val maxOffsetsPerTrigger = appconf.getInt("iotsim.maxOffsetsPerTrigger")
    val watermark = appconf.getString("iotsim.watermark")
    val tumblingWindow = appconf.getString("iotsim.tumblingWindow")
    val triggerInterval = appconf.getString("iotsim.triggerInterval")
    val workingDir = appconf.getString("iotsim.devicelogWorkingDir")
    val devicelogDir = appconf.getString("iotsim.devicelogDir")
    val devicelogCheckpointDir = appconf.getString("iotsim.devicelogCheckpointDir")
    val biCheckpointDir = appconf.getString("iotsim.biCheckpointDir")
    val messageFormat = appconf.getString("iotsim.messageFormat")
    
    val spark = SparkSession
      .builder
      .appName("iotsim")
      .getOrCreate
    import spark.implicits._
    
    //for testing, use file source instead of Kafka
    //val dfraw = spark.readStream.schema(devicelogSchema).option("header", "true").csv("/user/pliu/iotinput")

    val dfraw = spark.readStream.
      format("kafka").
      option("kafka.bootstrap.servers", kafkaBrokers).
      option("subscribe", kafkaTopic). 
      option("startingOffsets", "earliest"). //this is ignored when checkpoint passes in offsets
      option("maxOffsetsPerTrigger", maxOffsetsPerTrigger).  //this controls how many messages to read per trigger
      load
    
    val dftyped = if (messageFormat == "csv") toTypedDF.fromCSV(dfraw, spark) else toTypedDF.fromJSON(dfraw, spark)

    // if the events include a timestamp field
    //val df = dftyped.withColumn("ts", from_unixtime($"ts" /1000, "YYYY-MM-dd HH:mm:ss").cast(TimestampType))
    // else we add a timestamp field just to show how to use the windowing functions below    
    val df = dftyped.withColumn("ts", current_timestamp)
    
    /* aggregation */
    val dfagg = df.
      withWatermark("ts", watermark).
      groupBy(window($"ts", tumblingWindow), $"deviceid").
      agg(avg($"sensor9").alias("sensor9avg")).
      select($"window.start", $"window.end", lower($"deviceid").alias("deviceid"), $"sensor9avg").
      withColumn("year", year($"start")).
      withColumn("month", month($"start"))
    
    /* alerting - not used in this example */
    //spark.conf.get("spark.sql.caseSensitive") by default its false
    val dfalert = df.
      filter($"endofcycle" === 1 && $"sensor11" > 600).
      withColumn("message", concat(lit("temperature too high "),$"sensor11"))

    /* storing output */
    // the working folder is partitioned by year and month, so after the month ends, 
    // run the compaction job to compact the files in that month of the year to its destination folder
    val query = dfagg.
      writeStream.
      format("parquet").
      partitionBy("year", "month").
      trigger(Trigger.ProcessingTime(Duration(triggerInterval))). //trigger controls how often to read from Kafka
      option("path", workingDir).
      option("checkpointLocation",devicelogCheckpointDir).  //checkpoint controls offset to read from
      start
    
    /* also push to another topic for visualization */
    val querybi = dfagg.
      select($"deviceid".alias("key"), 
          concat(lit("{\"deviceid\":\""), $"deviceid", 
                 lit("\",\"readat\":\""), $"start", 
                 lit("\",\"sensor9\":"), $"sensor9avg", 
                 lit("}")).alias("value")).
      selectExpr("CAST(key as STRING)", "CAST(value AS STRING)").
      writeStream.
      format("kafka").
      option("kafka.bootstrap.servers", kafkaBrokers).
      option("topic", "bi").
      option("checkpointLocation", biCheckpointDir).
      start

    // Append mode only works with watermark, and will only produce outputs when 
    // max seen event time - watermark > end of the evaluated time window. Append mode is the default.
    // If you don't see results in console sink, try Update or Complete mode
    //    val query = dfagg.writeStream.format("console").outputMode(OutputMode.Update).option("truncate", false).start

    query.awaitTermination
    querybi.awaitTermination()
  }  
}  
