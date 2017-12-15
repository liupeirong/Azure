package org.pliu.iot

import org.apache.spark.sql.SparkSession
import org.apache.spark.sql.SaveMode
import com.typesafe.config._

object FileCompaction {
  def main(args: Array[String]): Unit = {
    //or specify in commandline ex. -Dcompact.partition="/year=2017/month=9", or in spark-submit command,
    //--driver-java-options='-Dcompact.partition=/year=2017/month=11'
    val appconf = ConfigFactory.load("compact")
    val partition = appconf.getString("compact.partition")
    val sourceDir = appconf.getString("compact.sourceDir")
    val targetDir = appconf.getString("compact.targetDir")
    
    val spark = SparkSession
      .builder
      .appName("compactfiles")
      .getOrCreate
    import spark.implicits._

    spark.read.parquet(sourceDir + partition).
      repartition($"deviceid").
      write.partitionBy("deviceid").
      mode(SaveMode.Overwrite).
      parquet(targetDir + partition)
  }
}