package com.sparkeventhub.sample

import org.apache.spark.SparkConf
import org.apache.spark.SparkContext
import org.apache.spark.streaming.eventhubs.EventHubsUtils
import org.apache.spark.streaming.{Seconds, StreamingContext}
import org.apache.spark.sql.SaveMode
import org.apache.spark.sql._
import org.apache.spark.sql.functions._
import scala.reflect.runtime.universe

object SparkEventHubSample {
  def main(args: Array[String]) {

    val outputDir = "sparkoutput/calls"
    val streamBatchIntervalInSeconds = 60
    val jdbccxn = "jdbc:sqlserver://<your db server>.database.windows.net:1433;database=<your db name>;user=<your db user name>;password=<your db password>;encrypt=false;loginTimeout=30;"

    val ehParams = Map[String, String](
      "eventhubs.policyname" -> "ReceiveRule",
      "eventhubs.policykey" -> "your event hubs policy key",
      "eventhubs.namespace" -> "clouderademo",
      "eventhubs.name" -> "stratahub",
      "eventhubs.partition.count" -> "2", //executor count must be twice that of partition count
      "eventhubs.consumergroup" -> "$default",
      "eventhubs.checkpoint.dir" -> "sparkcheckpoint",
      "eventhubs.checkpoint.interval" -> "600")

    // ----if spark-shell, comment out the following 2 lines
    val sparkConf = new SparkConf().setAppName("SparkEventHubSample")
    val sc = new SparkContext(sparkConf);
    // ----end if spark-shell
    val ssc = new StreamingContext(sc, Seconds(streamBatchIntervalInSeconds))
    val stream = EventHubsUtils.createUnionStream(ssc, ehParams)
    val lines = stream.map(msg => new String(msg))
    //lines.print()  //above is all we need to verify we can see each message from EventHub

    val isoformat = new java.text.SimpleDateFormat("yyyy-MM-dd'T'hh:mm:ss'Z'")
    def date2long: (String => Long) = (s: String) => try { isoformat.parse(s).getTime() } catch { case _: Throwable => 0 }
    val sqlfunc = udf(date2long)

    val sqlContext = new SQLContext(sc)
    import sqlContext.implicits._
    lines.foreachRDD {
      rdd =>
        if (rdd.count() > 0) {
          val rawdf = sqlContext.jsonRDD(rdd);
          // convert international time string to unix time for easy calculation
          val df = rawdf.withColumn("callTime", sqlfunc(rawdf("callrecTime")));
          // use the min time of this mini batch as the timestamp for the aggregated entry
          val minTime = df.filter(df("callTime") > 0).select("callTime").first().getLong(0);
          // real time aggregation by region
          val callsByRegion = df.groupBy("SwitchNum").count().withColumnRenamed("count", "callCount").withColumn("callTimeStamp", lit(minTime));
          // save real time aggregation to sql azure
          callsByRegion.insertIntoJDBC(jdbccxn, "callsByRegion", false);
          // save to hdfs for impala or hive queries
          df.registerTempTable("rawcalls");
          val calls = sqlContext.sql("SELECT callrecTime, SwitchNum, CallingIMSI, CallingNum, CalledNum, callTime from rawcalls");
          calls.save(outputDir, SaveMode.Append)
        }
    }

    ssc.start()
    ssc.awaitTermination()
  }
}
