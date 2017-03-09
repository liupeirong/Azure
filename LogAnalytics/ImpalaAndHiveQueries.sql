-- Hive create table from csv files in ADLS -- 
--    spark will write all fields into double quoted strings -- 
create table geo (eventts timestamp, playerid varchar(100), sessionid varchar(100), city varchar(100), lat double, lon double) 
ROW FORMAT SERDE 'org.apache.hadoop.hive.serde2.OpenCSVSerde' stored as textfile 
location "adl://mypond.azuredatalakestore.net/gamelogcsv/geo";

create table level (eventts timestamp, playerid varchar(100), sessionid varchar(100), level varchar(50)) 
ROW FORMAT SERDE 'org.apache.hadoop.hive.serde2.OpenCSVSerde' stored as textfile 
location "adl://mypond.azuredatalakestore.net/gamelogcsv/level";

create table purchase (eventts timestamp, playerid varchar(100), sessionid varchar(100), item varchar(100), quantity bigint, price bigint) 
ROW FORMAT SERDE 'org.apache.hadoop.hive.serde2.OpenCSVSerde' stored as textfile 
location "adl://mypond.azuredatalakestore.net/gamelogcsv/purchase";

-- Impala create table from parquet files in hdfs -- 
create table igeo (eventts timestamp, playerid varchar(100), sessionid varchar(100), city varchar(100), lat double, lon double) 
stored as parquet
location "/gamelog/geo";

create table ilevel (eventts timestamp, playerid varchar(100), sessionid varchar(100), level varchar(50)) 
stored as parquet
location "/gamelog/level";

create table ipurchase (eventts timestamp, playerid varchar(100), sessionid varchar(100), item varchar(100), quantity bigint, price bigint) 
stored as parquet
location "/gamelog/purchase";

-- To compact small part files into larger files -- 
--     same for Hive and Impala -- 
insert overwrite table geo select * from geo;
insert overwrite table level select * from level;
insert overwrite table purchase select * from purchase;

-- Time to purchase -- 
select p.item, avg(cast(p.eventts as bigint) - cast(s.eventts as bigint)) as seconds2purchaseAvg,
    stddev(cast(p.eventts as bigint) - cast(s.eventts as bigint)) as seconds2purchaseStdDev,
    min(cast(p.eventts as bigint) - cast(s.eventts as bigint)) as seconds2purchaseMin,
    max(cast(p.eventts as bigint) - cast(s.eventts as bigint)) as seconds2purchaseStdMax
from geo s join purchase p on p.sessionid = s.sessionid group by p.item

-- Time to purchase by location -- 
select p.item, s.lat, s.lon, avg(cast(p.eventts as bigint) - cast(s.eventts as bigint)) as seconds2purchaseAvg,
    stddev(cast(p.eventts as bigint) - cast(s.eventts as bigint)) as seconds2purchaseStdDev,
    min(cast(p.eventts as bigint) - cast(s.eventts as bigint)) as seconds2purchaseMin,
    max(cast(p.eventts as bigint) - cast(s.eventts as bigint)) as seconds2purchaseMax
from geo s join purchase p on p.sessionid = s.sessionid group by p.item, s.lat, s.lon

-- Sales by location -- 
select p.item, s.lat, s.lon, sum(p.quantity * p.price) as sales
from geo s join purchase p on p.sessionid = s.sessionid group by p.item, s.lat, s.lon

-- Time to reach level -- 
select l.level, avg(cast(l.eventts as bigint) - cast(s.eventts as bigint)) as seconds2levelAvg,
    stddev(cast(l.eventts as bigint) - cast(s.eventts as bigint)) as seconds2levelStdDev,
    min(cast(l.eventts as bigint) - cast(s.eventts as bigint)) as seconds2levelMin,
    max(cast(l.eventts as bigint) - cast(s.eventts as bigint)) as seconds2levelMax
from geo s join level l on l.sessionid = s.sessionid group by l.level
