/*======================================================================
	ut_ProcsFuncs.sql
======================================================================*/

--=========================
-- Suite: GetItem
--=========================

IF OBJECT_ID('ut_GetItem_setup') IS NOT NULL BEGIN
	DROP PROCEDURE "ut_GetItem_setup"
	PRINT 'Removing procedure ut_GetItem_setup'
END
PRINT 'Creating procedure ut_GetItem_setup'
GO

CREATE PROCEDURE "ut_GetItem_setup" AS
BEGIN
	DECLARE
		@nvcIsoCode NVARCHAR(4),
		@ncIsoCode1 NCHAR(2),
		@ncIsoCode2 NCHAR(3),
		@ncEthnologueCode NCHAR(3),
		@nLangCount INT

	--( Test for database changes

	--== Setup1 ==--

	SET @ncEthnologueCode = 'aay'

	SELECT @ncIsoCode1 = Iso6391
	FROM Ethnologue
	WHERE Iso6393 = @ncEthnologueCode

	IF @ncIsoCode1 IS NOT NULL
		EXEC tsu_failure 'Setup: Database changed. Ethnlogue code aay has picked up an ISO code.'

	--== Setup2 ==--

	SET @ncEthnologueCode = 'iii'

	SELECT @ncIsoCode1 = Iso6391
	FROM Ethnologue
	WHERE Iso6393 = @ncEthnologueCode

	IF @ncIsoCode1 IS NULL
		EXEC tsu_failure 'Setup: Database changed. Ethnlogue code iii dropped an ISO code.'

	--== Setup3 ==--

	SET @ncEthnologueCode = 'atj'

	SELECT @ncIsoCode1 = Iso6391
	FROM Ethnologue
	WHERE Iso6393 = @ncEthnologueCode

	IF @ncIsoCode1 IS NOT NULL
		EXEC tsu_failure 'Setup: Database changed. Ethnlogue code atj probably picked up an ISO639 code.'

	--== Setup4 ==--

	SET @ncEthnologueCode = 'xxx'

	SELECT @ncIsoCode1 = Iso6391
	FROM Ethnologue
	WHERE Iso6393 = @ncEthnologueCode

	IF @@ROWCOUNT != 0
		EXEC tsu_failure 'Setup: Database changed. The Ethnlogue has an xxx code now.'
END
GO
--------------------------------------------------------------------

IF OBJECT_ID('ut_GetItem_Icu') IS NOT NULL BEGIN
	DROP PROCEDURE "ut_GetItem_Icu"
	PRINT 'Removing procedure ut_GetItem_Icu'
END
PRINT 'Creating procedure ut_GetItem_Icu'
GO

CREATE PROCEDURE "ut_GetItem_Icu" AS
BEGIN
	DECLARE
		@cIcuCode CHAR(4),
		@cEthnologueCode CHAR(3)

	--== Check 1 ==--

	SET @cEthnologueCode = 'aay'
	EXEC GetIcuCode @cEthnologueCode, @cIcuCode OUTPUT

	IF @cIcuCode != 'aay' OR @cIcuCode IS NULL
		EXEC tsu_failure 'The ICU code for Ethnologue aay should be aay'

	--== Check 2 ==--

	SET @cEthnologueCode = 'eng'
	EXEC GetIcuCode @cEthnologueCode, @cIcuCode OUTPUT

	IF @cIcuCode != 'en' OR @cIcuCode IS NULL
		EXEC tsu_failure 'The ICU code for Ethnologue eng should be en'

	--== Check 3 ==--

	SET @cEthnologueCode = 'xxx'
	EXEC GetIcuCode @cEthnologueCode, @cIcuCode OUTPUT

	IF @cIcuCode != 'xxxx' OR @cIcuCode IS NULL
		EXEC tsu_failure 'Ethnologue xxx should not be found. The return should be xxxx'

END
GO

--------------------------------------------------------------------

IF OBJECT_ID('ut_GetItem_Iso') IS NOT NULL BEGIN
	DROP PROCEDURE "ut_GetItem_Iso"
	PRINT 'Removing procedure ut_GetItem_Iso'
END
PRINT 'Creating procedure ut_GetItem_Iso'
GO

CREATE PROCEDURE "ut_GetItem_Iso" AS
BEGIN
	DECLARE
		@cEthnologueCode CHAR(3),
		@cIcuCode CHAR(4)

	--== Check 1 ==--

	--( No ICU code or Ethnologue code. Strictly speaking, the front end
	--( could strip off the initial X, but this way the function can be
	--( used as a black box.

	SET @cIcuCode = 'xxxx'
	EXEC GetIsoCode @cIcuCode, @cEthnologueCode OUTPUT

	IF @cEthnologueCode IS NOT NULL
		EXEC tsu_failure 'The Ethnologue code for ICU xxxx should be null.'

	--== Check 2 ==--

	--( 2 letter Icu code

	SET @cIcuCode = 'ii'
	EXEC GetIsoCode @cIcuCode, @cEthnologueCode OUTPUT

	IF @cEthnologueCode != 'iii' OR @cEthnologueCode IS NULL
		EXEC tsu_failure 'The Ethnologue code for ICU ii should be iii.'

	SET @cIcuCode = 'xx'
	EXEC GetIsoCode @cIcuCode, @cEthnologueCode OUTPUT

	IF @cEthnologueCode IS NOT NULL
		EXEC tsu_failure 'The Ethnologue code for Icu xx should be NULL.'

	--== Check 3 ==--

	--( 3 letter Icu code

	SET @cIcuCode = 'atj'
	EXEC GetIsoCode @cIcuCode, @cEthnologueCode OUTPUT

	IF @cEthnologueCode != 'atj' OR @cEthnologueCode IS NULL
		EXEC tsu_failure 'The Ethnologue code for Icu atj should be atj'

	SET @cIcuCode = 'xxx'
	EXEC GetIsoCode @cIcuCode, @cEthnologueCode OUTPUT

	IF @cEthnologueCode IS NOT NULL
		EXEC tsu_failure 'The Ethnologue code for Icu xxx should be NULL.'

	--== Check 4 ==--

	--( No Icu code. Strictly speaking, the front end
	--( could strip off the initial E, but this way the function can be
	--( used as a black box.

	SET @cIcuCode = 'eaay'
	EXEC GetIsoCode @cIcuCode, @cEthnologueCode OUTPUT

	IF @cEthnologueCode != 'aay' OR @cEthnologueCode IS NULL
		EXEC tsu_failure 'The Ethnologue code eaay should be aay.'

END
GO

------------------------------------------------------------------------

--=========================
-- Suite: GetList
--=========================

IF OBJECT_ID('ut_GetList_LangNameLike') IS NOT NULL BEGIN
	DROP PROCEDURE "ut_GetList_LangNameLike"
	PRINT 'Removing procedure ut_GetList_LangNameLike'
END
PRINT 'Creating procedure ut_GetList_LangNameLike'
GO

--( The function fnGetLanguageNamesLike carries a second parameter:
--(		L = left part of the word
--(		R = right part of the word
--(		anything else gets all matches irregardless of word position

CREATE PROCEDURE "ut_GetList_LangNameLike" AS
BEGIN

	--== Setup ==--

	DECLARE
		@nvcNameLike NVARCHAR(75),
		@nLangCount INT,
		@nvcCountryName NVARCHAR(40),
		@ncEthnologueCode NCHAR(3)

	DECLARE @tblNames TABLE (
		LangId			INT,
		LangName		NVARCHAR(75),
		CountryId		NCHAR(2),
		CountryName		NVARCHAR(40),
		EthnologueId	SMALLINT,
		EthnologueCode	NCHAR(3))

	--== Right Matches ==--

	SET @nvcNameLike = 'd hills'

	SELECT @nLangCount = COUNT("Id")
	FROM LanguageName
	WHERE "Name" LIKE ('%' + @nvcNameLike)

	IF @nLangCount != 1
		EXEC tsu_failure 'Database changed. There were 1 record with ''hills'' at the end.'

	INSERT INTO @tblNames
		SELECT
			LangId,
			LangName,
			CountryId,
			CountryName,
			EthnologueId,
			EthnologueCode
		FROM dbo.fnGetLanguageNamesLike(@nvcNameLike, 'R')

	IF @@ROWCOUNT != 1
		EXEC tsu_failure 'Should be 1 record from function with ''hills'' at the end.'

	SELECT @nvcNameLike = LangName FROM @tblNames WHERE LangName = 'Aird Hills'
	IF @nvcNameLike != 'Aird hills'
		EXEC tsu_failure 'The name of ''Aird Hills'' should be in the list'

	DELETE FROM @tblNames

	--== Left Matches ==--

	SET @nvcNameLike = 'Apani'

	SELECT @nLangCount = COUNT("Id")
	FROM LanguageName
	WHERE "Name" LIKE (@nvcNameLike + '%')

	IF @nLangCount != 2
		EXEC tsu_failure 'Database changed. There were 2 records with ''Apani'' at the beginning.'

	INSERT INTO @tblNames
		SELECT
			LangId,
			LangName,
			CountryId,
			CountryName,
			EthnologueId,
			EthnologueCode
		FROM dbo.fnGetLanguageNamesLike(@nvcNameLike, 'L')

	IF @@ROWCOUNT != 2
		EXEC tsu_failure 'Should be 2 records with ''Apani'' at the beginning.'

	SELECT @nvcNameLike = LangName FROM @tblNames WHERE LangName = 'Apaniekra'
	IF @nvcNameLike != 'Apaniekra'
		EXEC tsu_failure 'The name of ''Apaniekra'' should be in the list'

	DELETE FROM @tblNames

	--== Any Matches ==--

	SET @nvcNameLike = 'Foothill'

	SELECT @nLangCount = COUNT("Id")
	FROM LanguageName
	WHERE "Name" LIKE ('%' + @nvcNameLike + '%')

	IF @nLangCount != 2
		EXEC tsu_failure 'Database changed. There were 2 records with ''Foothill'' in the name.'

	INSERT INTO @tblNames
		SELECT
			LangId,
			LangName,
			CountryId,
			CountryName,
			EthnologueId,
			EthnologueCode
		FROM dbo.fnGetLanguageNamesLike(@nvcNameLike, NULL)

	IF @@ROWCOUNT != 2
		EXEC tsu_failure 'Should be 2 records from function with ''Foothill'' in the name.'

	SELECT @nvcNameLike = LangName FROM @tblNames WHERE LangName = 'Northern Foothill Yokuts'
	IF @nvcNameLike != 'Northern Foothill Yokuts'
		EXEC tsu_failure 'The name of ''Northern Foothill Yokuts'' should be in the list'

	--== Other Info ==--

	SET @nvcNameLike = 'Laotian'

	SELECT @nLangCount = COUNT("Id")
	FROM LanguageName
	WHERE "Name" LIKE ('%' + @nvcNameLike + '%')

	IF @nLangCount != 3
		EXEC tsu_failure 'Database changed. There were 3 records with ''Laotian'' in the name.'

	INSERT INTO @tblNames
		SELECT
			LangId,
			LangName,
			CountryId,
			CountryName,
			EthnologueId,
			EthnologueCode
		FROM dbo.fnGetLanguageNamesLike(@nvcNameLike, NULL)

	SELECT
		@nvcCountryName = CountryName,
		@ncEthnologueCode = EthnologueCode
	FROM @tblNames WHERE LangName = 'Western Laotian'

	IF @nvcCountryName != 'Thailand'
		EXEC tsu_failure 'Western Laotian is spoken in Thailand'
	IF @ncEthnologueCode != 'NOD'
		EXEC tsu_failure 'The Ethnologue code for Western Laotian is NOD'

	--== Retry if Seek Unsuccessful ==--

	SET @nvcNameLike = 'Abnaki, Western'

	SELECT @nLangCount = COUNT("Id")
	FROM LanguageName
	WHERE "Name" LIKE ('%' + @nvcNameLike + '%')

	IF @nLangCount != 1
		EXEC tsu_failure 'Database changed. There was 1 record with "Abnaki, Western" as a name.'

	SET @nvcNameLike = 'Western Abnaki' --( should still find this

	SELECT @nLangCount = COUNT(LangId)
	FROM dbo.fnGetLanguageNamesLike(@nvcNameLike, NULL)

	IF @nLangCount != 1
		EXEC tsu_failure '"Western Abnaki" should be found'

END
GO

------------------------------------------------------------------------

IF OBJECT_ID('ut_GetList_OtherLangNames') IS NOT NULL BEGIN
	DROP PROCEDURE "ut_GetList_OtherLangNames"
	PRINT 'Removing procedure ut_GetList_OtherLangNames'
END
PRINT 'Creating procedure ut_GetList_OtherLangNames'
GO

CREATE PROCEDURE "ut_GetList_OtherLangNames" AS
BEGIN

	--== Setup ==--

	DECLARE
		@ncEthnoCode NCHAR(3),
		@nEthnoId SMALLINT,
		@nvcLangName NVARCHAR(75)

	SET @ncEthnoCode = 'lao'

	SELECT @nEthnoId = ll.EthnologueId
	FROM Ethnologue e
	JOIN LanguageLocation ll ON ll.EthnologueId = e."Id"
	WHERE e.Iso6393 = @ncEthnoCode

	IF @@ROWCOUNT = 0
		EXEC tsu_failure 'Database changed. Lao Kao doesn''t exist as a language name anymore.'

	SELECT TOP 1 @nvcLangName = LangName
	FROM dbo.fnGetOtherLanguageNames(@ncEthnoCode)
	ORDER BY LangName

	IF @nvcLangName != 'Eastern Thai'
		EXEC tsu_failure 'The language name should be Eastern Thai.'

END
GO

------------------------------------------------------------------------

IF OBJECT_ID('ut_GetList_LanguagesInCountry') IS NOT NULL BEGIN
	DROP PROCEDURE "ut_GetList_LanguagesInCountry"
	PRINT 'Removing procedure ut_GetList_LanguagesInCountry'
END
PRINT 'Creating procedure ut_GetList_LanguagesInCountry'
GO

--( The function fnGetLanguagesInCountry carries a second parameter:
--(		P = Primary Name, main country used in
--(		anything else gets all dialect and language names for the country

CREATE PROCEDURE "ut_GetList_LanguagesInCountry" AS
BEGIN

	DECLARE
		@nvcCountry NVARCHAR(40),
		@nCount INT,
		@nvcLanguageName NVARCHAR(75)

	DECLARE @tblNames TABLE (
		LangId			INT,
		LangName		NVARCHAR(75),
		CountryId		NCHAR(2),
		CountryName		NVARCHAR(40),
		EthnologueId	SMALLINT,
		EthnologueCode	NCHAR(3))

	SET @nvcCountry = 'Bahamas'

	--== All Dialects ==--

	SELECT @nCount = COUNT(ll.CountryUsedInId)
	FROM LanguageLocation ll
	JOIN Country c ON c."Id" = ll.CountryUsedInId
	WHERE c."Name" = @nvcCountry

	IF @nCount != 7
		EXEC tsu_failure 'Database changed. There were 7 languages and dialects in the Bahamas.'

	INSERT INTO @tblNames
		SELECT
			LangId,
			LangName,
			CountryId,
			CountryName,
			EthnologueId,
			EthnologueCode
		FROM dbo.fnGetLanguagesInCountry(@nvcCountry, NULL)

	SELECT @nvcLanguageName = LangName FROM @tblNames WHERE LangName = 'Greek'
	IF @nvcLanguageName != 'Greek'
		EXEC tsu_failure 'Greek is spoken in the Bahamas.'

	--== Primary Language ==--

	SELECT @nCount = COUNT(el.MainCountryUsedId)
	FROM EthnologueLocation el
	JOIN Country c ON c."Id" = el.MainCountryUsedId
	WHERE c."Name" = @nvcCountry

	IF @nCount != 2
		EXEC tsu_failure 'Database changed. There were 2 primary languages in the Bahamas.'

	INSERT INTO @tblNames
		SELECT
			LangId,
			LangName,
			CountryId,
			CountryName,
			EthnologueId,
			EthnologueCode
		FROM dbo.fnGetLanguagesInCountry(@nvcCountry, 'P')

	SELECT @nvcLanguageName = LangName FROM @tblNames WHERE LangName = 'Taino'
	IF @nvcLanguageName != 'Taino'
		EXEC tsu_failure 'Taino is one of two primary langugaes spoken in the Bahamas.'
END
GO

------------------------------------------------------------------------

IF OBJECT_ID('ut_GetList_LanguagesForEthno') IS NOT NULL BEGIN
	DROP PROCEDURE "ut_GetList_LanguagesForEthno"
	PRINT 'Removing procedure ut_GetList_LanguagesForEthno'
END
PRINT 'Creating procedure ut_GetList_LanguagesForEthno'
GO

CREATE PROCEDURE "ut_GetList_LanguagesForEthno" AS
BEGIN

	DECLARE
		@ncEthnoCode NCHAR(3),
		@nCount INT,
		@nvcLanguageName NVARCHAR(75)

	DECLARE @tblNames TABLE (
		LangId			INT,
		LangName		NVARCHAR(75),
		CountryId		NCHAR(2),
		CountryName		NVARCHAR(40),
		EthnologueId	SMALLINT,
		EthnologueCode	NCHAR(3))

	SET @ncEthnoCode = 'lao'

	--== All Dialects ==--

	SELECT @nCount = COUNT(ll.EthnologueId)
	FROM LanguageLocation ll
	JOIN Ethnologue e ON e."Id" = ll.EthnologueId
	WHERE e.Iso6393 = @ncEthnoCode

	IF @nCount != 28
		EXEC tsu_failure 'Database changed. There were 28 languages and dialect names for Ethnologue lao.'

	INSERT INTO @tblNames
		SELECT
			LangId,
			LangName,
			CountryId,
			CountryName,
			EthnologueId,
			EthnologueCode
		FROM dbo.fnGetLanguagesForIso(@ncEthnoCode)

	SELECT @nvcLanguageName = LangName FROM @tblNames WHERE LangName = 'Pakse'
	IF @nvcLanguageName != 'Pakse'
		EXEC tsu_failure 'Pakse is one of langugae names for Ethnologue NOL.'
END
GO

------------------------------------------------------------------------
