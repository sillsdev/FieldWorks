-- Update database from version 200209 to 200210
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- FWC-33: Shorten WfiWordForm_Form.Txt and MoForm_Form.Txt to 300 bytes
-------------------------------------------------------------------------------

IF OBJECT_ID('ShortenTxtFields') IS NOT NULL BEGIN
	PRINT N'removing procedure ShortenTxtFields';
	DROP PROCEDURE ShortenTxtFields;
END
GO
PRINT N'creating procedure ShortenTxtFields';
GO

CREATE PROCEDURE ShortenTxtFields
	@TableName NVARCHAR(100),
	@ColumnName NVARCHAR(100)
AS
	--==( Setup )==--

	DECLARE
		@Debug BIT,
		@Text NVARCHAR(4000),
		@Sql NVARCHAR(4000),
		@Sql2 NVARCHAR(4000),
		@Parms NVARCHAR(500),
		@ShortStr NVARCHAR(300),
		@IndexName NVARCHAR(100),
		@OverflwTable NVARCHAR(100),
		@RowCount INT,
		@ObjectID INT,
		@Ws INT,
		@RowID INT,
		@LastRowID INT,
		@Counter INT,		-- The number or records in the table
		@Counter2 INT,		-- Characters within the txt field
		@TempLen INT,		-- Characters within the txt field
		@LenRemain INT		-- Length remaining in txt

	SET @Debug = 0

	SET @IndexName = N'IND_' + @TableName + N'_' + @ColumnName
--Drop the index if it exists
	IF EXISTS(SELECT * FROM sys.indexes WHERE NAME = @IndexName) BEGIN
		IF @Debug = 1
			Print N'Entering Drop Index: '
		SET @Sql2 = N'DROP INDEX [' + @IndexName + N'] ON [dbo].[' + @TableName +
				N'] WITH ( ONLINE = OFF );'
		IF @Debug = 1
			Print N'Sql2 Drop Index: ' + @Sql2
		EXEC sp_executesql @Sql2
	END

	--Get the longest length of the text field to see if there are any rows longer than 300
	SET @Sql = N'SELECT @Counter = MAX(LEN(' + @ColumnName + N')) FROM ' + @TableName + N';'
	SET @Parms = N'@Counter INT OUTPUT'
	IF @Debug = 1
		Print N'Sql to get length of longest row: ' + @Sql
	EXEC sp_executesql @Sql, @Parms, @Counter = @Counter OUTPUT
	IF @Debug = 1
		Print N'Length of longest row is: ' + + CAST(@Counter AS VARCHAR(10))

	IF @Counter > 300 BEGIN
	-- Create a table to put the long text in
		SET @OverFlwTable = @TableName + N'_' + @ColumnName + N'OverFlow '
		IF @Debug = 1
			Print N'Entering Create Table for long txt fields @OverFlwTable: ' + @OverFlwTable
		SET @Sql = N'CREATE TABLE [' + @OverFlwTable +
			N'] (Obj INT, Ws INT, Seq INT, Txt NVARCHAR(300))'
		IF @Debug = 1
			Print N'Sql Create Table: ' + @Sql
		EXEC sp_executesql @Sql
		--Use a Loop with this dynamic SQL instead of a cursor

		SET @RowCount = 0
		SET @Sql = N'SELECT @RowCount = COUNT(*) FROM ' + @TableName + N';'

		SET @Parms = N'@RowCount INT OUTPUT'
		EXEC sp_executesql @Sql, @Parms, @RowCount = @RowCount OUTPUT

		SET @Counter = 0
		SET @ROWID =1
		SET @LastRowID =1

		WHILE @RowCount > @Counter BEGIN
			IF @Debug = 1
				PRINT N'RowCount: ' + CAST(@RowCount AS VARCHAR(10)) + N' Counter: ' + CAST(@Counter AS VARCHAR(10))
			SET @Sql = N'SELECT TOP 1 @RowID = Obj, @Ws = Ws, @Text = ' + @ColumnName + N' FROM ' + @TableName +
						N' WHERE Obj > @LastRowID;'
			SET @Parms = N'@RowID INT OUTPUT, @Ws INT OUTPUT, @Text NVARCHAR(4000) OUTPUT, @LastRowID INT'
			IF @Debug = 1
				PRINT N'Sql: ' + @Sql
			EXEC sp_executesql @Sql, @Parms, @RowID = @RowID OUTPUT, @Ws = @Ws OUTPUT, @Text = @Text OUTPUT, @LastRowID = @LastRowID
			IF @Debug = 1
				PRINT N'Text: ' + @Text + N' ROWID: ' + CAST(@RowID AS VARCHAR(10)) + N' Ws: ' + CAST(@Ws AS VARCHAR(10))

			SET @LastRowID = @RowID
			SET @Counter = @Counter + 1
			SET @LenRemain = LEN(@Text)
			SET @Counter2 = 1		-- Count of Generated Records
			WHILE @LenRemain > 300 BEGIN
				IF @Debug = 1
					PRINT N'Counter2: ' + CAST(@Counter2 AS NVARCHAR(10))

				IF @LenRemain > 300
					SET @TempLen = 300
				ELSE
					SET @TempLen = @LenRemain
				IF @Debug = 1
					PRINT N'TempLen: ' + CAST(@TempLen AS NVARCHAR(10))
				SET @ShortStr = SUBSTRING(@Text, @Counter2*300+1, @TempLen)
				IF @Debug = 1
					PRINT N'ShortString: ' + @ShortStr + N' Length: ' + CAST (@TempLen AS VARCHAR(10))
				SET @Sql2 = N'INSERT INTO ' + @OverFlwTable + N' (Obj, Ws, Seq, Txt) VALUES(' + CAST(@RowID AS NVARCHAR(8)) +
						N', ' + CAST(@Ws AS NVARCHAR(8)) + N', ' + CAST(@Counter2 AS CHAR(2)) + N', ''' + @Shortstr + N''');'
				IF @Debug = 1
					PRINT N'Sql to insert new row in overflow table ' + @OverflwTable
				EXEC sp_executesql @Sql2
				SET @Counter2 = @Counter2 + 1
				IF @LenRemain > 300
					SET @LenRemain = @LenRemain - 300
				IF @Debug = 1
					PRINT N'LenRemain: ' + CAST(@LenRemain AS NVARCHAR(10))
				SET @ShortStr = ' '
			END  --End While
			-- Truncate the data in the txt column to 300 characters
			IF LEN(@Text) > 300 BEGIN
				SET @ShortStr = SUBSTRING(@Text, 1, 300)
				IF @Debug = 1
					PRINT N'ShortString: ' + @ShortStr
				SET @Sql2 = N'UPDATE ' + @TableName + N' SET  ' + @ColumnName +
					N' = ''' + @Shortstr + N''' WHERE Obj = ' + CAST(@RowID AS VARCHAR(8))+ N';'
				IF @Debug = 1
					PRINT N'Sql2 to update new column value ' + @Sql2
				EXEC sp_executesql @Sql2
				SET @ShortStr = ' '
			END
		END  --End While
	END
	SET @Sql2 = N'ALTER TABLE [dbo].' + @TableName + N' ALTER COLUMN ' + @ColumnName + N' NVARCHAR(300);'
	IF @Debug = 1
		Print N'Sql2 Alter Column: ' + @Sql2
	EXEC sp_executesql @Sql2

	IF OBJECT_ID(@IndexName) IS NULL BEGIN
		SET @Sql2 = N'CREATE NONCLUSTERED INDEX [' + @IndexName + N'] ON [dbo].[' +
			@TableName + N'] ([' + @ColumnName + N'] ASC) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, ' +
			N'SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ' +
			N'ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY];'
		IF @Debug = 1
			Print N'Sql2 Create Index: ' + @Sql2
		EXEC sp_executesql @Sql2
	END
GO

/******************************************************************************
* Calling ShortenTxtFields
*
* Parameters:
*	@TableName
*	@ColumnName
*
* 	This routine shortens the specified column to NVARCHAR(300)
*	If the contained data is conger than 300, additional column are created to conta?n the data.
*	For example, if the column being shortened is named 'Txt' and is 650 chracters long:
*	Column Txt will contain characters 1-300
*	Column Txt1 will contain characters 301-600
*	Column Txt2 will contain characters 601-650
********************************************************************************/

	EXEC ShortenTxtFields N'MoForm_Form', N'Txt'
	EXEC ShortenTxtFields N'WfiWordForm_Form', N'Txt'

IF OBJECT_ID('ShortenTxtFields') IS NOT NULL BEGIN
	PRINT N'removing procedure ShortenTxtFields';
	DROP PROCEDURE ShortenTxtFields;
END
GO

-------------------------------------------------------------------------------
-- FDB-108: Remove unused Stored Procedures
-------------------------------------------------------------------------------

IF OBJECT_ID('GetIncomingRefsPrepDel$') IS NOT NULL BEGIN
	PRINT N'removing procedure GetIncomingRefsPrepDel$';
	DROP PROCEDURE GetIncomingRefsPrepDel$;
END
GO
IF OBJECT_ID('GetIncomingRefs$') IS NOT NULL BEGIN
	PRINT N'removing procedure GetIncomingRefs$';
	DROP PROCEDURE GetIncomingRefs$;
END
GO
IF OBJECT_ID('DeletePrepDelObjects$') IS NOT NULL BEGIN
	PRINT N'removing procedure DeletePrepDelObjects$';
	DROP PROCEDURE DeletePrepDelObjects$;
END
GO
IF OBJECT_ID('fnGetObjInOwnershipPathWithId$') IS NOT NULL BEGIN
	PRINT N'removing function fnGetObjInOwnershipPathWithId$';
	DROP FUNCTION fnGetObjInOwnershipPathWithId$;
END
GO
IF OBJECT_ID('fnGetSubObjects$') IS NOT NULL BEGIN
	PRINT N'removing function fnGetSubObjects$';
	DROP FUNCTION fnGetSubObjects$;
END
GO
IF OBJECT_ID('LockDetails') IS NOT NULL BEGIN
	PRINT N'removing procedure LockDetails';
	DROP PROCEDURE LockDetails;
END
GO
IF OBJECT_ID('GetStTexts$') IS NOT NULL BEGIN
	PRINT N'removing procedure GetStTexts$';
	DROP PROCEDURE GetStTexts$;
END
GO
IF OBJECT_ID('GetRunProp') IS NOT NULL BEGIN
	PRINT N'removing function GetRunProp';
	DROP FUNCTION GetRunProp;
END
GO
IF OBJECT_ID('fnIsMatch$') IS NOT NULL BEGIN
	PRINT N'removing function fnIsMatch$';
	DROP FUNCTION fnIsMatch$;
END
GO
IF OBJECT_ID('DeleteModelClass') IS NOT NULL BEGIN
	PRINT N'removing procedure DeleteModelClass';
	DROP PROCEDURE DeleteModelClass;
END
GO
IF OBJECT_ID('IsA') IS NOT NULL BEGIN
	PRINT N'removing function IsA';
	DROP FUNCTION IsA;
END
GO

---------------------------------------------------------------------------------
---- Finish or roll back transaction as applicable
---------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200209
BEGIN
	UPDATE Version$ SET DbVer = 200210
	COMMIT TRANSACTION
	PRINT 'database updated to version 200210'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200209 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
