-- Update database from version 200054 to 200055

--( Steve Miller, Dec. 6, 2006: removed readuncommitted.

BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------
-- This trigger was updated sometime after V2, so when migrating from V2 we need the fixed version.
-------------------------------------------------------------
if object_id('TR_CmObject_ValidateOwner') is not null begin
	print 'removing trigger TR_CmObject_ValidateOwner'
	drop trigger TR_CmObject_ValidateOwner
end
go
print 'creating trigger TR_CmObject_ValidateOwner'
go
create trigger [TR_CmObject_ValidateOwner] on [CmObject] for update
as
	--( We used to check to not allow an object's class to be changed,
	--( similar to the check for update([Id]) immediately below. We
	--( have since found the need to change Lex Entries. For instance,
	--( a LexSubEntry can turn into a LexMajorEntry.

	if update([Id]) begin
		raiserror('An object''s Id cannot be changed', 16, 1)
		rollback tran
	end

	-- only perform checks if one of the following columns are updated: id, owner$, ownflid$, or
	--	ownord$	because updates to UpdDttm or UpdStmp do not require the below validations
	if not ( update([Owner$]) or update([OwnFlid$]) or update([OwnOrd$]) )  return

	declare @idBad int, @own int, @flid int, @ord int, @cnt int
	declare @dupownId int, @dupseqId int
	declare @fIsNocountOn int

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	if update([Owner$]) or update([OwnFlid$]) or update([OwnOrd$]) begin
		-- Get the owner's class and make sure it is a subclass of the field's type. Get the
		--	inserted object's class and make sure it is a subclass of the field's dst type.
		-- 	Make sure the OwnOrd$ field is consistent with the field type (if not sequence
		--	then it should be null). Make sure more than one object is not added as a child
		--	of an object with an atomic owning relationship. Make sure there are no duplicate
		--	Ord values within a sequence
		select top 1
			@idBad = ins.[Id],
			@own = ins.[Owner$],
			@flid = ins.[OwnFlid$],
			@ord = ins.[OwnOrd$],
			@dupownId = dupown.[Id],
			@dupseqId = dupseq.[Id]
		from	inserted ins
			-- If there is no owner, there is nothing to check so an inner join is OK here.
			join [CmObject] own on own.[Id] = ins.[Owner$]
			-- The constraints on CmObject guarantee this join.
			join [Field$] fld on fld.[Id] = ins.[OwnFlid$]
			-- If this join has no matches the owner is of the wrong type.
			left outer join [ClassPar$] ot on ot.[Src] = own.[Class$]
				and ot.[Dst] = fld.[Class]
			-- If this join has no matches the inserted object is of the wrong type.
			left outer join [ClassPar$] it on it.[Src] = ins.[Class$]
				and it.[Dst] = fld.[DstCls]
			-- if this join has matches there is more than one owned object in an atomic relationship
			left outer join [CmObject] dupown on fld.[Type] = 23 and dupown.[Owner$] = ins.[Owner$]
				and dupown.[OwnFlid$] = ins.[OwnFlid$]
				and dupown.[Id] <> ins.[Id]
			-- if this join has matches there is a duplicate sequence order in a sequence relationship
			left outer join [CmObject] dupseq on fld.[Type] = 27 and dupseq.[Owner$] = ins.[Owner$]
				and dupseq.[OwnFlid$] = ins.[OwnFlid$]
				and dupseq.[OwnOrd$] = ins.[OwnOrd$]
				and dupseq.[Id] <> ins.[Id]
		where
			ot.[Src] is null
			or it.[Src] is null
			or (fld.[Type] = 23 and ins.[OwnOrd$] is not null)
			or (fld.[Type] = 25 and ins.[OwnOrd$] is not null)
			or (fld.[Type] = 27 and ins.[OwnOrd$] is null)
			or dupown.[Id] is not null
			or dupseq.[Id] is not null

		if @@rowcount <> 0 begin
			if @dupownId is not null begin
				raiserror('More than one owned object in an atomic relationship: New ID=%d, Owner=%d, OwnFlid=%d, Already Owned Id=%d', 16, 1,
						@idBad, @own, @flid, @dupownId)
			end
			else if @dupseqId is not null begin
				raiserror('Duplicate OwnOrd in a sequence relationship: New ID=%d, Owner=%d, OwnFlid=%d, OwnOrd=%d, Duplicate Id=%d', 16, 1,
						@idBad, @own, @flid, @ord, @dupseqId)
			end
			else begin
				raiserror('Bad owner information ID=%d, Owner$=%d, OwnFlid$=%d, OwnOrd$=%d', 16, 1, @idBad, @own, @flid, @ord)
			end
			rollback tran
		end
	end

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
GO

-------------------------------------------------------------
-- Handle collapsing entries
-------------------------------------------------------------

declare @clidEntry int
SELECT @clidEntry=id FROM Class$ WHERE name='LexEntry'

-- Move data from LexMajorEntry to LexEntry
declare @hvoMainEntry int, @clidMajorEntry int,
	@flidSummaryDefinition int, @flidSummaryDefinition2 int
SELECT @hvoMainEntry=id FROM CmObject WHERE Guid$='5541D063-2D43-4E49-AAAD-BBA4AE5ECCD1'
SELECT @clidMajorEntry=id FROM Class$ WHERE name='LexMajorEntry'
SELECT @flidSummaryDefinition2=id FROM Field$ WHERE name='SummaryDefinition2' AND Class=@clidEntry
SELECT @flidSummaryDefinition=id FROM Field$ WHERE name='SummaryDefinition' AND Class=@clidMajorEntry

UPDATE LexEntry SET EntryType=@hvoMainEntry FROM LexEntry le JOIN CmObject co on co.id=le.id
	WHERE co.Class$=@clidMajorEntry
UPDATE MultiStr$ SET flid=@flidSummaryDefinition2 WHERE flid=@flidSummaryDefinition
UPDATE CmObject SET Class$=@clidEntry WHERE Class$=@clidMajorEntry

-- Move data from LexMinorEntry to LexEntry
declare @hvoMinorEntry int, @clidMinorEntry int,
	@flidComment int, @flidComment2 int
SELECT @hvoMinorEntry=id FROM CmObject WHERE Guid$='FCC61889-00E6-467B-9CF0-8C4F48B9A486'
SELECT @clidMinorEntry=id FROM Class$ WHERE name='LexMinorEntry'
SELECT @flidComment=id FROM Field$ WHERE name='Comment' AND Class=@clidMinorEntry
SELECT @flidComment2=id FROM Field$ WHERE name='Comment2' AND Class=@clidEntry

UPDATE LexEntry SET EntryType=@hvoMinorEntry FROM LexEntry le JOIN CmObject co on co.id=le.id
	WHERE co.Class$=@clidMinorEntry
UPDATE MultiStr$ SET flid=@flidComment2 WHERE flid=@flidComment
UPDATE LexEntry SET Condition2=condition FROM LexMinorEntry me WHERE LexEntry.id=me.id
INSERT INTO LexEntry_MainEntriesOrSenses2 (Src, Dst, Ord)
	SELECT id,MainEntryOrSense,1
	FROM LexMinorEntry WHERE MainEntryOrSense is not null
UPDATE CmObject SET Class$=@clidEntry WHERE Class$=@clidMinorEntry

-- Move data from LexSubentry to LexEntry
declare @hvoCompoundOld int, @hvoDerivativeOld int, @hvoIdiomOld int,
	@hvoKeyTermPhraseOld int, @hvoSayingOld int
SELECT @hvoCompoundOld=id FROM CmObject WHERE Guid$='D7F713EA-E8CF-11D3-9764-00C04F186933'
SELECT @hvoDerivativeOld=id FROM CmObject WHERE Guid$='D7F713EB-E8CF-11D3-9764-00C04F186933'
SELECT @hvoIdiomOld=id FROM CmObject WHERE Guid$='D7F713EC-E8CF-11D3-9764-00C04F186933'
SELECT @hvoKeytermPhraseOld=id FROM CmObject WHERE Guid$='D7F713ED-E8CF-11D3-9764-00C04F186933'
SELECT @hvoSayingOld=id FROM CmObject WHERE Guid$='D7F713EE-E8CF-11D3-9764-00C04F186933'

declare @hvoCompound int, @hvoDerivative int, @hvoIdiom int, @hvoKeyTermPhrase int,
	@hvoSaying int, @hvoSpellingVariant int, @hvoDialectVariant int, @hvoFreeVariant int
SELECT @hvoCompound=id FROM CmObject WHERE Guid$='1F6AE209-141A-40DB-983C-BEE93AF0CA3C'
SELECT @hvoDerivative=id FROM CmObject WHERE Guid$='98C273C4-F723-4FB0-80DF-EEDE2204DFCA'
SELECT @hvoIdiom=id FROM CmObject WHERE Guid$='B2276DEC-B1A6-4D82-B121-FD114C009C59'
SELECT @hvoKeytermPhrase=id FROM CmObject WHERE Guid$='CCE519D8-A9C5-4F28-9C7D-5370788BFBD5'
SELECT @hvoSaying=id FROM CmObject WHERE Guid$='9466D126-246E-400B-8BBA-0703E09BC567'
SELECT @hvoSpellingVariant=id FROM CmObject WHERE Guid$='0C4663B3-4D9A-47af-B9A1-C8565D8112ED'
SELECT @hvoDialectVariant=id FROM CmObject WHERE Guid$='024B62C9-93B3-41A0-AB19-587A0030219A'
SELECT @hvoFreeVariant=id FROM CmObject WHERE Guid$='4343B1EF-B54F-4fA4-9998-271319A6D74C'

DECLARE @TypeMap TABLE ( OldTypeId int, NewTypeId int )
INSERT INTO @TypeMap (OldTypeId, NewTypeId ) VALUES (@hvoCompoundOld, @hvoCompound)
INSERT INTO @TypeMap (OldTypeId, NewTypeId ) VALUES (@hvoDerivativeOld, @hvoDerivative)
INSERT INTO @TypeMap (OldTypeId, NewTypeId ) VALUES (@hvoIdiomOld, @hvoIdiom)
INSERT INTO @TypeMap (OldTypeId, NewTypeId ) VALUES (@hvoKeytermPhraseOld, @hvoKeytermPhrase)
INSERT INTO @TypeMap (OldTypeId, NewTypeId ) VALUES (@hvoSayingOld, @hvoSaying)

DECLARE @CustomTypes TABLE ( TypeId int )

INSERT INTO @CustomTypes SELECT DISTINCT SubentryType
	FROM LexSubEntry
	WHERE SubentryType not in (@hvoCompoundOld, @hvoDerivativeOld, @hvoIdiomOld, @hvoKeyTermPhraseOld,
	@hvoSayingOld)

DECLARE @hvoSubtype int, @hvoNewType int, @hvoEntryTypeList int, @nRowCnt int
SELECT @hvoEntryTypeList=Dst FROM LexicalDatabase_EntryTypes

SELECT TOP 1 @hvoSubtype=TypeId FROM @CustomTypes
SET @nRowCnt = @@ROWCOUNT
WHILE @nRowCnt > 0
BEGIN
	EXEC CreateObject_LexEntryType		@CmPossibility_Name_ws = null, @CmPossibility_Name_txt = null, 		@CmPossibility_Abbreviation_ws = null, @CmPossibility_Abbreviation_txt = null, 		@CmPossibility_Description_ws = null, @CmPossibility_Description_txt = null, @CmPossibility_Description_fmt = null, 		@CmPossibility_SortSpec = 0, @CmPossibility_DateCreated = null, @CmPossibility_DateModified = null,		@CmPossibility_HelpId = null, @CmPossibility_ForeColor = 0, @CmPossibility_BackColor = 0,		@CmPossibility_UnderColor = 0, @CmPossibility_UnderStyle = 0, @CmPossibility_Hidden = 0,		@CmPossibility_IsProtected = 0,	@LexEntryType_ReverseAbbr_ws = null, @LexEntryType_ReverseAbbr_txt = null, 		@LexEntryType_Type = 2,		@Owner = @hvoEntryTypeList,
		@OwnFlid = 8008,
		@StartObj = null,
		@NewObjId = @hvoNewType output,
		@NewObjGuid = null,
		@fReturnTimestamp = 0,
		@NewObjTimestamp = null

	UPDATE CmPossibility_Name SET Obj=@hvoNewType WHERE Obj=@hvoSubtype
	UPDATE CmPossibility_Abbreviation SET Obj=@hvoNewType WHERE Obj=@hvoSubtype
	UPDATE CmPossibility_Description SET Obj=@hvoNewType WHERE Obj=@hvoSubtype

	INSERT INTO @TypeMap ( OldTypeId, NewTypeId )
		VALUES (@hvoSubtype, @hvoNewType)

	DELETE FROM @CustomTypes WHERE TypeId=@hvoSubtype
	SELECT TOP 1 @hvoSubtype=TypeId FROM @CustomTypes
	SET @nRowCnt = @@ROWCOUNT
END

UPDATE LexEntry SET EntryType=tm.NewTypeId
	FROM LexSubentry se
	JOIN @TypeMap tm on se.SubentryType=tm.OldTypeId
	WHERE se.id=LexEntry.id

declare @flidLiteralMeaningOld int, @flidLiteralMeaning2 int, @clidLexSubentry int
SELECT @clidLexSubentry=id FROM Class$ WHERE name='LexSubentry'
SELECT @flidLiteralMeaningOld=id FROM Field$ WHERE name='LiteralMeaning' AND Class=@clidLexSubentry
SELECT @flidLiteralMeaning2=id FROM Field$ WHERE name='LiteralMeaning2' AND Class=@clidEntry
UPDATE MultiStr$ SET flid=@flidLiteralMeaning2 WHERE flid=@flidLiteralMeaningOld

/* LexSubentry_IsBodyWithHeadword -> LexEntry_ExcludeAsHeadword  */
UPDATE LexEntry SET ExcludeAsHeadword=IsBodyWithHeadword FROM LexSubentry se WHERE se.id=LexEntry.id

/* LexSubentry_MainEntriesOrSenses -> LexEntry_MainEntriesOrSenses2 */
INSERT INTO LexEntry_MainEntriesOrSenses2 (Src, Dst, Ord)
	SELECT Src,Dst,Ord
	FROM LexSubentry_MainEntriesOrSenses

UPDATE CmObject SET Class$=@clidEntry WHERE Class$=@clidLexSubentry
UPDATE LexSubentry SET SubentryType=null
DECLARE @hvoSubTypeList int
SELECT top 1 @hvoSubTypeList=Dst FROM LexicalDatabase_SubentryTypes
EXEC DeleteObj$ @hvoSubTypeList

-- Transfer all custom fields and data contained therein for LexMaj/Min/Sub entries
DECLARE @flidOld int, @type int, @class int, @dstCls int, @name nvarchar(100), @min bigint, @max bigint,
	@big bit, @userLabel nvarchar(100), @helpString nvarchar(100),
	@listRootId int, @wsSelector int, @xmlUI nvarchar(4000), @oldClassName nvarchar(100), @sql nvarchar(4000)

DECLARE fldCursor CURSOR local static forward_only read_only FOR
	SELECT Id, Type, Class, DstCls, Name, Min, Max, Big, UserLabel, HelpString, ListRootId, WsSelector, XmlUI
		FROM Field$
		WHERE Custom<>0 AND Class in (5007, 5008, 5009) -- LexEntry 5002

DECLARE @flidNew int, @nameNew nvarchar(100), @hvoEntry int
OPEN fldCursor
FETCH NEXT FROM fldCursor INTO @flidOld, @type, @class, @dstCls, @name, @min, @max,
	@big, @userLabel, @helpString, @listRootId, @wsSelector, @xmlUI
WHILE @@FETCH_STATUS = 0
BEGIN

	-- 1. Create custom fields on LexEntry for all the custom fields on LexMaj/Min/Sub stuff
	SET @nameNew='x' + @name -- avoid duplicate field names for now
	EXEC AddCustomField$
		@flid = @flidNew output,
		@name = @nameNew,
		@type = @type,
		@clid = 5002,
		@clidDst = @dstCls,
		@Min = @min,
		@Max = @max,
		@Big = @big,
		@nvcUserLabel = @userLabel,
		@nvcHelpString = @helpString,
		@nListRootId = @listRootId,
		@nWsSelector = @wsSelector,
		@ntXmlUI = @xmlUI


	-- 2. Copy the contents of the old custom fields to the new ones.
	SELECT @oldClassName=Name FROM Class$ WHERE Id=@class

	IF @type=15 -- 'Unicode'
		BEGIN
			SET @sql='UPDATE LexEntry SET ' + @nameNew + '=' + @name + ' FROM LexEntry le,' +
					@oldClassName + ' x WHERE le.id=x.id AND x.' + @name + ' is not NULL'
			EXEC (@sql)
		END
	ELSE IF @type=16 -- 'MultiUnicode'
		BEGIN
			-- INSERT INTO LexEntry_xcustom SELECT * FROM LexSubentry_custom
			SET @sql='INSERT INTO LexEntry_' + @nameNew + ' (Obj, Ws, Txt) SELECT Obj,Ws,Txt FROM ' +
				@oldClassName + '_' + @name
			EXEC (@sql)
		END

	-- 3. Destroy the old ones.
	DELETE FROM Field$ WHERE id=@flidOld

	FETCH NEXT FROM fldCursor INTO @flidOld, @type, @class, @dstCls, @name, @min, @max,
		@big, @userLabel, @helpString, @listRootId, @wsSelector, @xmlUI
END
CLOSE fldCursor
DEALLOCATE fldCursor

-- Move data from LexVariant to LexEntry
DECLARE @wsAnal int
SELECT top 1 @wsAnal=Dst FROM LanguageProject_CurrentAnalysisWritingSystems ORDER BY ord
DECLARE varCursor CURSOR local static forward_only read_only FOR
	SELECT v.id, c.Owner$
	FROM LexVariant v
	JOIN CmObject c on c.id=v.id

DECLARE @hvoLexDb int
SELECT top 1 @hvoLexDb=Dst FROM LanguageProject_LexicalDatabase

DECLARE @flidVarComment int, @flidVarPronunciation int, @flidPronunciation int
SELECT @flidVarComment=id FROM Field$ WHERE name='Comment' AND Class=(SELECT id FROM Class$ WHERE name='LexVariant')
SELECT @flidVarPronunciation=id FROM Field$ WHERE name='Pronunciation' AND Class=(SELECT id FROM Class$ WHERE name='LexVariant')
SELECT @flidPronunciation=id FROM Field$ WHERE name='Pronunciation' AND Class=(SELECT id FROM Class$ WHERE name='LexEntry')

DECLARE @hvoVariant int, @hvoVarOwner int
OPEN varCursor
FETCH NEXT FROM varCursor INTO @hvoVariant, @hvoVarOwner
WHILE @@FETCH_STATUS = 0
BEGIN
	DECLARE @hvoNewEntry int, @hvoNewAllo int

	EXEC CreateObject_LexEntry 0, 0, null, null, null, null, null, null, null, null, null,
		null, null, null, null, null, null, null, null, null, 1, 1, @hvoLexDb, 5005001, null,
		@hvoNewEntry output, null, 0, null

	INSERT INTO LexEntry_CitationForm (Obj, Ws, Txt)
		SELECT @hvoNewEntry, f.Ws, f.Txt
		FROM LexVariant_Form f WHERE f.Obj=@hvoVariant

	UPDATE MultiStr$ SET Obj=@hvoNewEntry, Flid=@flidComment2 WHERE Obj=@hvoVariant AND Flid=@flidVarComment

	UPDATE CmObject SET Owner$=@hvoNewEntry, OwnFlid$=@flidPronunciation
		WHERE Owner$=@hvoVariant AND OwnFlid$=@flidVarPronunciation

	INSERT INTO LexEntry_MainEntriesOrSenses2 (Src, Dst, Ord)
		VALUES (@hvoNewEntry, @hvoVarOwner, 1)

	EXEC CreateObject_MoStemAllomorph null, null, 		@hvoNewEntry, 5002008, null, @hvoNewAllo output,
		null, 0, null

	INSERT INTO MoForm_Form (Obj, Ws, Txt)
		SELECT @hvoNewAllo, Ws, Txt
		FROM LexVariant_Form WHERE Obj=@hvoVariant

	DECLARE @nvc nvarchar(4000)
	SELECT @nvc=Txt FROM LexEntry_Comment2 WHERE Obj=@hvoNewEntry AND Ws=@wsAnal
	IF @nvc like '%free var%'
		UPDATE LexEntry SET EntryType=@hvoFreeVariant WHERE id=@hvoNewEntry
	ELSE IF @nvc like '%dialect%'
		UPDATE LexEntry SET EntryType=@hvoDialectVariant WHERE id=@hvoNewEntry
	ELSE
		UPDATE LexEntry SET EntryType=@hvoSpellingVariant WHERE id=@hvoNewEntry

	EXEC DeleteObj$ @hvoVariant

	FETCH NEXT FROM varCursor INTO @hvoVariant, @hvoVarOwner
END
CLOSE varCursor
DEALLOCATE varCursor

/***********************************************************************************************
 * Trigger: TR_Field$_UpdateModel_Del
 *
 * Description:
 *	This trigger cleans up columns and any additional tables that are associated with the
 *	deleted row in Field$
 *
 * Notes:
 *	This trigger might be used for data migration procedures, not just custom fields.
 *
 * Type: 	Delete
 * Table:	Field$
 **********************************************************************************************/
if object_id('TR_Field$_UpdateModel_Del') is not null begin
	print 'removing trigger TR_Field$_UpdateModel_Del'
	drop trigger TR_Field$_UpdateModel_Del
end
go
print 'creating trigger TR_Field$_UpdateModel_Del'
go
create trigger TR_Field$_UpdateModel_Del on Field$ for delete
as
	declare @Clid INT
	declare @DstCls INT
	declare @sName VARCHAR(100)
	declare @sClass VARCHAR(100)
	declare @sFlid VARCHAR(20)
	declare @Type INT
	DECLARE @nAbstract INT

	declare @Err INT
	declare @fIsNocountOn INT
	declare @sql VARCHAR(1000)

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- get the first custom field to process
	Select @sFlid= min([id]) from deleted

	-- loop through all of the custom fields to be deleted
	while @sFlid is not null begin

		-- get deleted fields
		select 	@Type = [Type], @Clid = [Class], @sName = [Name], @DstCls = [DstCls]
		from	deleted
		where	[Id] = @sFlid

		-- get class name
		select 	@sClass = [Name], @nAbstract = Abstract  from class$  where [Id] = @Clid
		if @type IN (14,16,18,20) begin
			-- Remove any data stored for this multilingual custom field.
			declare @sTable VARCHAR(20)
			set @sTable = case @type
				when 14 then 'MultiStr$'
				when 16 then 'MultiTxt$ (No Longer Exists)'
				when 18 then 'MultiBigStr$'
				when 20 then 'MultiBigTxt$'
				end
			IF @type != 16  -- MultiTxt$ data will be deleted when the table is dropped
			BEGIN
				set @sql = 'DELETE FROM [' + @sTable + '] WHERE [Flid] = ' + @sFlid
				exec (@sql)
				set @Err = @@error
				if @Err <> 0 goto LFail
			END

			-- Remove the view created for this multilingual custom field.
			IF @type != 16
				set @sql = 'DROP VIEW [' + @sClass + '_' + @sName + ']'
			ELSE
				SET @sql = 'DROP TABLE [' + @sClass + '_' + @sName + ']'
			exec (@sql)
			set @Err = @@error
			if @Err <> 0 goto LFail
		end
		else if @type IN (23,25,27) begin
			-- Remove the view created for this custom OwningAtom/Collection/Sequence field.
			set @sql = 'DROP VIEW [' + @sClass + '_' + @sName + ']'
			exec (@sql)
			set @Err = @@error
			if @Err <> 0 goto LFail
			-- Check for any objects stored for this custom OwningAtom/Collection/Sequence field.
			declare @DelId INT
			select @DelId = [Id] FROM CmObject WHERE [OwnFlid$] = @sFlid
			set @Err = @@error
			if @Err <> 0 goto LFail
			if @DelId is not null begin
				raiserror('TR_Field$_UpdateModel_Del: Unable to remove %s field until corresponding objects are deleted',
						16, 1, @sName)
				goto LFail
			end
		end
		else if @type IN (26,28) begin
			-- Remove the table created for this custom ReferenceCollection/Sequence field.
			set @sql = 'DROP TABLE [' + @sClass + '_' + @sName + ']'
			exec (@sql)
			set @Err = @@error
			if @Err <> 0 goto LFail

			-- Remove the procedure that handles reference collections or sequences for
			-- the dropped table
			set @sql = N'
				IF OBJECT_ID(''ReplaceRefColl_' + @sClass +  '_' + @sName + ''') IS NOT NULL
					DROP PROCEDURE [ReplaceRefColl_' + @sClass + '_' + @sName + ']
				IF OBJECT_ID(''ReplaceRefSeq_' + @sClass +  '_' + @sName + ''') IS NOT NULL
					DROP PROCEDURE [ReplaceRefSeq_' + @sClass + '_' + @sName + ']'
			exec (@sql)
			set @Err = @@error
			if @Err <> 0 goto LFail
		end
		else begin
			-- Remove the format column created if this was a custom String field.
			if @type in (13,17) begin
				set @sql = 'ALTER TABLE [' + @sClass + '] DROP COLUMN [' + @sName + '_Fmt]'
				exec (@sql)
				set @Err = @@error
				if @Err <> 0 goto LFail
			end
			-- Remove the constraint created if this was a custom ReferenceAtom field.
			-- Not necessary for CmObject : Foreign Key constraints are not created agains CmObject
			if @type = 24 begin
				declare @sTarget VARCHAR(100)
				select @sTarget = [Name] FROM [Class$] WHERE [Id] = @DstCls
				set @Err = @@error
				if @Err <> 0 goto LFail
				if @sTarget != 'CmObject' begin
					set @sql = 'ALTER TABLE [' + @sClass + '] DROP CONSTRAINT [' +
						'_FK_' + @sClass + '_' + @sName + ']'
					exec (@sql)
					set @Err = @@error
					if @Err <> 0 goto LFail
				end
			end
			-- Remove Default Constraint from Numeric or Date fields before dropping the column
			If @type in (1,2,3,4,5,8) begin
				select @sql = 'ALTER TABLE [' + @sClass + '] DROP CONSTRAINT [' + so.name + ']'
				from sysconstraints sc
					join sysobjects so on so.id = sc.constid and so.name like 'DF[_]%'
					join sysobjects so2 on so2.id = sc.id
					join syscolumns sco on sco.id = sc.id and sco.colid = sc.colid
				where so2.name = @sClass   -- Tablename
				and   sco.name = @sName    -- Fieldname
				and   so2.type = 'U'	   -- Userdefined table
				exec (@sql)
				set @Err = @@error
				if @Err <> 0 goto LFail
			end

			-- Remove the column created for this custom field.
			set @sql = 'ALTER TABLE [' + @sClass + '] DROP COLUMN [' + @sName + ']'
			exec (@sql)
			set @Err = @@error
			if @Err <> 0 goto LFail

			-- fix the view associated with this class.
			exec @Err = UpdateClassView$ @Clid, 1
			if @Err <> 0 goto LFail
		end

		--( Rebuild CreateObject_*
		IF @nAbstract != 1 BEGIN
			EXEC @Err = DefineCreateProc$ @Clid
			IF @Err <> 0 GOTO LFail
		END

		-- get the next custom field to process
		Select @sFlid= min([id]) from deleted  where [Id] > @sFlid

	end -- While loop

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return

LFail:
	rollback tran
	return
go

/***********************************************************************************************
 * Name: DeleteModelClass
 *
 * Description:
 *	This procedure deletes a row from Class$ and cleans up associated rows in Field$ as well as
 *	any tables and stored procedures generated for the deleted Class.
 *
 **********************************************************************************************/
if object_id('DeleteModelClass') is not null begin
	print 'removing proc DeleteModelClass'
	drop proc DeleteModelClass
end
print 'creating proc DeleteModelClass'
go

CREATE PROCEDURE DeleteModelClass
	@Clid INT
as
	declare @sName VARCHAR(100)
	DECLARE @nAbstract INT

	declare @Err INT
	declare @fIsNocountOn INT
	declare @sql VARCHAR(1000)

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on
	SELECT @sName=[Name], @nAbstract=[Abstract] FROM Class$ WHERE [id]=@Clid

	-- 1. Delete the associated rows from ClassPar$ and check for surviving subclasses
	DELETE FROM ClassPar$ WHERE Src=@Clid
	set @Err = @@error
	if @Err <> 0 goto LFail
	DECLARE @cSub int
	SELECT @cSub=COUNT(*) FROM ClassPar$ WHERE Dst=@Clid
	set @Err = @@error
	if @Err <> 0 goto LFail
	if @cSub<> 0 goto LFail

	-- 2. Delete any rows in Field$ where Class=<thisDeletedClass>
	DELETE FROM Field$ WHERE Class=@Clid
	set @Err = @@error
	if @Err <> 0 goto LFail

	DELETE FROM Field$ WHERE DstCls=@Clid 	-- Is this correct?
	set @Err = @@error
	if @Err <> 0 goto LFail

	-- 3. Delete the table and view for thisDeletedClass.
	SET @sql='DROP TABLE ' + @sName
	exec (@sql)
	set @Err = @@error
	if @Err <> 0 goto LFail

	SET @sql='DROP VIEW ' + @sName + '_'
	exec (@sql)
	set @Err = @@error
	if @Err <> 0 goto LFail

	-- 4. If it exists delete the CreateObject procedure for thisDeletedClass.
	SET @sql='IF OBJECT_ID(''CreateObject_' + @sName + ''') IS NOT NULL BEGIN' +
			' DROP PROCEDURE CreateObject_' + @sName +
		' END'
	exec (@sql)
	set @Err = @@error
	if @Err <> 0 goto LFail

	-- 5. Delete any rows in CmObject where Class$=<thisDeletedClass>.
	DELETE FROM CmObject WHERE Class$=@Clid
	set @Err = @@error
	if @Err <> 0 goto LFail

	-- Actually delete the affected rows
	DELETE FROM Class$ WHERE [Id]=@Clid
	set @Err = @@error
	if @Err <> 0 goto LFail

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return
LFail:
	rollback tran
	return
go

-- LexVariant
EXEC DeleteModelClass 5114

-- LexSubentryType
EXEC DeleteModelClass 5021

-- LexMinorEntry
ALTER TABLE [dbo].[LexMinorEntry] DROP CONSTRAINT [_FK_LexMinorEntry_MainEntryOrSense]
EXEC DeleteModelClass 5009

-- LexSubentry
EXEC DeleteModelClass 5007

-- LexMajorEntry
EXEC DeleteModelClass 5008

DELETE FROM Field$ WHERE [id]=5002015 -- LexEntry_Variants
DELETE FROM Field$ WHERE [id]=5002002 -- LexEntry_IsIncludedAsHeadword
DELETE FROM Field$ WHERE [id]=5005009 -- LexicalDatabase_SubentryTypes


-- LexEntry
ALTER TABLE Field$ DISABLE TRIGGER TR_Field$_No_Upd
UPDATE Field$ SET [Name]='MainEntriesOrSenses' WHERE [Name]='MainEntriesOrSenses2' AND Class=5002
UPDATE Field$ SET [Name]='SummaryDefinition' WHERE [Name]='SummaryDefinition2' AND Class=5002
UPDATE Field$ SET [Name]='Comment' WHERE [Name]='Comment2' AND Class=5002
UPDATE Field$ SET [Name]='Condition' WHERE [Name]='Condition2' AND Class=5002
UPDATE Field$ SET [Name]='LiteralMeaning' WHERE [Name]='LiteralMeaning2' AND Class=5002
ALTER TABLE Field$ ENABLE TRIGGER TR_Field$_No_Upd

CREATE TABLE [LexEntry_MainEntriesOrSenses]
(
	[Src] [int] NOT NULL ,
	[Dst] [int] NOT NULL ,
	[Ord] [int] NOT NULL ,
	CONSTRAINT [_PK_LexEntry_MainEntriesOrSenses] PRIMARY KEY  CLUSTERED
	(
		[Src],
		[Ord]
	)  ON [PRIMARY] ,
	CONSTRAINT [_FK_LexEntry_MainEntriesOrSenses_Dst] FOREIGN KEY
	(
		[Dst]
	) REFERENCES [CmObject] (
		[Id]
	),
	CONSTRAINT [_FK_LexEntry_MainEntriesOrSenses_Src] FOREIGN KEY
	(
		[Src]
	) REFERENCES [LexEntry] (
		[id]
	)
) ON [PRIMARY]
GO

INSERT INTO LexEntry_MainEntriesOrSenses (Src, Dst, Ord)
	SELECT Src, Dst, Ord
	FROM LexEntry_MainEntriesOrSenses2

if exists (select * from dbo.sysobjects where id = object_id(N'[LexEntry_MainEntriesOrSenses2]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [LexEntry_MainEntriesOrSenses2]
GO

CREATE VIEW [LexEntry_Comment] AS
	select [Obj], [Flid], [Ws], [Txt], [Fmt]
	FROM [MultiStr$]
	WHERE [Flid] = 5002025
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[LexEntry_Comment2]') and OBJECTPROPERTY(id, N'IsView') = 1)
drop view [dbo].[LexEntry_Comment2]
GO

CREATE VIEW [LexEntry_SummaryDefinition] AS
	select [Obj], [Flid], [Ws], [Txt], [Fmt]
	FROM [MultiStr$]
	WHERE [Flid] = 5002017
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[LexEntry_SummaryDefinition2]') and OBJECTPROPERTY(id, N'IsView') = 1)
drop view [dbo].[LexEntry_SummaryDefinition2]
GO

CREATE VIEW [LexEntry_LiteralMeaning] AS
	select [Obj], [Flid], [Ws], [Txt], [Fmt]
	FROM [MultiStr$]
	WHERE [Flid] = 5002018
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[LexEntry_LiteralMeaning2]') and OBJECTPROPERTY(id, N'IsView') = 1)
drop view [dbo].[LexEntry_LiteralMeaning2]
GO

ALTER TABLE LexEntry ADD Condition INT NULL
GO

ALTER TABLE [dbo].[LexEntry] ADD CONSTRAINT [_FK_LexEntry_Condition] FOREIGN KEY
	(
		[Condition]
	) REFERENCES [CmPossibility] (
		[id]
	)
GO

UPDATE LexEntry SET Condition=Condition2

ALTER TABLE [dbo].[LexEntry] DROP CONSTRAINT [_FK_LexEntry_Condition2]
GO

ALTER TABLE LexEntry DROP COLUMN Condition2

-- Finish updating LexEntry Class
EXEC UpdateClassView$ 5002
EXEC DefineCreateProc$ 5002


if object_id('WasParsingDataModified') is not null begin
	print 'removing proc WasParsingDataModified'
	drop proc WasParsingDataModified
end
print 'creating proc WasParsingDataModified'
go
/*****************************************************************************
 * WasParsingDataModified
 *
 * Description:
 *	Returns a table with zero or one row of any object
 *	of certain classes that have a newer timestamp than
 *	that given in the input parameter.
 * Parameters:
 *	@stampCompare=the timestamp to compare.
 * Returns:
 *	0
 *****************************************************************************/
create proc [WasParsingDataModified]
			@stampCompare timestamp
AS
	SELECT TOP 1 Id
	FROM CmObject co
	where co.UpdStmp > @stampCompare
		and (co.Class$ BETWEEN 5026 AND 5045
			OR co.Class$ IN
			(5005, --kclidLexicalDatabase
			5002 --kclidLexEntry
			))
go

-- DisplayName_LexEntry
if object_id('DisplayName_LexEntry') is not null begin
	drop proc DisplayName_LexEntry
end
go
print 'creating proc DisplayName_LexEntry'
go
/***********************************************************************************************
 * Procedure: DisplayName_LexEntry
 * Description: This procedure returns a variety of information about the entry:
 *	1. First of citation form, underlying form, or allomorph.
 *	2. hyphen+homographNumber, if greater than 0,
 *	3. Gloss of the first sense for major & subentries. Nothing for minor entries.
 *	4. Fmt and Ws for individual strings.
 *	5. Ids for various things for use by a view constructor.
 * Assumptions:
 *	1. The input XML is of the form: <root><Obj Id="7164"/><Obj Id="7157"/></root>
 *	2. This SP will use the first vernacular and analysis writing system, as needed for
 *	   the form and gloss, respectively.
 * Parameters:
 *    @XMLIds - Object IDs of the entry(ies), or null for all entries
 * Return: 0 if successful, otherwise 1.
***********************************************************************************************/
create  proc [DisplayName_LexEntry]
	@XMLIds ntext = null
as

declare @retval int, @fIsNocountOn int,
	@LeId int, @Class int, @HNum int, @FullTxt nvarchar(4000),
	@FormId int, @Ord int, @Flid int, @FormTxt nvarchar(4000), @FormFmt int, @FormEnc int,
	@SenseId int, @SenseGloss nvarchar(4000), @SenseFmt int, @SenseEnc int,
	@myCursor CURSOR

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- Gather two encodings,
	select top 1 @SenseEnc=le.Id
	from LanguageProject_CurrentAnalysisWritingSystems ce
	join LgWritingSystem le On le.Id = ce.Dst
	order by ce.Src, ce.ord
	select top 1 @FormEnc=le.Id
	from LanguageProject_CurrentVernacularWritingSystems ce
	join LgWritingSystem le On le.Id = ce.Dst
	order by ce.Src, ce.ord

	--Table variable.
	declare @DisplayNameLexEntry table (
		LeId int primary key,
		Class int,
		HNum int default 0,
		--( See the notes under string tables in FwCore.sql about the
		--( COLLATE clause.
		FullTxt NVARCHAR(4000) COLLATE Latin1_General_BIN,
		FormId int default 0,
		Ord int default 0,
		Flid int default 0,
		FormTxt nvarchar(4000),
		FormFmt int,
		FormEnc int,
		SenseId int default 0,
		SenseGloss nvarchar(4000),
		SenseFmt int,
		SenseEnc int
		)

	if @XMLIds is null begin
		-- Do all lex entries.
		set @myCursor = CURSOR FAST_FORWARD for
			select Id, Class$
			from CmObject
			where Class$=5002
			order by id
		open @myCursor
	end
	else begin
		-- Do lex entries provided in xml string.
		declare @hdoc int
		exec sp_xml_preparedocument @hdoc output, @XMLIds
		if @@error <> 0 begin
			set @retval = 1
			goto LExitNoCursor
		end
		set @myCursor = CURSOR FAST_FORWARD for
			select cmo.Id, cmo.Class$
			from	openxml(@hdoc, '/root/Obj')
			with ([Id] int) as ol
			-- Remove all pretenders, since they won't 'join'.
			join CmObject cmo
				On ol.Id=cmo.Id
				and cmo.Class$=5002
			order by ol.[Id]
		open @myCursor
		-- Turn loose of the handle
		exec sp_xml_removedocument @hdoc
		if @@error <> 0 begin
			set @retval = 1
			goto LExitWithCursor
		end
	end

	-- REVIEW (SteveMiller): MultiTxt$.fmt was always NULL. Don't understand the
	-- reason for @FormFmt being set to Fmt. Changed to cast(null as varbinary).

	-- Loop through all ids.
	fetch next from @myCursor into @LeId, @Class
	while @@fetch_status = 0
	begin
		select top 1 @FormId=0, @Ord=0, @Flid = 5002003, @FormTxt=Txt, @FormFmt=cast(null as varbinary)
		from LexEntry_CitationForm
		where Obj=@LeId and Ws=@FormEnc
		if @@rowcount = 0 begin
			select top 1 @FormId=f.Obj, @Ord=0, @Flid = 5035001, @FormTxt=f.Txt, @FormFmt=cast(null as varbinary)
			from LexEntry_UnderlyingForm uf
			join MoForm_Form f On f.Obj=uf.Dst and f.Ws=@FormEnc
			where uf.Src=@LeId
			if @@rowcount = 0 begin
				select top 1 @FormId=f.Obj, @Ord=a.Ord, @Flid=5035001, @FormTxt=f.Txt, @FormFmt=cast(null as varbinary)
				from LexEntry_Allomorphs a
				join MoForm_Form f On f.Obj=a.Dst and f.Ws=@FormEnc
				where a.Src=@LeId
				if @@rowcount = 0 begin
					set @FormId = 0
					set @Ord = 0
					set @Flid = 0
					set @FormTxt = '***'
				end
			end
		end
		set @FullTxt = @FormTxt

		-- Deal with homograph number.
		select @HNum=HomographNumber
		from LexEntry
		where Id=@LeId
		if @HNum > 0
			set @FullTxt = @FullTxt + '-' + cast(@HNum as nvarchar(100))

		-- Deal with conceptual model class.

		-- Deal with sense gloss.
		select top 1 @SenseId=ls.Id, @SenseGloss = isnull(g.Txt, '***'), @SenseFmt= cast(null as varbinary)
		from LexEntry_Senses mes
		left outer join LexSense ls
			On ls.Id=mes.Dst
		left outer join LexSense_Gloss g
			On g.Obj=ls.Id and g.Ws=@SenseEnc
		where mes.Src=@LeId
		order by mes.Ord
		set @FullTxt = @FullTxt + ' : ' + @SenseGloss

		insert into @DisplayNameLexEntry (LeId, Class, HNum, FullTxt,
					FormId, Ord, Flid, FormTxt, FormFmt, FormEnc,
					SenseId, SenseGloss, SenseFmt, SenseEnc)
			values (@LeId, @Class, @HNum, @FullTxt,
					@FormId, @Ord, @Flid, @FormTxt, @FormFmt, @FormEnc,
					@SenseId, @SenseGloss, @SenseFmt, @SenseEnc)
		-- Try for another one.
		fetch next from @myCursor into @LeId, @Class
	end

	set @retval = 0
	select * from @DisplayNameLexEntry order by FullTxt

LExitWithCursor:
	close @myCursor
	deallocate @myCursor

LExitNoCursor:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @retval
go

-- Delete ChangeLexSubToLexMajor
IF OBJECT_ID('ChangeLexSubToLexMajor') IS NOT NULL
	DROP PROCEDURE ChangeLexSubToLexMajor
GO

-- Delete ChangeLexMajorToLexSub
IF OBJECT_ID('ChangeLexMajorToLexSub') IS NOT NULL
	DROP PROCEDURE ChangeLexMajorToLexSub
GO

-- Delete ChangeLexMajorToLexMinor
IF OBJECT_ID('ChangeLexMajorToLexMinor') IS NOT NULL
	DROP PROCEDURE ChangeLexMajorToLexMinor
GO

-- Delete ChangeLexMinorToLexMajor
IF OBJECT_ID('ChangeLexMinorToLexMajor') IS NOT NULL
	DROP PROCEDURE ChangeLexMinorToLexMajor
GO

-- Delete ChangeLexMinorToLexSub
IF OBJECT_ID('ChangeLexMinorToLexSub') IS NOT NULL
	DROP PROCEDURE ChangeLexMinorToLexSub
GO

-- Delete ChangeLexSubToLexMinor
IF OBJECT_ID('ChangeLexSubToLexMinor') IS NOT NULL
	DROP PROCEDURE ChangeLexSubToLexMinor
GO

-- LexEntry is no longer abstract
-- NOTE that this was omitted from the original data migration.
-- Any database migrated FROM a version before 55 before 23 June 05
-- has a problem and needs manual correction.
update class$ set Abstract=0 where id = 5002
GO

---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200054
begin
	update Version$ set DbVer = 200055
	COMMIT TRANSACTION
	print 'database updated to version 200055'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200054 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO