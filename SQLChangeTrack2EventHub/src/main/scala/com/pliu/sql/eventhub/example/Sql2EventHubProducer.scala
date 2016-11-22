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
 * --conf spark.executorEnv.eventHubsNS=yourhubnamespace 
 * --conf spark.executorEnv.eventHubsName=yourhubname
 * --conf spark.executorEnv.policyName=yourpolicy 
 * --conf spark.executorEnv.policyKey="yourkey" 
 * --class com.pliu.sql.eventhub.example.Sql2EventHub /tmp/original-com-pliu-sql-eventhub-example-0.01.jar
 */

package com.pliu.sql.eventhub.example
import org.apache.spark.sql.SparkSession
import com.microsoft.azure.eventhubs.{EventData, EventHubClient}
import com.microsoft.azure.servicebus.ConnectionStringBuilder

object Sql2EventHub {
  def main(args: Array[String]): Unit = {
    val spark = SparkSession.builder().appName("SQL2EventHub").getOrCreate()
    import spark.implicits._
    val sqlCxnString = spark.conf.get("spark.myapp.sqlcxnstring")  
    val sqlUser = spark.conf.get("spark.myapp.sqluser")  
    val sqlPassword = spark.conf.get("spark.myapp.sqlpassword")  
    
    val jdbcDF = spark.read.
      format("jdbc").
      option("url", sqlCxnString).
      option("user", sqlUser).
      option("password", sqlPassword).
      option("dbtable", "(select top 10 * from memcci) as subset").
      load()
      
    val eventPayload = jdbcDF.toJSON
    eventPayload.foreachPartition{p => 
      var eventHubsNamespace: String = sys.env("eventHubsNS")
      var eventHubsName: String = sys.env("eventHubsName")
      var policyName: String = sys.env("policyName")
      var policyKey: String = sys.env("policyKey")
      val connectionString: String = new ConnectionStringBuilder(
        eventHubsNamespace, eventHubsName, policyName, policyKey).toString
      var eventHubsClient: EventHubClient = EventHubClient.createFromConnectionString(connectionString).get
      p.foreach {s => 
        val e = new EventData(s.getBytes())
        eventHubsClient.sendSync(e)
      }
    }
  }
}