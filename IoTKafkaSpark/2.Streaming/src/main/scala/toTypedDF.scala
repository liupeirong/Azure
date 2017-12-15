package org.pliu.iot.sim

import org.apache.spark.sql.SparkSession
import org.apache.spark.sql.DataFrame
import org.apache.spark.sql.types._
import org.apache.spark.sql.functions._

object toTypedDF {
  def fromCSV(dfraw: DataFrame, spark: SparkSession): DataFrame = {
    val delim = ","
    val nullvalue = "NULL"
    
    import spark.implicits._ 
    val dfv = dfraw.select($"value".cast(StringType).alias("val"))
    dfv.withColumn("_tmp", split($"val", delim)).select(
        $"_tmp".getItem(0).cast(StringType).alias("deviceid"),
        $"_tmp".getItem(1).cast(IntegerType).alias("cycle"),
        $"_tmp".getItem(2).cast(IntegerType).alias("counter"),
        $"_tmp".getItem(3).cast(IntegerType).alias("endofcycle"),
        $"_tmp".getItem(4).cast(FloatType).alias("sensor9"),
        $"_tmp".getItem(5).cast(FloatType).alias("sensor11"),
        $"_tmp".getItem(6).cast(FloatType).alias("sensor14"),
        $"_tmp".getItem(7).cast(FloatType).alias("sensor15")
        ).
        //if we have a nullable string value, make sure to convert it to null,capital "NULL" causes parsing failure
        //withColumn("stringvalue", regexp_replace(col("stringvalue"), nullvalue, null)).
        drop("_tmp")
  }
  
  def fromJSON(dfraw: DataFrame, spark: SparkSession): DataFrame = {
    //using JSON parser to convert data types is not reliable,so we keep everything as string 
    //see http://blog.antlypls.com/blog/2016/01/30/processing-json-data-with-sparksql/
   import spark.implicits._ 
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
    dfj.select(
        $"jsonval.deviceid".cast(StringType).alias("deviceid"),
        $"jsonval.cycle".cast(IntegerType).alias("cycle"),
        $"jsonval.counter".cast(IntegerType).alias("counter"),
        $"jsonval.endofcyle".cast(IntegerType).alias("endofcycle"),
        $"jsonval.sensor9".cast(FloatType).alias("sensor9"),
        $"jsonval.sensor11".cast(FloatType).alias("sensor11"),
        $"jsonval.sensor14".cast(FloatType).alias("sensor14"),
        $"jsonval.sensor15".cast(FloatType).alias("sensor15")
        )
  }
}