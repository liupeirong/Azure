/*
 * Issues: 
 * 1. using --properties-file causes jdbc call to hang, so specifying conf in commandline
 * 2. putting libraries in hdfs doesn't work, must be on each node
 * 3. mvn package produces a monolithic jar and a minimum(original) jar, use the minimum jar if additional jars 
 *    are already on each node
 * 4. "Configure Build Path" -> "Scala Compiler" -> Use 2.11 to be compatible with Spark 2.0
 * 
 */

package com.pliu.kafka.sql.example
import org.apache.spark.SparkConf
import org.apache.spark.SparkContext
import org.apache.spark.sql.SparkSession
import org.apache.spark.sql.SaveMode
import org.apache.spark.sql.functions._
import org.apache.kafka.clients.consumer.ConsumerRecord
import org.apache.kafka.common.serialization.StringDeserializer
import org.apache.kafka.common.serialization.ByteArrayDeserializer
import org.apache.spark.streaming.kafka010._
import org.apache.spark.streaming.kafka010.LocationStrategies.PreferConsistent
import org.apache.spark.streaming.kafka010.ConsumerStrategies.Subscribe
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
  var readall: Boolean = false
  var sparkCheckpointDir = ""
  var streamWindowSeconds: Int = 2
  var partitionCount = ""
  var consumerGroup = ""
  var targetDatalake = ""
  var kafkaServers = ""
  var kafkaTopic = ""
  def loadConfig(appProps: java.util.Properties): Unit = {
    sqlCxnString = appProps.getProperty("destSqlCxnString")  
    sqlUser = appProps.getProperty("destSqlUser")  
    sqlPassword = appProps.getProperty("destSqlPassword")
    targetTable = appProps.getProperty("destTargetTable")
    lastReadFile = appProps.getProperty("lastReadFile")
    runForMinutes = appProps.getProperty("runForMinutes").toInt
    sparkCheckpointDir = appProps.getProperty("checkpointDir") + "spark"
    streamWindowSeconds = if (appProps.containsKey("streamWindowSeconds")) appProps.getProperty("streamWindowSeconds").toInt else 10
    partitionCount = appProps.getProperty("partitionCount")
    consumerGroup = appProps.getProperty("consumerGroup")
    targetDatalake = appProps.getProperty("destTargetDatalake")
    kafkaServers = appProps.getProperty("kafkaServers")
    kafkaTopic = appProps.getProperty("kafkaTopic")
  }
}

object Kafka2Sql {
  def main(args: Array[String]): Unit = {
    val conf = new SparkConf().setAppName("Kafka2SQL")
    conf.
      set("spark.streaming.stopGracefullyOnShutdown","true")
      //set("spark.streaming.backpressure.enabled","true").
      //set("spark.streaming.blockInterval","1000ms").
      //set("spark.streaming.receiver.writeAheadLog.enable","true")
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
    
    val ssc = new StreamingContext(sc, Seconds(GlobalConfig.streamWindowSeconds))     
    ssc.checkpoint(GlobalConfig.sparkCheckpointDir)
    
    val kafkaParams = Map[String, Object](
      "bootstrap.servers" -> GlobalConfig.kafkaServers,
      "key.deserializer" -> classOf[StringDeserializer],
      "value.deserializer" -> classOf[ByteArrayDeserializer],
      "group.id" -> GlobalConfig.consumerGroup,
      "auto.offset.reset" -> "latest",
      "enable.auto.commit" -> (false: java.lang.Boolean)
    )
    
    val stream = KafkaUtils.createDirectStream[Nothing, Array[Byte]](
      ssc, 
      PreferConsistent, 
      Subscribe[Nothing, Array[Byte]](Array(GlobalConfig.kafkaTopic), kafkaParams)
    )
    val lines = stream.map(msg => new String(msg.value))
    
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