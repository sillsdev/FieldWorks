-- Update database from version 200231 to 200232
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- LT-8707: Delete orphan discourse items
-------------------------------------------------------------------------------
	DECLARE @NumObjs INT,
			@RowCount INT,
			@Objlist NVARCHAR(2000),
			@tblId INT

	select @RowCount = Count(*) from DsConstChart where BasedOn is null
	set @ObjList = '';
	set @NumObjs = 1
	select top 1 @tblId = id from DsConstChart where BasedOn is null order by id
	while @@FETCH_STATUS = 0 AND @NumObjs <= @RowCount begin
		SET @ObjList = @ObjList + CAST(@tblId as varchar(10))
		set @NumObjs = @NumObjs + 1
		if @NumObjs <= @RowCount set @ObjList = @ObjList + ', '
		select top 1 @tblId = id from DsConstChart where BasedOn is null and id > @TblId order by id
	end
	exec DeleteObjects @Objlist
GO
-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200231
BEGIN
	UPDATE Version$ SET DbVer = 200232
	COMMIT TRANSACTION
	PRINT 'database updated to version 200232'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200231 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
