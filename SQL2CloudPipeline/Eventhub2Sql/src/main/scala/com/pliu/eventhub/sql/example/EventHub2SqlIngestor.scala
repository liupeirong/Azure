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
  var sparkCheckpointDir = ""
  var eventhubCheckpointDir = ""
  var streamWindowSeconds: Int = 2
  var partitionCount = ""
  var consumerGroup = ""
  var targetDatalake = ""
  def loadConfig(appProps: java.util.Properties): Unit = {
    sqlCxnString = appProps.getProperty("destSqlCxnString")  
    sqlUser = appProps.getProperty("destSqlUser")  
    sqlPassword = appProps.getProperty("destSqlPassword")
    targetTable = appProps.getProperty("destTargetTable")
    lastReadFile = appProps.getProperty("lastReadFile")
    runForMinutes = appProps.getProperty("runForMinutes").toInt
    eventHubsNamespace = appProps.getProperty("eventHubsNamespace")
    eventHubsName = appProps.getProperty("eventHubsName")
    policyName = appProps.getProperty("policyName")
    policyKey = appProps.getProperty("policyKey")
    eventHubCxnString = new com.microsoft.azure.servicebus.ConnectionStringBuilder(eventHubsNamespace, eventHubsName, policyName, policyKey).toString
    sparkCheckpointDir = appProps.getProperty("checkpointDir") + "spark"
    eventhubCheckpointDir = appProps.getProperty("checkpointDir") + "eventhub"
    streamWindowSeconds = if (appProps.containsKey("streamWindowSeconds")) appProps.getProperty("streamWindowSeconds").toInt else 10
    partitionCount = appProps.getProperty("partitionCount")
    consumerGroup = appProps.getProperty("consumerGroup")
    targetDatalake = appProps.getProperty("destTargetDatalake")
  }
}


object EventHub2Sql {
  def main(args: Array[String]): Unit = {
    val conf = new SparkConf().setAppName("EventHub2SQL")
    conf.
      set("spark.streaming.stopGracefullyOnShutdown","true").
      //set("spark.streaming.backpressure.enabled","true").
      //set("spark.streaming.blockInterval","1000ms").
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
    val GlobalConfig = new GlobalConfigDef()
    GlobalConfig.loadConfig(appProps)
    
    val eventhubParameters = Map[String, String](
      "eventhubs.policyname" -> GlobalConfig.policyName,
      "eventhubs.policykey" -> GlobalConfig.policyKey,
      "eventhubs.namespace" -> GlobalConfig.eventHubsNamespace,
      "eventhubs.name" -> GlobalConfig.eventHubsName,
      "eventhubs.partition.count" -> GlobalConfig.partitionCount, //executor core count must be twice that of partition count
      "eventhubs.consumergroup" -> GlobalConfig.consumerGroup,
      "eventhubs.checkpoint.dir" -> GlobalConfig.eventhubCheckpointDir, 
      "eventhubs.checkpoint.interval" -> GlobalConfig.streamWindowSeconds.toString)
    
    val ssc = new StreamingContext(sc, Seconds(GlobalConfig.streamWindowSeconds))     
    ssc.checkpoint(GlobalConfig.sparkCheckpointDir)
    
    val stream = EventHubsUtils.createUnionStream(ssc, eventhubParameters) //DStrem[Array[Byte]]
    val lines = stream.map(msg => new String(msg))
    
    lines.foreachRDD {rdd => 
      if (!rdd.isEmpty())
      {
        val sqlCxnString = GlobalConfig.sqlCxnString   
        val sqlUser = GlobalConfig.sqlUser   
        val sqlPassword = GlobalConfig.sqlPassword 
        val targetTable = GlobalConfig.targetTable 
        val targetDatalake = GlobalConfig.targetDatalake
  
        val jdbcProp = new java.util.Properties 
        jdbcProp.setProperty("user", sqlUser)
        jdbcProp.setProperty("password", sqlPassword)
        val utcDateTime = Instant.now.toString
  
        val spark = SparkSession.builder.config(rdd.sparkContext.getConf).getOrCreate()
        import spark.implicits._
        val myDF = spark.read.json(rdd).toDF.withColumn("consumedat", lit(utcDateTime))
        
        try {
          myDF.write.mode(SaveMode.Append).jdbc(sqlCxnString, targetTable, jdbcProp)
          //myDF.write.mode(SaveMode.Append).csv(targetDatalake)
        } catch {
          case e: Throwable => println(e.getMessage() + " -- ignore")
        }
      }
      else
      {
        println ("nothing to save")
      }
    }
    
    ssc.start()
    ssc.awaitTerminationOrTimeout(GlobalConfig.runForMinutes * 60 * 1000)
  }
}