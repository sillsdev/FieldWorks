if exists (select *
			 from sysobjects
			where name = 'Import_AppendPara')
	drop proc Import_AppendPara
go
print 'creating proc Import_AppendPara'
go
/*****************************************************************************
 * Import_AppendPara
 *
 * Description: Adds a paragraph to the end of the given StText.
 *
 * Parameters:
 *	hvoText		Id of owning StText
 *	ord		position for new paragraph - must be max(OwningOrd$ + 1)
 *	stuContents	Contents of new paragraph
 *	rgbContentsFmt	Format data for Contents
 *	hvoPara		id of new object - output (optional)
 *      rgbStParaFmt	Format data (named style) for StPara (optional)
 * Returns: Error code if an error occurs
 *
 *****************************************************************************/
create proc Import_AppendPara
	@hvoText int,
	@ord int,				-- position for new paragraph
	@stuContents ntext,		-- Contents of new paragraph
	@rgbContentsFmt image,	-- format data for Contents
	@hvoPara int = null output,		-- id of new object
	@rgbStParaFmt varbinary(8000) = null	-- format data for paragraph
as
	declare @clid int, @flid int, @guid uniqueidentifier,
		@err int, @nTrnCnt int, @sTranName varchar(50),
		@fIsNocountOn int

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	set @clid = 16 -- StTxtPara
	set @flid = 14001 -- StText_Paragraphs

	-- Determine if a transaction already exists; if one does then create a savepoint, otherwise create a
	-- transaction
	set @nTrnCnt = @@trancount
	set @sTranName = 'AppendPara_tr' + convert(varchar(8), @@nestlevel)
	if @nTrnCnt = 0 begin tran @sTranName
	else save tran @sTranName

	-- Create the new object
	set @guid = NewId()
	insert [CmObject] ([Guid$], [Class$], [Owner$], [OwnFlid$], [OwnOrd$])
	values(@guid, @clid, @hvoText, @flid, @ord)
	if @@error <> 0 begin
		set @err = @@error
		goto LFail
	end
	set @hvoPara = @@identity

	-- Insert into StPara
	insert [StPara] ([Id], [StyleRules])
	values(@hvoPara, @rgbStParaFmt)
	if @@error <> 0 begin
		set @err = @@error
		raiserror('Unable to add a row to StPara: ID=%d', 16, 1, @hvoPara)
		goto LFail
	end

	-- Insert into StTxtPara
	insert StTxtPara ([Id],[Contents],[Contents_Fmt])
	values (@hvoPara, @stuContents, @rgbContentsFmt)
	if @@error <> 0 begin
		set @err = @@error
		raiserror('Unable to add a row to StTextPara: ID=%d', 16, 1, @hvoPara)
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
