if object_id('DisplayName_PhPhonContext') is not null begin
	drop proc DisplayName_PhPhonContext
end
go
print 'creating proc DisplayName_PhPhonContext'
go
/***********************************************************************************************
 * DisplayName_PhPhonContext
 * Returns a list of ids and strings.
 * The ids are PhPhonContext objects given in the XML input.
 * The strings are a 'pretty print' representation of a PhPhonContext
 * in the form of a left or right context of a string environment constraint,
 * as used by XAmple (e.g., # [C] _ a).
 *
 * Parameters
 *   @XMLIds - XML string of ids to look for, or all of them, if null.
 *   @Cls - The class that is looked for, or all classes, if null.
***********************************************************************************************/
create proc DisplayName_PhPhonContext
	@XMLIds ntext = null
as
	declare @retval int, @fIsNocountOn int,
		@CtxId int, @CtxForm nvarchar(4000),
		@myCursor CURSOR

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	--Table variable.
	declare @DisplayNamePhPhonContext table (
		CtxId int primary key,
		CtxForm nvarchar(4000)
		)

	if @XMLIds is null begin
		-- Do all contexts.
		set @myCursor = CURSOR FAST_FORWARD for
			select Id
			from PhPhonContext
			order by id
		open @myCursor
	end
	else begin
		-- Do contexts provided in xml string.
		declare @hdoc int
		exec sp_xml_preparedocument @hdoc output, @XMLIds
		if @@error <> 0 begin
			set @retval = 1
			goto LExitNoCursor
		end
		set @myCursor = CURSOR FAST_FORWARD for
			select cmo.Id
			from	openxml(@hdoc, '/root/Obj')
			with ([Id] int) as ol
			-- Remove all pretenders, since they won't 'join'.
			join CmObject cmo
				On ol.Id=cmo.Id
				and cmo.Class$ IN (5082, 5083, 5085, 5086, 5087)
			order by ol.[Id]
		open @myCursor
		-- Turn loose of the handle
		exec sp_xml_removedocument @hdoc
		if @@error <> 0 begin
			set @retval = 1
			goto LExitWithCursor
		end
	end

	-- Loop through all ids.
	fetch next from @myCursor into @CtxId
	while @@fetch_status = 0
	begin
		exec @retval = DisplayName_PhPhonContextID @CtxId, @CtxForm output
		if @retval > 0 begin
			delete @DisplayNamePhPhonContext
			goto LExitWithCursor
		end
		-- Update the temporary table
		insert @DisplayNamePhPhonContext (CtxId, CtxForm)
		values (@CtxId, @CtxForm)

		-- Try for another one.
		fetch next from @myCursor into @CtxId
	end

	select * from @DisplayNamePhPhonContext
	set @retval = 0

LExitWithCursor:
	close @myCursor
	deallocate @myCursor

LExitNoCursor:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	return @retval
go
