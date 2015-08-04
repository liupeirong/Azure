package com.sparkeh

import org.apache.spark._
import org.apache.spark.SparkContext._
import org.apache.spark.SparkConf
import org.apache.spark.streaming.eventhubs.EventHubsUtils
import org.apache.spark.streaming.{Minutes, Seconds, StreamingContext}

object sparkeh {
  def main(args: Array[String]) {
  val outputDir = "ehoutput"
  val ehParams = Map[String, String](
      "eventhubs.policyname" -> "manage",
      "eventhubs.policykey" -> "Gv2FwJJXNAumGmh76bjZjqmxdGRgD0X9YVY2Ud+fGE4=",
      "eventhubs.namespace" -> "clouderademo",
      "eventhubs.name" -> "stratahub",
      "eventhubs.partition.count" -> "4",
      "eventhubs.consumergroup" -> "stratahubcg1",
      "eventhubs.checkpoint.dir" -> "ehcheckpoint",
      "eventhubs.checkpoint.interval" -> "10"
    )
    
   val partitionCount = ehParams("eventhubs.partition.count").toInt
   val sparkConf = new SparkConf().setAppName("sparkeh").set("spark.cores.max",
      (partitionCount*2).toString)   
      
   val ssc =  new StreamingContext(sparkConf, Seconds(5))
   ssc.checkpoint(ehParams("eventhubs.checkpoint.dir"))
   
   //val stream = EventHubsUtils.createUnionStream(ssc, ehParams)
   val stream = EventHubsUtils.createStream(ssc, ehParams, "0")
   stream.checkpoint(Seconds(ehParams("eventhubs.checkpoint.interval").toInt))
   
   val counts = stream.count()
   counts.saveAsTextFiles(outputDir)
   counts.print()
   
   ssc.start()
   ssc.awaitTermination()
  }
}