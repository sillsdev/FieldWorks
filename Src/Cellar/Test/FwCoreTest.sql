/*******************************************************************************
 *	Procedure: CreateFlidCollations
 *
 *	Dependencies:
 *		Tables:
 *			FlidCollation$
 *  		Field$
 *****************************************************************************/

DECLARE
	@nUnusedFlid INT,
	@nCurrentWritingSysCount INT,
	@nFlidCollationsMade INT

PRINT '------------------------------'
PRINT 'Procedure CreateFlidCollations'

PRINT '    Using new writing system'
PRINT '        TODO'

PRINT '    Using new collation'
PRINT '        TODO'

--==( New Flid )==--

PRINT '    Using new flid (a new record added to CmSortSpec)'

--( get Flid from Field$ not used in FlidCollation$
SELECT TOP 1 @nUnusedFlid = f.[ID]
FROM Field$ f
LEFT OUTER JOIN FlidCollation$ fc ON fc.[Flid] = f.[Id]
WHERE fc.[Id] IS NULL

IF @@ROWCOUNT < 1
	PRINT '        BAD TEST CODE'

--( get count of current collations
SELECT @nCurrentWritingSysCount = COUNT(ews.[Src])
FROM (
	SELECT [Dst] AS [Ws] FROM LangProject_CurAnalysisWss
	UNION
	SELECT [Dst] AS [Ws] FROM LangProject_CurVernWss) e
JOIN LgWritingSystem_OldWritingSystems ews ON ews.[Src] = e.[Ws]
JOIN LgOldWritingSystem_Collations wsc ON wsc.[Src] = ews.[Dst]

IF @@ROWCOUNT < 1
	PRINT '        BAD TEST CODE'

--( execute the proc
EXEC CreateFlidCollations NULL, NULL, @nUnusedFlid

--( see how many FlidCollation$ records got created by the proc
SELECT @nFlidCollationsMade = COUNT([CollationID])
FROM FlidCollation$
WHERE [Flid] = @nUnusedFlid

IF @nFlidCollationsMade = @nCurrentWritingSysCount
	PRINT '        good'
ELSE
	PRINT '        BAD'

--( wipe out test records
DELETE FROM FlidCollation$ WHERE [Flid] = @nUnusedFlid

PRINT ' '

GO

/***********************************************************************************************
 *	Function: fnGetLastCommaDelimID
 *
 *	Dependencies:
 *		None
 **********************************************************************************************/

PRINT '--------------------'
PRINT 'Function fnGetLastCommaDelimID'
DECLARE @nMenuObjectID SMALLINT
DECLARE @nvcCmSortSpecField NVARCHAR(4000)

SET @nvcCmSortSpecField = NULL
SET @nMenuObjectID = dbo.fnGetLastCommaDelimID(@nvcCmSortSpecField)
IF @nMenuObjectID IS NULL
	PRINT '    good'
ELSE
	PRINT '    BAD'

SET @nvcCmSortSpecField = '1111'
SET @nMenuObjectID = dbo.fnGetLastCommaDelimID(@nvcCmSortSpecField)
IF @nMenuObjectID = 1111
	PRINT '    good'
ELSE
	PRINT '    BAD'

SET @nvcCmSortSpecField = '1111,2222'
SET @nMenuObjectID = dbo.fnGetLastCommaDelimID(@nvcCmSortSpecField)
IF @nMenuObjectID = 2222
	PRINT '    good'
ELSE
	PRINT '    BAD'

SET @nvcCmSortSpecField = '1111,2222,3333'
SET @nMenuObjectID = dbo.fnGetLastCommaDelimID(@nvcCmSortSpecField)
IF @nMenuObjectID = 3333
	PRINT '    good'
ELSE
	PRINT '    BAD'

PRINT ' '

GO