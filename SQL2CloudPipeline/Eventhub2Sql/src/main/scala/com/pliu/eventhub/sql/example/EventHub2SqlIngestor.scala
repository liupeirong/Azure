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

object EventHub2Sql {
  def main(args: Array[String]): Unit = {
    val conf = new SparkConf().setAppName("SQL2EventHub")
    val sc = new SparkContext(conf);
    //if spark-shell use
    //val conf = sc.getConf
    val streamWindowSeconds: Int = if (conf.contains("spark.myapp.streamwindowseconds"))
      conf.get("spark.myapp.streamwindowseconds").toInt else 60
    val runForMinutes: Int = if (conf.contains("spark.myapp.runforminutes"))
      conf.get("spark.myapp.runforminutes").toInt else 2

    val checkpointDir: String = "/user/pliu/sparkcheckpoint"
    
    val eventhubParameters = Map[String, String](
      "eventhubs.policyname" -> conf.get("spark.executorEnv.policyName"),
      "eventhubs.policykey" -> conf.get("spark.executorEnv.policyKey"),
      "eventhubs.namespace" -> conf.get("spark.executorEnv.eventHubsNS"),
      "eventhubs.name" -> conf.get("spark.executorEnv.eventHubsName"),
      "eventhubs.partition.count" -> conf.get("spark.myapp.partitionCount"), //executor core count must be twice that of partition count
      "eventhubs.consumergroup" -> conf.get("spark.myapp.consumerGroup"),
      "eventhubs.checkpoint.dir" -> checkpointDir, 
      "eventhubs.checkpoint.interval" -> streamWindowSeconds.toString)
    
    val ssc = new StreamingContext(sc, Seconds(streamWindowSeconds)) 
    ssc.checkpoint(checkpointDir)
    
    val stream = EventHubsUtils.createUnionStream(ssc, eventhubParameters)
    val lines = stream.map(msg => new String(msg))
    
    lines.foreachRDD {rdd => if (!rdd.isEmpty())
      {
        val sqlCxnString = sys.env("sqlcxnstring")   
        val sqlUser = sys.env("sqluser")   
        val sqlPassword = sys.env("sqlpassword") 
        val targetTable: String = "diskncci"
  
        val spark = SparkSession.builder.config(rdd.sparkContext.getConf).getOrCreate()
        import spark.implicits._
        val jdbcProp = new java.util.Properties 
        jdbcProp.setProperty("user", sqlUser)
        jdbcProp.setProperty("password", sqlPassword)
        val utcDateTime = Instant.now.toString
  
        val myDF = spark.read.json(rdd).toDF.withColumn("consumedat", lit(utcDateTime))
        myDF.show()
        myDF.write.mode(SaveMode.Append).jdbc(sqlCxnString, targetTable, jdbcProp);
      }
    }
    
    ssc.start()
    ssc.awaitTerminationOrTimeout(runForMinutes * 60 * 1000)
  }
}