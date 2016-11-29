/*
 * Issues: 
 * 1. using --properties-file causes jdbc call to hang, so specifying conf in commandline
 * 2. putting libraries in hdfs doesn't work, must be on each node
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
import java.util.Properties;
import org.apache.hadoop.fs.FileSystem;
import org.apache.hadoop.fs.Path;
import org.apache.hadoop.conf.Configuration;

//
//defining a static object will work in spark-shell but not spark-submit, because the closure will bind
//to the static object in the executor's context, which was never initialized
//http://stackoverflow.com/questions/34859468/scala-class-lazy-val-variables-strange-behaviour-with-spark
//
class GlobalConfigDef extends Serializable {
  var sqlCxnString= ""  
  var sqlUser= ""  
  var sqlPassword= ""  
  var tag = ""  
  var targetTable= ""  
  var targetTableKey= ""  
  var lastReadFile= ""  
  var runForMinutes: Int = 60  
  var eventHubsNamespace= ""  
  var eventHubsName= ""  
  var policyName= ""  
  var policyKey= ""  
  var readall: Boolean = false
  var eventHubCxnString = ""
  def loadConfig(appProps: Properties): Unit = {
    sqlCxnString = appProps.getProperty("sqlCxnString")  
    sqlUser = appProps.getProperty("sqlUser")  
    sqlPassword = appProps.getProperty("sqlPassword")
    tag = appProps.getProperty("tag")
    targetTable = appProps.getProperty("targetTable")
    targetTableKey = appProps.getProperty("targetTableKey")
    lastReadFile = appProps.getProperty("lastReadFile")
    runForMinutes = appProps.getProperty("runForMinutes").toInt
    eventHubsNamespace = appProps.getProperty("eventHubsNamespace")
    eventHubsName = appProps.getProperty("eventHubsName")
    policyName = appProps.getProperty("policyName")
    policyKey = appProps.getProperty("policyKey")
    eventHubCxnString = new com.microsoft.azure.servicebus.ConnectionStringBuilder(eventHubsNamespace, eventHubsName, policyName, policyKey).toString
    readall = if (appProps.containsKey("readall")) appProps.getProperty("readall") == "1" else false
  }
}

object Sql2EventHub {
  def main(args: Array[String]): Unit = {
    val spark = SparkSession.builder().appName("SQL2EventHub").getOrCreate()
    import spark.implicits._
    val appConf = spark.conf.get("spark.myapp.conf")
    val pt: Path = new Path(appConf)
    val fs: FileSystem = FileSystem.get(spark.sparkContext.hadoopConfiguration)
    val appConfStream = fs.open(pt)
    val appProps = new Properties()
    appProps.load(appConfStream)

    val GlobalConfig = new GlobalConfigDef()
    GlobalConfig.loadConfig(appProps)
    
    //read the last id stored in hdfs, if none, select everything, otherwise, select newer ones
    var lastread: Int = -1
    if (!GlobalConfig.readall)
    {
      try {
        val lastreadDF = spark.read.load(GlobalConfig.lastReadFile)
        lastread = lastreadDF.first().getInt(0)
      } catch {
        case _: Throwable => println("read sql from beginning")
      }
    }

    var millisecToRun: Int = GlobalConfig.runForMinutes * 60 * 1000;
    val partition: Int = spark.conf.get("spark.executor.cores").toInt * spark.conf.get("spark.executor.instances").toInt
    
    //put GlobalConfig in a Closure, so that it's serialized to executors
    //   http://stackoverflow.com/questions/30181582/spark-use-the-global-config-variables-in-executors
    //if you define a function at the partition level, it won't work, possibly because it binds to GlobalConfig in the executor's context
    //    val processPartitionFunc = (p: Iterator[Array[Byte]]) => {
    //      val eventHubsClient: EventHubClient = EventHubClient.createFromConnectionString(GlobalConfig.getConnectionString()).get
    //      p.foreach {s => 
    //        val e = new EventData(s)
    //        eventHubsClient.sendSync(e)
    //      }
    //    }
    val processDSFunc = (ds: org.apache.spark.sql.Dataset[Array[Byte]]) => {
      ds.foreachPartition { p => 
          val eventHubsClient: EventHubClient = EventHubClient.createFromConnectionString(GlobalConfig.eventHubCxnString).get
          p.foreach {s => 
            val e = new EventData(s)
            eventHubsClient.sendSync(e)
          }
      }
    }
    
    val jdbcDFReader = spark.read.
      format("jdbc").
      option("url", GlobalConfig.sqlCxnString).
      option("user", GlobalConfig.sqlUser).
      option("password", GlobalConfig.sqlPassword)
    
    while (millisecToRun > 0) 
    {
      val myDF = jdbcDFReader.
        option("dbtable", "(select * from " + GlobalConfig.targetTable + " where " + GlobalConfig.targetTableKey + " > " + lastread + ") as intable").
//        spark will natively divide the column value by the partition count to set the range, so you end up all in one partition
//        option("partitionColumn", GlobalConfig.targetTableKey)).
//        option("lowerBound", lastread.toString).
//        option("upperBound", "1000000000").
//        option("numPartitions", partition.toString).
        load()
      
      try {
        //is there anything to read?
        myDF.first()
        
        // jdbc selects a datetime column as timestamp, 
        // toJSON converts timestamp to "2016-11-22T18:28:54.350+00:00" which is not convertible back to datetime in sql, 
        // hence the conversion below
        // also add tag and produce date
        val utcDateTime = Instant.now.toString
        val eventPayload = myDF.
          withColumn("createdat", myDF("createdat").cast(StringType)).
          withColumn("tag", lit(GlobalConfig.tag)).
          withColumn("publishedat", lit(utcDateTime)).
          toJSON.
          map(m => m.getBytes()).
          repartition(partition)

        processDSFunc(eventPayload)
        
        lastread = myDF.agg(max(GlobalConfig.targetTableKey)).first().getInt(0)
        spark.sql("select " + lastread).write.mode(SaveMode.Overwrite).parquet(GlobalConfig.lastReadFile)
      } catch {
        case emptydf: NoSuchElementException => println("nothing to read")
      }
            
      Thread.sleep(10000)
      millisecToRun -= 10000
    }
  }
}