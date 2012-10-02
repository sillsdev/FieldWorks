-- update database FROM version 200110 to 200111
-- NOTE: This had to be completed by another migration (200111 to 200112).  Renaming fields
-- by updating Field$ does not rename the table columns!

BEGIN TRANSACTION  --( will be rolled back if wrong version#

-- FWM-102 "Changes related to LexPronunciation" --
-- 1) Create class CmMedia : CmObject
	-- Add CmMedia_Label: MultiString
	-- Add CmMedia_MediaFile: ReferenceAtomic of CmFile
-- 2) Add LanguageProject_Media: OwningCollection of CmFolder
-- 3) Change LexPronunciation class
	-- Add LexPronunciation_Location: ReferenceAtomic to CmLocation
	-- Add LexPronunciation_MediaFiles: OwningSequence of CmMedia
	-- Add LexPronunciation_CVPattern: String
	-- Add LexPronunciation_Tone: String
	-- Delete LexPronunciation_Sound (no data migration needed)
-- 4) Change LexEntry class & Migrate pronunciation data
	-- Add LexEntry_Pronunciations: OwningSequence of LexPronunciation
	-- Migrate existing LexEntry_Pronunciation(s) data to LexEntry_Pronunciations[0]
	-- Delete LexEntry_Pronunciation objects & field (data migration finished)

-- Create Function: fnGetFormatForWs
-- FWM-103 "Change residue fields to big string" --
	-- 1) Migrate LexEntry_ImportResidue from Unicode to BigString
	-- 2) Migrate LexSense_ImportResidue from Unicode to BigString
-- FWM-104 "Convert StringRepresentation to String"
	-- Migrate PhEnvironment_StringRepresentation from Unicode to BigString

-- TE CmPossibility Stuff(TE-1341 & TE-1750)

-- 1) Create class CmMedia : CmObject
	-- Add CmMedia_Label: MultiString (kcptMultiString 14)
	-- Add CmMedia_MediaFile: ReferenceAtomic (kcptReferenceAtom 24) of CmFile (47)
	insert into Class$ ([Id], [Mod], [Base], [Abstract], [Name])
		values(69, 0, 0, 0, 'CmMedia')
go
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(69001, 14, 69, null, 'Label',0,Null, null, null, null)
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(69002, 24, 69, 47, 'MediaFile',0,Null, null, null, null)
go

-- 2) Add LanguageProject_Media: OwningCollection (kcptOwningCollection 25) of CmFolder (2)
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(6001051, 25, 6001, 2, 'Media',0,Null, null, null, null)
go

-- 3) Change LexPronunciation class
	-- Add LexPronunciation_Location: ReferenceAtomic to CmLocation
	-- Add LexPronunciation_MediaFiles: OwningSequence of CmMedia
	-- Add LexPronunciation_CVPattern: String
	-- Add LexPronunciation_Tone: String
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(5014003, 24, 5014, 12, 'Location',0,Null, null, null, null)
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(5014004, 27, 5014, 69, 'MediaFiles',0,Null, null, null, null)
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(5014005, 13, 5014, null, 'CVPattern',0,Null, null, null, null)
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(5014006, 13, 5014, null, 'Tone',0,Null, null, null, null)
go

	-- Delete LexPronunciation_Sound (no data migration needed)
	DELETE from Field$ where id=5014002 -- LexPronunciation_Sound
go
-- 4) Change LexEntry class & Migrate pronunciation data
	-- Add LexEntry_Pronunciations: OwningSequence (kcptOwningSequence 27) of LexPronunciation (5014)
	-- Migrate existing LexEntry_Pronunciation(s) data to LexEntry_Pronunciations[0]
	-- Delete LexEntry_Pronunciation objects & field (data migration finished)
	/* Create new field */
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(5002031, 27, 5002, 5014, 'Pronunciations',0,Null, null, null, null)

	/* Migration: Change owning flid to new OwningSequence and set OwnOrd to 1 */
	UPDATE CmObject SET OwnFlid$=5002031, OwnOrd$=1 WHERE OwnFlid$=5002016

	/* Delete Old field */
	DELETE from Field$ where id=5002016 -- LexEntry_Pronunciation (old)
go
-- Create Function: fnGetFormatForWs
/***********************************************************************************************
 * Function: fnGetFormatForWs
 *
 * Description:
 *	Create the minimal format value for a string in the given writing system.  This is needed,
 *  for example, in data migration when the type of a field has changed from Unicode to String.
 *
 * Parameters:
 *	@ws = database id of a writing system
 *
 * Returns:
 *   varbinary(20) value containing the desired format value (which uses 19 of the 20 bytes)
 *
 * Notes:
 *   This is more deterministic and reliable than former approaches, which were not even as good
 *   as the following SQL:
 *		SELECT TOP 1 Fmt
 *		FROM MultiStr$
 *		WHERE Ws=@ws AND DATALENGTH(Fmt) = 19
 *		GROUP BY Fmt
 *		ORDER BY COUNT(Fmt) DESC
 **********************************************************************************************/
if object_id('fnGetFormatForWs') is not null begin
	print 'removing function fnGetFormatForWs'
	drop function fnGetFormatForWs
end
go

create function fnGetFormatForWs (@ws int)
returns varbinary(20)
as
begin
	DECLARE @hexVal varbinary(20)

	-- one run with one property (the writing system), starting at the beginning of the string
	SET @hexVal= 0x010000000000000000000000010006

	-- CAST (@ws AS varbinary(4)) puts the bytes in the wrong order for the format string,
	-- so we'll add it one byte at a time in the desired order.
	DECLARE @byte int, @x1 int
	SET @byte = @ws % 256
	SET @x1 = @ws / 256
	SET @hexVal = @hexVal  + CAST (@byte AS varbinary(1))

	SET @byte = @x1 % 256
	SET @x1 = @x1 / 256
	SET @hexVal = @hexVal  + CAST (@byte AS varbinary(1))

	SET @byte = @x1 % 256
	SET @x1 = @x1 / 256
	SET @hexVal = @hexVal  + CAST (@byte AS varbinary(1))

	SET @byte = @x1 % 256
	return @hexVal  + CAST (@byte AS varbinary(1))
end
go

-- FWM-103 "Change residue fields to big string" --
	/* Create new field LexEntry_ImportResidue */
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(5002032, 17, 5002, null, 'ImportResidue2',0,Null, null, null, null)
	/* Create new field LexSense_ImportResidue */
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(5016030, 17, 5016, null, 'ImportResidue2',0,Null, null, null, null)
	/* Create new field PhEnvironment_StringRepresentation (FWM-104) */
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(5097008, 13, 5097, null, 'StringRepresentation2',0,Null, null, null, null)
go
	/* Migration: change from Unicode to formatted string */
	-- The next bit of code (setting @fmtResidue) is shared with the LexSense migration
	DECLARE @fmtResidue varbinary(20)
	DECLARE @wsAnal int
	SELECT @wsAnal=Id FROM LgWritingSystem WHERE ICULocale=N'en'
	IF @wsAnal is null
		SELECT TOP 1 @wsAnal=Dst FROM LanguageProject_CurrentAnalysisWritingSystems ORDER BY Ord
	IF @wsAnal is null
		SELECT TOP 1 @wsAnal=Dst FROM LanguageProject_AnalysisWritingSystems
	SET @fmtResidue = dbo.fnGetFormatForWs(@wsAnal)

	UPDATE LexEntry
	SET ImportResidue2=ImportResidue, ImportResidue2_Fmt=@fmtResidue
	WHERE ImportResidue is not null

	-- 2) Migrate LexSense_ImportResidue from Unicode to BigString (kcptBigString 17)
	/* Migration: change from Unicode to formatted string */
	UPDATE LexSense
	SET ImportResidue2=ImportResidue, ImportResidue2_Fmt=@fmtResidue
	WHERE ImportResidue is not null

-- FWM-104 "Convert StringRepresentation to String"
	-- Migrate PhEnvironment_StringRepresentation from Unicode to BigString
	/* Migration from Unicode to formatted string*/
	UPDATE PhEnvironment
	SET StringRepresentation2=StringRepresentation, StringRepresentation2_Fmt=@fmtResidue
	WHERE StringRepresentation is not null

	/* Delete Old fields */
	DELETE from Field$ where id=5002028 -- LexEntry_ImportResidue (old)
-- Without this go, the next line will fail in some dbs (such as gilaki with data in the fields) when in a transaction.
-- The error is: Invalid column name 'ImportResidue'. something in the delete trigger is causing this.
go
	DELETE from Field$ where id=5016028 -- LexSense_ImportResidue (old)
go
	DELETE from Field$ where id=5097007 -- PhEnvironment_StringRepresentation (old)
go
	/* Rename New fields */
	ALTER TABLE Field$ DISABLE TRIGGER TR_Field$_No_Upd
	UPDATE Field$ SET Name='ImportResidue' WHERE Id=5002032 -- LexEntry_ImportResidue
	UPDATE Field$ SET Name='ImportResidue' WHERE Id=5016030 -- LexSense_ImportResidue
	UPDATE Field$ SET Name='StringRepresentation' WHERE Id=5097008 -- PhEnvironment_StringRepresentation
	ALTER TABLE Field$ ENABLE TRIGGER TR_Field$_No_Upd
go

-- TE CmPossibility Stuff (TE-1341 & TE-1750)
-------------------------------------------------------------------------------
-- Get a binary representation of the hvo for the English writing system for
-- use in Format fields.
-------------------------------------------------------------------------------
BEGIN
	DECLARE	@hvoNoteList int,
		@idPossibility int,
		@idFilter int,
		@idRow int,
		@idCell int,
		@matchTxt nvarchar(20),
		@formatEnOneRun varbinary(8000),
		@format varbinary(8000),
		@formatWithGuid varbinary(8000),
		@guid uniqueidentifier,
		@hvoEnglish int,
		@hvoHexEnglish binary(4),
		@byte1 int,
		@byte2 int,
		@byte3 int,
		@byte4 int

	SELECT	@hvoEnglish = [id]
	FROM	LgWritingSystem
	WHERE	[ICULocale] = 'en'

	SET @byte1 = @hvoEnglish / CONVERT(int, 0x1000000)
	SET @byte2 = (@hvoEnglish - @byte1 * 0x1000000) / CONVERT(int, 0x10000)
	SET @byte3 = (@hvoEnglish - @byte1 * 0x1000000 - @byte2 * 0x10000) / CONVERT(int, 0x100)
	SET @byte4 = @hvoEnglish - @byte1 * 0x1000000 - @byte2 * 0x10000 - @byte3 * 0x100
	-- Format fields require byte order to be reversed
	SET @hvoHexEnglish =	CONVERT(binary(1), @byte4) +
				CONVERT(binary(1), @byte3) +
				CONVERT(binary(1), @byte2) +
				CONVERT(binary(1), @byte1)

-------------------------------------------------------------------------------
-- Create a "Translator Note" CmPossibility, owned by the "Note" CmPossibility
-- (identified by a fixed GUID)
-------------------------------------------------------------------------------
	SELECT	@hvoNoteList = id
	FROM	cmobject
	WHERE	guid$='7FFC4EAB-856A-43CC-BC11-0DB55738C15B'

	SET	@formatEnOneRun = 0x010000000000000000000000010006 + @hvoHexEnglish

	EXEC	CreateObject_CmAnnotationDefn
		@CmPossibility_Name_ws = @hvoEnglish ,
		@CmPossibility_Name_txt = 'Translator Note',
		@CmPossibility_Abbreviation_ws = @hvoEnglish,
		@CmPossibility_Abbreviation_txt = 'TransNt',
		@CmPossibility_Description_ws = @hvoEnglish,
		@CmPossibility_Description_txt = 'Note written by translation team.',
		@CmPossibility_Description_fmt = @formatEnOneRun,
		@CmPossibility_IsProtected = 1,
		@CmAnnotationDefn_AllowsComment = 1,
		@CmAnnotationDefn_UserCanCreate = 1,
		@CmAnnotationDefn_CanCreateOrphan = 1,
		@CmAnnotationDefn_PromptUser = 1,
		@CmAnnotationDefn_CopyCutPastable = 1,
		@CmAnnotationDefn_ZeroWidth = 1,
		@Owner = @hvoNoteList,
		@OwnFlid = 7004,
		@NewObjId = @idPossibility OUTPUT,
		@NewObjGuid = null -- Version 22 actually gets this guid set to a known value

-------------------------------------------------------------------------------
-- Add built-in filters needed for the Notes view
-------------------------------------------------------------------------------
	SET	@matchTxt = CONVERT(nvarchar(20), 'Matches ') + NCHAR(0xFFFC)
	SET	@format = 0x0200000000000000000000000800000007000000010006 + @hvoHexEnglish + 0x010106 + @hvoHexEnglish + 0x06090300

	----------------------------------------------------
	-- Create Consultant Notes Filter
	----------------------------------------------------
	SET @idFilter = null
	SET @guid = null
	EXEC	CreateObject_CmFilter
		@CmFilter_Name = 'Consultant',
		@CmFilter_ClassId = 3018,
		@CmFilter_App = 'A7D421E1-1DD3-11D5-B720-0010A4B54856',
		@CmFilter_Type = 0,
		@CmFilter_ColumnInfo = '3018,34003',
		@CmFilter_ShowPrompt = 0,
		@CmFilter_PromptText = null,
		@Owner = 1,
		@OwnFlid =6001024,
		@NewObjId = @idFilter OUTPUT,
		@NewObjGuid = @guid OUTPUT

	SET @idRow = null
	SET @guid = null
	EXEC	CreateObject_CmRow
		@Owner = @idFilter,
		@OwnFlid = 9007,
		@NewObjId = @idRow OUTPUT,
		@NewObjGuid = @guid OUTPUT

	SET @idCell = null
	SET @guid = null
	SET @formatWithGuid = @format + 0x1A9BDE56E71CA142AA76512EBEFF0DDA
	EXEC	CreateObject_CmCell
		@CmCell_Contents = @matchTxt,
		@CmCell_Contents_fmt = @formatWithGuid,
		@Owner = @idRow,
		@OwnFlid = 10001,
		@NewObjId = @idCell OUTPUT,
		@NewObjGuid = @guid OUTPUT

	----------------------------------------------------
	-- Create Translator Notes Filter
	----------------------------------------------------
	SET @idFilter = null
	SET @guid = null
	EXEC	CreateObject_CmFilter
		@CmFilter_Name = 'Translator',
		@CmFilter_ClassId = 3018,
		@CmFilter_App = 'A7D421E1-1DD3-11D5-B720-0010A4B54856',
		@CmFilter_Type = 0,
		@CmFilter_ColumnInfo = '3018,34003',
		@CmFilter_ShowPrompt = 0,
		@CmFilter_PromptText = null,
		@Owner = 1,
		@OwnFlid =6001024,
		@NewObjId = @idFilter OUTPUT,
		@NewObjGuid = @guid OUTPUT


	SET @idRow = null
	SET @guid = null
	EXEC	CreateObject_CmRow
		@Owner = @idFilter,
		@OwnFlid = 9007,
		@NewObjId = @idRow OUTPUT,
		@NewObjGuid = @guid OUTPUT

	SET @idCell = null
	SET @guid = null
	SET @formatWithGuid = @format + 0x2957AE80D89C4D428E7196C1A8FD5821 -- see Version 22
	EXEC	CreateObject_CmCell
		@CmCell_Contents = @matchTxt,
		@CmCell_Contents_fmt = @formatWithGuid,
		@Owner = @idRow,
		@OwnFlid = 10001,
		@NewObjId = @idCell OUTPUT,
		@NewObjGuid = @guid OUTPUT

	----------------------------------------------------
	-- Create Open (Resolution Status) Filter
	----------------------------------------------------
	SET @idFilter = null
	SET @guid = null
	EXEC	CreateObject_CmFilter
		@CmFilter_Name = 'Open',
		@CmFilter_ClassId = 3018,
		@CmFilter_App = 'A7D421E1-1DD3-11D5-B720-0010A4B54856',
		@CmFilter_Type = 0,
		@CmFilter_ColumnInfo = '3018,3018001',
		@CmFilter_ShowPrompt = 0,
		@CmFilter_PromptText = null,
		@Owner = 1,
		@OwnFlid =6001024,
		@NewObjId = @idFilter OUTPUT,
		@NewObjGuid = @guid OUTPUT


	SET @idRow = null
	SET @guid = null
	EXEC	CreateObject_CmRow
		@Owner = @idFilter,
		@OwnFlid = 9007,
		@NewObjId = @idRow OUTPUT,
		@NewObjGuid = @guid OUTPUT

	SET @idCell = null
	SET @guid = null
	SET @matchTxt = '= 0'
	EXEC	CreateObject_CmCell
		@CmCell_Contents = @matchTxt,
		@CmCell_Contents_fmt = @formatEnOneRun,
		@Owner = @idRow,
		@OwnFlid = 10001,
		@NewObjId = @idCell OUTPUT,
		@NewObjGuid = @guid OUTPUT


	----------------------------------------------------
	-- Create Category Filter
	----------------------------------------------------
	SET @idFilter = null
	SET @guid = null
	EXEC	CreateObject_CmFilter
		@CmFilter_Name = 'Category',
		@CmFilter_ClassId = 3018,
		@CmFilter_App = 'A7D421E1-1DD3-11D5-B720-0010A4B54856',
		@CmFilter_Type = 0,
		@CmFilter_ColumnInfo = '3018,34003',
		@CmFilter_ShowPrompt = 1,
		@CmFilter_PromptText = 'Choose the category of note to display:',
		@Owner = 1,
		@OwnFlid =6001024,
		@NewObjId = @idFilter OUTPUT,
		@NewObjGuid = @guid OUTPUT

	SET @idRow = null
	SET @guid = null
	EXEC	CreateObject_CmRow
		@Owner = @idFilter,
		@OwnFlid = 9007,
		@NewObjId = @idRow OUTPUT,
		@NewObjGuid = @guid OUTPUT

	SET @idCell = null
	SET @guid = null
	SET @matchTxt = 'Matches'
	EXEC	CreateObject_CmCell
		@CmCell_Contents = @matchTxt,
		@CmCell_Contents_fmt = @formatEnOneRun,
		@Owner = @idRow,
		@OwnFlid = 10001,
		@NewObjId = @idCell OUTPUT,
		@NewObjGuid = @guid OUTPUT

END


-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200110
begin
	UPDATE Version$ SET DbVer = 200111
	COMMIT TRANSACTION
	print 'database updated to version 200111'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200110 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
