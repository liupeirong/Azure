-- Create Hive tables in parquet format for ad-hoc query -- 
create table pqgeo (eventts timestamp, playerid varchar(100), sessionid varchar(100), city varchar(100), lat double, lon double)
clustered by (sessionid) into 3 buckets
stored as parquet
location "adl://mypond.azuredatalakestore.net/gamelog/geo";

create table pqlevel (eventts timestamp, playerid varchar(100), sessionid varchar(100), level varchar(50)) 
clustered by (sessionid) into 3 buckets
stored as parquet
location "adl://mypond.azuredatalakestore.net/gamelog/level";

create table pqpurchase (eventts timestamp, playerid varchar(100), sessionid varchar(100), item varchar(100), quantity bigint, price bigint) 
clustered by (sessionid) into 3 buckets
stored as parquet
location "adl://mypond.azuredatalakestore.net/gamelog/purchase";


-- To compact small part files into larger files -- 
--     same for Hive and Impala -- 
--     add "limit 10000000" at the end to honor bucketing, otherwise, all files will be combined to 1 --
insert overwrite table pqgeo select * from pqgeo;
insert overwrite table pqlevel select * from pqlevel;
insert overwrite table pqpurchase select * from pqpurchase;


-- Refresh csv tables in ADLS for visualization in Power BI --
--     ESRI map in Power BI can plot up to 30,000 [lat,lon] points --  
drop table if exists playergeo;
CREATE TABLE playergeo row format delimited fields terminated by ',' STORED AS textfile  
location "adl://mypond.azuredatalakestore.net/gamebi/playergeo"
as select from_unixtime(unix_timestamp(eventts), 'yyyy-MM-dd HH:mm') as slice, lat, lon, count(playerid) as players from pqgeo 
where eventts >= date_sub(current_date, 7)
group by from_unixtime(unix_timestamp(eventts), 'yyyy-MM-dd HH:mm'), lat, lon;

drop table if exists purchasegeo;
CREATE TABLE purchasegeo row format delimited fields terminated by ',' STORED AS textfile  
location "adl://mypond.azuredatalakestore.net/gamebi/purchasegeo"
as select item, price, lat, lon, sum(quantity) as sumquantity
from pqgeo g join pqpurchase p on g.sessionid = p.sessionid 
where g.eventts >= date_sub(current_date, 7)
group by item, price, lat, lon;

drop table if exists purchasefacts;
CREATE TABLE purchasefacts row format delimited fields terminated by ',' STORED AS textfile  
location "adl://mypond.azuredatalakestore.net/gamebi/purchasefacts"
as select item, price, lat, lon, level,
       sum(quantity) as sumquantity, 
       avg(unix_timestamp(p.eventts) - unix_timestamp(g.eventts)) as avgSeconds2Purchase,
       stddev_samp(unix_timestamp(p.eventts) - unix_timestamp(g.eventts)) as stdSeconds2Purchase,
       count(g.playerid) as players
from pqgeo g join pqpurchase p on g.sessionid = p.sessionid join pqlevel l on g.sessionid = l.sessionid
where g.eventts >= date_sub(current_date, 7)
group by item, price, lat, lon, level;


-- Sample ad-hoc queries in Impala-- 
--     Time to purchase -- 
select p.item, avg(cast(p.eventts as bigint) - cast(s.eventts as bigint)) as seconds2purchaseAvg,
    stddev(cast(p.eventts as bigint) - cast(s.eventts as bigint)) as seconds2purchaseStdDev,
    min(cast(p.eventts as bigint) - cast(s.eventts as bigint)) as seconds2purchaseMin,
    max(cast(p.eventts as bigint) - cast(s.eventts as bigint)) as seconds2purchaseStdMax
from pqgeo s join pqpurchase p on p.sessionid = s.sessionid group by p.item

--     Time to purchase by location -- 
select p.item, s.lat, s.lon, avg(cast(p.eventts as bigint) - cast(s.eventts as bigint)) as seconds2purchaseAvg,
    stddev(cast(p.eventts as bigint) - cast(s.eventts as bigint)) as seconds2purchaseStdDev,
    min(cast(p.eventts as bigint) - cast(s.eventts as bigint)) as seconds2purchaseMin,
    max(cast(p.eventts as bigint) - cast(s.eventts as bigint)) as seconds2purchaseMax
from pqgeo s join pqpurchase p on p.sessionid = s.sessionid group by p.item, s.lat, s.lon

--     Sales by location -- 
select p.item, s.lat, s.lon, sum(p.quantity * p.price) as sales
from pqgeo s join pqpurchase p on p.sessionid = s.sessionid group by p.item, s.lat, s.lon

--     Time to reach level -- 
select l.level, avg(cast(l.eventts as bigint) - cast(s.eventts as bigint)) as seconds2levelAvg,
    stddev(cast(l.eventts as bigint) - cast(s.eventts as bigint)) as seconds2levelStdDev,
    min(cast(l.eventts as bigint) - cast(s.eventts as bigint)) as seconds2levelMin,
    max(cast(l.eventts as bigint) - cast(s.eventts as bigint)) as seconds2levelMax
from pqgeo s join pqlevel l on l.sessionid = s.sessionid group by l.level
