--------------------------------------
--  simulate original OLTP table
--------------------------------------

IF (EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'srcoltp'))
BEGIN
    drop table srcoltp
END

create table srcoltp (accountkey int not null, accountdescription nvarchar(50), unitsold int, createdat datetime,
	constraint [pk_srcoltp] primary key nonclustered (accountkey)
	) with (memory_optimized = on, durability = schema_and_data)

set nocount on
go
set statistics time off
go
set statistics IO off
go

declare @outerloop int = 0
declare @i int = 0
declare @curtime datetime = getutcdate()
while (@outerloop < 10)
begin
	begin tran
	while (@i < 2)
	begin
		insert srcoltp values (@i + @outerloop, 'test' + cast(@outerloop as varchar), @i, @curtime)
		set @i += 1;
	end
	commit

	set @outerloop += 2
	set @i = 0
	set @curtime = getutcdate()
end

declare @outerloop int = 11000001
declare @i int = 0
declare @curtime datetime = getutcdate()
while (@outerloop < 12000000)
begin
	begin tran
	while (@i < 2000)
	begin
		insert srcoltp values (@i + @outerloop, 'test' + cast(@outerloop as varchar), @i, @curtime))
		set @i += 1;
	end
	commit

	set @outerloop += 2000
	set @i = 0
    set @curtime = getutcdate()
end

--alter table srcoltp add index srcoltp_cci clustered columnstore

select object_name(object_id), index_id, row_group_id, delta_store_hobt_id, state_desc, total_rows, trim_reason_desc, transition_to_compressed_state_desc
from sys.dm_db_column_store_row_group_physical_stats
where object_id = object_id('srcoltp')

set statistics time on
go
select avg(convert(bigint, unitsold)) from srcoltp
select avg(convert(bigint, unitsold)) from srcoltp with (index = [pk_srcoltp])

select * from srcoltp
