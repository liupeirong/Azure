# SQL Azure Data Sync Monitor Sample

This sample demonstrates how to monitor synchronization status of a bi-directional SQL Azure Data Sync group. Main functional capabilities include the following:

  - periodically update a record in each database in the Data Sync group and check the same record in every other database in the group is updated within specified threshold
  - run periodic update and monitoring in an Azure WebJob
  - output the sync status to an Azure blob in json format, and displayed in an Azure Web Site

Even though the sample is originally created to monitor SQL Azure Data Sync, it can be used to monitor any databases that are supposed to be bi-directionally synced independent of the underlying syncing technology.
### Usage
Download the Visual Studio solution and update the following configurations. Search for the term "your" in the following files.  The solution is built such that you can either deploy the Web Site and Web Job to Azure, or run them locally on your dev machine as standalone web application and console application respectively.

  - App.js
  - App.config

Provide the following configurations:

```java
sql1                   //this is the connection string of a SQL server in an sync group
sql2                   //this is the connection string of a 2nd SQL server in an sync group
...                    //you can add more SQL databases in the sync group
syncgroup              //pick the databases you want to put in the sync group for monitoring
blobConnectionString   //this is the Azure Storage Account in which monitor status will be generated
```

You must prepare the database so that the monitor can update it.  Create an empty table using the following statement:

```sql
create table SyncAgent (Name varchar(32) primary key, Value int not null, LastUpdated datetime not null default getdate()). 
```

Sync the "Name" and "Value" fields.  Don't sync the "LastUpdated" fields.
### Todo's
Support for one-directional sync. 
License
----
MIT
