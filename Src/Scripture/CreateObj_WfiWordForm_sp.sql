if exists (select *
			 from sysobjects
			where name = 'CreateObj_WfiWordForm')
	drop proc CreateObj_WfiWordForm
go
print 'creating proc CreateObj_WfiWordForm'
go
/*****************************************************************************
 * CreateObj_WfiWordForm
 *
 * Description: Adds a WfiWordForm object, if needed. If the wordform to be
 * created already exists, then just return the HVO. Otherwise, create it and
 * set its Form property using the given writing system.
 * Parameters:
 *	hvoWfi		Id of owning Wordform Inventory
 *	stuForm		wordform
 *	ws		character offset into StTxtPara
 *	id		Id of new (or existing) wordform
 *	nSpellingStat	Spelling status
 * Returns: Error code if an error occurs
 *
 *****************************************************************************/
create proc CreateObj_WfiWordForm
	@hvoWfi		int,
	@stuForm	nvarchar(4000),
	@Ws		int,
	@id		int = null output,
	@nSpellingStat	tinyint = 0 output
as
	-- Begin by checking to see if it already exists. If so, then our
	-- job is easy.
	select	@id = f.[Obj],
		@nSpellingStat = w.[SpellingStatus]
	from	WfiWordform_Form f
	join	WfiWordform w on w.[id] = f.[Obj]
	where	CONVERT ( varbinary(8000), f.[Txt]) = CONVERT ( varbinary(8000), @stuForm)
	and	f.[Ws] = @Ws

	if @id is not null return 0

	declare @clid int, @flid int, @guid uniqueidentifier,
		@err int, @nTrnCnt int, @sTranName varchar(50),
		@fIsNocountOn int

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	set @clid = 5062 -- WfiWordform
	set @flid = 5063001 -- WordformInventory_Wordforms

	-- Determine if a transaction already exists; if one does then create a savepoint, otherwise create a
	-- transaction
	set @nTrnCnt = @@trancount
	set @sTranName = 'WfiWordform_tr' + convert(varchar(8), @@nestlevel)
	if @nTrnCnt = 0 begin tran @sTranName
	else save tran @sTranName

	-- Create the new object
	set @guid = NewId()
	insert [CmObject] ([Guid$], [Class$], [Owner$], [OwnFlid$], [OwnOrd$])
	values(@guid, @clid, @hvoWfi, @flid, NULL)
	if @@error <> 0 begin
		set @err = @@error
		goto LFail
	end
	set @id = @@identity

	-- Insert into WfiWordform
	insert WfiWordform ([Id], [SpellingStatus])
	values (@id, @nSpellingStat)
	if @@error <> 0 begin
		set @err = @@error
		raiserror('Unable to add a row to WfiWordform: ID=%d', 16, 1, @id)
		goto LFail
	end

	-- Insert into WfiWordform_Form
	insert WfiWordform_Form ([Obj],[Ws],[Txt])
	values (@id, @Ws, @stuForm)
	if @@error <> 0 begin
		set @err = @@error
		raiserror('Unable to add a row to WfiWordform_Form: ID=%d, Ws=%d, Txt=%s', 16, 1, @id, @Ws, @stuForm)
		goto LFail
	end

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	-- if a transaction was created within this procedure commit it
	if @nTrnCnt = 0 commit tran @sTranName
	return 0

LFail:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	rollback tran @sTranName
	return @err
GO
