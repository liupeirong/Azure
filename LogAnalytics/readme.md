# Location based game log sample generator and consumer

This sample simulates the generation of location based game logs. It uses Spark Streaming to process in-game purchase events and send purchase revenue to Power BI real time dashboard. It also stores events in HDFS for Impala queries and Power BI reports. It includes the following components - 

### GeoLogGenerator: a .NET application that generates log events and pushes to Azure Event Hub
 * The logs are in the format:
   ```javascript
   {"eventTime":"YYYY-MM-DDTHH:mm:ss","playerId":"guid","sessionId":"guid","eventType":"gameStart|itemPurchased|levelReached|gameEnd","eventParams":{"vary by event type"}}
   ``` 
 * The geo coordinates come from world's 200 most populated cities in the file world_cities.csv
 * The sample rotates player locations around the world in 60 minutes
 * Put your Event Hub configuration in app.config
 * To run the sample, 
   ```javascript
   geoLogGen.exe [hours to run] [num of players] [power bi pro or free] [push to event hub or print only] [path to world_cities.csv]
   ``` 

### GeoLogConsumreRealTime: a Spark application that streams events from Azure Event Hub, filters purchase events, aggregates revenue, and sends to Power BI real time dashboard
 * Put your Event Hub, Power BI, and Streaming configuration in src\main\resources\logConsumerRT.conf
 * You can optionally put the following dependency jars in a folder in the Spark driver and executors to avoid copying the large uber jar from your dev machine to the cluster during development
  * spark-streaming-eventhubs_2.11-2.0.3.jar
  * scalaj-http_2.11-2.3.0.jar
  * config-1.3.1.jar
 * Sample command line: 
 ```javascript
 spark2-submit --master yarn --deploy-mode client  --executor-cores 4 --jars /path/to/spark-streaming-eventhubs_2.11-2.0.3.jar,/path/to/scalaj-http_2.11-2.3.0.jar,/path/to/config-1.3.1.jar,/opt/cloudera/parcels/CDH/jars/commons-lang3-3.3.2.jar --conf spark.driver.userClasspathFirst=true --conf spark.executor.extraClassPath=/opt/cloudera/parcels/CDH/jars/commons-lang3-3.3.2.jar --conf spark.executor.userClasspathFirst=true --class com.pliu.logconsumerrt.GeoLogConsumerRT /path/to/original-logconsumerrt-0.0.1.jar
 ```
