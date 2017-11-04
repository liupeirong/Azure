import org.apache.spark.sql.SparkSession
import org.apache.spark.sql.functions._
import org.apache.spark.sql.types._
import org.apache.spark.sql.SaveMode
import org.apache.spark.sql.streaming.{OutputMode, Trigger}
import scala.concurrent.duration._
import com.typesafe.config._

object IoTStreaming {
  def main(args: Array[String]): Unit = {
    val appconf = ConfigFactory.load("iotconf")
    val kafkaBrokers = appconf.getString("iotsim.kafkaBrokers")
    val kafkaTopic = appconf.getString("iotsim.kafkaTopic")
    val maxOffsetsPerTrigger = appconf.getString("iotsim.maxOffsetPerTrigger")
    val workingDir = appconf.getString("iotsim.devicelogWorkingDir")
    val devicelogDir = appconf.getString("iotsim.devicelogDir")
    val devicelogCheckpointDir = appconf.getString("iotsim.devicelogCheckpointDir")
    
    val spark = SparkSession
      .builder
      .appName("iotsim")
      .getOrCreate
    import spark.implicits._
      
    val dfraw = spark.readStream.
      format("kafka").
      option("kafka.bootstrap.servers", kafkaBrokers).
      option("subscribe", kafkaTopic).
      option("startingOffsets", "earliest").
      option("maxOffsetsPerTrigger", maxOffsetsPerTrigger).
      load()
    //for testing, instead of Kafka, use file source
    //val dfraw = spark.readStream.schema(devicelogSchema).option("header", "true").csv("/user/pliu/iotinput")

    /* if CSV */
    val devicelogSchema = new StructType().
      add("deviceid", StringType).
      add("cycle", IntegerType).
      add("counter",IntegerType).
      add("endofcycle",IntegerType).
      add("sensor9",FloatType).
      add("sensor11",FloatType).
      add("sensor14",FloatType).
      add("sensor15",FloatType)
    /* end if CSV */
    
    //TODO: type CSV fields, and add csv/json converter
      
    /* if JSON */
    //using JSON parser to convert data types is not reliable,so we keep everything as string 
    //see http://blog.antlypls.com/blog/2016/01/30/processing-json-data-with-sparksql/
    val devicelogSchemaJ = new StructType().
      add("deviceid", StringType).
      add("cycle", StringType).
      add("counter",StringType).
      add("endofcycle",StringType).
      add("sensor9",StringType).
      add("sensor11",StringType).
      add("sensor14",StringType).
      add("sensor15",StringType)
    val dfj = dfraw.select(from_json($"value".cast(StringType), devicelogSchemaJ).alias("jsonval"))
    
    //use Spark SQL to convert to the right data types 
    val dftyped = dfj.select(
        $"jsonval.deviceid".cast(StringType).alias("deviceid"),
        $"jsonval.cycle".cast(IntegerType).alias("cycle"),
        $"jsonval.counter".cast(IntegerType).alias("counter"),
        $"jsonval.endofcyle".cast(IntegerType).alias("endofcycle"),
        $"jsonval.sensor9".cast(FloatType).alias("sensor9"),
        $"jsonval.sensor11".cast(FloatType).alias("sensor11"),
        $"jsonval.sensor14".cast(FloatType).alias("sensor14"),
        $"jsonval.sensor15".cast(FloatType).alias("sensor15")
        )
    /* end if JSON */    
        
    // if the events include a timestamp field
    //val df = dftyped.withColumn("ts", from_unixtime($"ts" /1000, "YYYY-MM-dd HH:mm:ss").cast(TimestampType))
    // else we add a timestamp field just to show how to use the windowing functions below    
    val df = dftyped.withColumn("ts", current_timestamp)
    
    val dfagg = df.
      withWatermark("ts", "1 minute").
      groupBy(window($"ts", "1 minute"), $"deviceid").
      agg(avg($"sensor9")).
      select($"window.start", $"window.end", $"deviceid", $"count").
      withColumn("year", year($"ts")).
      withColumn("month", month($"ts"))
        
    val query = dfagg.
      writeStream.
      format("parquet").
      partitionBy("year", "month").
      option("path", workingDir).
      option("checkpointLocation",devicelogCheckpointDir).
      start

    query.awaitTermination
      
    // for testing, use console sink. Note that only Append mode is supported for file sink. Append mode only works 
    // with watermark, and will only produce outputs when the next trigger kicks in AND
    // max seen event time - watermark > evaluated time window, so by default it's append mode
    //    val query = dfagg.writeStream.format("console").option("truncate", false).start 
    // if you don't see result, try Update or Complete mode
    //    val query = dfagg.writeStream.format("console").outputMode(OutputMode.Update).option("truncate", false).start

    /* compacting small files in working dir to larger files*/
    /*
    spark.read.parquet(workingDir + "/year=2017/month=10").
      repartition($"deviceid").
      write.partitionBy("deviceid").
      mode(SaveMode.Overwrite).
      parquet(devicelogDir + "/year=2017/month=10")
    */
            
    /* alerting */
    /*
    //spark.conf.get("spark.sql.caseSensitive") by default its false
    import org.apache.spark.sql.functions.{concat, lit}
    val dfalert = df.
    	filter($"tag" === "temperature" && $"sensor11" > 600).
    	withColumn("message", concat(lit("temperature too high"),$"sensor11"))

    val queryalert = dfalert.writeStream.format("console").option("truncate", false).start
    */    
    //TODO use Kafka console producer to simulate device
    //TODO create hive tables on parquet, all partitioned fields must be lower case
  }  
}  
