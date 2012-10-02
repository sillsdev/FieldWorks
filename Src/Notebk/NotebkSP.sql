/***********************************************************************************************
	Final model creation processes for the Data Notebook domain.

	Note: These declarations need to be ordered, such that if stored procedure X
	calls stored procedure Y, then X should be created first, then Y.
	Doing this avoids an error message about dependencies.
***********************************************************************************************/

print '**************************** Loading NotebkSP.sql ****************************'
go

/***********************************************************************************************
 * Function: fnGetAddedNotebookObjects$
 *
 * Description:
 *		Returns a table of object ids for Possibility List items that have been added since the
 *		"factory" items were loaded from an XML parse.  This function is used by the
 *		DeleteAddedNotebookObjects$ stored procedure defined below.
 *
 * Parameters:
 *		none
 *
 * Returns:
 *		Table containing the object ids in the format:
 *			[ObjId] int
 **********************************************************************************************/

if object_id('fnGetAddedNotebookObjects$') is not null begin
	print 'removing function fnGetAddedNotebookObjects$'
	drop function [fnGetAddedNotebookObjects$]
end
go
print 'creating function fnGetAddedNotebookObjects$'
go
create function [fnGetAddedNotebookObjects$] ()
returns @DelList table ([ObjId] int not null)
as
begin
	declare @nRowCnt int
	declare @nOwnerDepth int
	declare @Err int
	set @nOwnerDepth = 1
	set @Err = 0

	insert into @DelList
	select [Id] from CmObject
	where OwnFlid$ = 4001001 --( kflidRnResearchNbk_Records

	-- use a table variable to hold the possibility list item object ids
	declare @PossItems table (
		[ObjId] int primary key not null,
		[OwnerDepth] int null,
		[DateCreated] datetime null
	)

	-- Get the object ids for all of the possibility items in the lists used by Data Notebook
	-- (except for the Anthropology List, which is loaded separately).
	-- Note the hard-wired sets of possibility list flids.
	-- First, get the top-level possibility items from the standard data notebook lists.

	insert into @PossItems (ObjId, OwnerDepth, DateCreated)
	select co.[Id], @nOwnerDepth, cp.DateCreated
	from [CmObject] co
	join [CmPossibility] cp on cp.[id] = co.[id]
	join CmObject co2 on co2.[id] = co.Owner$ and co2.OwnFlid$ in (
			4001003, --( kflidRnResearchNbk_EventTypes
			6001025, --( kflidLangProject_ConfidenceLevels
			6001026, --( kflidLangProject_Restrictions
			6001027, --( kflidLangProject_WeatherConditions
			6001028, --( kflidLangProject_Roles
			6001029, --( kflidLangProject_AnalysisStatus
			6001030, --( kflidLangProject_Locations
			6001031, --( kflidLangProject_People
			6001032, --( kflidLangProject_Education
			6001033, --( kflidLangProject_TimeOfDay
			6001036  --( kflidLangProject_Positions
			)

	if @@error <> 0 goto LFail
	set @nRowCnt=@@rowcount

	-- Repeatedly get the list items owned at the next depth.

	while @nRowCnt > 0 begin
		set @nOwnerDepth = @nOwnerDepth + 1

		insert into @PossItems (ObjId, OwnerDepth, DateCreated)
		select co.[id], @nOwnerDepth, cp.DateCreated
		from [CmObject] co
		join [CmPossibility] cp on cp.[id] = co.[id]
		join @PossItems pi on pi.[ObjId] = co.[Owner$] and pi.[OwnerDepth] = @nOwnerDepth - 1

		if @@error <> 0 goto LFail
		set @nRowCnt=@@rowcount
	end

	-- Extract all the items which are newer than the language project, ie, which cannot be
	-- factory list items.
	-- Omit list items which are owned by other non-factory list items, since they will be
	-- deleted by deleting their owner.

	insert into @DelList
	select pi.ObjId
	from @PossItems pi
	join CmObject co on co.[id] = pi.ObjId
	where pi.DateCreated > (select top 1 DateCreated from CmProject order by DateCreated DESC)

	delete from @PossItems

	-- Get the object ids for all of the possibility items in the anthropology list.
	-- First, get the top-level possibility items from the anthropology list.

	set @nOwnerDepth = 1

	insert into @PossItems (ObjId, OwnerDepth, DateCreated)
	select co.[Id], @nOwnerDepth, cp.DateCreated
	from [CmObject] co
	join [CmPossibility] cp on cp.[id] = co.[id]
	where co.[Owner$] in (select id from CmObject where OwnFlid$ = 6001012)

	set @nRowCnt=@@rowcount
	if @@error <> 0 goto LFail

	-- Repeatedly get the anthropology list items owned at the next depth.

	while @nRowCnt > 0 begin
		set @nOwnerDepth = @nOwnerDepth + 1

		insert into @PossItems (ObjId, OwnerDepth, DateCreated)
		select co.[id], @nOwnerDepth, cp.DateCreated
		from [CmObject] co
		join [CmPossibility] cp on cp.[id] = co.[id]
		join @PossItems pi on pi.[ObjId] = co.[Owner$] and pi.[OwnerDepth] = @nOwnerDepth - 1

		if @@error <> 0 goto LFail
		set @nRowCnt=@@rowcount
	end

	declare @cAnthro int
	declare @cTimes int
	select @cAnthro = COUNT(*) from @PossItems
	select @cTimes = COUNT(distinct DateCreated) from @PossItems

	if @cTimes = @cAnthro begin
		-- Assume that none of them are factory if they all have different creation
		-- times.  This is true even if there's only one item.
		insert into @DelList
		select pi.ObjId
		from @PossItems pi
		where pi.OwnerDepth = 1
	end
	else if @cTimes != 1 begin
		-- assume that the oldest items are factory, the rest aren't
		insert into @DelList
		select pi.ObjId
		from @PossItems pi
		where pi.DateCreated > (select top 1 DateCreated from @PossItems order by DateCreated)
	end

return

LFail:
	delete from @DelList
	return
end

go

/***********************************************************************************************
 * DeleteAddedNotebookObjects$
 *
 * Description:
 *		Removes an object and any objects it owns from the database, also references (ref.
 *		pointers, ref. collections, and ref. sequences) are appropriately cleaned up
 *
 * Parameters:
 *		none
 *
 * Returns:
 *		0 if successful, otherwise an error code
 **********************************************************************************************/

if object_id('DeleteAddedNotebookObjects$') is not null begin
	print 'removing proc DeleteAddedNotebookObjects$'
	drop proc [DeleteAddedNotebookObjects$]
end
go
print 'creating proc DeleteAddedNotebookObjects$'
go
create proc [DeleteAddedNotebookObjects$]
as
	declare @Err int
	set @Err = 0

	-- determine if the procedure was called within a transaction;
	-- if yes then create a savepoint, otherwise create a transaction
	declare @nTrnCnt int
	set @nTrnCnt = @@trancount
	if @nTrnCnt = 0 begin tran DelObj$_Tran
	else save tran DelObj$_Tran
	set @Err = @@error
	if @Err <> 0 begin
		raiserror ('DeleteAddedNotebookObjects$: SQL Error %d; Unable to create a transaction.', 16, 1, @Err)
		goto LFail
	end

	-- delete the objects (records and list items) added to the data notebook
	-- first, build a comma delimited string containing all of the object ids

	declare @ObjId int
	declare @CommaDelimited varchar(8000)
	declare @cObj int
	set @CommaDelimited = ',' --( The stored procedure will if you don't, anyway.
	set @cObj = 0

	DECLARE curObj CURSOR FOR SELECT [ObjId] FROM dbo.fnGetAddedNotebookObjects$()
	OPEN curObj
	FETCH NEXT FROM curObj INTO @ObjId
	WHILE @@FETCH_STATUS = 0
	BEGIN
		set @CommaDelimited = @CommaDelimited + cast(@ObjId as varchar(10)) + ','
		set @cObj = @cObj + 1
		if len(@CommaDelimited) > 7970 begin
			-- we are close to filling the string, so delete all the
			-- objects (in one swell foop).
			EXEC @Err = DeleteObjects @CommaDelimited;
			set @cObj = 0
			if @Err <> 0 goto LFail
		end
		FETCH NEXT FROM curObj INTO @ObjId
	END
	CLOSE curObj
	DEALLOCATE curObj

	if @cObj <> 0 begin
		-- now, delete all the objects (in one swell foop).
		EXEC @Err = DeleteObjects @CommaDelimited;
		if @Err <> 0 goto LFail
	end

	if @nTrnCnt = 0 commit tran DelObj$_Tran

	return 0

LFail:
	rollback tran DelObj$_Tran
	return @Err

print '*********************** Finished loading NotebkSP.sql ************************'
go
