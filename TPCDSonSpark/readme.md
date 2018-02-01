# Running TPCDS on Spark #

TPCDS has been modified to run on Spark as a Spark SQL performance tests by Databricks.  You can run either existing notebooks in Databricks, or from scratch in your own Spark environment.  The following instructions describe steps to take in both cases.  These steps have been tested on a Cloudera cluster with Spark 2.2.0, an HDInsight cluster with Spark 2.1.1, and Azure Databricks with Spark 2.2.1.  

### To run TPCDS in your own Spark environment
1. On every Spark node, clone and build [Databricks TPCDS kit](https://github.com/databricks/tpcds-kit) as stated in the readme. Note that you must ```git clone https://github.com/databricks/tpcds-kit.git```, not https://github.com/databricks/tpcds-kit.git as stated in the readme. 

2. On the Spark driver, clone an build [Databricks Spark SQL Performance tools](https://github.com/databricks/spark-sql-perf):
    * ```git clone https://github.com/databricks/spark-sql-perf.git```
    * ```bin/run --benchmark DatasetPerformance```
    * ```build/sbt package``` this will build the jar file in the target folder

3. Generate TPCDS data
    * You can generate data in different formats. Parquet is generally recommended. If you create temporary tables, Hive is not required. However, if you create external tables, Hive is required. 
        * Hive2 is required to support Parquet. As of this writing (CDH5.14) comes with Hive1. So you need to use another format such as ORC.
        * In Cloudera, you must enable "Hive Service" for Spark2.
    * Data is generated in an HDFS folder, not local file system.
    * If you are comparing performance in different Spark environments, make sure data is generated with the same parameters, for example, the following parameters to the genData function must match in each environment: 
```csv
format
partitionTables
clusterByPartitionColumns
filterOutNullPartitionValues
numPartitions
```

    * Follow the [readme in the repo]https://github.com/databricks/spark-sql-perf) or [this scala file](/TPCDSonSpark/run_tpcds.scala) to generate the data.

4. Run TPCDS queries
    * Add the following package and jar to Spark command line, the jar is the one you built in step 2 above:
```sh
--packages com.typesafe.scala-logging:scala-logging-slf4j_2.10:2.1.2 --jars /path/to/spark-sql-perf_2.11-0.5.0-SNAPSHOT.jar
```

    * If you are comparing performance in different Spark environments, make sure Spark and Java settings are as same as possible, for example:
```sh
--conf spark.executor.memory=7g 
--conf spark.driver.maxResultSize=4g 
--conf spark.driver.memory=7g 
--num-executors=1 
--conf spark.executor.extraJavaOptions="-XX:ReservedCodeCacheSize=256m -XX:+UseCodeCacheFlushing -Xss4m"
```

    * If you are comparing performance in difference Spark environments, make sure parameters to runExperiment function are same.
    * Follow the [readme in the repo]https://github.com/databricks/spark-sql-perf) or [this scala file](/TPCDSonSpark/run_tpcds.scala#L76) to run the tests.

5. Get TPCDS results
    * experiment.getCurrentResults documented in [readme](https://github.com/databricks/spark-sql-perf) is not the same format as the results written to HDFS. If you want to look at the results from the result file written to HDFS, follow the code in [this scala file](/TPCDSonSpark/run_tpcds.scala#L97). 
    
### To run TPCDS in databricks
1. Import [tpcds_datagen notebook](https://github.com/databricks/spark-sql-perf/blob/master/src/main/notebooks/tpcds_datagen.scala). This notebook takes care of installing tpcds-kit on every worker node. If you are comparing performance in different Spark environments, make sure all data gen parameters are same as described in Step 3 in the previous section. 
2. Import [tpcds_run notebook](https://github.com/databricks/spark-sql-perf/blob/master/src/main/notebooks/tpcds_run.scala). If you are comparing performance in different Spark environments, make sure all parameters are same as described in Step 4 in the previous section.


