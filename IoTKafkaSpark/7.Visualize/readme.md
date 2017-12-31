# Spark structured streaming from Kafka to Power BI real time dashboard

In the [streaming job](/IoTKafkaSpark/2.Streaming) where Spark reads device data from Kafka and aggregates the results, it outputs the results in files and also sends the results to another Kafka topic.  Here another Spark structured streaming job reads from Kafka, and outputs the data to Power BI realtime dashboard with a feacheach sink. 

* You can create Power BI streaming, push, or pushStreaming datasets for real time data.  When you create these datasets using REST API, you will later push data to these APIs by authenticating against Power BI with Azure AD OAuth2.  However, if you create such a dataset in the Power BI portal, it gives you an application key to push data to Power BI, which makes it easier to code in Spark, especially for testing.

* Without awaitTermination on the streaming query, YARN container will exit before Spark streaming can start. 

* Dynamic Allocation of Spark executors is sometimes turned on by default.  Sometimes, when one streaming job becomes idle, its executors are deallocated, and maybe taken by another streaming job. So when data becomes available, the idle streaming job doesn't have any resource available to be allocated to it to start.  Turning off Dyanmic Allocation may provide better control.

* Hadoop distributions often comes with older versions of org.apache.http.*.  If you develop in Eclipse, you'll see DefaultHttpClient is deprecated, instead you should use HttpClientBuilder.  However, even if you set the classpath of the Spark job to use 4.3+ versions of org.apache.http.httpcore and org.apache.http.httpclient, other dependencies might bring in older versions of these packages which causes conflict.  You can relocate these packages in the maven shaded uber jar as following:
```xml
  <relocations>
    <relocation>
      <pattern>org.apache.http</pattern>
      <shadedPattern>shaded.org.apache.http</shadedPattern>
    </relocation>
  <relocations>
```

* To troubleshoot requests sent to Power BI, [RequestBin](https://requestb.in/) could be a very effective tool. Replace the Power BI URL with RequestBin URL, and examine the requests.

run the shaded uber jar in spark-sumbit:
```bash
spark2-submit --master yarn --deploy-mode client --num-executors 2 --driver-java-options='-Dlog4j.configuration=file:log4j.properties' --class org.pliu.iot.bi.stream2powerbi ./powerbi-sink-0.0.1.jar
```
