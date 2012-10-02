if object_id('DisplayName_PhEnvironment') is not null
	drop proc DisplayName_PhEnvironment
go
print 'creating proc DisplayName_PhEnvironment'
go
/***********************************************************************************************
 * DisplayName_PhEnvironment
 * Returns a list of ids and strings.
 * The ids are PhEnvironment objects given in the XML input.
 * The strings are a 'pretty print' representation of an environment
 * in the form of a string environment constraint, as used by XAmple
 * (e.g., # [C] _ a).
 * Parameters
 *   @XMLIds - An XMl string that contains one or more ids to work on.
***********************************************************************************************/
create proc DisplayName_PhEnvironment
	@XMLIds ntext = null
as
	declare @retval int, @fIsNocountOn int,
		@EnvId int, @EnvTxt nvarchar(4000),
		@CurContext int, @Txt nvarchar(4000),
		@myCursor CURSOR

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	--Table variable.
	declare @DisplayNamePhEnvironment table (
		EnvId int primary key,
		--( See the notes under string tables in FwCore.sql about the
		--( COLLATE clause.
		EnvTxt NVARCHAR(4000) COLLATE Latin1_General_BIN
		)

	if @XMLIds is null begin
		-- Do all environments.
		set @myCursor = CURSOR FAST_FORWARD for
			select Id
			from PhEnvironment
			order by id
		open @myCursor
	end
	else begin
		-- Do environments provided in xml string.
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
				and cmo.Class$ = 5097
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
	fetch next from @myCursor into @EnvId
	while @@fetch_status = 0
	begin
		select @EnvTxt = isnull(StringRepresentation, '_')
		from PhEnvironment env
		where Id = @EnvId

		-- Update the table variable
		insert @DisplayNamePhEnvironment (EnvId, EnvTxt)
		values (@EnvId, @EnvTxt)

		-- Try for another one.
		fetch next from @myCursor into @EnvId
	end

	select * from @DisplayNamePhEnvironment
	set @retval = 0

LExitWithCursor:
	close @myCursor
	deallocate @myCursor

LExitNoCursor:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @retval
go
