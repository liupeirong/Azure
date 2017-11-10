package org.pliu.iot.bi

import org.apache.spark.sql.SparkSession
import org.apache.spark.sql.functions._
import org.apache.spark.sql.types._
import org.apache.spark.sql.streaming.{Trigger, OutputMode}
import scala.concurrent.duration._
import com.typesafe.config._

object stream2powerbi {
  def main(args: Array[String]): Unit = {
    val appconf = ConfigFactory.load("iotbi")
    val kafkaBrokers = appconf.getString("iotbi.kafkaBrokers")
    val kafkaTopic = appconf.getString("iotbi.kafkaTopic")
    val maxOffsetsPerTrigger = appconf.getInt("iotbi.maxOffsetsPerTrigger")
    val triggerInterval = appconf.getString("iotbi.triggerInterval")
    val biSinkCheckpointDir = appconf.getString("iotbi.biSinkCheckpointDir")
    val pbiUrl = appconf.getString("iotbi.pbiUrl")
    
    val spark = SparkSession
      .builder
      .appName("iotbi")
      .getOrCreate
    import spark.implicits._
    
    val dfraw = spark.readStream.
      format("kafka").
      option("kafka.bootstrap.servers", kafkaBrokers).
      option("subscribe", kafkaTopic). 
      option("startingOffsets", "earliest").
      option("maxOffsetsPerTrigger", maxOffsetsPerTrigger).
      load
    
    val query = dfraw.select($"value".cast(StringType)).as[(String)].
      writeStream.
      foreach(new PowerBISinkForeach(pbiUrl)).
      start
  } 
}