/***********************************************************************************************
 *	The procedures in this file are for testing stored procedures that require a second
 *	connection to the database.
 **********************************************************************************************/

/***********************************************************************************************
 *	Suite: Sync$Table
 *
 *	Dependencies:
 *		None
 **********************************************************************************************/

IF OBJECT_ID('ut_Sync$Table_ClearSyncTable2$') IS NOT NULL BEGIN
	DROP PROCEDURE [ut_Sync$Table_ClearSyncTable2$]
END
GO
CREATE PROCEDURE ut_Sync$Table_ClearSyncTable2$ AS
BEGIN
		DECLARE @SyncCount int, @SyncCountNew int, @errMessage NVARCHAR(200)

		-- This test requires an additional connection to the DB. We want to make sure no
		-- records are deleted in this case.
		if (select count(distinct spid) from master.dbo.sysprocesses sproc
			join master.dbo.sysdatabases sdb on sdb.dbid = sproc.dbid and name = 'TestLangProj'
			where sproc.spid != @@spid) = 0 BEGIN
			EXEC tsu_failure 'Second test connection not established'
			return 1
		END

		-- If there are any existing records sitting in the table, we need to take them into account
		select @SyncCount = (select count(*) from sync$)

		-- Add a couple new records to the sync$ table so we can see if they get smoked
		insert sync$ (LpInfoId, Msg, ObjId, ObjFlid)
		values (newid(), 1, 99, 999)
		insert sync$ (LpInfoId, Msg, ObjId, ObjFlid)
		values (newid(), 2, 199, 1999)
		Set @SyncCount = @SyncCount + 2
		if (select count(*) from sync$) != @SyncCount
			EXEC tsu_failure 'Failed to insert new records into sync$ table'

		EXEC ClearSyncTable$ 'TestLangProj'

		select @SyncCountNew = (select count(*) from sync$)
		if @SyncCountNew != @SyncCount BEGIN
			SET @errMessage = 'Expected ' + CAST(@SyncCountNew AS varchar(15)) +
				' records in sync$ table, but found ' + CAST(@SyncCount AS varchar(15))
			EXEC tsu_failure @errMessage
		END
END
GO

---------------------------------------------------------------------
IF OBJECT_ID('ut_Sync$Table_StoreSyncRec2$') IS NOT NULL BEGIN
	DROP PROCEDURE [ut_Sync$Table_StoreSyncRec2$]
END
GO
CREATE PROCEDURE ut_Sync$Table_StoreSyncRec2$ AS
BEGIN
		DECLARE @SyncCount int, @SyncCountNew int, @uidNew uniqueidentifier

		-- If there are any existing records sitting in the table, we need to take them into account
		select @SyncCount = (select count(*) from sync$)

		-- This test requires a second connection to the DB because we want records to be added to the
		-- Sync$ table.
		if (select count(distinct spid) from master.dbo.sysprocesses sproc
			join master.dbo.sysdatabases sdb on sdb.dbid = sproc.dbid and name = 'TestLangProj'
			where sproc.spid != @@spid) = 0 BEGIN
			EXEC tsu_failure 'Second test connection not established'
			return 1
		END

		-- Add a new record to the sync$ table
		SET @uidNew = newid()
		EXEC StoreSyncRec$ @dbName = 'TestLangProj', @uid = @uidNew, @msg = 3, @hvo = 299, @flid = 2999
		select @SyncCountNew = count(*) from sync$
		if @SyncCountNew != @SyncCount + 1
			EXEC tsu_failure 'Failed to insert new record into sync$ table'
		if NOT EXISTS(SELECT * FROM Sync$ WHERE LpInfoId = @uidNew AND Msg = 3 AND ObjId = 299 and ObjFlid = 2999)
			EXEC tsu_failure 'Values for inserted Sync$ record were not set correctly.'
END
GO
