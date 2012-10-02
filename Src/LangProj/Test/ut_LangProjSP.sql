--===================================================================
--== Triggers
--===================================================================

IF OBJECT_ID('ut_Trigger_Field$InsLast') IS NOT NULL BEGIN
	DROP PROCEDURE ut_Trigger_Field$InsLast
END
GO
CREATE PROCEDURE ut_Trigger_Field$InsLast AS
BEGIN

	DECLARE
		@nAllowsNull INT,
		@nOrdinalPosition INT

	--( We're using the class Id for WfiWordform

	INSERT INTO Field$
		(Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
	VALUES
		(5062999, 2, 5062, NULL, 'TestFieldInsert', 0, NULL, NULL)

	--( Check update view. We don't really care whether the column allows null or
	--( not. Rather, COLUMNPROPERTY() will return NULL if the column isn't there
	--( which is really what we want to know.

	SELECT @nAllowsNull = COLUMNPROPERTY(OBJECT_ID('WfiWordform_'),'TestFieldInsert','ALLOWSNULL')
	IF @nAllowsNull IS NULL
		EXEC tsu_failure 'View WfiWordform_ was not updated.'

	--( A check exists in GenMakeObjProc to keep from executing on an abstract
	--( class, and raises an error. No need to check here. Check to see if the
	--( MakeObj_WfiWordform still exists, and if GenMakeObjProc update it.

	IF OBJECT_ID('MakeObj_WfiWordform') IS NULL
		EXEC tsu_failure 'Stored procedure MakeObj_WfiWordform disappeared.'

	--( We don't really care about the ordinal position below, just that the new
	--( parameter was added to MakeObj_WfiWordform.

	SELECT @nOrdinalPosition = Ordinal_Position
	FROM Information_Schema.Parameters
	WHERE Specific_Name = 'MakeObj_WfiWordform'
		AND Parameter_Name = '@WfiWordform_TestFieldInsert'

	IF @@ROWCOUNT != 1
		EXEC tsu_failure 'Stored procedure MakeObj_WfiWordform was not updated.'

END
GO

--===================================================================
--== GetOrderedMultiTxt
--===================================================================

IF OBJECT_ID('ut_Lists_GetOrderedMultiTxt') IS NOT NULL BEGIN
	DROP PROCEDURE ut_Lists_GetOrderedMultiTxt
END
GO
CREATE PROCEDURE ut_Lists_GetOrderedMultiTxt AS
BEGIN

	DECLARE
		@VerbId NVARCHAR(20),
		@NounId NVARCHAR(20),
		@FieldId INT,
		@Analysis TINYINT,
		@WritingSystem INT,
		@Txt NVARCHAR(MAX),
		@Ids NVARCHAR(40);

	DECLARE @tblGetOrderedMultiTxt TABLE (
		Txt NVARCHAR(MAX),
		Ws INT,
		Ord INT);

	SET @FieldId = 7001;
	SET @Analysis = 0;

	SELECT @VerbId = CAST(pn.Obj AS NVARCHAR(20))
	FROM CmPossibility_Name pn
	WHERE pn.Txt = 'verb';

	IF @@ROWCOUNT != 1
		EXEC tsu_failure 'Can''t find "verb" The database has changed.';

	SELECT @NounId = CAST(pn.Obj AS NVARCHAR(20))
	FROM CmPossibility_Name pn
	WHERE pn.Txt = 'noun';

	IF @@ROWCOUNT != 1
		EXEC tsu_failure 'Can''t find "noun" The database has changed.';

	SELECT @WritingSystem = Id FROM LgWritingSystem ws where ws.ICULocale = 'en';
	IF @@ROWCOUNT != 1
		EXEC tsu_failure 'Had trouble picking up the English writing system.';

	--( Test for a single ID passed
	INSERT INTO @tblGetOrderedMultiTxt
		EXEC GetOrderedMultiTxt @VerbId, @FieldId, @Analysis;

	SELECT @Txt = Txt FROM @tblGetOrderedMultiTxt WHERE Ws = @WritingSystem
	IF @@ROWCOUNT != 1
		EXEC tsu_failure 'The list should have "verb".';

	--( Test for a two IDs passed

	DELETE FROM @tblGetOrderedMultiTxt
	SET @Ids = @NounId + N',' + @VerbId;

	INSERT INTO @tblGetOrderedMultiTxt
		EXEC GetOrderedMultiTxt @Ids, @FieldId, @Analysis;

	SELECT @Txt = Txt
	FROM @tblGetOrderedMultiTxt
	WHERE Ws = @WritingSystem AND Txt = 'verb';

	IF @Txt != 'verb'
		EXEC tsu_failure 'The list should have "verb".';

	SELECT @Txt = Txt
	FROM @tblGetOrderedMultiTxt
	WHERE Ws = @WritingSystem AND Txt = 'noun';

	IF @Txt != 'noun'
		EXEC tsu_failure 'The list should have "noun".';

END
GO
