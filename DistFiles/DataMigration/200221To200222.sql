-- Update database from version 200221 to 200222
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- FDB-202: Migration for Key Terms Enhancements (FWM-141)
-------------------------------------------------------------------------------

/*****************************************************************************
 * SetChkTermLocalizedInfo
 *
 * Description: Sets the Name, Description, and SeeAlso fields for a ChkTerm
 * for the given writing system. Stored procedure used by
 * TeKeyTermsInit.SetLocalizedInfo
 * Parameters:
 *	ObjId			Id of the ChkTerm
 *	WritingSystem	The ID of the localized Writing System
 *	Gloss			The primary gloss
 *	Description_txt	The text of the description of the term
 *	Description_fmt	The TsTxtProps of the description
 *	SeeAlso			Alternate glosses for the term. Multiple glosses are
 *					separated by semi-colons.
 * Returns: Error code if an error occurs
 *
 *****************************************************************************/

if exists (select *
			 from sysobjects
			where name = 'SetChkTermLocalizedInfo')
	drop proc SetChkTermLocalizedInfo
go
print 'creating proc SetChkTermLocalizedInfo'
go
CREATE PROC SetChkTermLocalizedInfo	@ObjId int,
	@WritingSystem int,	@Gloss nvarchar(max) = null, 	@Description_txt ntext = null,	@Description_fmt image = null, 	@SeeAlso nvarchar(max) = null AS
	declare @fIsNocountOn int, @Err int, @nTrnCnt int, @sTranName sysname, @Class int

	set @nTrnCnt = null

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- determine if a transaction already exists; if one does then create a savepoint, otherwise create a
	--	transaction
	set @nTrnCnt = @@trancount
	set @sTranName = 'SetChkTermLocalizedInfo_tr' + convert(varchar(2), @@nestlevel)
	if @nTrnCnt = 0 begin tran @sTranName
	else save tran @sTranName

	if @Gloss is not null begin		INSERT INTO CmPossibility_Name WITH (ROWLOCK) (Obj, Ws, Txt)		values (@ObjId,@WritingSystem,@Gloss)		set @Err = @@error		if @Err <> 0 goto LCleanUp	end	if @Description_txt is not null begin		insert into [MultiBigStr$] with (rowlock) ([Flid],[Obj],[Ws],[Txt],[Fmt])		values (7003,@ObjId,@WritingSystem,@Description_txt,@Description_fmt)		set @Err = @@error		if @Err <> 0 goto LCleanUp	end	if @SeeAlso is not null begin		INSERT INTO ChkTerm_SeeAlso WITH (ROWLOCK) (Obj, Ws, Txt)		values (@ObjId,@WritingSystem,@SeeAlso)		set @Err = @@error		if @Err <> 0 goto LCleanUp	end
LCleanUp:

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- determine if a transaction or savepoint was created
	if @nTrnCnt is not null begin
		if @Err = 0 begin
			-- if a transaction was created within this procedure commit it
			if @nTrnCnt = 0 commit tran @sTranName
		end
		else begin
			rollback tran @sTranName
		end
	end

	return @Err
GO

---------------------------------------------------------------------------------
---- Finish or roll back transaction as applicable
---------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200221
BEGIN
	UPDATE Version$ SET DbVer = 200222
	COMMIT TRANSACTION
	PRINT 'database updated to version 200222'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200221 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
