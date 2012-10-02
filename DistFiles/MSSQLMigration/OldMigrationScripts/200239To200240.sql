-- Update database from version 200239 to 200240
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-- BEGIN TEST SELECT
--DECLARE @hvoTestList INT
--SELECT TOP 1 @hvoTestList = Dst FROM LexDb_EntryTypes ; EXEC GetPossibilities @hvoTestList, 0xfffffffa
--SELECT TOP 1 @hvoTestList = Dst FROM LexDb_AllomorphConditions ; EXEC GetPossibilities @hvoTestList, 0xfffffffa
--SELECT let.*, a.Ws, a.Txt 'Abbr', ra.Txt 'RevAbbr'
--FROM LexEntryType let
--LEFT OUTER JOIN CmPossibility_Abbreviation a ON a.Obj=let.Id
--LEFT OUTER JOIN LexEntryType_ReverseAbbr ra ON ra.Obj=let.Id AND ra.Ws=a.Ws
--SELECT DISTINCT le.Id, le.HomographNumber, le.ExcludeAsHeadword, nc.Ws 'ConditionWs', nc.Txt 'Condition', nt.Ws 'EntryTypeWs', nt.Txt 'EntryType', let.Type, mes.Dst, mes.Ord, sd.Ws 'SDWs', sd.Txt 'SummaryDef'
--FROM LexEntry le
--LEFT OUTER JOIN CmPossibility_Name nc ON nc.Obj=le.Condition
--JOIN LexEntryType let ON let.Id=le.EntryType AND let.Type != 0
--LEFT OUTER JOIN CmPossibility_Name nt ON nt.Obj=le.EntryType
--LEFT OUTER JOIN LexEntry_MainEntriesOrSenses mes ON mes.Src=le.Id
--LEFT OUTER JOIN LexEntry_SummaryDefinition sd ON sd.Obj=le.Id
-- END TEST SELECT

-------------------------------------------------------------------------------
-- LT-7277, FWM-157, FDB-233:  Data Migration to go with Code Changes
-------------------------------------------------------------------------------

--	NOTE (SteveMiller):
--	Instructions from Ken originally came from the bottom of:
--	https://wiki.insitehome.org/download/attachments/25171449/Proposed+Entry+Type+model.doc?version=3
--  These instructions are marked "KZ". They were not necessarily
--	followed. Ken and I discussed some things since then, and in
--	other cases I coded differently to make better use of relational sets.

DECLARE @hvoLexDb int,
	@LexEntryId int,
	@CmMajorObject_Name_txt nvarchar(max),
	@today DATETIME,
	@ws INT,
	@CmPossibilityList_ItemClsid int,
	@AnalysisWs int,
	@NewObjId int,
	@NewObjGuid uniqueidentifier,
	@NewObjTStamp int,
	@MoveObject int,
	@ntIds varchar(max),
	@Debug bit,
	@EntryOrSenseId int,
	@LexEntryTypeId int,
	@OldOwner INT,
	@ExcludeAsHeadword BIT,
	@LexEntryRefId INT,
	@Id INT,
	@Order INT,
	@Condition INT,
	@WS1 INT,
	@WS2 INT,
	@Txt NVARCHAR(4000),
	@Fmt VARBINARY(8000)

-- KZ: 1. The EntryTypes list needs to be split and rearranged into
--		VariantEntryTypes and ComplexEntryTypes lists.

-- Create the VariantEntryTypes possibility list
Set @Debug = 1
SELECT @hvoLexDb=Dst from LangProject_LexDb --( to find owner of new lists, which will be LexDb
SELECT @AnalysisWs = id from LgWritingSystem WHERE ICULocale=N'en'

select top 1 @fmt=Fmt from MultiStr$ where Ws=@AnalysisWs order by len(Fmt) --( The
	--( shortest string will most likely not have any embedding in it.
SET @CmMajorObject_Name_txt = N'Variant Entry Types'
SET @today = CURRENT_TIMESTAMP

EXEC MakeObj_CmPossibilityList
	@AnalysisWs,
	@CmMajorObject_Name_txt,
	@today, --( Date created
	@today, --( Date Modified
	@AnalysisWs,
	N'Variant entry types for lexicons.',
	@fmt, -- Description_fmt
	127, --( Depth
	0, --( PreventChoiceAboveLevel
	1, --( Ordered
	0, --( IsClosed
	1, --( PreventDuplicates
	0, --( PreventNodeChoices
	@AnalysisWs, --( Abbreviation_WS
	N'VarEnt',
	NULL, --( HelpFile
	0, --( UseExtendedFields
	0, --(DisplayOption
	5118, --( ItemClsId, in this case, the class for LexEntryType
	0, --( IsVernacular,
	-3, --( WsSelector, in this case, All analysis writing system
	NULL, --( ListVersion
	@hvoLexDb, --( owner
	N'5005022', -- Owning Flid
	NULL, --( StartObj
	@NewObjId OUTPUT,
	@NewObjGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Move any LexEntryType with Type = 1 to the VariantEntryTypes list.

DECLARE VariantCursor CURSOR FOR
	SELECT id
	FROM LexEntryType where Type = 1 --( Type of entry for display; 1 = treat like minor entry

Open VariantCursor

FETCH NEXT FROM VariantCursor INTO @MoveObject
WHILE @@FETCH_STATUS = 0
BEGIN
	EXECUTE MoveOwnedObject$
		6701,           --SrcObjId
		5005018,        --SrcFlid
		NULL,           --ListStmp not used
		@MoveObject,    --StartObj
		@MoveObject,    --EndObj
		@NewObjId,      --DstObjid
		8008,        --DstFlid
		NULL            --DstStartObj
	FETCH NEXT FROM VariantCursor INTO @MoveObject
END
CLOSE VariantCursor
DEALLOCATE VariantCursor

-- Create ComplexEntryTypes List in LexicalDatabase.

SET @CmMajorObject_Name_txt = N'Complex Entry Types'
SET @today = CURRENT_TIMESTAMP --( Get the most current time

EXEC MakeObj_CmPossibilityList
	@AnalysisWs,
	@CmMajorObject_Name_txt,
	@today, --( Date created
	@today, --( Date Modified
	@AnalysisWs,
	N'Complex entry types for lexicons.',
	@fmt, -- Description_fmt
	127, --( Depth
	0, --( PreventChoiceAboveLevel
	1, --( Ordered
	0, --( IsClosed
	1, --( PreventDuplicates
	0, --( PreventNodeChoices
	@AnalysisWs, --( Abbreviation_WS
	N'ComEnt',
	NULL, --( HelpFile
	0, --( UseExtendedFields
	0, --(DisplayOption
	5118, --( ItemClsId, in this case, the class for LexEntryType
	0, --( IsVernacular,
	-3, --( WsSelector, in this case, All analysis writing system
	NULL, --( ListVersion
	@hvoLexDb, --( owner
	N'5005023', -- Owning Flid
	NULL, --( StartObj
	@NewObjId OUTPUT,
	@NewObjGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

-- Move any LexEntryType with Type = 2 to the ComplexEntryTypes list.

DECLARE ComplexCursor CURSOR FOR
	SELECT id
	FROM LexEntryType where Type = 2 --( Type of entry for display; 2 = treat like subentry

Open ComplexCursor

FETCH NEXT FROM ComplexCursor INTO @MoveObject
WHILE @@FETCH_STATUS = 0
BEGIN
	EXECUTE MoveOwnedObject$
		6701,           --SrcObjId
		5005018,        --SrcFlid
		NULL,           --ListStmp not used
		@MoveObject,    --StartObj
		@MoveObject,    --EndObj
		@NewObjId,      --DstObjid
		8008,			--DstFlid
		NULL            --DstStartObj
	FETCH NEXT FROM ComplexCursor INTO @MoveObject
END
CLOSE ComplexCursor
DEALLOCATE ComplexCursor

--	NOTE (SteveMiller):
--	While the following instructions originally came from Ken,
--	at the link I gave at the top of this script, I edited the
--	instructions so they would make sense to me. If you compare
--	the following with Ken's original instructions, you'll see
--	what I mean.

-- KZ: 2. For each LexEntry where LexEntry.EntryType points to a Type 1
--		(variant), add a new LexEntryRef to LexEntry.EntryRefs.
--
--      2.1. LexEntry.Condition "indicates the reason the variant exists."
--
--			2.1.1. If LexEntry.Condition is empty, set LexEntryRef.VariantEntryTypes
--			to LexEntry.LexEntryType.
--
--			2.1.2. (NOTE: this section is currently under debate.)
--			If LexEntry.Condition is not empty, produce a target string
--			by concatenating name it points to from LexEntryType (a subclass
--			of CmPossibility) to the name of the LexEntryType that
--			LexEntry.EntryType points to. Put a space between the two names.
--			Do the same with the abbreviation. Look for the concatenated
--			abbreviation in the subpossibilities of LexEntryType. (Remember
--			that LexEntryType is a subclass	of CmPossibility.) If found, set
--			LexEntryRef_VariantEntryTypes to LexEntryType (let1). Otherwise
--			add a new LexEntryType (let2) to LexEntryType_SubPossibilities and
--			set the name to Name of LexEntry.Condition, Abbreviation to the
--			target, and ReverseAbbr to LexEntryType_ReverseAbbr (letra1).
--			Repeat the settings for any other writing systems that are present.
--			Then set LexEntryRef_VariantEntryTypes to LexEntryType.
--
--      2.2. Set LexEntryRef.PrimaryLexemes to LexEntry.MainEntriesOrSenses.
--
--      2.3. Set LexEntryRef.HideMinorEntry to 0 or 1 based on
--		LexEntry.ExcludeAsHeadword, and clear LexEntry.ExcludeAsHeadword.
--		(NOTE: ExcludeAsHeadword cannot not be set to null.)
--
--      Caution! This part of the migration where we have to combine condition
--		to type is the most complex part of the migration and might possibly
--		end up with wrong information since the user may have the original
--		settings customized differently from this algorithm. The user will
--		need to review the Abbreviation and ReverseAbbr of their resulting
--		entry types to make sure they are displaying the proper information.

--( Loop through LexEntry's MainEntriesOrSenses and move them to
--( the new LexEntryRef class.

--( Much of this block of code is copied below for complex entries.
--( Tacky, I know, but I didn't want to take any more time cleaning
--( it up, and this is not a production procedure.

DECLARE LexEntryVariant CURSOR FOR
	SELECT le.id, lemeos.Dst, let.id,
		le.ExcludeAsHeadword, le.Condition
	FROM LexEntry le
	LEFT  OUTER JOIN LexEntry_MainEntriesOrSenses lemeos ON lemeos.Src = le.Id
	JOIN LexEntryType let ON let.Id = le.EntryType
		AND let.Type = 1  --( Type of entry for display; 1 = treat like minor entry
	ORDER BY le.Id, lemeos.Ord

OPEN LexEntryVariant
FETCH NEXT FROM LexEntryVariant INTO
	@LexEntryId, @EntryOrSenseId, @LexEntryTypeId, @ExcludeAsHeadword, @Condition
WHILE @@FETCH_STATUS = 0
BEGIN

	--( Check to see if a LexEntryRef already exists for this LexEntry.
	--( We only need one for this set of (old) entries or senses.
	--( Create it if needed.

	SET @LexEntryRefId = NULL --( reset

	SELECT @LexEntryRefId = leer.Dst
	FROM LexEntry_EntryRefs leer
	WHERE leer.Src = @LexEntryId

	IF @LexEntryRefId IS NULL BEGIN
		EXECUTE MakeObj_LexEntryRef
			@ExcludeAsHeadword,		-- @LexEntryRef_HideMinorEntry int = 0,
			null,					-- @LexEntryRef_Summary_ws int = null,
			null,					-- @LexEntryRef_Summary_txt
			null,					-- @LexEntryRef_Summary_fmt
			@LexEntryId,			-- @Owner (LexEntry)
			5002034,				-- @OwnFlid (LexEntry)
			@LexEntryTypeId,		-- @StartObj
			@LexEntryRefId OUTPUT,	-- @NewObjId output,
			@NewObjGuid OUTPUT,		-- @NewObjGuid output,
			1,						-- @fReturnTimestamp tinyint = 0,
			@NewObjTStamp			-- @NewObjTimestamp int = null output
	END

	--( Moving the LexEntry_SummaryDefinition records over to
	--( LexEntryRef_Summary is done later.

	--( Create the relation(s) between LexEntryRef and its entry or sense
	--( The following INSERT is not in a loop because the outer loop
	--( already has MainEntriesOrSenses.

	--( Per Ken and Steve McConnel (Feb 5), the variant MainEntryOrSense
	--( ought to go in LexEntryRef_ComponentLexemes instead of
	--( LexEntryRef_PrimaryLexemes.

	SELECT @Order = COALESCE(MAX(Ord) + 1, 1)
	FROM LexEntryRef_ComponentLexemes
	WHERE Src = @LexEntryRefId

	IF @EntryOrSenseId IS NOT NULL
		INSERT INTO LexEntryRef_ComponentLexemes (Src, Dst, Ord)
			VALUES (@LexEntryRefId, @EntryOrSenseId, @Order)

	--( Check to see if the Variant entry type already exists.
	--( Create it if needed, or move it if it's there.

	IF @Condition IS NULL BEGIN
		SET @Id = NULL

		SELECT @Id = lervet.Src
		FROM LexEntryRef_VariantEntryTypes lervet
		WHERE lervet.Src = @LexEntryRefId AND lervet.Dst = @LexEntryTypeID

		IF @Id IS NULL BEGIN
			SELECT @Order = COALESCE(MAX(Ord) + 1, 1)
			FROM LexEntryRef_VariantEntryTypes
			WHERE Src = @LexEntryRefId

			INSERT INTO LexEntryRef_VariantEntryTypes (Src, Dst, Ord)
				VALUES (@LexEntryRefId, @LexEntryTypeId, @Order)
		END
	END --( IF @Condition IS NULL BEGIN
	ELSE BEGIN
		SET @Id = NULL

		--( See if the subpossibility already exists for the LexEntry.
		--( If not, create it.

		SELECT @Id = psp.Dst
		FROM CmPossibility_SubPossibilities psp
		WHERE psp.Src = @LexEntryTypeId AND psp.Dst = @Condition

		IF @Id IS NULL BEGIN
			UPDATE CmObject SET class$ = 5118 WHERE Id = @Condition
			INSERT INTO LexEntryType (ID, Type) VALUES (@Condition, 1)
			--( Copy the abbreviation to LexEntryType_ReverseAbbr.  This isn't right,
			--( but is the best we can do. (and better than nothing!)
			INSERT INTO LexEntryType_ReverseAbbr (Obj, Ws, Txt)
				SELECT Obj, Ws, Txt FROM CmPossibility_Abbreviation WHERE Obj=@Condition

			SELECT @OldOwner = Owner$ FROM CmObject	WHERE Id = @Condition

			EXEC MoveOwnedObject$
				@OldOwner, --( current owner
				8008, --( current owning flid
				NULL, --( timestamp, unused
				@Condition, --( ID of 1st object to move
				@Condition, --( ID of last object to move
				@LexEntryTypeId, --( new owner
				7004, --( new owning flid
				null  --( has to do with order
		END
		INSERT INTO LexEntryRef_VariantEntryTypes (Src, Dst, Ord)
			VALUES (@LexEntryRefId, @Condition, @Order)
	END

	FETCH NEXT FROM LexEntryVariant INTO
		@LexEntryId, @EntryOrSenseId, @LexEntryTypeId, @ExcludeAsHeadword, @Condition
END
CLOSE LexEntryVariant
DEALLOCATE LexEntryVariant

-- KZ 3. For each LexEntry where LexEntry.EntryType points to a Type 2 (complex)
--	LexEntryType.
--      3.1. add a new LexEntryRef to LexEntry_EntryRefs.
--      3.2. Set LexEntryRef.Type to LexEntryType.
--      3.3. Set LexEntryRef_PrimaryLexemes and LexEntryRef_ComponentLexemes
--			to LexEntry_MainEntriesOrSenses.
--      3.4. Set LexEntryRef.HideMinorEntry to 0 or 1 based on
--			LexEntry.ExcludeAsHeadword, and clear LexEntry.ExcludeAsHeadword.
--			(NOTE: ExcludeAsHeadword cannot be set to null.)
--      3.5. Set all alternatives of LexEntryRef_Summary to
--			LexEntry_SummaryDefinition and then clear LexEntry_SummaryDefinition.

--( Much of this block of code was copied from variant entries above.
--( Tacky, I know, but I didn't want to take any more time cleaning
--( it up, and this is not a production procedure.

DECLARE LexEntryComplex CURSOR FOR
	SELECT le.id, lemeos.Dst, let.id,
		le.ExcludeAsHeadword, le.Condition, o.Owner$
	FROM LexEntry le
	LEFT  OUTER JOIN LexEntry_MainEntriesOrSenses lemeos ON lemeos.Src = le.Id
	JOIN LexEntryType let ON let.Id = le.EntryType
		AND let.Type = 2  --( Type of entry for display; 2 = treat like sub entry
	JOIN CmObject o ON o.Id = le.Id
	ORDER BY le.Id, lemeos.Ord

OPEN LexEntryComplex
FETCH NEXT FROM LexEntryComplex INTO
	@LexEntryId, @EntryOrSenseId, @LexEntryTypeId,
	@ExcludeAsHeadword, @Condition, @OldOwner
WHILE @@FETCH_STATUS = 0
BEGIN
	--( Check to see if a LexEntryRef already exists for this LexEntry.
	--( We only need one for this set of (old) entries or senses.
	--( Create it if needed.

	SET @LexEntryRefId = NULL --( reset

	SELECT @LexEntryRefId = leer.Dst
	FROM LexEntry_EntryRefs leer
	WHERE leer.Src = @LexEntryId

	IF @LexEntryRefId IS NULL BEGIN
		EXECUTE MakeObj_LexEntryRef
			@ExcludeAsHeadword,		-- @LexEntryRef_HideMinorEntry int = 0,
			null,					-- @LexEntryRef_Summary_ws int = null,
			null,					-- @LexEntryRef_Summary_txt
			null,					-- @LexEntryRef_Summary_fmt
			@LexEntryId,			-- @Owner (LexEntry)
			5002034,				-- @OwnFlid (LexEntry.EntryRefs)
			@LexEntryTypeId,		-- @StartObj
			@LexEntryRefId OUTPUT,	-- @NewObjId output,
			@NewObjGuid OUTPUT,		-- @NewObjGuid output,
			1,						-- @fReturnTimestamp tinyint = 0,
			@NewObjTStamp			-- @NewObjTimestamp int = null output
	END

	--( Moving the LexEntry_SummaryDefinition records over to
	--( LexEntryRef_Summary is done later.

	--( Create the relation between LexEntryRef and its entry or sense

	SELECT @Order = COALESCE(MAX(Ord) + 1, 1)
	FROM LexEntryRef_PrimaryLexemes
	WHERE Src = @LexEntryRefId

	IF @EntryOrSenseId IS NOT NULL
		INSERT INTO LexEntryRef_PrimaryLexemes (Src, Dst, Ord)
			VALUES (@LexEntryRefId, @EntryOrSenseId, @Order)

	--( Create the the component lexemes

	SELECT @Order = COALESCE(MAX(Ord) + 1, 1)
	FROM LexEntryRef_ComponentLexemes
	WHERE Src = @LexEntryRefId

	IF @EntryOrSenseId IS NOT NULL
		INSERT INTO LexEntryRef_ComponentLexemes (Src, Dst, Ord)
			VALUES (@LexEntryRefId, @EntryOrSenseId, @Order)

	--( Check to see if the Variant entry type already exists.
	--( Create it if needed, or move it if it's there.

	SET @Id = NULL

	SELECT @Id = lervet.Src
	FROM LexEntryRef_ComplexEntryTypes lervet
	WHERE lervet.Src = @LexEntryRefId AND lervet.Dst = @LexEntryTypeID

	IF @Id IS NULL BEGIN
		SELECT @Order = COALESCE(MAX(Ord) + 1, 1)
		FROM LexEntryRef_ComplexEntryTypes
		WHERE Src = @LexEntryRefId

		INSERT INTO LexEntryRef_ComplexEntryTypes (Src, Dst, Ord)
			VALUES (@LexEntryRefId, @LexEntryTypeId, @Order)
	END

	FETCH NEXT FROM LexEntryComplex INTO
		@LexEntryId, @EntryOrSenseId, @LexEntryTypeId,
		@ExcludeAsHeadword, @Condition, @OldOwner
END
CLOSE LexEntryComplex
DEALLOCATE LexEntryComplex

--( Now we can move the summary definitions from LexEntry to the newly created LexEntryRef.

INSERT INTO LexEntryRef_Summary (Obj, Flid, Ws, Txt, Fmt)
	SELECT DISTINCT er.Dst, 5127006, sd.Ws, sd.Txt, sd.Fmt
	FROM LexEntry_SummaryDefinition sd
	JOIN LexEntry_EntryRefs er ON er.Src=sd.Obj
	WHERE sd.Flid = 5002017

DELETE FROM LexEntry_SummaryDefinition
	WHERE Flid = 5002017 AND Obj IN (SELECT Src FROM LexEntry_EntryRefs)


--( Susanna wants to change "Derivation" to "Derivative" in the complex entry types,
--( "Inflectional Variant" to "Irregularly Inflected Form" in the variant types,
--( and also delete some obsolete discussion text from a couple of variant types.
UPDATE CmPossibility_Name
	SET Txt=N'Derivative'
	WHERE Txt=N'Derivation'
		AND Obj IN (
			SELECT pss.Dst
			FROM LexDb_ComplexEntryTypes cet
			JOIN CmPossibilityList_Possibilities pss ON pss.Src=cet.Dst)
UPDATE CmPossibility_Name
	SET Txt=N'Irregularly Inflected Form'
	WHERE Txt=N'Inflectional Variant'
		AND Obj IN (
			SELECT pss.Dst
			FROM LexDb_VariantEntryTypes cet
			JOIN CmPossibilityList_Possibilities pss ON pss.Src=cet.Dst)
UPDATE StTxtPara
	SET Contents=NULL, Contents_Fmt=NULL
	WHERE Contents LIKE N'A % variant is a minor entry in the lexical database that is related to a major entry. It contains minimal phonological, semantic, and grammatical information about the variant.'
		AND Id IN (
			SELECT tp.Dst
			FROM LexDb_VariantEntryTypes vet
			JOIN CmPossibilityList_Possibilities pss ON pss.Src=vet.Dst
			JOIN CmPossibility_Discussion d ON d.Src=pss.Dst
			JOIN StText_Paragraphs tp ON tp.Src=d.Dst
			JOIN StTxtPara p ON p.Id=tp.Dst AND p.Contents LIKE N'A % variant is a minor entry in the lexical database that is related to a major entry. It contains minimal phonological, semantic, and grammatical information about the variant.')

-- 4.	Delete all remaining data in properties we plan to delete:
--      CmPossibilityList owned in LexEntry_Condition
--      all LexEntry_EntryType fields
--      CmPossibilityList owned in LexDb_EntryTypes
--      CmPossibilityList owned in LexDb_AllomorphConditions

--( Delete the list owned in LexEntry_Condition. The LexEntry records
--( should have been moved

--( We have decided not to delete anything for now.

/*
DECLARE DeleteCursor CURSOR FOR
	SELECT DISTINCT Owner$ FROM LexEntry_ WHERE Condition > 0
OPEN DeleteCursor
FETCH NEXT FROM DeleteCursor INTO @Id
WHILE @@FETCH_STATUS = 0
BEGIN
	SET @ntIds = @ntIds + ', ' + @Id
	FETCH NEXT FROM DeleteCursor INTO @Id
END
CLOSE DeleteCursor
DEALLOCATE DeleteCursor

IF @Debug != 0
	PRINT 'LexEntry Condition entries to delete' + @ntIds
EXEC DeleteObjects @ntIds
*/

--( Delete all LexEntry_EntryType fields.

/*
DECLARE DeleteCursor CURSOR FOR
	SELECT DISTINCT EntryType from LexEntry where EntryType > 0
Open DeleteCursor

FETCH NEXT FROM DeleteCursor INTO @MoveObject
WHILE @@FETCH_STATUS = 0
BEGIN
	@ntIds = @ntIds + ', ' + @MoveObject

	FETCH NEXT FROM DeleteCursor INTO @MoveObject
END
CLOSE DeleteCursor
DEALLOCATE DeleteCursor
IF @Debug != 0
	PRINT 'LexEntry EntryType entries to delete' + @ntIds
EXEC DeleteObjects @ntIds
*/

--( Delete CmPossibilityList owned in LexDb_EntryTypes

/*
SELECT @Id = Dst FROM LexDb_EntryTypes --( REVIEW (SteveMiller): Is this deletion right?
SET @ntIds = CAST(@Id AS NVARCHAR(10)) + ','
IF @Debug != 0
	PRINT 'LexEntry EntryType entries to delete' + @ntIds
EXEC DeleteObjects @ntIds
*/

--( Delete CmPossibilityList owned in LexDb_AllomorphConditions
/*
SELECT @Id = Dst FROM LexDb_AllomorphConditions
SET @ntIds = CAST(@Id AS NVARCHAR(10)) + ','
IF @Debug != 0
	PRINT 'LexEntry EntryType entries to delete' + @ntIds
EXEC DeleteObjects @ntIds
*/

--5.	Delete properties we no longer want:
--      LexEntry_Condition
--      LexEntry_EntryType
--      LexDb_EntryTypes
--      LexDb_AllomorphConditions
--      LexEntryType_Type

-- BEGIN TEST SELECT
--SELECT DISTINCT le.Id, le.HomographNumber, le.ExcludeAsHeadword,
--				er.Ord, ler.HideMinorEntry, nc.Ws 'ComplexWs', nc.Txt 'ComplexName', cet.Ord 'ComplexOrd', nv.Ws 'VariantWs', nv.Txt 'VariantName', vet.Ord 'VariantOrd',
--				cl.Dst 'Component', cl.Ord 'ComponentOrd', pl.Dst 'Primary', pl.Ord 'PrimaryOrd', su.Ws 'SummaryWs', su.Txt 'Summary'
--FROM LexEntry le
--JOIN LexEntry_EntryRefs er ON er.Src=le.id
--JOIN LexEntryRef ler ON ler.Id=er.Dst
--LEFT OUTER JOIN LexEntryRef_ComplexEntryTypes cet ON cet.Src=ler.Id
--LEFT OUTER JOIN CmPossibility_Name nc ON nc.Obj=cet.Dst
--LEFT OUTER JOIN LexEntryRef_VariantEntryTypes vet ON vet.Src=ler.Id
--LEFT OUTER JOIN CmPossibility_Name nv ON nv.Obj=vet.Dst
--LEFT OUTER JOIN LexEntryRef_ComponentLexemes cl ON cl.Src=ler.Id
--LEFT OUTER JOIN LexEntryRef_PrimaryLexemes pl ON pl.Src=ler.Id
--LEFT OUTER JOIN LexEntryRef_Summary su ON su.Obj=ler.Id
--SELECT TOP 1 @hvoTestList = Dst FROM LexDb_EntryTypes ; EXEC GetPossibilities @hvoTestList, 0xfffffffa
--SELECT TOP 1 @hvoTestList = Dst FROM LexDb_AllomorphConditions ; EXEC GetPossibilities @hvoTestList, 0xfffffffa
--SELECT TOP 1 @hvoTestList = Dst FROM LexDb_ComplexEntryTypes ; EXEC GetPossibilities @hvoTestList, 0xfffffffa
--SELECT TOP 1 @hvoTestList = Dst FROM LexDb_VariantEntryTypes ; EXEC GetPossibilities @hvoTestList, 0xfffffffa
--SELECT let.*, a.Ws, a.Txt 'Abbr', ra.Txt 'RevAbbr'
--FROM LexEntryType let
--LEFT OUTER JOIN CmPossibility_Abbreviation a ON a.Obj=let.Id
--LEFT OUTER JOIN LexEntryType_ReverseAbbr ra ON ra.Obj=let.Id AND ra.Ws=a.Ws
-- END TEST SELECT

-----------------------------------------------------------------------------------
------ Finish or roll back transaction as applicable
-----------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200239
BEGIN
	UPDATE Version$ SET DbVer = 200240
	COMMIT TRANSACTION
	PRINT 'database updated to version 200240'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200239 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
