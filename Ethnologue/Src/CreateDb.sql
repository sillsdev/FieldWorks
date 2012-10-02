/*======================================================================
CreateDb.sql

When in development, it is helpful to have the @nDevelopment flag
turned on, in order to clean up dirty data we get from the Ethnologue
database. But we don't need the speed or the data integrity when in
production, because the db is used only during project creation or
updating.
======================================================================*/

USE Master

DECLARE @sysDbName SYSNAME
SELECT @sysDbName = Name FROM sysdatabases WHERE Name = 'Ethnologue'
IF @@ROWCOUNT = 1
	DROP DATABASE Ethnologue

CREATE DATABASE Ethnologue
GO

USE Ethnologue

--== Table Creation ==--

--( See note in header about @nDevelopment

DECLARE @nDevelopment BIT
SET @nDevelopment = 0

IF @nDevelopment = 1 BEGIN
	CREATE TABLE LanguageStatus (
		Id	NCHAR(1)		CONSTRAINT pkLanguageStatusId PRIMARY KEY CLUSTERED,
		Type	NVARCHAR(20)	CONSTRAINT ukLanguageStatusType UNIQUE)

	CREATE TABLE LanguageType (
		Id	NCHAR(2)		CONSTRAINT pkLanguageTypeId PRIMARY KEY CLUSTERED,
		Type 	NVARCHAR(20)	CONSTRAINT ukLanguageTypeType UNIQUE)

	CREATE TABLE WorldRegion (
		Id	TINYINT IDENTITY (1, 1)	CONSTRAINT pkWorldRegionId PRIMARY KEY CLUSTERED,
		Name	NVARCHAR(20)			CONSTRAINT ukWorldRegionName UNIQUE)

	CREATE TABLE Country (
		--( The ID name here is not an identity field!
		ID			NCHAR(2)		CONSTRAINT pkCountriesId PRIMARY KEY CLUSTERED,
		Name		NVARCHAR(100)	CONSTRAINT ukCountriesName UNIQUE)

	CREATE TABLE LanguageName (
		Id		INT IDENTITY (1, 1)	CONSTRAINT pkLangNameId PRIMARY KEY CLUSTERED,
		Name		NVARCHAR(75) 		, --CONSTRAINT ukLangNameName UNIQUE,
		Derogatory	BIT)

	CREATE INDEX ndxLangNameNameDerogatory ON LanguageName (Name, Derogatory)

	CREATE TABLE Ethnologue (
		Iso6391	CHAR(2),
		Iso6393 CHAR(3) CONSTRAINT nnEthnoCodeEthno NOT NULL,
		Icu		CHAR(4))

	CREATE INDEX ndxEthnoCode ON Ethnologue (Code)
	--( The ISO codes won't be actively used since the ICU column is added
	--( CREATE INDEX ndxEthnoIso6391 ON Ethnologue (Iso6391)
	--( CREATE INDEX ndxEtnhoIso6392 ON Ethnologue (Iso6392)
	CREATE INDEX ndxEtnhoIcu ON Ethnologue (Icu)

	CREATE TABLE LanguageLocation (
		Id			INT IDENTITY (1, 1)	CONSTRAINT pkLangLocId PRIMARY KEY CLUSTERED,
		EthnologueId	SMALLINT,			--( Will add the fk after the ID is created.
		CountryUsedInId	NCHAR(2)			CONSTRAINT fkLanguageLocCountry REFERENCES Country(Id),
		LanguageTypeId	NCHAR(2)			CONSTRAINT fkLanguageLocLangType REFERENCES LanguageType(Id),
		LanguageId		INT					CONSTRAINT fkLanguageLocLangId REFERENCES LanguageName(Id))

	CREATE INDEX ndxLanguageLocEthno ON LanguageLocation (EthnologueId)
	CREATE INDEX ndxLanguageLocCountry ON LanguageLocation (CountryUsedInId)
	CREATE INDEX ndxLanguageLocLangTypeId ON LanguageLocation (LanguageTypeId)
	CREATE INDEX ndxLanguageLocLangId ON LanguageLocation (LanguageId)

	CREATE TABLE EthnologueLocation (
		Id				SMALLINT IDENTITY (1, 1)
										CONSTRAINT pkEthnoLocId PRIMARY KEY CLUSTERED,
		EthnologueId		SMALLINT,	--( Will add the fk after the ID is created.
		MainCountryUsedId	NCHAR(2)	CONSTRAINT fkEthnoLocMainCountry REFERENCES Country(Id),
		LanguageStatusId 	NCHAR(1) 	CONSTRAINT fkEthnoLocLangStat REFERENCES LanguageStatus(Id),
		PrimaryNameId		INT			CONSTRAINT fkEthnoLocPrimaryNameId REFERENCES LanguageName(Id),
			CONSTRAINT ukEthnoLoc234 UNIQUE (EthnologueId, MainCountryUsedId, LanguageStatusId))

	CREATE INDEX ndxEthnoLocEthno ON EthnologueLocation (EthnologueId)
	CREATE INDEX ndxEthnoLocMainCountry ON EthnologueLocation (MainCountryUsedId)
	CREATE INDEX ndxEthnoLocLangStat ON EthnologueLocation (LanguageStatusId)
	CREATE INDEX ndxEthnoLocPrimaryName ON EthnologueLocation (PrimaryNameId)

END

ELSE BEGIN --( IF @nDevelopment != 1
	CREATE TABLE LanguageStatus (Id NCHAR(1), Type NVARCHAR(20))
	CREATE TABLE LanguageType (Id	NCHAR(2), Type NVARCHAR(20))
	CREATE TABLE WorldRegion (Id TINYINT IDENTITY (1, 1), Name NVARCHAR(20))
	CREATE TABLE Country (ID NCHAR(2), Name NVARCHAR(100))

	CREATE TABLE LanguageName (
		Id INT IDENTITY (1, 1),
		Name NVARCHAR(75),
		Derogatory	BIT)

	CREATE TABLE Ethnologue (
		Iso6391	CHAR(2),
		Iso6393 CHAR(3),
		Icu		CHAR(4))

	CREATE TABLE LanguageLocation (
		Id			INT IDENTITY (1, 1),
		EthnologueId	SMALLINT,
		CountryUsedInId	NCHAR(2),
		LanguageTypeId	NCHAR(2),
		LanguageId		INT)

	CREATE TABLE EthnologueLocation (
		Id				SMALLINT IDENTITY (1, 1),
		EthnologueId		SMALLINT,
		MainCountryUsedId	NCHAR(2),
		LanguageStatusId 	NCHAR(1),
		PrimaryNameId		INT)
END

GO

--== Tables for Bulk Import ==--

CREATE TABLE Iso639Temp (
	Id CHAR(3), --( Part 3
	Part2B CHAR(3),
	Part2T CHAR(3),
	Part1 CHAR(3),
	Scope CHAR(1),
	Language_Type CHAR(1),
	Ref_Name NVARCHAR(120),
	Comment NVARCHAR(120)); --( Doesn't have anything in it at present.

CREATE TABLE LanguageCodesTemp (
	Code CHAR(3),
	MainCountryUsedId CHAR(2),
	LanguageStatusId CHAR(1),
	Name NVARCHAR(120));

CREATE TABLE Icu36Temp (
	Id CHAR(4), --( 4 is for root
	Name NVARCHAR(120));

CREATE TABLE LanguageIndexTemp (
	EthnologueCode	NCHAR(3),
	CountryUsedInId	NCHAR(2),
	LanguageTypeId	NCHAR(2),
	Name			NVARCHAR(75));

CREATE TABLE CountryTemp (
	ID NCHAR(2),
	Name NVARCHAR(100),
	Worldregion NVARCHAR(20))

GO

--( The BULK INSERT commands are fired in CreateEthnologue.bat This could have
--( been done with a script, but we would have lost the the path parameter given
--( to the .bat file.