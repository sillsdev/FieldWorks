/***********************************************************************************************
 * Function: ManageConstraints$
 *
 * Parameteres:
 *	@nvcTableName		= Name of the table with the constraints. NULL = all tables
 *	@cConstraintType	= Type of constraint. Default is 'F'. From Books On Line, under the
 *						topic sysobjects:
 *							C = CHECK constraint
 *							D = Default or DEFAULT constraint
 *							F = FOREIGN KEY constraint
 *							L = Log
 *							FN = Scalar function
 *							IF = Inlined table-function
 *							P = Stored procedure
 *							PK = PRIMARY KEY constraint (type is K)
 *							RF = Replication filter stored procedure
 *							S = System table
 *							TF = Table function
 *							TR = Trigger
 *							U = User table
 *							UQ = UNIQUE constraint (type is K)
 *							V = View
 *							X = Extended stored procedure
 *	@cAction			= Action to preform on the trigger. Current supported values are:
 *							CHECK, NOCHECK, and DROP. CHECK and NOCHECK can only be used with
 *							FOREIGN KEY (type F) and CHECK (type C) constraints. Defaults to
 *							'CHECK'
 *
 * Description:
 *  Activate or deactivate constraints for a table. May be expanded more someday for extra
 *	functionality.
 **********************************************************************************************/

IF OBJECT_ID('ManageConstraints$') IS NOT NULL BEGIN
	PRINT 'removing procedure ManageConstraints$'
	DROP PROCEDURE [ManageConstraints$]
END
GO
PRINT 'creating procedure ManageConstraints$'
GO

CREATE PROCEDURE [ManageConstraints$]
		@nvcTableName NVARCHAR(100) = NULL,
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
