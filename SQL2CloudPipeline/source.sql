--------------------------------------
--  simulate original OLTP table
--------------------------------------

IF (EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'srcoltp'))
BEGIN
    drop table dbo.srcoltp
END

create table dbo.srcoltp (accountkey int not null, accountdescription nvarchar(50), unitsold int, createdat datetime,
	constraint [pk_srcoltp] primary key nonclustered (accountkey)
	) with (memory_optimized = on, durability = schema_and_data)

IF EXISTS (SELECT * FROM SYS.OBJECTS WHERE OBJECT_ID=OBJECT_ID('dbo.AccountInsertSP'))  
  DROP PROCEDURE dbo.AccountInsertSP  
go 

create procedure dbo.AccountInsertSP (@startKey int, @endKey int, @incRec int)
with native_compilation, schemabinding
as
begin atomic with (transaction isolation level = snapshot, language = N'English')
	declare @outerloop int = @startKey
	declare @i int = 0
	declare @curtime datetime = getutcdate()
	while (@outerloop < @endKey)
	begin
		while (@i < @incRec)
		begin
			insert dbo.srcoltp values (@i + @outerloop, 'test' + cast(@outerloop as varchar), @i, @curtime)
			set @i += 1;
		end

		set @outerloop += @incRec
		set @i = 0
		set @curtime = getutcdate()
	end
end

set nocount on
go
set statistics time off
go
set statistics IO off
go

delete from dbo.srcoltp
go

exec dbo.AccountInsertSP @startKey = 70000, @endKey = 80000, @incRec = 200
go

select count(*) from srcoltp

--alter table srcoltp add index srcoltp_cci clustered columnstore

select object_name(object_id), index_id, row_group_id, delta_store_hobt_id, state_desc, total_rows, trim_reason_desc, transition_to_compressed_state_desc
from sys.dm_db_column_store_row_group_physical_stats
where object_id = object_id('srcoltp')

set statistics time on
go
select avg(convert(bigint, unitsold)) from dbo.srcoltp
select avg(convert(bigint, unitsold)) from dbo.srcoltp with (index = [pk_srcoltp])

select * from sys.dm_db_xtp_table_memory_stats where object_id = object_id('dbo.srcoltp')  
select * from sys.dm_db_xtp_table_memory_stats where object_id = object_id('dbo.destolap')  
