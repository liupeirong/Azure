/*
 * Issues: 
 * 1. using --properties-file causes jdbc call to hang, so specifying conf in commandline
 * 2. putting libraries in hdfs doesn't work in spark-shell, only works in cluster mode. Libraries must be on each node
 * 3. mvn package produces a monolithic jar and a minimum(original) jar, use the minimum jar if additional jars 
 *    are already on each node
 * 4. "Configure Build Path" -> "Scala Compiler" -> Use 2.11 to be compatible with Spark 2.0
 * 
 */

package com.pliu.eventhub.sql.example
import org.apache.spark.sql.SparkSession
import org.apache.spark.sql.SaveMode
import org.apache.spark.sql.functions._
import org.apache.spark._
import org.apache.spark.streaming.eventhubs.EventHubsUtils
import org.apache.spark.streaming.{Seconds, StreamingContext}
import java.time._
import java.util.Properties;
import org.apache.hadoop.fs.FileSystem;
import org.apache.hadoop.fs.Path;

case class GlobalConfig (appProps: java.util.Properties) {
  val sqlCxnString = appProps.getProperty("destSqlCxnString")  
  val sqlUser = appProps.getProperty("destSqlUser")  
  val sqlPassword = appProps.getProperty("destSqlPassword")
  val targetTable = appProps.getProperty("destTargetTable")
  val lastReadFile = appProps.getProperty("lastReadFile")
  val runForMinutes = appProps.getProperty("runForMinutes").toInt
  val eventHubsNamespace = appProps.getProperty("eventHubsNamespace")
  val eventHubsName = appProps.getProperty("eventHubsName")
  val policyName = appProps.getProperty("policyName")
  val policyKey = appProps.getProperty("policyKey")
  val eventHubCxnString = new com.microsoft.azure.servicebus.ConnectionStringBuilder(eventHubsNamespace, eventHubsName, policyName, policyKey).toString
  val sparkCheckpointDir = appProps.getProperty("checkpointDir") + "spark"
  val eventhubCheckpointDir = appProps.getProperty("checkpointDir") + "eventhub"
  val streamWindowSeconds = if (appProps.containsKey("streamWindowSeconds")) appProps.getProperty("streamWindowSeconds").toInt else 10
  val partitionCount = appProps.getProperty("partitionCount")
  val consumerGroup = appProps.getProperty("consumerGroup")
  val targetDatalake = appProps.getProperty("destTargetDatalake")
}

object EventHub2Sql {
  def main(args: Array[String]): Unit = {
    //if spark-shell use
    //val conf = sc.getConf
    val conf = new SparkConf().setAppName("EventHub2SQL")
    conf.
      set("spark.streaming.stopGracefullyOnShutdown","true").
      //set("spark.streaming.backpressure.enabled","true").
      //set("spark.streaming.blockInterval","1000ms").
      set("spark.streaming.receiver.writeAheadLog.enable","true")
    val sc = new SparkContext(conf);
    
    //read app configuration
    val appConf = conf.get("spark.myapp.conf")
    val fs = FileSystem.get(sc.hadoopConfiguration)
    val appProps = new Properties()
    appProps.load(fs.open(new Path(appConf)))
    val cfg = GlobalConfig(appProps)
    
    val eventhubParameters = Map[String, String](
      "eventhubs.policyname" -> cfg.policyName,
      "eventhubs.policykey" -> cfg.policyKey,
      "eventhubs.namespace" -> cfg.eventHubsNamespace,
      "eventhubs.name" -> cfg.eventHubsName,
      "eventhubs.partition.count" -> cfg.partitionCount, //executor core count must be twice that of partition count
      "eventhubs.consumergroup" -> cfg.consumerGroup,
      "eventhubs.checkpoint.dir" -> cfg.eventhubCheckpointDir, 
      "eventhubs.checkpoint.interval" -> cfg.streamWindowSeconds.toString)
    
    //@transient is needed if checkpoint as checkpoint is trying to serialize streamingContext
    @transient val ssc = new StreamingContext(sc, Seconds(cfg.streamWindowSeconds))      
    ssc.checkpoint(cfg.sparkCheckpointDir)
    
    val stream = EventHubsUtils.createUnionStream(ssc, eventhubParameters) //DStrem[Array[Byte]]
    val lines = stream.map(msg => new String(msg))
    
    lines.foreachRDD {rdd => 
      if (!rdd.isEmpty)
      {
        val sqlCxnString = cfg.sqlCxnString   
        val sqlUser = cfg.sqlUser   
        val sqlPassword = cfg.sqlPassword 
        val targetTable = cfg.targetTable 
        val targetDatalake = cfg.targetDatalake
  
        val jdbcProp = new java.util.Properties 
        jdbcProp.setProperty("user", sqlUser)
        jdbcProp.setProperty("password", sqlPassword)
        val utcDateTime = Instant.now.toString
  
        val spark = SparkSession.builder.config(rdd.sparkContext.getConf).getOrCreate
        import spark.implicits._
        val myDF = spark.read.json(rdd).toDF.withColumn("consumedat", lit(utcDateTime))
        
        try {
          myDF.write.mode(SaveMode.Append).jdbc(sqlCxnString, targetTable, jdbcProp)
          //myDF.write.mode(SaveMode.Append).csv(targetDatalake)
        } catch {
          case e: Throwable => println(e.getMessage + " -- ignore")
        }
      }
      else
      {
        println ("nothing to save")
      }
    }
    
    ssc.start
    ssc.awaitTerminationOrTimeout(cfg.runForMinutes * 60 * 1000)
  }
}