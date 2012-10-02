-- Update database from version 200188 to 200189
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- FDB-178: Get annotations for a text
-------------------------------------------------------------------------------

IF OBJECT_ID('fnGetTextAnnotations') IS NOT NULL BEGIN
	PRINT 'removing function fnGetTextAnnotations'
	DROP FUNCTION fnGetTextAnnotations
END
GO
PRINT 'creating function fnGetTextAnnotations'
GO

CREATE FUNCTION dbo.fnGetTextAnnotations(
	@nvcTextName NVARCHAR(4000),
	@nVernacularWs INT = NULL,
	@nAnalysisWS INT = NULL)
RETURNS @tblTextAnnotations TABLE (
	TextId INT,
	TextName NVARCHAR(4000),
	Paragraph INT,
	StTxtParaId INT,
	BeginOffset INT,
	EndOffset INT,
	AnnotationId INT,
	WordFormId INT,
	Wordform NVARCHAR(4000),
	AnalysisId INT,
	GlossId INT,
	Gloss NVARCHAR(4000))
AS
BEGIN
	DECLARE @nAnnotationDefnPIC INT

	SELECT @nAnnotationDefnPIC = Obj
	FROM CmPossibility_Name
	WHERE Txt = 'Punctuation In Context'

	IF @nAnalysisWS IS NULL
		SELECT TOP 1 @nAnalysisWS = Dst
		FROM LanguageProject_CurrentAnalysisWritingSystems ORDER BY Ord
	IF @nVernacularWS IS NULL
		SELECT TOP 1 @nVernacularWS = dst
		FROM LanguageProject_CurrentVernacularWritingSystems ORDER BY Ord

	-- REVIEW (SteveMiller): Most of these queries (joined together by the
	-- UNIONs) can be optimized by dropping out some tables. Since this is
	-- utility function, and it moves pretty fast already, I didn't take
	-- the time to tweak it anymore.

	-- REVIEW (SteveMiller): Text segment queries are still not being
	-- picked up. If those are desired, another query will be needed,
	-- UNIONed with the rest of them.

	--== Annotation is not an InstanceOf anything ==--
	INSERT INTO @tblTextAnnotations
	SELECT
		cmon.Obj AS TextId,
		cmon.Txt AS TextName,
		tp.Ord AS Paragraph,
		stp.Id AS StTxtParaId,
		cba.BeginOffset,
		cba.EndOffset,
		cba.Id AS AnnotationId,
		NULL AS WordFormId,
		SUBSTRING(stp.Contents, cba.BeginOffset + 1, cba.EndOffset - cba.BeginOffset)
			COLLATE SQL_Latin1_General_CP1_CI_AS AS WordForm, --( avoids collate mismatch
		NULL AS AnalysisId,
		NULL AS GlossId,
		NULL AS Gloss
	FROM CmMajorObject_Name cmon
	JOIN Text_Contents tc ON tc.Src = cmon.Obj
	JOIN StText st ON st.Id = tc.Dst
	JOIN StText_Paragraphs tp ON tp.Src = st.Id
	JOIN StTxtPara stp ON stp.Id = tp.Dst
	JOIN CmBaseAnnotation cba ON cba.BeginObject = stp.Id
	JOIN CmAnnotation ca ON ca.Id = cba.Id
	WHERE ca.InstanceOf IS NULL
		AND cmon.Txt = @nvcTextName
		AND ca.AnnotationType = @nAnnotationDefnPIC
	--== Annotation is an InstanceOf Wordform ==--
	UNION
	SELECT
		cmon.Obj AS TextId,
		cmon.Txt AS TextName,
		tp.Ord AS Paragraph,
		stp.Id AS StTxtParaId,
		cba.BeginOffset,
		cba.EndOffset,
		cba.Id AS AnnotationId,
		wwff.Obj AS WordFormId,
		wwff.Txt AS WordForm,
		NULL AS AnalysisId,
		NULL AS GlossId,
		NULL AS Gloss
	FROM CmMajorObject_Name cmon
	JOIN Text_Contents tc ON tc.Src = cmon.Obj
	JOIN StText st ON st.Id = tc.Dst
	JOIN StText_Paragraphs tp ON tp.Src = st.Id
	JOIN StTxtPara stp ON stp.Id = tp.Dst
	JOIN CmBaseAnnotation cba ON cba.BeginObject = stp.Id
	JOIN CmAnnotation ca ON ca.Id = cba.Id
	JOIN WfiWordForm_Form wwff ON wwff.Obj = ca.InstanceOf AND wwff.WS = @nVernacularWS
	WHERE cmon.Txt = @nvcTextName
	--== Annotation is an InstanceOf Annotation ==--
	UNION
	SELECT
		cmon.Obj AS TextId,
		cmon.Txt AS TextName,
		tp.Ord AS Paragraph,
		stp.Id AS StTxtParaId,
		cba.BeginOffset,
		cba.EndOffset,
		cba.Id AS AnnotationId,
		wwff.Obj AS WordFormId,
		wwff.Txt AS WordForm,
		wa.Id AS AnalysisId,
		NULL AS GlossId,
		NULL AS Gloss
	FROM CmMajorObject_Name cmon
	JOIN Text_Contents tc ON tc.Src = cmon.Obj
	JOIN StText st ON st.Id = tc.Dst
	JOIN StText_Paragraphs tp ON tp.Src = st.Id
	JOIN StTxtPara stp ON stp.Id = tp.Dst
	JOIN CmBaseAnnotation cba ON cba.BeginObject = stp.Id
	JOIN CmAnnotation ca ON ca.Id = cba.Id
	JOIN WfiAnalysis wa ON wa.Id = ca.InstanceOf
	LEFT OUTER JOIN WfiWordForm_Analyses wwfa ON wwfa.Dst = wa.Id
	LEFT OUTER JOIN WfiWordForm_Form wwff ON wwff.Obj = wwfa.Src AND wwff.WS = @nVernacularWS
	WHERE cmon.Txt = @nvcTextName
	--== Annotation is an InstanceOf Gloss ==--
	UNION
	SELECT
		cmon.Obj AS TextId,
		cmon.Txt AS TextName,
		tp.Ord AS Paragraph,
		stp.Id AS StTxtParaId,
		cba.BeginOffset,
		cba.EndOffset,
		cba.Id AS AnnotationId,
		wwff.Obj AS WordFormId,
		wwff.Txt AS WordForm,
		wa.Id AS AnalysisId,
		wgf.Obj AS GlossId,
		wgf.Txt AS Gloss
	FROM CmMajorObject_Name cmon
	JOIN Text_Contents tc ON tc.Src = cmon.Obj
	JOIN StText st ON st.Id = tc.Dst
	JOIN StText_Paragraphs tp ON tp.Src = st.Id
	JOIN StTxtPara stp ON stp.Id = tp.Dst
	JOIN CmBaseAnnotation cba ON cba.BeginObject = stp.Id
	JOIN CmAnnotation ca ON ca.Id = cba.Id
	JOIN WfiGloss_Form wgf ON wgf.Obj = ca.InstanceOf AND wgf.WS = @nAnalysisWS
	LEFT OUTER JOIN WfiAnalysis_Meanings wam ON wam.Dst = wgf.Obj
	LEFT OUTER JOIN WfiAnalysis wa ON wa.Id = wam.Src
	LEFT OUTER JOIN WfiWordForm_Analyses wwfa ON wwfa.Dst = wa.Id
	LEFT OUTER JOIN WfiWordForm_Form wwff ON wwff.Obj = wwfa.Src AND wwff.WS = @nVernacularWS
	WHERE cmon.Txt = @nvcTextName
	ORDER BY tp.Ord, cba.BeginOffset

	RETURN
END
GO

-------------------------------------------------------------------------------
-- FDB-180: Create Index Defragmenter
-------------------------------------------------------------------------------

IF OBJECT_ID('DefragmentIndexes') IS NOT NULL BEGIN
	PRINT 'removing procedure DefragmentIndexes';
	DROP PROCEDURE DefragmentIndexes;
END
GO
PRINT 'creating procedure DefragmentIndexes';
GO
CREATE PROCEDURE DefragmentIndexes
AS

	DECLARE
		@TableName NVARCHAR (4000),
		@Sql NVARCHAR (255),
		@ObjectOwner VARCHAR(255),
		@IndexName CHAR(255),
		@DbName SYSNAME,
		@TableId INT,
		@vcTableId VARCHAR(255),
		@Debug BIT;

	--( Variables for filtering which indexes to rebuild. These may become
	--( parameters sometime.

	DECLARE
		@MinPages TINYINT,
		@MaxLogicalFrag TINYINT,
		@MinScanDensity TINYINT;

	--( From http://www.microsoft.com/technet/prodtechnol/sql/2000/maintain/ss2kidbp.mspx:
	--(	"Generally, you should not be concerned with fragmentation levels of indexes with
	--(	less than 1,000 pages". However, a "small-scale" environment in this article is
	--(	10-20 GB, which is about 100 times bigger than we expect our databases to get,
	--(	running on "two spindles", which is twice as many as we expect to get. Searching
	--( some more, a SQL Server MVP said, "The first 8 pages for an index comes out of
	--( mixed extents, so it is meaningless to talk about fragmentation in those cases [of
	--( 6 to 8 pages]. I suggest you start with indexes that has some 500 to 1000 pages."
	--( http://www.developerfood.com/cannot-defrag-index/microsoft-public-sqlserver-server/01f1eecc-3848-43ec-9006-0a99309a987a/article.aspx

	IF @MinPages IS NULL
		SET @MinPages = 125;

	--( From http://www.microsoft.com/technet/prodtechnol/sql/2000/maintain/ss2kidbp.mspx:
	--( "In the tests, workload performance increased after defragmenting when clustered
	--( indexes had logical fragmentation greater than 10 percent, and significant
	--( increases were attained when logical fragmentation levels were greater than 20
	--( percent. Consider defragmenting indexes with 20 percent or more logical
	--( fragmentation. Remember that this value is meaningless when reporting on a heap
	--( (Index ID = 0)"

	IF @MaxLogicalFrag IS NULL
		SET @MaxLogicalFrag = 10;

	--( I haven't seen a good number for this yet. It was a parameter in the
	--( procedure I used as a base for this one. There's a hint that it's not
	--( used as much with the coming of SQL Server 2005, but I haven't found
	--( any solid information on it.

	IF @MinScanDensity IS NULL
		SET @MinScanDensity = 80;

	SET @Debug = 0;

	--==( Stage 1: Check Fragmentation )==--

	DECLARE curTables CURSOR FOR
		SELECT CONVERT(VARCHAR, so.id)
		FROM sysobjects so
		JOIN sysindexes si ON so.id = si.id
		WHERE so.type = 'U' AND si.indid < 2 AND si.rows > 0;

	DECLARE @tblShowContig TABLE (
		ObjectName CHAR (255),
		ObjectId INT,
		IndexName CHAR (255),
		IndexId INT,
		Level INT,
		Pages INT,
		Rows INT,
		MinRecSize INT,
		MaxRecSize INT,
		AvgRecSize INT,
		ForwardedRecCount INT,
		Extents INT,
		ExtentSwitches INT,
		AvgFreeBytes INT,
		AvgPageDensity INT,
		ScanDensity DECIMAL,
		BestCount INT,
		ActualCount INT,
		LogicalFragmentation DECIMAL,
		ExtentFragmentation DECIMAL);

	--( Loop through the table list, executing DBCC SHOWCONTIG on each one.

	OPEN curTables;
	FETCH NEXT FROM curTables INTO @vcTableId;
	WHILE @@FETCH_STATUS = 0 BEGIN
		INSERT INTO @tblShowContig
			EXEC ('DBCC SHOWCONTIG (' + @vcTableId +
			') WITH TABLERESULTS, ALL_INDEXES, NO_INFOMSGS;');
		FETCH NEXT FROM curTables INTO @vcTableId;
	END
	CLOSE curTables;
	DEALLOCATE curTables;

	IF @Debug = 1
		SELECT * FROM @tblShowContig;

	--==( Stage 2: Defrag indexes )==--

	DECLARE curIndexes CURSOR FOR
		SELECT
			f.ObjectName,
			USER_NAME(so.uid) AS ObjectOwner,
			f.IndexName
		FROM @tblShowContig f
		JOIN sysobjects so ON f.ObjectId = so.id
		WHERE f.Pages >= @MinPages
			AND (ScanDensity <= @MinScanDensity
			OR LogicalFragmentation >= @MaxLogicalFrag)
		AND INDEXPROPERTY (ObjectId, IndexName, 'IndexDepth') > 0;

	IF @Debug = 1
		SELECT
			f.ObjectName,
			USER_NAME(so.uid) AS ObjectOwner,
			f.IndexName
		FROM @tblShowContig f
		JOIN sysobjects so ON f.ObjectId = so.id
		WHERE f.Pages >= @MinPages
			AND (ScanDensity <= @MinScanDensity
			OR LogicalFragmentation >= @MaxLogicalFrag)
		AND INDEXPROPERTY (ObjectId, IndexName, 'IndexDepth') > 0;

	--( Write to output start time for information purposes
	IF @Debug = 1
		PRINT 'Started defragmenting indexes at ' + CONVERT(VARCHAR,GETDATE());

	OPEN curIndexes;
	FETCH NEXT FROM curIndexes INTO @TableName, @ObjectOwner, @IndexName;
	WHILE @@FETCH_STATUS = 0
	BEGIN
		SET @Sql = N'ALTER INDEX ' + RTRIM(@IndexName) + N' ON ' +
			RTRIM(@ObjectOwner) + '.' + RTRIM(@TableName) + N' REBUILD;';
		IF @Debug = 1
			PRINT @Sql;

		EXEC (@Sql)

		FETCH NEXT FROM curIndexes INTO @TableName, @ObjectOwner, @IndexName;
	END

	-- Close and deallocate the cursor
	CLOSE curIndexes;
	DEALLOCATE curIndexes;

	-- Report on finish time for information purposes
	IF @Debug = 1
		PRINT 'Finished defragmenting indexes at ' + CONVERT(VARCHAR,GETDATE());
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200188
BEGIN
	UPDATE Version$ SET DbVer = 200189
	COMMIT TRANSACTION
	PRINT 'database updated to version 200189'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200188 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
