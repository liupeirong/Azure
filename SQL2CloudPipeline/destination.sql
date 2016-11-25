--------------------------------------
--  simulate target OLTP + OLAP table
--------------------------------------

IF (EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'destolap'))
BEGIN
    drop table destolap
END

create table destolap (accountkey int not null, accountdescription nvarchar(50), unitsold int, createdat datetime,
    tag nvarchar(50), publishedat datetime, consumedat datetime, storedat datetime default(getutcdate()),
	constraint [pk_destolap] primary key nonclustered (accountkey)
	) with (memory_optimized = on, durability = schema_and_data)

alter table destolap add index destolap_cci clustered columnstore

select * from destolap
select object_name(object_id), index_id, row_group_id, delta_store_hobt_id, state_desc, total_rows, trim_reason_desc, transition_to_compressed_state_desc
from sys.dm_db_column_store_row_group_physical_stats
where object_id = object_id('destolap')

set statistics time on
go
select avg(convert(bigint, unitsold)) from destolap
select avg(convert(bigint, unitsold)) from destolap with (index (pk_destolap))

-----------------------------------------
--   compute statistics for power bi
-----------------------------------------
IF (EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'pipelineduration'))
BEGIN
    drop table pipelineduration
END
create table pipelineduration (tag nvarchar(50), publishtakes int, consumetakes int, storetakes int, queriedat datetime default(getutcdate()))

IF (EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'pipelinethroughput'))
BEGIN
    drop table pipelinethroughput
END
create table pipelinethroughput (tag nvarchar(50), recordcnt int, create2storesec int, create2storetps int, 
	publish2storesec int, publish2storetps int, queriedat datetime default(getutcdate()))

IF (EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'queryduration'))
BEGIN
    drop table queryduration
END
create table queryduration (tag nvarchar(50), querytakes int, queriedat datetime default(getutcdate()))

IF (EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'lastanalyzedkey'))
BEGIN
    drop table lastanalyzedkey
END
create table lastanalyzedkey (accountkey int)

IF EXISTS (SELECT * FROM SYS.OBJECTS WHERE OBJECT_ID=OBJECT_ID('dbo.AccountAnalyzeSP'))  
  DROP PROCEDURE dbo.AccountAnalyzeSP  
go 

create procedure dbo.AccountAnalyzeSP as
begin 

	declare @lastkey int
	select @lastkey = accountkey from lastanalyzedkey
	set @lastkey = isnull(@lastkey, -1)

	declare @begintime datetime
	select @begintime = getutcdate()

	insert into pipelineduration (tag, publishtakes, consumetakes, storetakes)
	select distinct tag, 
	  percentile_disc(0.9) within GROUP (order by DATEDIFF(second, createdat, publishedat)) over (partition by tag) as pubduration,
	  percentile_disc(0.9) within GROUP (order by DATEDIFF(second, publishedat, consumedat)) over (partition by tag) as conduration,
	  percentile_disc(0.9) within GROUP (order by DATEDIFF(second, consumedat, storedat)) over (partition by tag) as storduration
	from destolap with (SNAPSHOT)
	where accountkey > @lastkey

	if @@rowcount > 0 
	begin
		insert into pipelinethroughput (tag, recordcnt, create2storesec, create2storetps, publish2storesec, publish2storetps)
		select tag, count(accountkey), 
				datediff(second, min(createdat), max(storedat)), 
				count(accountkey)/datediff(second, min(createdat), max(storedat)),
				datediff(second, min(publishedat), max(storedat)), 
				count(accountkey)/datediff(second, min(publishedat), max(storedat))
		from destolap with (SNAPSHOT)
		where accountkey > @lastkey
		group by tag

		insert into queryduration (tag, querytakes)
		select 'mssql', datediff(millisecond, @begintime, getutcdate())

		select @lastkey = max (accountkey) from destolap
		update lastanalyzedkey set accountkey = @lastkey 
		if @@rowcount = 0 insert into lastanalyzedkey values (@lastkey)
	end
end

set nocount on
go
set statistics time off
go
set statistics IO off
go

exec AccountAnalyzeSP

select count(*) from destolap 
select * from pipelineduration
select * from pipelinethroughput
select * from queryduration
select * from lastanalyzedkey


truncate table pipelineduration
truncate table pipelinethroughput
truncate table queryduration
truncate table lastanalyzedkey
delete from destolap