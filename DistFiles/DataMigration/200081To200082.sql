-- Update database from version 200081 to 200082
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-- Fixed problem with migration from 79-80

/***********************************************************************************************
 * AddCustomField$
 *
 * Description:
 *	Adds a custom field to a class in the FieldWorks conceptual model schema.
 *
 * Parameters:
 *	@flid=the newly generated field Id (input/output parameter);
 *	@name=the name of the custom field;
 *	@type=the type code for the custom field;
 *	@clid=the class id of the class to which the field is being added;
 *	@clidDst=the class id of the target class for OwningAtom or ReferenceAtom fields (opt);
 *	@Min=the minimum value allowed for Type 2 integer field (opt);
 *	@Max=the maximum value allowed for Type 2 integer field (opt);
 *	@Big=flag that determines if a binary datatype should be stored as varbinary
 *		(@Big=0) or image (@Big=1) (opt)
 *
 * Notes:
 *	The @Big paramter has a default value of NULL because it is used only for 6 of 23
 *	possible types of custom fields.
 *
 * 	4/20/2001 (ValerieN) Modified only to insert into the Field$ table.  The other
 *	functionality is now in the trigger TR_Field$_UpdateModel_Ins  This procedure can be
 *	removed when we figure out how to auto-generate the [ID] with the class prefix.
 **********************************************************************************************/
if object_id('AddCustomField$') is not null begin
	print 'removing proc AddCustomField$'
	drop proc [AddCustomField$]
end
go
print 'creating proc AddCustomField$'
go
create proc [AddCustomField$]
	@flid int output,
	@name varchar(100),
	@type int,
	@clid int,
	@clidDst int = null,
	@Min bigint = null,
	@Max bigint = null,
	@Big bit = null,
	@nvcUserLabel	NVARCHAR(100) = NULL,
	@nvcHelpString	NVARCHAR(100) = NULL,
	@nListRootId	INT  = NULL,
	@nWsSelector	INT = NULL,
	@ntXmlUI		NTEXT = NULL
AS
	declare @flidNew int, @flidMax int
	declare @sql varchar(1000)
	declare @Err int

	-- If this procedure was called within a transaction, then create a savepoint; otherwise
	-- create a transaction.
	declare @nTrnCnt int
	set @nTrnCnt = @@trancount
	if @nTrnCnt = 0 begin tran AddCustomField$_Tran
	else save tran AddCustomField$_Tran

	-- calculate the new flid if needed
	if @flid is null or @flid = 0 begin
		select	@flidMax = max([Id])
		from	[Field$] (REPEATABLEREAD)
		where	[Class] = @clid
		if @flidMax is null or @flidMax - @clid * 1000 < 500 set @flidNew = 1000 * @clid + 500
		else set @flidNew = @flidMax + 1
		set @flid = @flidNew
	end
	else begin
		-- make sure the provided flid is legal
		if @flid < (@clid * 1000 + 500) or @flid > (@clid * 1000 + 999) begin
			set @Err = @flid
			goto HandleError
		end
		-- make sure the provided flid is not already used
		select @flidNew = [Id]
		from   [Field$] (REPEATABLEREAD)
		where  [Id] = @flid
		if (@flidNew is not null) begin
			set @Err = @flid
			goto HandleError
		end
		set @flidNew = @flid
	end

	-- perform the insert into Field$
	insert into [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [Min], [Max], [Big],
		UserLabel, HelpString, ListRootId, WsSelector, XmlUI)
	values (@flidNew, @type, @clid, @clidDst, @name, 1, @Min, @Max, @Big,
		@nvcUserLabel, @nvcHelpString, @nListRootId, @nWsSelector, @ntXmlUI)

	set @Err = @@error
	if @Err <> 0 goto HandleError

	if @nTrnCnt = 0 commit tran AddCustomField$_Tran
	return 0

HandleError:
	rollback tran AddCustomField$_Tran
	return @Err
GO
---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200081
begin
	update Version$ set DbVer = 200082
	COMMIT TRANSACTION
	print 'database updated to version 200082'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200081 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
