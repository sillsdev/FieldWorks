/*=================================================================
	NormalizeData.sql
=================================================================*/

USE Ethnologue

--==( LanguageStatus )==--

INSERT INTO LanguageStatus VALUES ('L', 'Living')
INSERT INTO LanguageStatus VALUES ('N', 'Nearly Extinct')
INSERT INTO LanguageStatus VALUES ('X', 'Extinct')
INSERT INTO LanguageStatus VALUES ('S', 'Second Language Only')

--==( LanguageType )==--

INSERT INTO LanguageType VALUES ('L', 'Language')
INSERT INTO LanguageType VALUES ('LA', 'Language Alternate')
INSERT INTO LanguageType VALUES ('D', 'Dialect')
INSERT INTO LanguageType VALUES ('DA', 'Dialect Alternate')
INSERT INTO LanguageType VALUES ('DP', 'Dialect Perjorative')
INSERT INTO LanguageType VALUES ('LP', 'Language Perjorative')

--==( Countries )==--

PRINT 'Countries'
INSERT INTO Country	SELECT c.Id, c.Name FROM CountryTemp c

GO

--==( LanguageName, Part I )==--

PRINT 'LanguageName Part I'
INSERT INTO LanguageName (Name, Derogatory)
	SELECT DISTINCT Name, 0 FROM LanguageIndexTemp --( Derogatory names changed below
	UNION
	SELECT DISTINCT Name, 0 FROM LanguageCodesTemp
	ORDER BY 1

GO

--==( Ethnologue )==--

PRINT 'Ethnologue'

--( Get new codes

INSERT INTO Ethnologue
SELECT
	iso.Part1,
	l.Code,
	COALESCE(iso.Part1, l.Code)
FROM LanguageCodesTemp l
LEFT OUTER JOIN Iso639Temp iso ON iso.Id = l.Code
LEFT OUTER JOIN Icu36Temp icu ON icu.Id = iso.Part1

--( ISO 639-1 and -2 codes were given for Standard Arabic (ara) but not for
--( Arabic (arb). In the meantime, Arabic (ara) got missed in LanguageCodes.
--( We didn't have ara before, so we're running with arb.

UPDATE Ethnologue SET Iso6391 = 'ar', Icu = 'ar' WHERE Iso6393 = 'arb';

--( Last I heard there is no good solution for Mandarin Chinese. See Jira issue
--( LT-8112 for details.

UPDATE Ethnologue SET Iso6391 = 'zh', Icu = 'zh' WHERE Iso6393 = 'cmn';

--( Also no good solution for Farsi. See LT-9820 for details.
UPDATE Ethnologue SET Iso6391 = 'fa', Icu = 'fa' WHERE Iso6393 = 'pes';

ALTER TABLE Ethnologue ADD Id SMALLINT IDENTITY (1, 1)
	CONSTRAINT pkEthnoId PRIMARY KEY CLUSTERED
GO

--==( LanguageLocation )==--

PRINT 'LanguageLocation'
INSERT INTO LanguageLocation (EthnologueId, CountryUsedInId, LanguageTypeId, LanguageId)
	SELECT e.Id, lt.CountryUsedInId, lt.LanguageTypeId, ln.Id -- should be no null ce.Id's. ln.Id's
	FROM LanguageIndexTemp lt
	LEFT OUTER JOIN Ethnologue e ON e.Iso6393 = lt.EthnologueCode
	LEFT OUTER JOIN LanguageName ln ON ln.Name = lt.Name

GO

--==( EthnologueLocation )==--

PRINT 'EthnologueLocation'
INSERT INTO EthnologueLocation (EthnologueId, MainCountryUsedId, LanguageStatusId, PrimaryNameId)
	SELECT e.Id, l.MainCountryUsedId, l.LanguageStatusId, ln.Id -- should be no null ec.Id's or ln.Id's
	FROM LanguageCodesTemp l
	LEFT OUTER JOIN Ethnologue e ON e.Iso6393 = l.Code
	LEFT OUTER JOIN LanguageName ln ON ln.Name = l.Name

--( LanguageName, Part II )==--

-- REVIEW (SteveMiller): The last set of names didn't have any double
-- quotes in it. This might be pulled out if this continues to be the case.

PRINT 'LanguageName Part II'
UPDATE LanguageName SET
	Name = REPLACE(Name, '"', ''),
	Derogatory = 1
WHERE CHARINDEX('"', "Name") != 0

GO

--( Temp Table Cleanup

IF 1 = 1 BEGIN  --( These are nice to keep during development
	DROP TABLE Iso639Temp
	DROP TABLE LanguageCodesTemp
	DROP TABLE Icu36Temp
	DROP TABLE LanguageIndexTemp
	DROP TABLE CountryTemp
END

GO

--( Reindex, now that we have data imported. 95% fill factors are used on tables not
--( likely to change.

DBCC DBREINDEX (LanguageType, '', 95)
DBCC DBREINDEX (WorldRegion, '', 95)
DBCC DBREINDEX (Country, '', 90)
DBCC DBREINDEX (LanguageName)
DBCC DBREINDEX (Ethnologue)
DBCC DBREINDEX (LanguageLocation)
DBCC DBREINDEX (EthnologueLocation)

GO

--== View Creation ==--

CREATE VIEW ViewEthnologueCodes AS
SELECT
	e.Id AS EthnologueID,
	e.Iso6393 AS EthnologueCode,
	e.Iso6391,
	e.Icu AS IcuCode,
	el.Id AS LocationId,
	el.MainCountryUsedId,
	c.Name AS MainCountryUsed,
	el.LanguageStatusId,
	ls.Type,
	el.PrimaryNameId,
	ln.Name AS PrimaryName
FROM Ethnologue e
LEFT OUTER JOIN EthnologueLocation el ON el.EthnologueId = e.Id
LEFT OUTER JOIN Country c ON c.Id = el.MainCountryUsedId
LEFT OUTER JOIN LanguageStatus ls ON ls.Id = el.LanguageStatusId
LEFT OUTER JOIN LanguageName ln on ln.Id = el.PrimaryNameId

GO

CREATE VIEW ViewLanguagesDialects AS
SELECT
	e.Id AS EthnologueID,
	e.Iso6393 AS EthnologueCode,
	ll.Id AS LanguageLocationId,
	ll.CountryUsedInId,
	c.Name AS CountryUsedIn,
	ll.LanguageTypeId,
	lt.Type,
	ll.LanguageId,
	ln.Name AS LanguageName,
	ln.Derogatory
FROM Ethnologue e
LEFT OUTER JOIN LanguageLocation ll ON ll.EthnologueId = e.Id
LEFT OUTER JOIN Country c ON c.Id = ll.CountryUsedInId
LEFT OUTER JOIN LanguageType lt ON lt.Id = ll.LanguageTypeId
LEFT OUTER JOIN LanguageName ln on ln.Id = ll.LanguageId

GO
