/***********************************************************************************************
	This procedure copies all CmObjects to the ObjHierarchy$ table and orders
	them in a hierarchy according to ownership as ordered by the strDepth column.
	This is a string column and so string comparisons are used to order it.
	To list the objects in order of ownership enter
		select * from ObjHierarchy$ order by strDepth
***********************************************************************************************/
if not object_id('UpdateHierarchy') is null begin
	print 'removing proc UpdateHierarchy'
	drop proc UpdateHierarchy
end
print 'creating proc UpdateHierarchy'
go
create proc UpdateHierarchy
as
	set nocount on

	--  Delete all objects in the ObjHierarchy$ table.
	truncate table ObjHierarchy$

	--  Copy all the CmObject records to the ObjHierarchy$ table.
	insert into ObjHierarchy$ (strDepth, intDepth, ownOrd, ownFlid, owner, class, guid, id)
		select NULL, NULL, ownOrd$, ownFlid$, owner$, class$, guid$, id from CmObject


	--  Set the intDepth=1 and strDepth for top level (root) objects.
	--  These will be numbered according to the order of id.

	declare @intChildCounter int
	declare @intId int
	declare @intNumChildren int
	declare @intNumSigDigits int

	select @intChildCounter=1
	select @intNumChildren=count(*) from ObjHierarchy$ where owner is null
	select @intNumSigDigits=log10(@intNumChildren + 0.5) + 1

	declare curRootObjects cursor FAST_FORWARD for select id from objhierarchy$ where owner is null order by id
	open curRootObjects
	fetch next from curRootObjects into @intId
	while @@fetch_status = 0
	begin
		update ObjHierarchy$ set intDepth=1,
			strDepth=replicate('0', @intNumSigDigits - log10(@intChildCounter + 0.5)) + cast(@intChildCounter as varchar)
			where id=@intId
		select @intChildCounter=@intChildCounter + 1
		fetch next from curRootObjects into @intId
	end
	close curRootObjects
	deallocate curRootObjects


	--  Determine how many objects there are

	declare @intDepth int
	declare @strDepth varchar(100)

	select @intDepth=2
	select @intNumChildren=count(*) from ObjHierarchy$ where owner in (select id from ObjHierarchy$ where intDepth=1)
	select @intNumSigDigits=log10(@intNumChildren + 0.5) + 1

	while (@intNumChildren > 0)
	begin
		select @intChildCounter=1

		-- This is ordered by FLID and OWNORD so 2 or more owning sequences are not interleaved

		declare curObjects cursor FAST_FORWARD for select c.id, p.strDepth from objhierarchy$ c, objHierarchy$ p where c.owner=p.id and p.intdepth=(select max(intDepth) from ObjHierarchy$ where intDepth is not NULL) order by p.strDepth, c.ownFlid, c.ownOrd, c.id
		open curObjects
		fetch next from curObjects into @intId, @strDepth
		while @@fetch_status = 0
		begin
			update ObjHierarchy$ set intDepth=@intDepth,
				strDepth=@strDepth + replicate('0', @intNumSigDigits - log10(@intChildCounter + 0.5)) + cast(@intChildCounter as varchar)
				where id=@intId
			select @intChildCounter=@intChildCounter + 1
			fetch next from curObjects into @intId, @strDepth
		end
		close curObjects
		deallocate curObjects

		--  Increment the depth and see if there are more child records
		select @intNumChildren=count(*) from ObjHierarchy$ where owner in (select id from ObjHierarchy$ where intDepth=@intDepth)
		select @intDepth=@intDepth + 1
		if (@@ROWCOUNT = 0 ) return
		select @intNumSigDigits=log10(@intNumChildren + 0.5) + 1
	end
go
