/***********************************************************************************************
 * StoreSyncRec$
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