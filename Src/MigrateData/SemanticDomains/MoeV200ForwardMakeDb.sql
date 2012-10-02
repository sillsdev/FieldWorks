/******************************************************************************
** MoeV200Forward
**
** This script finds the data for Ron Moe's Semantic Domains from
** TestLangProj, where the most recent lists are maintained. Then the script
** puts the data into a plain, vanilla database for shipment to users.
*******************************************************************************/

--==( Create the new database, with the appropriate schema )==--

DROP DATABASE MoeV200Forward

CREATE DATABASE MoeV200Forward ON (
	NAME = MoeV200Forawrd,
	FILENAME = 'C:\fw\DistFiles\DataMigration\MoeV200Forward.mdf',
	SIZE = 1)

CREATE TABLE MoeV200Forward..CmObject (
	Id int,
	Guid$ uniqueidentifier,
	Class$ int,
	Owner$ int,
	OwnFlid$ int,
	OwnOrd$ int,
	UpdStmp timestamp,
	UpdDttm smalldatetime)

CREATE TABLE MoeV200Forward..CmPossibility (
	id int,
	SortSpec int,
	Confidence int,
	Status int,
	DateCreated datetime,
	DateModified datetime,
	HelpId nvarchar (4000),
	ForeColor int,
	BackColor int,
	UnderColor int,
	UnderStyle tinyint,
	Hidden bit,
	IsProtected bit)

CREATE TABLE MoeV200Forward..CmPossibility_Abbreviation (
	Obj int,
	Ws int,
	Txt nvarchar (4000) COLLATE Latin1_General_BIN)

CREATE TABLE MoeV200Forward..CmPossibility_Name (
	Obj int,
	Ws int,
	Txt nvarchar (4000) COLLATE Latin1_General_BIN)

CREATE TABLE MoeV200Forward..CmPossibility_Researchers (
	Src int,
	Dst int)

CREATE TABLE MoeV200Forward..CmPossibility_Restrictions (
	Src int,
	Dst int)

CREATE TABLE MoeV200Forward..CmSemanticDomain (
	id int,
	LouwNidaCodes nvarchar (4000),
	OcmCodes nvarchar (4000))

CREATE TABLE MoeV200Forward..CmSemanticDomain_OcmRefs (
	Src int,
	Dst int)

CREATE TABLE MoeV200Forward..CmSemanticDomain_RelatedDomains (
	Src int,
	Dst int)

CREATE TABLE MoeV200Forward..CmDomainQuestion (
	id int)

CREATE TABLE MoeV200Forward..CmDomainQuestion_ExampleWords (
	Obj int,
	Ws int,
	Txt nvarchar (4000) COLLATE Latin1_General_BIN)

CREATE TABLE MoeV200Forward..CmDomainQuestion_Question (
	Obj int,
	Ws int,
	Txt nvarchar (4000) COLLATE Latin1_General_BIN)

CREATE TABLE MoeV200Forward..MultiBigStr$ (
	Flid int,
	Obj int,
	Ws int,
	Txt ntext,
	Fmt image)

CREATE TABLE MoeV200Forward..MultiStr$ (
	Flid int,
	Obj int,
	Ws int,
	Txt nvarchar (4000),
	Fmt varbinary (8000))

--==( Copy Data )==--

--( All we care about are the CmSemanticDomain records,
--( and the CmDomainQuestion they own. That makes the problem
--( of recursion go away.

-- CmSemanticDomain --

INSERT INTO MoeV200Forward..CmObject
SELECT
	osd.[Id],
	Guid$,
	Class$,
	Owner$,
	OwnFlid$,
	OwnOrd$,
	NULL, --( "Can't insert a non-null value into a timestamp column"
	UpdDttm
FROM TestLangProj..CmObject osd
JOIN TestLangProj..CmSemanticDomain sd ON sd.[Id] = osd.[Id]

INSERT INTO MoeV200Forward..CmPossibility
SELECT p.*
FROM TestLangProj..CmPossibility p
JOIN TestLangProj..CmSemanticDomain sd ON sd.[Id] = p.[Id]

INSERT INTO MoeV200Forward..CmSemanticDomain
SELECT * FROM TestLangProj..CmSemanticDomain

INSERT INTO MoeV200Forward..CmPossibility_Abbreviation
SELECT p.*
FROM TestLangProj..CmPossibility_Abbreviation p
JOIN TestLangProj..CmSemanticDomain sd ON sd.[Id] = p.Obj

INSERT INTO MoeV200Forward..CmPossibility_Name
SELECT p.*
FROM TestLangProj..CmPossibility_Name p
JOIN TestLangProj..CmSemanticDomain sd ON sd.[Id] = p.Obj

INSERT INTO MoeV200Forward..CmPossibility_Researchers
SELECT p.*
FROM TestLangProj..CmPossibility_Researchers p
JOIN TestLangProj..CmSemanticDomain sd ON sd.[Id] = p.Src

INSERT INTO MoeV200Forward..CmPossibility_Restrictions
SELECT p.*
FROM TestLangProj..CmPossibility_Restrictions p
JOIN TestLangProj..CmSemanticDomain sd ON sd.[Id] = p.Src

INSERT INTO MoeV200Forward..CmSemanticDomain
SELECT sd.*
FROM TestLangProj..CmSemanticDomain sd

INSERT INTO MoeV200Forward..CmSemanticDomain_OcmRefs
SELECT s.*
FROM TestLangProj..CmSemanticDomain_OcmRefs s
JOIN TestLangProj..CmSemanticDomain sd ON sd.[Id] = s.Src

INSERT INTO MoeV200Forward..CmSemanticDomain_RelatedDomains
SELECT s.*
FROM TestLangProj..CmSemanticDomain_RelatedDomains s
JOIN TestLangProj..CmSemanticDomain sd ON sd.[Id] = s.Src

INSERT INTO MoeV200Forward..MultiBigStr$
SELECT s.*
FROM TestLangProj..MultiBigStr$ s
JOIN TestLangProj..CmSemanticDomain sd ON sd.[Id] = s.Obj

INSERT INTO MoeV200Forward..MultiStr$
SELECT s.*
FROM TestLangProj..MultiStr$ s
JOIN TestLangProj..CmSemanticDomain sd ON sd.[Id] = s.Obj

-- CmDomainQuestion --

INSERT INTO MoeV200Forward..CmObject
SELECT
	o.[Id],
	Guid$,
	Class$,
	Owner$,
	OwnFlid$,
	OwnOrd$,
	NULL, --( "Can't insert a non-null value into a timestamp column"
	UpdDttm
FROM TestLangProj..CmObject o
JOIN TestLangProj..CmDomainQuestion q ON q.[Id] = o.[Id]

INSERT INTO MoeV200Forward..CmDomainQuestion
SELECT q.*
FROM TestLangProj..CmDomainQuestion q

INSERT INTO MoeV200Forward..CmDomainQuestion_ExampleWords
SELECT qew.*
FROM TestLangProj..CmDomainQuestion_ExampleWords qew

INSERT INTO MoeV200Forward..CmDomainQuestion_Question
SELECT qq.*
FROM TestLangProj..CmDomainQuestion_Question qq

INSERT INTO MoeV200Forward..MultiBigStr$
SELECT s.*
FROM TestLangProj..MultiBigStr$ s
JOIN TestLangProj..CmDomainQuestion q ON q.[Id] = s.Obj

INSERT INTO MoeV200Forward..MultiStr$
SELECT s.*
FROM TestLangProj..MultiStr$ s
JOIN TestLangProj..CmDomainQuestion q ON q.[Id] = s.Obj

EXEC sp_detach_db 'MoeV200Forward', 'true'