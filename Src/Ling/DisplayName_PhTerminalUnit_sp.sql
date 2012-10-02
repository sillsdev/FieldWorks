if object_id('DisplayName_PhTerminalUnit') is not null begin
	drop proc DisplayName_PhTerminalUnit
end
go
print 'creating proc DisplayName_PhTerminalUnit'
go
/***********************************************************************************************
 * Procedure: DisplayName_PhTerminalUnit
 * Description: This procedure returns display information for one subclass of PhTerminalUnit.
 * Assumptions:
 *	The input XML is of the form: <root><Obj Id="7164"/><Obj Id="7157"/>...</root>
 * Parameters:
 *    @XMLIds - the object IDs of the MSA(s), or all of them if null
 *    @Cls - ID for class of objects to return.
 * Return: 0 if successful, otherwise 1.
***********************************************************************************************/
create proc [DisplayName_PhTerminalUnit]
	@XMLIds ntext = null,
	@Cls int = 5092	-- PhPhoneme. 5091 is PhBdryMarker
as

declare @retval int, @fIsNocountOn int,
	@TUId int, @TUForm nvarchar(4000),
	@myCursor CURSOR

	if @Cls < 5091 or @Cls > 5092
		return 1	-- Wrong class.

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	--Table variable.
	declare @DisplayNameTU table (
		TUId int,	-- 1
		TUForm nvarchar(4000)	-- 2
		)

	if @XMLIds is null begin
		-- Do all MSAes.
		set @myCursor = CURSOR FAST_FORWARD for
			select Id
			from CmObject
			where Class$ = @Cls
			order by Id
		open @myCursor
	end
	else begin
		-- Do MSAes provided in xml string.
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
				and cmo.Class$ = @Cls
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
	fetch next from @myCursor into @TUId
	while @@fetch_status = 0
	begin
		set @TUForm = '***'
		select top 1 @TUForm = isnull(Txt, '***')
		from PhTerminalUnit_Name
		where Obj = @TUId
		order by Ws

		select top 1 @TUForm = @TUForm + ' : ' + isnull(r.Txt, '***')
		from PhTerminalUnit_Codes c
		left outer join PhCode_Representation r On r.Obj = c.Dst
		where c.Src = @TUId
		order by c.Ord, r.Ws

		--Put everything in temporary table
		insert @DisplayNameTU (TUId, TUForm)
		values (@TUId, @TUForm)
		-- Try for another MSA.
		fetch next from @myCursor into @TUId
	end

	set @retval = 0
	select * from @DisplayNameTU order by TUForm

LExitWithCursor:
	close @myCursor
	deallocate @myCursor

LExitNoCursor:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @retval
go
