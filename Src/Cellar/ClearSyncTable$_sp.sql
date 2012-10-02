/***********************************************************************************************
 * ClearSyncTable$
 *
 * Description:
 *	Deletes all rows from the sync$ table.
 *
 * Paramters:
 *	@dbName=Database name;
 *
 * Returns:
 *	Returns: 0 if successful, otherwise an error code
 *
 * Notes:
 **********************************************************************************************/

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