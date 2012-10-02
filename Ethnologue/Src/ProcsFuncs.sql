/*======================================================================
	ProcsFuncs.sql
======================================================================*/

USE Ethnologue

------------------------------------------------------------------------

IF OBJECT_ID('GetIcuCode') IS NOT NULL BEGIN
	DROP PROCEDURE GetIcuCode
	PRINT 'Removing procedure GetIcuCode'
END
PRINT 'Creating procedure GetIcuCode'
GO

CREATE PROCEDURE GetIcuCode
	@cEthnologueCode CHAR(3),
	@cIcuCode CHAR(4) OUTPUT
AS
	SELECT @cIcuCode = Icu
	FROM Ethnologue
	WHERE Iso6393 = @cEthnologueCode

	IF @@ROWCOUNT = 0 OR @cIcuCode IS NULL --( if nothing could be found
		SET @cIcuCode = 'x' + LTRIM(@cEthnologueCode)

	SET @cIcuCode = LTRIM(RTRIM(LOWER(@cIcuCode)))

GO

------------------------------------------------------------------------

IF OBJECT_ID('GetIsoCode') IS NOT NULL BEGIN
	DROP PROCEDURE GetIsoCode
	PRINT 'Removing procedure GetIsoCode'
END
PRINT 'Creating procedure GetIsoCode'
GO

CREATE PROCEDURE GetIsoCode
	@cCode VARCHAR(4),
	@cEthnologueCode CHAR(3) OUTPUT
AS
	--( This procedure was originally "GetEthnoCodeFromIcu", but
	--( with recent changes with the ISO standard, became what
	--( it is.
	DECLARE
		@nLenCode TINYINT,
		@nCount TINYINT

	SET @cCode = LOWER(LTRIM(RTRIM(@cCode)))
	SET @nLenCode = LEN(@cCode)
	SET @cEthnologueCode = NULL

	IF @nLenCode = 2 OR @nLenCode = 3
		SELECT @cEthnologueCode = Iso6393 FROM Ethnologue WHERE Icu = @cCode
	ELSE IF @nLenCode = 4 --( either 'e' + code, or 'x' + code
		BEGIN
			IF SUBSTRING(@cCode, 1, 1) = 'e'
				SET @cEthnologueCode = SUBSTRING(@cCode, 2, 3)
			ELSE --( the first letter is 'x'
				SET @cEthnologueCode = NULL
		END
	ELSE --( error out, at least in the unit test
		SET @cEthnologueCode = NULL
GO

------------------------------------------------------------------------

IF OBJECT_ID('fnGetLanguageNamesLike') IS NOT NULL BEGIN
	DROP FUNCTION fnGetLanguageNamesLike
	PRINT 'Removing function fnGetLanguageNamesLike'
END
PRINT 'Creating function fnGetLanguageNamesLike'
GO

--( Names returned include dialects, not just primary names

--( The function carries a second parameter:
--(		L = left part of the word
--(		R = right part of the word
--(		anything else gets all matches irregardless of word position

CREATE FUNCTION fnGetLanguageNamesLike (
	@nvcNameLike NVARCHAR(75),
	@cWhichPart CHAR(1) = NULL)
RETURNS @tblNames TABLE (
	LangId			INT,
	LangName		NVARCHAR(75),
	CountryId		NCHAR(2),
	CountryName		NVARCHAR(40),
	EthnologueId	SMALLINT,
	EthnologueCode	NCHAR(3))
AS

BEGIN
	IF @cWhichPart = 'L'
		SET @nvcNameLike = @nvcNameLike + N'%'
	ELSE IF @cWhichPart = 'R'
		SET @nvcNameLike = N'%' + @nvcNameLike
	ELSE
		SET @nvcNameLike = N'%' + @nvcNameLike + N'%'

	INSERT INTO @tblNames
	SELECT DISTINCT
		ln.Id,
		ln.Name,
		ll.CountryUsedInId,
		c.Name,
		ll.EthnologueId,
		e.Iso6393
	FROM LanguageName ln
	JOIN LanguageLocation ll ON ll.LanguageId = ln.Id
	JOIN Country c ON c.Id = ll.CountryUsedInId
	JOIN Ethnologue e ON e.Id = ll.EthnologueId
	WHERE ln.Name LIKE @nvcNameLike

	--== Reseek ==--

	--( If the first attempt fails, split name apart, and
	--( retry using the parts of the name.

	IF @@ROWCOUNT = 0 BEGIN
		DECLARE
			@nSpacePosition INT,
			@nCount INT,
			@nvcWord NVARCHAR(15),
			@nvcWord1 NVARCHAR(15),
			@nvcWord2 NVARCHAR(15),
			@nvcWord3 NVARCHAR(15),
			@nvcWord4 NVARCHAR(15),
			@nvcWord5 NVARCHAR(15)

		SET @nvcWord2 = '%%'
		SET @nvcWord3 = '%%'
		SET @nvcWord4 = '%%'
		SET @nvcWord5 = '%%'

		--( strip out periods, commas, and percents
		SET @nvcNameLike = REPLACE(@nvcNameLike, N'%', '')
		SET @nvcNameLike = REPLACE(@nvcNameLike, N'.', '')
		SET @nvcNameLike = REPLACE(@nvcNameLike, N',', '')

		--( Dynamic SQL can't be used because of the table
		--( variable. This is ugly code, but at least it works.

		SET @nCount = 0
		SET @nSpacePosition = 1
		WHILE @nSpacePosition != 0 BEGIN
			SET @nCount = @nCount + 1
			SET @nSpacePosition = CHARINDEX(N' ', @nvcNameLike)
			IF @nSpacePosition = 0
				SET @nvcWord = @nvcNameLike
			ELSE BEGIN
				SET @nvcWord = SUBSTRING(@nvcNameLike, 1, @nSpacePosition - 1)
				SET @nvcNameLike = SUBSTRING(@nvcNameLike,
					@nSpacePosition + 1,
					LEN(@nvcNameLike) - @nSpacePosition)
			END

			IF @nCount = 1
				SET @nvcWord1 = '%' + @nvcWord + '%'
			ELSE IF @nCount = 2
				SET @nvcWord2 = '%' + @nvcWord + '%'
			ELSE IF @nCount = 3
				SET @nvcWord3 = '%' + @nvcWord + '%'
			ELSE IF @nCount = 4
				SET @nvcWord4 = '%' + @nvcWord + '%'
			ELSE IF @nCount = 5
				SET @nvcWord5 = '%' + @nvcWord + '%'

		END --( WHILE @nSpacePosition != 0 BEGIN

		INSERT INTO @tblNames
		SELECT DISTINCT
			ln.Id,
			ln.Name,
			ll.CountryUsedInId,
			c.Name,
			ll.EthnologueId,
			e.Iso6393
		FROM LanguageName ln
		JOIN LanguageLocation ll ON ll.LanguageId = ln.Id
		JOIN Country c ON c.Id = ll.CountryUsedInId
		JOIN Ethnologue e ON e.Id = ll.EthnologueId
		WHERE ln.Name LIKE @nvcWord1
			AND ln.Name LIKE @nvcWord2
			AND ln.Name LIKE @nvcWord3
			AND ln.Name LIKE @nvcWord4
			AND ln.Name LIKE @nvcWord5

	END --( IF @@ROWCOUNT = 0 BEGIN

	RETURN
END
GO

------------------------------------------------------------------------

IF OBJECT_ID('fnGetOtherLanguageNames') IS NOT NULL BEGIN
	DROP FUNCTION fnGetOtherLanguageNames
	PRINT 'Removing function fnGetOtherLanguageNames'
END
PRINT 'Creating function fnGetOtherLanguageNames'
GO

CREATE FUNCTION fnGetOtherLanguageNames (@ncEthnoCode NCHAR(3))
RETURNS @tblNames TABLE (
	IsPrimaryName	TINYINT,
	LangName		NVARCHAR(75))
AS
BEGIN

	INSERT INTO @tblNames
	SELECT DISTINCT
		CASE WHEN el.Id IS NULL THEN 0 ELSE 1 END,
		ln.Name
	FROM Ethnologue e
	JOIN LanguageLocation ll ON ll.EthnologueId = e.Id
	JOIN LanguageName ln ON ln.Id = ll.LanguageId
	LEFT OUTER JOIN EthnologueLocation el ON el.PrimaryNameId = ll.LanguageId
	WHERE e.Iso6393 = @ncEthnoCode
	ORDER BY 1 DESC, 2

	RETURN
END
GO

------------------------------------------------------------------------

--( The function fnGetLanguagesInCountry carries a second parameter:
--(		P = Primary Name, main country used in
--(		anything else gets all dialect and language names for the country

IF OBJECT_ID('fnGetLanguagesInCountry') IS NOT NULL BEGIN
	DROP FUNCTION fnGetLanguagesInCountry
	PRINT 'Removing function fnGetLanguagesInCountry'
END
PRINT 'Creating function fnGetLanguagesInCountry'
GO

CREATE FUNCTION fnGetLanguagesInCountry (
	@nvcCountryName NVARCHAR(40),
	@cWhich CHAR(1) = NULL)
RETURNS @tblNames TABLE (
	LangId			INT,
	LangName		NVARCHAR(75),
	CountryId		NCHAR(2),
	CountryName		NVARCHAR(40),
	EthnologueId	SMALLINT,
	EthnologueCode	NCHAR(3))
AS
BEGIN
	IF @cWhich = 'P'

		INSERT INTO @tblNames
		SELECT DISTINCT
			ln.Id,
			ln.Name,
			el.MainCountryUsedId,
			c.Name,
			el.EthnologueId,
			e.Iso6393
		FROM Country c
		JOIN EthnologueLocation el ON el.MainCountryUsedId = c.Id
		JOIN LanguageName ln ON ln.Id = el.PrimaryNameId
		JOIN Ethnologue e ON e.Id = el.EthnologueId
		WHERE c.Name = @nvcCountryName

	ELSE

		INSERT INTO @tblNames
		SELECT DISTINCT
			ln.Id,
			ln.Name,
			ll.CountryUsedInId,
			c.Name,
			ll.EthnologueId,
			e.Iso6393
		FROM Country c
		JOIN LanguageLocation ll ON ll.CountryUsedInId = c.Id
		JOIN LanguageName ln ON ln.Id = ll.LanguageId
		JOIN Ethnologue e ON e.Id = ll.EthnologueId
		WHERE c.Name = @nvcCountryName


	RETURN
END
GO

------------------------------------------------------------------------

IF OBJECT_ID('fnGetLanguagesForIso') IS NOT NULL BEGIN
	DROP FUNCTION fnGetLanguagesForIso
	PRINT 'Removing function fnGetLanguagesForIso'
END
PRINT 'Creating function fnGetLanguagesForIso'
GO

CREATE FUNCTION fnGetLanguagesForIso (
	@ncEthnoCode NCHAR(3))
RETURNS @tblNames TABLE (
	LangId			INT,
	LangName		NVARCHAR(75),
	CountryId		NCHAR(2),
	CountryName		NVARCHAR(40),
	EthnologueId	SMALLINT,
	EthnologueCode	NCHAR(3))
AS
BEGIN

	--( This function was originally named "fnGetLangaugesForEthnor".
	--( Now that ISO 639-3 is the standard, the function is renamed.

	INSERT INTO @tblNames
	SELECT DISTINCT
		ln.Id,
		ln.Name,
		ll.CountryUsedInId,
		c.Name,
		ll.EthnologueId,
		e.Iso6393
	FROM Ethnologue e
	JOIN LanguageLocation ll ON ll.EthnologueId = e.Id
	JOIN LanguageName ln ON ln.Id = ll.LanguageId
	JOIN Country c ON c.Id = ll.CountryUsedInId
	WHERE e.Iso6393 = @ncEthnoCode

	RETURN
END
GO

------------------------------------------------------------------------

--( Shrink this thing down now--assuming this is called by a .bat or a
--( NAnt procedure.

DBCC SHRINKFILE(Ethnologue, 1)
DBCC SHRINKFILE(Ethnologue_log, TRUNCATEONLY)

GO