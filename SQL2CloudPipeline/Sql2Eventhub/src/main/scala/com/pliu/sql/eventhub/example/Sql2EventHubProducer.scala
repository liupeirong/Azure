/*
 * Issues: 
 * 1. using --properties-file causes jdbc call to hang, so specifying conf in commandline
 * 2. putting libraries in hdfs doesn't work in spark-shell, only works in cluster mode. Libraries must be on each node
 * 3. mvn package produces a monolithic jar and a minimum(original) jar, use the minimum jar if additional jars 
 *    are already on each node
 * 4. "Configure Build Path" -> "Scala Compiler" -> Use 2.11 to be compatible with Spark 2.0
 * 
 */

package com.pliu.sql.eventhub.example
import org.apache.spark.sql.SparkSession
import org.apache.spark.sql.SaveMode
import org.apache.spark.sql.types.StringType
import org.apache.spark.sql.functions._
import com.microsoft.azure.eventhubs.{EventData, EventHubClient}
import com.microsoft.azure.servicebus.ConnectionStringBuilder
import java.time._
import java.util.Properties
import org.apache.hadoop.fs.FileSystem
import org.apache.hadoop.fs.Path
import org.apache.hadoop.conf.Configuration

//
//defining a static object will work in spark-shell but not spark-submit, because the closure will bind
//to the static object in the executor's context, which was never initialized
//http://stackoverflow.com/questions/34859468/scala-class-lazy-val-variables-strange-behaviour-with-spark
//
case class GlobalConfig(appProps: java.util.Properties) {
  val sqlCxnString = appProps.getProperty("sqlCxnString")  
  val sqlUser = appProps.getProperty("sqlUser")  
  val sqlPassword = appProps.getProperty("sqlPassword")
  val tag = appProps.getProperty("tag")
  val targetTable = appProps.getProperty("targetTable")
  val targetTableKey = appProps.getProperty("targetTableKey")
  val lastReadFile = appProps.getProperty("lastReadFile")
  val runForMinutes = appProps.getProperty("runForMinutes").toInt
  val eventHubsNamespace = appProps.getProperty("eventHubsNamespace")
  val eventHubsName = appProps.getProperty("eventHubsName")
  val policyName = appProps.getProperty("policyName")
  val policyKey = appProps.getProperty("policyKey")
  val eventHubCxnString = new com.microsoft.azure.servicebus.ConnectionStringBuilder(eventHubsNamespace, eventHubsName, policyName, policyKey).toString
  val readall = if (appProps.containsKey("readall")) appProps.getProperty("readall") == "1" else false
}

object Sql2EventHub {
  def main(args: Array[String]): Unit = {
    val spark = SparkSession.builder().appName("SQL2EventHub").getOrCreate()
    import spark.implicits._
    val appConf = spark.conf.get("spark.myapp.conf")
    val fs = FileSystem.get(spark.sparkContext.hadoopConfiguration)
    val appProps = new java.util.Properties()
    appProps.load(fs.open(new Path(appConf)))
    val cfg = GlobalConfig(appProps)

    //read the last id stored in hdfs, if none, select everything, otherwise, select newer ones
    var lastread: Int = -1
    if (!cfg.readall)
    {
      try {
        val lastreadDF = spark.read.load(cfg.lastReadFile)
        lastread = lastreadDF.first().getInt(0)
      } catch {
        case _: Throwable => println("read sql from beginning")
      }
    }

    var millisecToRun: Int = cfg.runForMinutes * 60 * 1000;
    val partition: Int = spark.conf.get("spark.executor.cores").toInt * spark.conf.get("spark.executor.instances").toInt
    
    while (millisecToRun > 0) {
      val myDF = spark.read.
        format("jdbc").
        option("url", cfg.sqlCxnString).
        option("user", cfg.sqlUser).
        option("password", cfg.sqlPassword).
        option("dbtable", "(select * from " + cfg.targetTable + " where " + cfg.targetTableKey + " > " + lastread + ") as intable").
//        spark will natively divide the column value by the partition count to set the range, so you end up all in one partition
//        option("partitionColumn", cfg.targetTableKey)).
//        option("lowerBound", lastread.toString).
//        option("upperBound", "1000000000").
//        option("numPartitions", partition.toString).
        load
      
      try {
        //is there anything to read?
        myDF.first
        
        // jdbc selects a datetime column as timestamp, 
        // toJSON converts timestamp to "2016-11-22T18:28:54.350+00:00" which is not convertible back to datetime in sql, 
        // hence the conversion below
        // also add tag and produce date
        val utcDateTime = Instant.now.toString
        val eventPayload = myDF.
          withColumn("createdat", myDF("createdat").cast(StringType)).
          withColumn("tag", lit(cfg.tag)).
          withColumn("publishedat", lit(utcDateTime)).
          toJSON.
          map(_.getBytes).
          repartition(partition)

        //if you have variable for DataFrameReader, and then call load() on the reader to get the DataFrame, the following
        //foreachPartition will fail on Serialization error as it will attempt to serialize the DataFrameReader
        eventPayload.foreachPartition { p => 
          val eventHubsClient: EventHubClient = EventHubClient.createFromConnectionString(cfg.eventHubCxnString).get
          p.foreach {s => 
            val e = new EventData(s)
            eventHubsClient.sendSync(e)
          }
        }
        
        lastread = myDF.agg(max(cfg.targetTableKey)).first().getInt(0)
        spark.sql("select " + lastread).write.mode(SaveMode.Overwrite).parquet(cfg.lastReadFile)
      } catch {
        case emptydf: NoSuchElementException => println("nothing to read")
      }
            
      Thread.sleep(10000)
      millisecToRun -= 10000
    }
  }
}