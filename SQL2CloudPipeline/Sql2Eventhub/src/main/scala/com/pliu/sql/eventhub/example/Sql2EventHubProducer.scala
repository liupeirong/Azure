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
     
    val sqlCxnString = appProps.getProperty("sqlcxnstring")  
    val sqlUser = appProps.getProperty("sqluser")  
    val sqlPassword = appProps.getProperty("sqlpassword")
    val tag = appProps.getProperty("tag")
    val targetTable = appProps.getProperty("targetTable")
    val targetTableKey = appProps.getProperty("targetTableKey")
    val lastReadFile = appProps.getProperty("lastReadFile")
    
    val loop = if (appProps.containsKey("loop")) appProps.getProperty("loop") == "1" else false
    val readall = if (appProps.containsKey("readall")) appProps.getProperty("readall") == "1" else false
    
    //read the last id stored in hdfs, if none, select everything, otherwise, select newer ones
    var lastread: Int = -1
    if (!readall)
    {
      try {
        val lastreadDF = spark.read.load(lastReadFile)
        lastread = lastreadDF.first().getInt(0)
      } catch {
        case _: Throwable => println("read sql from beginning")
      }
    }
    
    val jdbcDFReader = spark.read.
      format("jdbc").
      option("url", sqlCxnString).
      option("user", sqlUser).
      option("password", sqlPassword)
    
    var done = false;
    while (!done) 
    {
      val myDF = jdbcDFReader.
        option("dbtable", "(select * from " + targetTable + " where " + targetTableKey + " > " + lastread + ") as intable").
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
          withColumn("tag", lit(tag)).
          withColumn("publishedat", lit(utcDateTime)).
          toJSON
        
        eventPayload.foreachPartition{p => 
          val eventHubsNamespace: String = sys.env("eventHubsNS")
          val eventHubsName: String = sys.env("eventHubsName")
          val policyName: String = sys.env("policyName")
          val policyKey: String = sys.env("policyKey")
          val connectionString: String = new ConnectionStringBuilder(
            eventHubsNamespace, eventHubsName, policyName, policyKey).toString
          var eventHubsClient: EventHubClient = EventHubClient.createFromConnectionString(connectionString).get
          p.foreach {s => 
            val e = new EventData(s.getBytes())
            eventHubsClient.sendSync(e)
          }
        }
        
        lastread = myDF.agg(max(targetTableKey)).first().getInt(0)
        spark.sql("select " + lastread).write.mode(SaveMode.Overwrite).parquet(lastReadFile)
      } catch {
        case emptydf: NoSuchElementException => println("nothing to read")
      }
            
      if (loop) Thread.sleep(10000) else done = !loop
    }
  }
}