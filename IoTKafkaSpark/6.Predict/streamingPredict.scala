import org.apache.spark.sql.types._
import org.apache.spark.ml.{Pipeline, PipelineModel}
import org.apache.spark.sql.streaming.Trigger
import scala.concurrent.duration._

// load the trained model we saved in the previous step
val datadir = "/user/pliu/CMAPSS/"
val model = PipelineModel.load(datadir + "gbtmodel")

val kafkaBrokers = "{{your Kafka brokers}}"
val kafkaTopic = "kafkalab"
val delim = ","
val maxOffsetPerTrigger = 50
val triggerInterval = "10 seconds"

val dfraw = spark.readStream.
  format("kafka").
  option("kafka.bootstrap.servers", kafkaBrokers).
  option("subscribe", kafkaTopic). 
  option("startingOffsets", "earliest"). 
  option("maxOffsetsPerTrigger", maxOffsetPerTrigger).  
  load
val dftyped = dfraw.
  select($"key".cast(StringType).alias("id"), $"value".cast(StringType).alias("val")).
  withColumn("_tmp", split($"val", delim)).
  select(
    $"id", 
    $"_tmp".getItem(0).cast(IntegerType).alias("cycle"),
    $"_tmp".getItem(3).cast(FloatType).alias("s9"),
    $"_tmp".getItem(4).cast(FloatType).alias("s11"),
    $"_tmp".getItem(5).cast(FloatType).alias("s14"),
    $"_tmp".getItem(6).cast(FloatType).alias("s15")
    ).drop("_tmp")
val df = dftyped.withColumn("ts", current_timestamp)

val predictions = model.transform(df)

val query = predictions.select($"ts", $"id", $"cycle", $"RUL_Pred").writeStream.
  format("console").  
  option("truncate", false).
  trigger(Trigger.ProcessingTime(Duration(triggerInterval))).
  start
      