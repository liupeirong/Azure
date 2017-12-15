# Query the aggregated data

Create a Hive table or Impala table on the output parquet files, so that this data can be queried by SQL client or BI tools.  In this example, the Spark compaction job stores the output parquet files partitioned by year, month, and deviceid. 

In Hive, the partition field names as well as the field names in parquet files must match exactly to the column names in Hive tables.  All field names should be in lower case.  Otherwise, Hive may not be able to read the data, or read data as NULL values.  Impala doesn't have this limitation.

```sql
create table deviceagg (start timestamp, `end` timestamp, sensor9avg double)
partitioned by (year int, month int, deviceid string)
stored as parquet
location "/user/pliu/devicelog";

msck repair table deviceagg
```
