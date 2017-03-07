package com.pliu.logconsumerbatch

import org.apache.spark.SparkContext
import org.apache.spark.sql.SparkSession
import org.apache.spark.streaming.{Seconds, StreamingContext}
import org.apache.spark.streaming.eventhubs.EventHubsUtils
import org.apache.spark.sql.SQLContext
import org.apache.spark.sql.SaveMode
import org.apache.spark.sql.functions._
import java.util.Calendar
import java.util.TimeZone
import org.apache.http._
import org.apache.http.client._
import org.apache.http.client.methods.HttpPost
import org.apache.http.impl.client.DefaultHttpClient
import org.apache.http.entity.StringEntity
import com.typesafe.config._

object GeoLogConsumerBatch {
  def main(args: Array[String]): Unit = {
    val appconf = ConfigFactory.load("logBatch")
    val eventHubNameSpace = appconf.getString("logBatch.eventHubNameSpace")
    val eventHubName = appconf.getString("logBatch.eventHubName")
    val eventHubPolicyName = appconf.getString("logBatch.eventHubPolicyName")
    val eventHubPolicyKey = appconf.getString("logBatch.eventHubPolicyKey")
    val eventHubPartitionCount = appconf.getString("logBatch.eventHubPartitionCount")
    val eventHubConsumerGroup = appconf.getString("logBatch.eventHubConsumerGroup")
    val progressDir = appconf.getString("logBatch.progressDir")
    val streamWindowInSeconds = appconf.getInt("logBatch.streamWindowInSeconds")
    val hdfsOutputBaseDir = appconf.getString("logBatch.hdfsOutputBaseDir")
    
    val geoDir = hdfsOutputBaseDir + "/geo"
    val purchaseDir = hdfsOutputBaseDir + "/purchase"
    val levelDir = hdfsOutputBaseDir + "/level"
    
    val eventhubParameters = Map[String, String] (
      "eventhubs.policyname" -> eventHubPolicyName,
      "eventhubs.policykey" -> eventHubPolicyKey,
      "eventhubs.namespace" -> eventHubNameSpace,
      "eventhubs.name" -> eventHubName,
      "eventhubs.partition.count" -> eventHubPartitionCount,
      "eventhubs.consumergroup" -> eventHubConsumerGroup
    )

    val sc = new SparkContext
    val ssc = new StreamingContext(sc, Seconds(streamWindowInSeconds))
    val stream = EventHubsUtils.createDirectStreams(
      ssc,
      eventHubNameSpace,
      progressDir,
      Map(eventHubName -> eventhubParameters))
    val lines = stream.map(msg => new String(msg.getBody))

    lines.foreachRDD { rdd => 
      val spark = SparkSession.builder.getOrCreate
      import spark.implicits._
      
      try {
        val rawDF = spark.read.json(rdd)
        val tsDF = rawDF
                    .withColumn("eventts", rawDF("eventTime").cast("timestamp")).drop(rawDF("eventTime"))
                    .createOrReplaceTempView("logs")
        val gameStart = spark.sql("select eventts, playerId, sessionId, eventParams.city, eventParams.lat, eventParams.lon from logs where eventType = 'gameStart' ")
        val itemPurchased = spark.sql("select eventts, playerId, sessionId, eventParams.item, eventParams.quantity, eventParams.price from logs where eventType = 'itemPurchased' ")
        val levelReached = spark.sql("select eventts, playerId, sessionId, eventParams.level from logs where eventType = 'levelReached' ")
        gameStart.show
        gameStart.write.mode(SaveMode.Append).parquet(geoDir)
        itemPurchased.show
        itemPurchased.write.mode(SaveMode.Append).parquet(purchaseDir)
        levelReached.show
        levelReached.write.mode(SaveMode.Append).parquet(levelDir)
      } catch {
        case e: Exception => println(e.getMessage())
      }
    }
    
    ssc.start
    ssc.awaitTermination()
  }  
}