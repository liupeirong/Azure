/*
 * Issues: 
 * 1. using --properties-file causes jdbc call to hang, so specifying conf in commandline
 * 2. putting libraries in hdfs doesn't work, must be on each node
 * 3. mvn package produces a monolithic jar and a minimum(original) jar, use the minimum jar if additional jars 
 *    are already on each node
 * 4. "Configure Build Path" -> "Scala Compiler" -> Use 2.11 to be compatible with Spark 2.0
 * 
 * run spark-shell: 
 * spark-shell --master yarn --deploy-mode client --executor-cores 2 -usejavacp 
 * --jars /opt/libs/azure-eventhubs-0.9.0.jar,/opt/libs/proton-j-0.15.0.jar 
 * --driver-class-path /opt/libs/sqljdbc4.jar 
 * --conf spark.executor.extraClassPath=/opt/libs/sqljdbc4.jar 
 * --conf spark.myapp.sqlcxnstring="jdbc:sqlserver://yourserver.database.windows.net:1433;database=yourdb;" 
 * --conf spark.myapp.sqluser=youruser@yourserver 
 * --conf spark.myapp.sqlpassword=yourpassword
 * --conf spark.myapp.tag=DS14 
 * --conf spark.executorEnv.eventHubsNS=yourhubnamespace 
 * --conf spark.executorEnv.eventHubsName=yourhubname 
 * --conf spark.executorEnv.policyName=yourpolicy 
 * --conf spark.executorEnv.policyKey="yourkey"
 * 
 * run spark-submit:
 * spark-submit --master yarn --deploy-mode client --executor-cores 2 
 * --jars /opt/libs/azure-eventhubs-0.9.0.jar,/opt/libs/proton-j-0.15.0.jar 
 * --driver-class-path /opt/libs/sqljdbc4.jar 
 * --conf spark.executor.extraClassPath=/opt/libs/sqljdbc4.jar 
 * --conf spark.myapp.sqlcxnstring="jdbc:sqlserver://yourserver.database.windows.net:1433;database=yourdb;" 
 * --conf spark.myapp.sqluser=youruser@yourserver 
 * --conf spark.myapp.sqlpassword=yourpassword 
 * --conf spark.myapp.tag=DS14 
 * --conf spark.executorEnv.eventHubsNS=yourhubnamespace 
 * --conf spark.executorEnv.eventHubsName=yourhubname
 * --conf spark.executorEnv.policyName=yourpolicy 
 * --conf spark.executorEnv.policyKey="yourkey" 
 * --class com.pliu.sql.eventhub.example.Sql2EventHub /tmp/original-com-pliu-sql-eventhub-example-0.01.jar
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

object EventHub2Sql {
  def main(args: Array[String]): Unit = {
    val conf = new SparkConf().setAppName("EventHub2SQL")
    conf.
      set("spark.streaming.stopGracefullyOnShutdown","true").
      set("spark.streaming.backpressure.enabled","true").
      set("spark.streaming.blockInterval","1000ms").
      set("spark.streaming.receiver.writeAheadLog.enable","true")
    val sc = new SparkContext(conf);
    //if spark-shell use
    //val conf = sc.getConf
    
    //read app configuration
    val appConf = conf.get("spark.myapp.conf")
    val pt: Path = new Path(appConf)
    val fs: FileSystem = FileSystem.get(sc.hadoopConfiguration)
    val appConfStream = fs.open(pt)
    val appProps = new Properties()
    appProps.load(appConfStream)
    
    val sparkCheckpointDir = appProps.getProperty("checkpointDir") + "spark"
    val eventhubCheckpointDir = appProps.getProperty("checkpointDir") + "eventhub"
    val streamWindowSeconds: Int = if (appProps.containsKey("streamwindowseconds")) appProps.getProperty("streamwindowseconds").toInt else 60
    val runForMinutes: Int = if (appProps.containsKey("runforminutes")) appProps.getProperty("runforminutes").toInt else 60
    println("runForMinutes " + runForMinutes.toString)
    
    val eventhubParameters = Map[String, String](
      "eventhubs.policyname" -> appProps.getProperty("policyName"),
      "eventhubs.policykey" -> appProps.getProperty("policyKey"),
      "eventhubs.namespace" -> appProps.getProperty("eventHubsNS"),
      "eventhubs.name" -> appProps.getProperty("eventHubsName"),
      "eventhubs.partition.count" -> appProps.getProperty("partitionCount"), //executor core count must be twice that of partition count
      "eventhubs.consumergroup" -> appProps.getProperty("consumerGroup"),
      "eventhubs.checkpoint.dir" -> eventhubCheckpointDir, 
      "eventhubs.checkpoint.interval" -> streamWindowSeconds.toString)
    
    val ssc = new StreamingContext(sc, Seconds(streamWindowSeconds))     
    ssc.checkpoint(sparkCheckpointDir)
    
    val stream = EventHubsUtils.createUnionStream(ssc, eventhubParameters)
    val lines = stream.map(msg => new String(msg))
    
    lines.foreachRDD {rdd => 
      if (!rdd.isEmpty())
      {
        val sqlCxnString = sys.env("sqlcxnstring")   
        val sqlUser = sys.env("sqluser")   
        val sqlPassword = sys.env("sqlpassword") 
        val targetTable = sys.env("targetTable") 
        val targetDatalake = sys.env("targetDatalake")
  
        val spark = SparkSession.builder.config(rdd.sparkContext.getConf).getOrCreate()
        import spark.implicits._
        val jdbcProp = new java.util.Properties 
        jdbcProp.setProperty("user", sqlUser)
        jdbcProp.setProperty("password", sqlPassword)
        val utcDateTime = Instant.now.toString
  
        val myDF = spark.read.json(rdd).toDF.withColumn("consumedat", lit(utcDateTime))
        
        try {
          myDF.write.mode(SaveMode.Append).jdbc(sqlCxnString, targetTable, jdbcProp)
          //myDF.write.mode(SaveMode.Append).csv(targetDatalake)
        } catch {
          case e: Throwable => println(e.getMessage() + " -- ignore")
        }
      }
      else {
        println ("nothing to save")
      }
    }
    
    ssc.start()
    ssc.awaitTerminationOrTimeout(runForMinutes * 60 * 1000)
  }
}