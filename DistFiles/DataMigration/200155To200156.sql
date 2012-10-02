-- update database FROM version 200155 to 200156
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

--( In this one migration only, we need to update the version at the beginning
--( instead of the end. The reason is that the stored proc we're working on is
--( in the master database. I haven't figured out a way to date to change back
--( to the database we were using when we started this procedure. All
--( variables get reset after a GO statement. And a GO is needed because a
--( CREATE PROC wants to be the first line of a batch.

IF object_id('master..sp_GetFWDBs') IS NOT NULL BEGIN
	PRINT 'removing procedure sp_GetFWDBs'
	DROP PROCEDURE sp_GetFWDBs
END
GO
PRINT 'creating procedure sp_GetFWDBs'
GO

--( This block usually is at the bottom.

DECLARE @dbVersion int
SELECT @dbVersion = [DbVer] FROM [Version$]
IF @dbVersion = 200155
BEGIN
	UPDATE [Version$] SET [DbVer] = 200156
	COMMIT TRANSACTION
	PRINT 'database updated to version 200156'
END

ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200155 (DbVer = ' +
		convert(varchar, @dbVersion) + ')'
END
GO

USE master
GO

-------------------------------------------------------------------------------
-- SQL Express takes much longer to open databases than MSDE. Since we're on a
-- local FW instance, we're going to assume all the databases within the
-- instance are FW databases. Modified sp_GetFwdbs accordingly.
-------------------------------------------------------------------------------

CREATE procedure [sp_GetFWDBs]
as
	declare @sDynSql nvarchar(4000), @nCurDBId int, @fIsNocountOn int
	declare @dbid int
	declare @Err int
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on
	-- create a temporary table to hold all FieldWorks databases
	create table #dblist ( sDBName sysname )
	set @Err = @@error
	if @Err <> 0 goto LCleanUp2

	 --( SQL Express takes much longer to open databases. Since we're on a local FW
	 --( instance, we're going to assume all the databases within the instance are
	 --( FW databases.
--	-- get all of the databases associated with this server except the system catalog databases
--	declare cur_DBs cursor local fast_forward for
--	select [dbid]
--	from	master..sysdatabases
--	where has_dbaccess([name]) = 1
--		and [name] not in ('master', 'model', 'tempdb', 'msdb', 'Northwind', 'pubs')
--	-- process each database determining whether or not it's a FieldWorks database
--	open cur_DBs
--	set @Err = @@error
--	if @Err <> 0 goto LCleanUp
--	fetch cur_DBs into @dbid
--	while @@fetch_status = 0 begin
--		set @sDynSql = N'if object_id(N''[' + db_name(@dbid) + N']..Class$'') is not null ' +
--				N'and object_id(N''[' + db_name(@dbid) + N']..Field$'') is not null ' +
--				N'and object_id(N''[' + db_name(@dbid) + N']..ClassPar$'') is not null ' +
--				N'insert into #dblist (sDBName) values (' +
--				N'N''' + db_name(@dbid) + N''')'
--		exec ( @sDynSql )
--		set @Err = @@error
--		if @Err <> 0 begin
--			raiserror('Unable to execute dynamic SQL', 16, 1)
--			goto LCleanUp
--		end
--		fetch cur_DBs into @dbid
--	end
--	select [sDBName] from #dblist
-- close cur_DBs
--  deallocate cur_DBs

	insert into #dblist
	select [name]
	from master..sysdatabases
	where upper([name]) not in ('MASTER', 'MODEL', 'TEMPDB', 'MSDB', 'NORTHWIND', 'PUBS', 'ETHNOLOGUE')

	select [sDBName] from #dblist
LCleanUp:
	drop table #dblist
LCleanUp2:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @Err
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

--( In this migration only, this block of code is moved to the beginning. See
--( comments at the top.
