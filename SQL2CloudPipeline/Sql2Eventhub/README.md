# SQL Server to EventHub Producer
A spark 2.0 example showing how to query from SQL Server and push to Azure Eventhubs

Usage:
* spark-shell --master yarn --deploy-mode client --executor-cores 2 -usejavacp 
  --jars /opt/libs/azure-eventhubs-0.9.0.jar,/opt/libs/proton-j-0.15.0.jar 
  --driver-class-path /opt/libs/sqljdbc4.jar 
  --conf spark.executor.extraClassPath=/opt/libs/sqljdbc4.jar 
  --conf spark.myapp.sqlcxnstring="jdbc:sqlserver://yourserver.database.windows.net:1433;database=yourdb;" 
  --conf spark.myapp.sqluser=youruser@yourserver 
  --conf spark.myapp.sqlpassword=yourpassword 
  --conf spark.executorEnv.eventHubsNS=yourhubnamespace 
  --conf spark.executorEnv.eventHubsName=yourhubname 
  --conf spark.executorEnv.policyName=yourpolicy 
  --conf spark.executorEnv.policyKey="yourkey"

* spark-submit --master yarn --deploy-mode client --executor-cores 2 
  --jars /opt/libs/azure-eventhubs-0.9.0.jar,/opt/libs/proton-j-0.15.0.jar 
  --driver-class-path /opt/libs/sqljdbc4.jar 
  --conf spark.executor.extraClassPath=/opt/libs/sqljdbc4.jar 
  --conf spark.myapp.sqlcxnstring="jdbc:sqlserver://yourserver.database.windows.net:1433;database=yourdb;" 
  --conf spark.myapp.sqluser=youruser@yourserver 
  --conf spark.myapp.sqlpassword=yourpassword 
  --conf spark.executorEnv.eventHubsNS=yourhubnamespace 
  --conf spark.executorEnv.eventHubsName=yourhubname
  --conf spark.executorEnv.policyName=yourpolicy 
  --conf spark.executorEnv.policyKey="yourkey" 
  --class com.pliu.sql.eventhub.example.Sql2EventHub /tmp/original-com-pliu-sql-eventhub-example-0.01.jar

Issues: 
 * using --properties-file causes jdbc call to hang, so specifying conf in commandline
 * putting libraries in hdfs doesn't work, must be on each node
 * mvn package produces a monolithic jar and a minimum(original) jar, use the minimum jar if additional jars are already on each node
 * In Eclipse, "Configure Build Path" -> "Scala Compiler" -> Use 2.11 to be compatible with Spark 2.0
