package com.pliu.logconsumerrt

import org.apache.spark.SparkContext
import org.apache.spark.sql.SparkSession
import org.apache.spark.streaming.{Seconds, StreamingContext}
import org.apache.spark.streaming.eventhubs.EventHubsUtils
import org.apache.spark.sql.functions._
import java.util.Calendar
import java.util.TimeZone
import org.apache.http._
import org.apache.http.client._
import org.apache.http.client.methods.HttpPost
import org.apache.http.impl.client.DefaultHttpClient
import org.apache.http.entity.StringEntity
import com.typesafe.config._

object GeoLogConsumerRT {
  def main(args: Array[String]): Unit = {
    val appconf = ConfigFactory.load("logConsumerRT")
    val eventHubNameSpace = appconf.getString("logConsumerRT.eventHubNameSpace")
    val eventHubName = appconf.getString("logConsumerRT.eventHubName")
    val eventHubPolicyName = appconf.getString("logConsumerRT.eventHubPolicyName")
    val eventHubPolicyKey = appconf.getString("logConsumerRT.eventHubPolicyKey")
    val eventHubPartitionCount = appconf.getString("logConsumerRT.eventHubPartitionCount")
    val eventHubConsumerGroup = appconf.getString("logConsumerRT.eventHubConsumerGroup")
    val progressDir = appconf.getString("logConsumerRT.progressDir")
    val pbiUrl = appconf.getString("logConsumerRT.pbiUrl")
    val streamWindowInSeconds = appconf.getInt("logConsumerRT.streamWindowInSeconds")
    
    val eventhubParameters = Map[String, String] (
      "eventhubs.policyname" -> eventHubPolicyName,
      "eventhubs.policykey" -> eventHubPolicyKey,
      "eventhubs.namespace" -> eventHubNameSpace,
      "eventhubs.name" -> eventHubName,
      "eventhubs.partition.count" -> eventHubPartitionCount,
      "eventhubs.consumergroup" -> eventHubConsumerGroup
    )

    def pushToPowerBI(s: String): Unit = {
      val body = new StringEntity(s)
      val post = new HttpPost(pbiUrl)
      post.addHeader("Content-Type", "application/json")
      post.setEntity(body)
      val httpClient = new DefaultHttpClient
      val resp = httpClient.execute(post)
      if (resp.getStatusLine.getStatusCode != 200) 
      {
        println("Failed to send data to PowerBI: " + resp.getStatusLine.getReasonPhrase);
      }
    }
    
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
        val salesDF = rawDF
                    .filter($"eventType" === "itemPurchased")
                    .withColumn("eventtsraw", rawDF("eventTime").cast("timestamp")).drop(rawDF("eventTime"))
        val purchaseList = salesDF
                    .select(date_format($"eventtsraw", "yyyy-MM-dd HH:mm:ss").alias("eventts"), 
                            ($"eventParams.quantity" * $"eventParams.price").alias("sales"))
                    .groupBy("eventts").agg(sum("sales").alias("sales"))
                    .orderBy("eventts")
                    .toJSON.collect()
        val purchases = purchaseList.mkString("[", ",", "]")
        println(purchases)
        pushToPowerBI(purchases)
      } catch {
        case e: Exception => println(e.getMessage())
      }
    }

    ssc.start
    ssc.awaitTermination()
  }  
}