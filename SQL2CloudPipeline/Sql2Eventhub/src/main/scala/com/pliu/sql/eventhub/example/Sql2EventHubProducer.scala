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

//
//http://stackoverflow.com/questions/34859468/scala-class-lazy-val-variables-strange-behaviour-with-spark
//
object GlobalConfig extends Serializable {
  val sqlCxnString= "sqlCxnString"  
  val sqlUser= "sqlUser"  
  val sqlPassword= "sqlPassword"  
  val tag= "tag"  
  val targetTable= "targetTable"  
  val targetTableKey= "targetTableKey"  
  val lastReadFile= "lastReadFile"  
  val runForMinutes= "runForMinutes"  
  val eventHubsNamespace= "eventHubsNamespace"  
  val eventHubsName= "eventHubsName"  
  val policyName= "policyName"  
  val policyKey= "policyKey"  
  val readall= "readall"
  val eventHubCxnString = "ehCxnString"
  def loadConfig(appProps: Properties): Map[String, String] = {
    val a: Map[String, String] = Map(
      sqlCxnString -> appProps.getProperty(sqlCxnString),  
      sqlUser -> appProps.getProperty(sqlUser),  
      sqlPassword -> appProps.getProperty(sqlPassword),
      tag -> appProps.getProperty(tag),
      targetTable -> appProps.getProperty(targetTable),
      targetTableKey -> appProps.getProperty(targetTableKey),
      lastReadFile -> appProps.getProperty(lastReadFile),
      runForMinutes -> appProps.getProperty(runForMinutes),
      eventHubsNamespace -> appProps.getProperty(eventHubsNamespace),
      eventHubsName -> appProps.getProperty(eventHubsName),
      policyName -> appProps.getProperty(policyName),
      policyKey -> appProps.getProperty(policyKey),
      eventHubCxnString -> new ConnectionStringBuilder(appProps.getProperty(eventHubsNamespace),
            appProps.getProperty(eventHubsName), appProps.getProperty(policyName), appProps.getProperty(policyKey)).toString,
      readall -> (if (appProps.containsKey(readall)) appProps.getProperty(readall) == 1 else false).toString
    )
    a
  }
}

object Sql2EventHub {
  def main(args: Array[String]): Unit = {
    val spark = SparkSession.builder().appName("SQL2EventHub").getOrCreate()
    import spark.implicits._
    
    //read app configuration
    val appConf = spark.conf.get("spark.myapp.conf")
    val pt: Path = new Path(appConf)
    val fs: FileSystem = FileSystem.get(spark.sparkContext.hadoopConfiguration)
    val appConfStream = fs.open(pt)
    val appProps = new Properties()
    appProps.load(appConfStream)
    
    val myconf = GlobalConfig.loadConfig(appProps)
    
    //read the last id stored in hdfs, if none, select everything, otherwise, select newer ones
    var lastread: Int = -1
    if (!myconf(GlobalConfig.readall).toBoolean)
    {
      try {
        val lastreadDF = spark.read.load(myconf(GlobalConfig.lastReadFile))
        lastread = lastreadDF.first().getInt(0)
      } catch {
        case _: Throwable => println("read sql from beginning")
      }
    }
    
    val jdbcDFReader = spark.read.
      format("jdbc").
      option("url", myconf(GlobalConfig.sqlCxnString)).
      option("user", myconf(GlobalConfig.sqlUser)).
      option("password", myconf(GlobalConfig.sqlPassword))
    
    var millisecToRun: Int = myconf(GlobalConfig.runForMinutes).toInt * 60 * 1000;
    val partition: Int = spark.conf.get("spark.executor.cores").toInt * spark.conf.get("spark.executor.instances").toInt
    
    //put GlobalConfig in a Closure, so that it's serialized before sending to executors
    //   http://stackoverflow.com/questions/30181582/spark-use-the-global-config-variables-in-executors
    //on the other hand, if you define a function at the partition level, then it's already in the executor, spark doesn't know
    //how to serialize it.  The following won't work:
    //    val processPartitionFunc = (p: Iterator[Array[Byte]]) => {
    //      val eventHubsClient: EventHubClient = EventHubClient.createFromConnectionString(GlobalConfig.getConnectionString()).get
    //      p.foreach {s => 
    //        val e = new EventData(s)
    //        eventHubsClient.sendSync(e)
    //      }
    //    }
    val processDSFunc = (ds: org.apache.spark.sql.Dataset[Array[Byte]]) => {
      ds.foreachPartition { p => 
          val eventHubsClient: EventHubClient = EventHubClient.createFromConnectionString(myconf(GlobalConfig.eventHubCxnString)).get
          p.foreach {s => 
            val e = new EventData(s)
            eventHubsClient.sendSync(e)
          }
      }
    }
    
    while (millisecToRun > 0) 
    {
      val myDF = jdbcDFReader.
        option("dbtable", "(select * from " + myconf(GlobalConfig.targetTable) + " where " + myconf(GlobalConfig.targetTableKey) + " > " + lastread + ") as intable").
//        spark will natively divide the column value by the partition count to set the range, so you end up all in one partition
//        option("partitionColumn", myconf(GlobalConfig.targetTableKey)).
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
          withColumn("tag", lit(myconf(GlobalConfig.tag))).
          withColumn("publishedat", lit(utcDateTime)).
          toJSON.
          map(m => m.getBytes()).
          repartition(partition)

        processDSFunc(eventPayload)
        
        lastread = myDF.agg(max(myconf(GlobalConfig.targetTableKey))).first().getInt(0)
        spark.sql("select " + lastread).write.mode(SaveMode.Overwrite).parquet(myconf(GlobalConfig.lastReadFile))
      } catch {
        case emptydf: NoSuchElementException => println("nothing to read")
      }
            
      Thread.sleep(10000)
      millisecToRun -= 10000
    }
  }
}