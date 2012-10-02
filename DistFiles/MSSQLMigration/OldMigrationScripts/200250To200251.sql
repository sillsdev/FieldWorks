-- Update database from version 200250 to 200251
BEGIN TRANSACTION  --( will be rolled back if wrong version #)
-------------------------------------------------------------------------------
--( TE-7876: Lock time out on ClearSyncTable$
-------------------------------------------------------------------------------

if object_id('ClearSyncTable$') is not null begin
	print 'removing proc ClearSyncTable$'
	drop proc [ClearSyncTable$]
end
go
print 'creating proc ClearSyncTable$'
go
create proc [ClearSyncTable$]
	@dbName nvarchar(4000)
as
	declare
		@fIsNocountOn int,
		@nTranCount INT,
		@vcTranName VARCHAR(50);

	-- check for the arbitrary case where the db name is null
	if @dbName is null
		return 0

	if (not exists(
		select spid
		from master.dbo.sysprocesses sproc
		join master.dbo.sysdatabases sdb on sdb.dbid = sproc.dbid and name = @dbName
		where sproc.spid != @@spid)) BEGIN

		--( TRUNCATE TABLE gets an exclusive lock on the table, so let's try
		--( to make sure to commit the truncate when we're done.

		SET @nTranCount = @@TRANCOUNT;
		SET @vcTranName = 'ClearSyncTable' + CONVERT(VARCHAR(8), @@NESTLEVEL)
		IF (@nTranCount = 0)
			BEGIN TRAN @vcTranName;
		ELSE
			SAVE TRAN @vcTranName;

		truncate table sync$

		IF (@@ERROR != 0)
			ROLLBACK TRAN @vcTranName;
		IF (@nTranCount = 0)
			COMMIT TRAN @vcTranName;
	END
	select max(id) from sync$

	return 0
go
-------------------------------------------------------------------------------

if object_id('StoreSyncRec$') is not null begin
	print 'removing proc StoreSyncRec$'
	drop proc [StoreSyncRec$]
end
go
print 'creating proc StoreSyncRec$'
go
create proc [StoreSyncRec$]
	@dbName nvarchar(4000),
	@uid uniqueidentifier,
	@msg int,
	@hvo int,
	@flid int
as
	declare
		@fIsNocountOn int,
		@nTranCount INT,
		@vcTranName VARCHAR(50);

	-- check for the arbitrary case where the db name is null
	if @dbName is null
		return 0

	if (exists(
		select spid
		from master.dbo.sysprocesses sproc
		join master.dbo.sysdatabases sdb on sdb.dbid = sproc.dbid and name = @dbName
		where sproc.spid != @@spid)) BEGIN

		--( Let's try to make sure to commit the insert when we're done.

		SET @nTranCount = @@TRANCOUNT;
		SET @vcTranName = 'StoreSyncRec' + CONVERT(VARCHAR(8), @@NESTLEVEL)
		IF (@nTranCount = 0)
			BEGIN TRAN @vcTranName;
		ELSE
			SAVE TRAN @vcTranName;

		insert sync$ (LpInfoId, Msg, ObjId, ObjFlid)
			values (@uid, @msg, @hvo, @flid)

		IF (@@ERROR != 0)
			ROLLBACK TRAN @vcTranName;
		IF (@nTranCount = 0)
			COMMIT TRAN @vcTranName;
	END
	return 0
go

-----------------------------------------------------------------------------------
------ Finish or roll back transaction as applicable
-----------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200250
BEGIN
	UPDATE Version$ SET DbVer = 200251
	COMMIT TRANSACTION
	PRINT 'database updated to version 200251'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200250 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
