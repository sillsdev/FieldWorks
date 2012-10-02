-- update database from version 200028 to 200029
BEGIN TRANSACTION  --( will be rolled back if wrong version#

-------------------------------------------------------------------------------
-- NoteInterlinProcessTime needs to return the set of created objects
-- This is a repeat of 200025To200026 which was missing a GO
-------------------------------------------------------------------------------

--( Add LexicalDatabase Styles.
--( If Custom, CustomID, and Big are omitted from the insert statement, their
--( associated default constraints will set the fields to 1, a new id, and 0,
--( respectively.

--( Steve Miller, Dec. 7, 2006: This field is already here.
--INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
--VALUES (5005016, 25, 5005, 17, 'Styles', 0, NULL, NULL)
--
--GO

-------------------------------------------------------------------------------

--( 200008To200009.sql had an error in it, making MainEntriesOrSenses a custom
--( field. The script has been fixed. This fixes the data.


ALTER TABLE Field$ DISABLE TRIGGER TR_Field$_No_Upd
GO

UPDATE Field$ SET Custom = 0, CustomId = NULL, Big = NULL
WHERE Id IN (5007001, 5009001)

ALTER TABLE Field$ ENABLE TRIGGER TR_Field$_No_Upd
GO

-------------------------------------------------------------------------------

--( Added some defaults to ManageConstraints$. Some comments were changed,
--( but shouldn't be necessary to add here.

IF OBJECT_ID('ManageConstraints$') IS NOT NULL BEGIN
	PRINT 'removing procedure ManageConstraints$'
	DROP PROCEDURE [ManageConstraints$]
END
GO
PRINT 'creating procedure ManageConstraints$'
GO

CREATE PROCEDURE [ManageConstraints$]
		@nvcTableName NVARCHAR(50) = NULL,
		@cConstraintType CHAR(2) = 'F',
		@cAction CHAR(10) = 'CHECK'
AS
	DECLARE
		@sysConstraintName SYSNAME,
		@nvcSQL NVARCHAR(200)

	--( Run for all tables
	IF @nvcTableName IS NULL BEGIN
		DECLARE curConstraints CURSOR FOR
			SELECT consobjs.[Name] AS FKName, tableobjs.[Name] AS TableName
			FROM sysconstraints sc
			JOIN sysobjects consobjs ON consobjs.[id] = sc.[constid] --( to get constraint names
			JOIN sysobjects tableobjs ON tableobjs.[id] = sc.[id] --( to get table names
			WHERE consobjs.[xtype] = @cConstraintType

		OPEN curConstraints
		FETCH NEXT FROM curConstraints INTO @sysConstraintName, @nvcTableName
		WHILE @@FETCH_STATUS = 0
		BEGIN
			SET @nvcSQL = 'ALTER TABLE ' + @nvcTableName + ' ' + @cAction +
				' CONSTRAINT ' + @sysConstraintName
			EXEC (@nvcSQL)

			FETCH NEXT FROM curConstraints INTO @sysConstraintName, @nvcTableName
		END
		CLOSE curConstraints
		DEALLOCATE curConstraints
	END

	ELSE BEGIN --( @nvcTableName is not null, run for individual table
		DECLARE curConstraints CURSOR FOR
			SELECT consobjs.[Name]
			FROM sysconstraints sc
			JOIN sysobjects consobjs ON consobjs.[id] = sc.[constid] --( to get constraint names
			JOIN sysobjects tableobjs ON tableobjs.[id] = sc.[id] --( to get table names
			WHERE tableobjs.[Name] = @nvcTableName AND consobjs.[xtype] = @cConstraintType

		OPEN curConstraints
		FETCH NEXT FROM curConstraints INTO @sysConstraintName
		WHILE @@FETCH_STATUS = 0
		BEGIN
			SET @nvcSQL = 'ALTER TABLE ' + @nvcTableName + ' ' + @cAction +
				' CONSTRAINT ' + @sysConstraintName
			EXEC (@nvcSQL)

			FETCH NEXT FROM curConstraints INTO @sysConstraintName
		END
		CLOSE curConstraints
		DEALLOCATE curConstraints
	END
GO

-------------------------------------------------------------------------------

--( Some comments were added to fnGetOwnedObjects$, in FwCore.sql, but it's
--( not worthwhile here to drop and add the stored proc just to updated the
--( comments.

-------------------------------------------------------------------------------

declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200028
begin
	update Version$ set DbVer = 200029
	COMMIT TRANSACTION
	print 'database updated to version 200029'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200028 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
