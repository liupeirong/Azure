package com.sparkeventhub.sample

//
// run spark-shell with this command:
// spark-shell --master yarn --deploy-mode client --num-executors 3 --executor-cores 8 -deprecation -usejavacp --jars /usr/lib/sparkeventhub/eventhubs-client-1.0.jar,/usr/lib/sparkeventhub/qpid-amqp-1-0-client-jms-0.32.jar,/usr/lib/sparkeventhub/qpid-client-0.32.jar,/usr/lib/sparkeventhub/spark-streaming-eventhubs-example-1.5.2.2.3.3.1-7-jar-with-dependencies.jar,/usr/lib/sparkeventhub/qpid-amqp-1-0-client-0.32.jar,/usr/lib/sparkeventhub/qpid-amqp-1-0-common-0.32.jar,/usr/lib/sparkeventhub/qpid-common-0.32.jar,/usr/lib/sparkeventhub/nimbus-jose-jwt-3.1.2.jar,/usr/lib/sparkeventhub/adal4j-1.1.2.jar,/usr/lib/sparkeventhub/oauth2-oidc-sdk-4.5.jar,/usr/lib/sparkeventhub/json-20090211.jar,/usr/lib/sparkeventhub/json-smart-1.1.1.jar,/usr/lib/sparkeventhub/accessors-smart-1.1.jar
// run spark-submit with this command:
// spark-submit --master yarn --deploy-mode client --num-executors 3 --executor-cores 8 --jars /usr/lib/sparkeventhub/eventhubs-client-1.0.jar,/usr/lib/sparkeventhub/qpid-amqp-1-0-client-jms-0.32.jar,/usr/lib/sparkeventhub/qpid-client-0.32.jar,/usr/lib/sparkeventhub/spark-streaming-eventhubs-example-1.5.2.2.3.3.1-7-jar-with-dependencies.jar,/usr/lib/sparkeventhub/qpid-amqp-1-0-client-0.32.jar,/usr/lib/sparkeventhub/qpid-amqp-1-0-common-0.32.jar,/usr/lib/sparkeventhub/qpid-common-0.32.jar,/usr/lib/sparkeventhub/nimbus-jose-jwt-3.1.2.jar,/usr/lib/sparkeventhub/adal4j-1.1.2.jar,/usr/lib/sparkeventhub/oauth2-oidc-sdk-4.5.jar,/usr/lib/sparkeventhub/json-20090211.jar,/usr/lib/sparkeventhub/json-smart-1.1.1.jar,/usr/lib/sparkeventhub/accessors-smart-1.1.jar --class com.sparkeventhub.sample.SparkEventHubSample /tmp/EventHubStream-0.0.1-SNAPSHOT.jar
//

import org.apache.spark.SparkConf
import org.apache.spark.SparkContext
import org.apache.spark.streaming.eventhubs.EventHubsUtils
import org.apache.spark.streaming.{Seconds, StreamingContext}
import org.apache.spark.sql.SQLContext
import org.apache.spark.sql.SaveMode
import org.apache.spark.sql.functions._
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.Future;
import com.microsoft.aad.adal4j.AuthenticationContext;
import com.microsoft.aad.adal4j.AuthenticationResult;
import org.apache.http._
import org.apache.http.client._
import org.apache.http.client.methods.HttpPost
import org.apache.http.impl.client.DefaultHttpClient
import org.apache.http.entity.StringEntity
import com.google.gson.Gson

object SparkEventHubSample {
  case class Entity(Detected:String, IMSI:String, Num1:String, Num2:String, Country1:String, Country2:String)
  case class Table(rows:Array[Entity])

  def main(args: Array[String]) {
   
    val outputDir = "sparkoutput/calls"
    val streamBatchIntervalInSeconds = 20
    val jdbccxn = "jdbc:sqlserver://<your db server>.database.windows.net:1433;database=<your db name>;user=<your db user name>;password=<your db password>;encrypt=false;loginTimeout=30;"

    //Power BI settings
    val url:String = "https://api.powerbi.com/v1.0/myorg/datasets/<guid>/tables/<tablename>/rows"
    
    val AUTHORITY:String = "https://login.windows.net/common/oauth2/authorize";
    val CLIENT_ID:String = "client id";
    val service:ExecutorService = Executors.newFixedThreadPool(1);
    val context:AuthenticationContext = new AuthenticationContext(AUTHORITY, false, service);
    val result:AuthenticationResult = context.acquireToken("https://analysis.windows.net/powerbi/api", CLIENT_ID, "<username>", "<password>", null).get();
    val tk=result.getAccessToken()
    
    val ehParams = Map[String, String](
       "eventhubs.policyname" -> "<your policy>",
       "eventhubs.policykey" -> "<your key>",
       "eventhubs.namespace" -> "<your ns>",
       "eventhubs.name" -> "<your name>",
       "eventhubs.partition.count" -> "4", //executor core count must be twice that of partition count
       "eventhubs.consumergroup" -> "$default",
       "eventhubs.checkpoint.dir" -> "sparkcheckpoint", //for simplicity we are not using reliable receiver with checkpoint in this example
       "eventhubs.checkpoint.interval" -> "600")

    // ----if spark-shell, comment out the following 2 lines
    val sparkConf = new SparkConf().setAppName("SparkEventHubSample")
    val sc = new SparkContext(sparkConf);
    // ----end if spark-shell
    val ssc = new StreamingContext(sc, Seconds(streamBatchIntervalInSeconds))
    val stream = EventHubsUtils.createUnionStream(ssc, ehParams)
    val lines = stream.map(msg => new String(msg))
    //lines.print()  //above is all we need to verify we can see each message from EventHub

    //convert international datetime string to unix time
    val isoformat = new java.text.SimpleDateFormat("yyyy-MM-dd'T'hh:mm:ss'Z'")
    def date2long: (String => Long) = (s: String) => try { isoformat.parse(s).getTime() } catch { case _: Throwable => 0 }
    val sqlfunc = udf(date2long)

    val wlines = lines.window(Seconds(streamBatchIntervalInSeconds*2), Seconds(streamBatchIntervalInSeconds))
    wlines.foreachRDD { rdd => if (!rdd.isEmpty()) 
        {
            val sqlContext = SQLContext.getOrCreate(rdd.sparkContext)
            val rawdf = sqlContext.read.json(rdd)
            val df = rawdf.withColumn("callTime", sqlfunc(rawdf("callrecTime")));
            df.registerTempTable("calls")
            val frauddf = sqlContext.sql("SELECT current_timestamp() as Time, CS1.CallingIMSI, CS1.CallingNum as CallingNum1,CS2.CallingNum as CallingNum2, CS1.SwitchNum as Switch1, CS2.SwitchNum as Switch2 FROM calls CS1 inner join calls CS2 ON CS1.CallingIMSI = CS2.CallingIMSI WHERE CS1.SwitchNum != CS2.SwitchNum and (CS1.callTime - CS2.callTime) > 0 and (CS1.callTime - CS2.callTime)<5000")
            //frauddf.show()

            //push to Power BI
            val items = frauddf.na.fill("").map( e => Entity(e(0).toString, e(1).toString, e(2).toString, e(3).toString, e(4).toString, e(5).toString) ).collect()
            val myt = new Table(items)
            val mytAsJson = new Gson().toJson(myt)
            val se:StringEntity = new StringEntity(mytAsJson)
            val client = new DefaultHttpClient
            val post = new HttpPost(url)
            post.setEntity(se)
            post.setHeader("Authorization", "Bearer " + tk)
            client.execute(post)
            
            // push to db
            // use the min time of this mini batch as the timestamp for the aggregated entry
            //val minTime = df.filter(df("callTime") > 0).select("callTime").first().getLong(0);
            // real time aggregation by region
            //val callsByRegion = df.groupBy("SwitchNum").count().withColumnRenamed("count", "callCount").withColumn("callTimeStamp", lit(minTime));
            // save real time aggregation to sql azure
            //callsByRegion.insertIntoJDBC(jdbccxn, "callsByRegion", false);
          
            //
            // save to hdfs for impala or hive queries
            // use this to create impala table:
            // Create external table calls (callrecTime string, SwitchNum string, CallingIMSI string, CallingNum string, CalledNum string, callTime bigint) stored as parquet location '/user/hdfs/sparkoutput/calls'
            // use "refresh calls" to get new data
            //
            val calls = sqlContext.sql("SELECT callrecTime, SwitchNum, CallingIMSI, CallingNum, CalledNum, callTime from calls");
            calls.write.mode(SaveMode.Append).save(outputDir)
        }
    }

    ssc.start()
    ssc.awaitTermination()
  }
}
