/***********************************************************************************************
* Procedure: DefragmentIndexes
*
* Description:
*	Defragments indexes. Database indexes fragment over time. The more fragmented data
*	and indexes are, the slower the app will be.
*
* Parameters:
*	@MinScanDensity: Minimum allowable scan density. The scan density is "the ratio of
*		best count to actual count. This value is 100 if everything is contiguous; if this
*		value is less than 100, some fragmentation exists. Best count is the ideal number
*		of extent changes if everything is contiguously linked. Actual Count is the actual
*		number of extent changes."
*
* Returns:
*	Nothing
*
* Calling sample:
*	EXECUTE DefragmentIndexes
*
* Notes:
*	The Microsoft white paper on deframentation best practices is at:
*	http://www.microsoft.com/technet/prodtechnol/sql/2000/maintain/ss2kidbp.mspx
*
*	This procedure was adapted from an article and code by T.Pullen:
*	http://www.sql-server-performance.com/articles/per/automatic_reindexing_sql2000_p2.aspx
*	This article was based on Microsoft's documentation.
*
*	According to Books On Line, under the topic "DBCC SHOWCONTIG statement", "the algorithm
*	for caluclating fragmentation is more precise in SQL Server 2005 than in SQL Server 2000.
*	As a result, fragmentation values will appear higher."
**********************************************************************************************/

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