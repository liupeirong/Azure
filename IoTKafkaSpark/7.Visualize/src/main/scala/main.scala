package org.pliu.iot.bi

import org.apache.spark.sql.SparkSession
import org.apache.spark.sql.functions._
import org.apache.spark.sql.types._
import org.apache.spark.sql.streaming.{Trigger, OutputMode}
import scala.concurrent.duration._
import com.typesafe.config._
import org.apache.log4j.{Level, LogManager}
import org.apache.spark.sql.ForeachWriter
import org.apache.http.client.methods.HttpPost
import org.apache.http.impl.client.DefaultHttpClient
import org.apache.http.entity.StringEntity

object stream2powerbi {
  def main(args: Array[String]): Unit = {
    val appconf = ConfigFactory.load("bi")
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
    
    val powerbiForeachSink = new ForeachWriter[String] {
     override def open(partitionId: Long, version: Long): Boolean = { 
       true 
     }
     override def process(record: String) = {
       val bilog = LogManager.getLogger("org.pliu.iot")
       val body = new StringEntity("[" + record + "]")
       val post = new HttpPost(pbiUrl)
       post.addHeader("Content-Type", "application/json")
       post.setEntity(body)
       val httpClient = new DefaultHttpClient
       val resp = httpClient.execute(post)
       if (resp.getStatusLine.getStatusCode != 200) 
         bilog.error("Failed to send data to PowerBI: " + resp.getStatusLine.getReasonPhrase)
     }
     override def close(errorOrNull: Throwable): Unit = {} 
    }
    
    val dfraw = spark.readStream.
      format("kafka").
      option("kafka.bootstrap.servers", kafkaBrokers).
      option("subscribe", kafkaTopic). 
      option("startingOffsets", "earliest").
      option("maxOffsetsPerTrigger", maxOffsetsPerTrigger).
      load
    
    val query = dfraw.select($"value".cast(StringType)).as[(String)].
      writeStream.
      foreach(powerbiForeachSink).
      trigger(Trigger.ProcessingTime(Duration(triggerInterval))).
      option("checkpointLocation", biSinkCheckpointDir).
      queryName("push2powerbi").
      start
  } 
}