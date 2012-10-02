/***********************************************************************************************
 * MergeWritingSystem
 *
 * Description:
 *	Updates an old Writings System (ws) value with a new ws value.
 *
 * Parameters:
 *	@nOldWs = ID of the old writing system (ws)
 *	@nNewWs = ID of the new ws
 *
 * Returns:
 *	none
 **********************************************************************************************/

if object_id('MergeWritingSystem') is not null begin
	print 'removing proc MergeWritingSystem'
	drop proc [MergeWritingSystem]
end
go
print 'creating proc MergeWritingSystem'
go

CREATE PROCEDURE MergeWritingSystem
	@nOldWs INT,
	@nNewWs INT
AS
	DECLARE
		@nRowCount INT,
		@nFlid INT,
		@nvcTableName NVARCHAR(60),
		@nvcSql NVARCHAR(200)

	SET @nRowCount = 1
	SELECT TOP 1 @nFlid = [Id] FROM Field$ WHERE Type = 16 ORDER BY [Id]
	WHILE @nRowCount > 0 BEGIN
		EXEC GetMultiTableName @nFlid, @nvcTableName OUTPUT

		SET @nvcSql = N'UPDATE ' + @nvcTableName + CHAR(13) +
			N'SET Ws = @nNewWs WHERE Ws = @nOldWs'
		EXECUTE sp_executesql @nvcSql,
			N'@nNewWs INT, @nOldWs INT', @nNewWs, @nOldWs

		SELECT TOP 1 @nFlid = [Id]
		FROM Field$
		WHERE Type = 16 AND [Id] > @nFlid
		ORDER BY [Id]

		SET @nRowCount = @@ROWCOUNT
	END

	UPDATE MultiBigTxt$ SET Ws = @nNewWs WHERE Ws = @nOldWs
	UPDATE MultiStr$ SET Ws = @nNewWs WHERE Ws = @nOldWs
	UPDATE MultiBigStr$ SET Ws = @nNewWs WHERE Ws = @nOldWs
GO
