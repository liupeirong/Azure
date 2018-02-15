//
// This file combines generating data, running tests, and retrieving test results in a single spark-shell run.  
// you can certainly selectively run separate parts.
// A sample command line will be: 
// spark-shell --packages com.typesafe.scala-logging:scala-logging-slf4j_2.10:2.1.2 --jars /path/to/spark-sql-perf_2.11-0.5.0-SNAPSHOT.jar --conf spark.executor.memory=7g 
//   --conf spark.driver.maxResultSize=4g --conf spark.driver.memory=7g --num-executors=1 
//   --conf spark.executor.extraJavaOptions="-XX:ReservedCodeCacheSize=256m -XX:+UseCodeCacheFlushing -Xss4m"
//

val sqlContext = new org.apache.spark.sql.SQLContext(sc)

/////////////////////////////////
// Generate data
/////////////////////////////////
val databaseName="tpcds"
val rootDir="/path/to/hdfs/tpcdsdata"
val tpcdskitDir="/path/to/local/tpcds-kit/tools"

// data gen parameters
val scaleFactor="1"
val format="orc"
val useDecimal = false 
val useDate = false
val filterNull = false
val shuffle = true
val dsdgen_nonpartitioned = 10
val dsdgen_partitioned = 10000
sqlContext.setConf("spark.sql.parquet.compression.codec", "snappy")
// TPCDS has around 2000 dates.
spark.conf.set("spark.sql.shuffle.partitions", "2000")
// Don't write too huge files.
sqlContext.setConf("spark.sql.files.maxRecordsPerFile", "20000000")

import com.databricks.spark.sql.perf.tpcds.TPCDSTables
val tables = new TPCDSTables(sqlContext,dsdgenDir=tpcdskitDir, scaleFactor = scaleFactor, useDoubleForDecimal = useDecimal, useStringForDate = useDate)

// generate small tables
val nonPartitionedTables = Array("call_center", "catalog_page", "customer", "customer_address", "customer_demographics", "date_dim", "household_demographics", "income_band", "item", "promotion", "reason", "ship_mode", "store",  "time_dim", "warehouse", "web_page", "web_site")
nonPartitionedTables.foreach { t => {
  tables.genData(
      location = rootDir,
      format = format,
      overwrite = true,
      partitionTables = true,
      clusterByPartitionColumns = shuffle,
      filterOutNullPartitionValues = filterNull,
      tableFilter = t,
      numPartitions = dsdgen_nonpartitioned)
  println("Done generating non partitioned tables.")
}}

// leave the biggest/potentially hardest tables to be generated last.
val partitionedTables = Array("inventory", "web_returns", "catalog_returns", "store_returns", "web_sales", "catalog_sales", "store_sales") 
partitionedTables.foreach { t => {
  tables.genData(
      location = rootDir,
      format = format,
      overwrite = true,
      partitionTables = true,
      clusterByPartitionColumns = shuffle,
      filterOutNullPartitionValues = filterNull,
      tableFilter = t,
      numPartitions = dsdgen_partitioned)
  println("Done generating partitioned tables.")
}}

// Create the specified database
sql(s"drop database if exists $databaseName cascade")
sql(s"create database $databaseName") //on HDInsight, specify location '/path/to/hdfs/db', otherwise, it uses current local folder name
sql(s"use $databaseName")
tables.createExternalTables(rootDir, format, databaseName, overwrite = true, discoverPartitions = true)
// If you don't have Hive, or Hive 2 which supports parquet, you can create temporary tables which only exists in this session
//tables.createTemporaryTables(rootDir, format)

/////////////////////////////////
// Run tests
/////////////////////////////////
import com.databricks.spark.sql.perf.tpcds.TPCDS
val tpcds = new TPCDS (sqlContext = sqlContext)
sql(s"use $databaseName")

// test run parameters
val resultLocation = "/path/to/hdfs/tpcdsresult"
val iterations = 1 // how many iterations of queries to run.
val queries = tpcds.tpcds2_4Queries // queries to run.
val timeout = 24*60*60 // timeout, in seconds.
spark.conf.set("spark.sql.broadcastTimeout", "10000") // good idea for Q14, Q88.

val experiment = tpcds.runExperiment(
  queries, 
  iterations = iterations,
  resultLocation = resultLocation,
  forkThread = true)
experiment.waitForFinish(timeout)

/////////////////////////////////
// Retrieve Results
/////////////////////////////////

// if we are still in the current session
import org.apache.spark.sql.functions.{col, lit, substring, explode}
val results = experiment.getCurrentResults.
  withColumn("Name", substring(col("name"), 2, 100)).
  withColumn("Runtime", (col("parsingTime") + col("analysisTime") + col("optimizationTime") + col("planningTime") + col("executionTime")) / 1000.0).
  select('Name, 'Runtime)

// if we are looking at the results in the results file  
val results = spark.read.json(resultLocation).  //.filter($"timestamp" === 1518659108336L)
  select(explode($"results")).
  withColumn("Name", substring($"col.name", 2, 100)).
  withColumn("RunTime", ($"col.parsingTime" + $"col.analysisTime" + $"col.optimizationTime" + $"col.planningTime" + $"col.executionTime") / 1000.0).
  select('Name, 'Runtime)
  
results.show
results.collect.foreach(println)
results.repartition(1).write.format("csv").save("/path/to/tpcdsresult.csv")
