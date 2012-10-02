-- Update database from version 200202 to 200203
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- FWC-10: Create stored procedure RenameClass
-------------------------------------------------------------------------------

IF OBJECT_ID('RenameClass_RenameFieldId') IS NOT NULL BEGIN
	PRINT 'removing procedure RenameClass_RenameFieldId';
	DROP PROCEDURE RenameClass_RenameFieldId;
END
GO
PRINT 'creating procedure RenameClass_RenameFieldId';
GO

CREATE PROCEDURE RenameClass_RenameFieldId
	@OldClassId INT,
	@NewClassId INT,
	@OldFieldId INT,
	@NewFieldId INT OUTPUT
AS

	------------------------------------------------------------------
	--( Note! This is a supporting procedure of RenameClass_sp.sql )--
	------------------------------------------------------------------

	SET @NewFieldId =
		CAST(
			CAST(@NewClassId AS VARCHAR(20)) +
			SUBSTRING(
				CAST(@OldFieldId AS VARCHAR(20)),
				LEN(@OldClassId) + 1,
				LEN(@OldFieldId) - LEN(@OldClassId))
		AS INT);
GO

---------

IF OBJECT_ID('RenameClass') IS NOT NULL BEGIN
	PRINT 'removing procedure RenameClass';
	DROP PROCEDURE RenameClass;
END
GO
PRINT 'creating procedure RenameClass';
GO

CREATE PROCEDURE RenameClass
	@ClassId INT,
	@NewClassName NVARCHAR(100)
AS

	/******************************************************************
	** Warning! Do not use this procedure unless you absolutely know **
	** what you are doing! Then think about it twice. And back up    **
	** your database first.                                          **
	******************************************************************/

	--==( Setup )==--

	DECLARE
		@DummyClassId INT,
		@DummyFieldId INT,
		@FieldId INT,
		@Type INT,
		@Class INT,
		@DstCls INT,
		@FieldName NVARCHAR(100),
		@Custom TINYINT,
		@CustomId UNIQUEIDENTIFIER,
		@Min BIGINT,
		@Max BIGINT,
		@Big BIT,
		@UserLabel NVARCHAR(100),
		@HelpString NVARCHAR(100),
		@ListRootId INT,
		@WsSelector INT,
		@OldClassName NVARCHAR(100),
		@ClassName NVARCHAR(100),
		@Abstract TINYINT,
		@Sql NVARCHAR(4000),
		@Debug BIT

	SET @Debug = 0

	SET @DummyClassId = 9999
	SELECT @OldClassName = Name, @Abstract = Abstract FROM Class$ WHERE Id = @ClassId

	--==( Disable Security )==--

	SET @Sql = '
		ALTER TABLE Class$ NOCHECK CONSTRAINT ALL;
		ALTER TABLE ClassPar$ NOCHECK CONSTRAINT ALL;
		ALTER TABLE Field$ NOCHECK CONSTRAINT ALL;
		ALTER TABLE CmObject NOCHECK CONSTRAINT ALL;
		ALTER TABLE MultiStr$ NOCHECK CONSTRAINT ALL;
		ALTER TABLE MultiBigStr$ NOCHECK CONSTRAINT ALL;
		ALTER TABLE MultiBigTxt$ NOCHECK CONSTRAINT ALL;
		DISABLE TRIGGER TR_Field$_UpdateModel_Del ON Field$;
		DISABLE TRIGGER TR_Field$_No_Upd ON Field$;'
	EXEC (@Sql);

	--==( Create the new class )==--

	INSERT INTO Class$
		SELECT @DummyClassId, [Mod], Base, Abstract, @NewClassName
		FROM Class$
		WHERE Id = @ClassId

	--( Load the attributes of the new class.

	-- REVIEW (SteveMiller/AnnB): Will need to add XmlUI if it ever starts
	-- getting to used.

	DECLARE ClassAttributes CURSOR FOR
		SELECT
			Id, Type, Class, DstCls, Name, Custom, CustomId,
			[Min], [Max], Big, UserLabel, HelpString, ListRootId, WsSelector
		FROM Field$
		WHERE Class = @ClassId
		ORDER BY Id

	OPEN ClassAttributes
	FETCH NEXT FROM ClassAttributes INTO
		@FieldId, @Type, @Class, @DstCls, @FieldName, @Custom, @CustomId,
		@Min, @Max, @Big, @UserLabel, @HelpString, @ListRootId,	@WsSelector;

	IF @Debug = 1 BEGIN
		print '========================================================'
		print 'starting loop 1 for class ' + cast(@ClassId as nvarchar(20))
		print '========================================================'
	END
	WHILE @@FETCH_STATUS = 0 BEGIN
		EXEC RenameClass_RenameFieldId
			@ClassId, @DummyClassId, @FieldId, @DummyFieldId OUTPUT
		IF @Debug = 1 BEGIN
			print '@OldClassId: ' + CAST(@ClassId AS NVARCHAR(20))
			print '@NewClassId: ' + CAST(@DummyClassId AS NVARCHAR(20))
			print '@OldFieldId: ' + CAST(@FieldId AS NVARCHAR(20))
			print '@NewFieldId: ' + CAST(@DummyFieldId AS NVARCHAR(20))
			print '--------------------------------------------------------'
		END

		INSERT INTO Field$
			([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId],
			[Min], [Max], [Big], UserLabel, HelpString, ListRootId, WsSelector)
		VALUES
			(@DummyFieldId, @Type, @DummyClassId, @DstCls, @FieldName, @Custom, @CustomID,
				@Min, @Max, @Big, @UserLabel, @HelpString, @ListRootId, @WsSelector)

		FETCH NEXT FROM ClassAttributes INTO
			@FieldId, @Type, @Class, @DstCls, @FieldName, @Custom, @CustomId,
			@Min, @Max, @Big, @UserLabel, @HelpString, @ListRootId,	@WsSelector
	END
	CLOSE ClassAttributes
	DEALLOCATE ClassAttributes

	--==( Copy Data Over )==--

	SET @Sql = N'INSERT INTO ' + @NewClassName + N' SELECT * FROM ' + @OldClassName
	EXEC (@Sql)

	DECLARE NewAttributeTables CURSOR FOR
		SELECT Id, Type, f.Name
		FROM Field$ f
		WHERE Class = @ClassId
			AND Type IN (16, 26, 28) --( 16 MultiUnicode, 26 ReferenceCollection, 28 ReferenceSequence

	OPEN NewAttributeTables
	FETCH NEXT FROM NewAttributeTables INTO @DummyFieldId, @Type, @FieldName
	WHILE @@FETCH_STATUS = 0 BEGIN
		--( INSERT INTO CmPoss_Name SELECT * FROM CmPossibility_Name
		SET @Sql = N'INSERT INTO ' + @NewClassName + N'_' + @FieldName +
			N' SELECT * FROM ' + @OldClassName + N'_' + @FieldName
		EXEC (@Sql)
		FETCH NEXT FROM NewAttributeTables INTO @DummyFieldId, @Type, @FieldName
	END
	CLOSE NewAttributeTables
	DEALLOCATE NewAttributeTables

	--==( Delete the Old )==--

	--( Remove references of the object

	DECLARE Refs CURSOR FOR
		SELECT c.Name, f.Name, Type, Class
		FROM Field$ f
		JOIN Class$ c ON c.Id = f.Class
		WHERE f.Class = @ClassId and f.Type IN (26, 28);
	OPEN Refs
	FETCH NEXT FROM Refs INTO @ClassName, @FieldName, @Type, @Class
	WHILE @@FETCH_STATUS = 0 BEGIN
		SET @Sql = N'ALTER TABLE ' + @ClassName + N'_' + @FieldName +
			N' DROP CONSTRAINT _FK_' + @ClassName + N'_' + @FieldName  + N'_Src';
		EXECUTE (@Sql);
		FETCH NEXT FROM Refs INTO @ClassName, @FieldName, @Type, @Class
	END
	CLOSE Refs
	DEALLOCATE Refs

	--( Remove referencing constraints to the object

	DECLARE OutsideRefs CURSOR FOR
		SELECT c.Name, f.Name, Type, Class, DstCls
		FROM Field$ f
		JOIN Class$ c ON c.Id = f.Class
		WHERE f.DstCls = @ClassId;
	OPEN OutsideRefs
	FETCH NEXT FROM OutsideRefs INTO @ClassName, @FieldName, @Type, @Class, @DstCls
	WHILE @@FETCH_STATUS = 0 BEGIN
		IF @Type = 24 AND @ClassName != 'CmObject' BEGIN
			SET @Sql = N'ALTER TABLE ' + @ClassName + N' DROP CONSTRAINT _FK_' +
				@ClassName + N'_' + @FieldName;
			EXECUTE (@Sql);
		END
		IF @Type = 26 OR @Type = 28 BEGIN
			SET @Sql = N'ALTER TABLE ' + @ClassName + N'_' + @FieldName +
				N' DROP CONSTRAINT _FK_' + @ClassName + N'_' + @FieldName  + N'_Dst';
			EXECUTE (@Sql);
		END
		FETCH NEXT FROM OutsideRefs INTO @ClassName, @FieldName, @Type, @Class, @DstCls
	END
	CLOSE OutsideRefs
	DEALLOCATE OutsideRefs

	--( Remove subclass referencing constraints

	DECLARE Subclasses CURSOR FOR SELECT Name FROM Class$ WHERE Base = @ClassId;
	OPEN Subclasses
	FETCH NEXT FROM Subclasses INTO @ClassName
	WHILE @@FETCH_STATUS = 0 BEGIN
		SET @Sql = N'ALTER TABLE ' + @ClassName + N' DROP CONSTRAINT _FK_' +
			@ClassName + N'_id';
		EXECUTE (@Sql);
		FETCH NEXT FROM Subclasses INTO @ClassName
	END
	CLOSE Subclasses
	DEALLOCATE Subclasses

	--( Normally the Field$ delete trigger takes care of all this. However,
	--( the trigger also wipes out data, which we don't want.

	--( Drop all the constraints
	EXEC ManageConstraints$ @OldClassName, 'C', 'DROP'
	EXEC ManageConstraints$ @OldClassName, 'D', 'DROP'
	EXEC ManageConstraints$ @OldClassName, 'F', 'DROP'
	EXEC ManageConstraints$ @OldClassName, 'UQ', 'DROP'
	EXEC ManageConstraints$ @OldClassName, 'PK', 'DROP'

	DECLARE OldClassAttributes CURSOR FOR
		SELECT Id, Type, f.Name
		FROM Field$ f
		WHERE Class = @ClassId

	OPEN OldClassAttributes
	FETCH NEXT FROM OldClassAttributes INTO @FieldId, @Type, @FieldName
	WHILE @@FETCH_STATUS = 0 BEGIN
		--( Type 14, 18, 20, 23, 25, 27
		IF @Type IN (14, 18, 20, 23, 25, 27) BEGIN
			SET @Sql = N'DROP VIEW ' + @OldClassName + N'_' + @FieldName
			EXEC (@Sql)
		END

		--( Type 16, 26, or 28
		IF @Type IN (16, 26, 28) BEGIN
			SET @Sql = N'DROP TABLE ' + @OldClassName + N'_' + @FieldName
			EXEC (@Sql)

			IF @Type = 26 BEGIN
				SET @Sql = N'DROP PROCEDURE ReplaceRefColl_' +
					@OldClassName + N'_' + @FieldName
				EXEC (@Sql)
			END
			ELSE IF @Type = 28 BEGIN
				SET @Sql = N'DROP PROCEDURE ReplaceRefSeq_' +
					@OldClassName + N'_' + @FieldName
				EXEC (@Sql)
			END
		END

		FETCH NEXT FROM OldClassAttributes INTO @FieldId, @Type, @FieldName
	END
	CLOSE OldClassAttributes
	DEALLOCATE OldClassAttributes

	--( Drop the table's view
	SET @Sql = N'DROP VIEW ' + @OldClassName + N'_'
	EXEC (@Sql)

	DELETE FROM Field$ WHERE Class = @ClassId
	DELETE FROM ClassPar$ WHERE Src = @ClassId
	DELETE FROM Class$ WHERE Id = @ClassId

	SET @Sql = N'DROP TABLE ' + @OldClassName
	EXEC (@Sql)

	--Ann added because this sp doesn't exist for abstract classes
	IF @Abstract = N'0' BEGIN

		SET @Sql = N'DROP PROCEDURE CreateObject_' + @OldClassName
		EXEC (@Sql)

	END
	--==( Now that the old class is gone, give the new class the proper id and name )==--

	--( Take care of the class and field

	UPDATE ClassPar$
	SET Src = @ClassId, Dst = @ClassId
	WHERE Src = @DummyClassId AND Dst = @DummyClassId

	UPDATE ClassPar$ SET Src = @ClassId WHERE Src = @DummyClassId
	UPDATE Class$ SET Id = @ClassId WHERE Id = @DummyClassId

	DECLARE CorrectFieldIds CURSOR FOR SELECT ID FROM Field$ f WHERE Class = @DummyClassId --ORDER BY Id
	OPEN CorrectFieldIds
	FETCH NEXT FROM CorrectFieldIds INTO @DummyFieldId
	IF @Debug = 1 BEGIN
		print '========================================================'
		print 'starting loop 2 for class ' + cast(@ClassId as nvarchar(20))
		print '========================================================'
	END
	WHILE @@FETCH_STATUS = 0 BEGIN
		EXECUTE RenameClass_RenameFieldId @DummyClassId, @ClassId, @DummyFieldId, @FieldId OUTPUT
		IF @Debug = 1 BEGIN
			print '@OldClassId: ' + CAST(@DummyClassId AS NVARCHAR(20))
			print '@NewClassId: ' + CAST(@ClassId AS NVARCHAR(20))
			print '@OldFieldId: ' + CAST(@DummyFieldId AS NVARCHAR(20))
			print '@NewFieldId: ' + CAST(@FieldId AS NVARCHAR(20))
			print '--------------------------------------------------------'
		END
		UPDATE Field$ SET Id = @FieldId WHERE Id = @DummyFieldId
		FETCH NEXT FROM CorrectFieldIds INTO @DummyFieldId
	END
	CLOSE CorrectFieldIds
	DEALLOCATE CorrectFieldIds

	UPDATE Field$ SET Class = @ClassId WHERE Class = @DummyClassId
	UPDATE Field$ SET DstCls = @ClassId WHERE DstCls = @DummyClassId

	--==( Rebuild )==--

	--( Rebuild Class View

	EXECUTE UpdateClassView$ @ClassId, 1

	--( Rebuild Multi Views and procedures ReplaceRef* (they still have class 999)

	DECLARE ClassAttributes CURSOR FOR
		SELECT Id, Type, f.Name
		FROM Field$ f
		WHERE Class = @ClassId
			-- REVIEW (SteveMiller/AnnB):  Type 20 has an ntext, which isn't being used.
			AND Type IN (14, 18, 23, 25, 26, 27, 28)

	OPEN ClassAttributes
	FETCH NEXT FROM ClassAttributes INTO @FieldId, @Type, @FieldName
	WHILE @@FETCH_STATUS = 0 BEGIN
		IF @Type = 26 BEGIN
			SET @Sql = @NewClassName + N'_' + @FieldName
			EXEC DefineReplaceRefCollProc$ @Sql;
		END
		ELSE IF @Type = 28 BEGIN
			SET @Sql = @NewClassName + N'_' + @FieldName
			EXEC DefineReplaceRefSeqProc$ @Sql, @FieldId;
		END
		ELSE BEGIN
			SET @Sql = N'DROP VIEW ' + @NewClassName + N'_' + @FieldName
			EXEC (@Sql)

			SET @Sql = N'CREATE VIEW ' + @NewClassName + N'_' + @FieldName + ' AS SELECT '
			IF @Type = 14
				SET @Sql = @Sql +
					N'[Obj], [Flid], [Ws], [Txt], [Fmt]
					FROM [MultiStr$]
					WHERE [Flid] = ' + CAST(@FieldId AS NVARCHAR(20))
			ELSE IF @Type = 18
				SET @Sql = @Sql +
					N'[Obj], [Flid], [Ws], [Txt], CAST(NULL AS VARBINARY) AS [Fmt]
					FROM [MultiBigStr$]
					WHERE [Flid] = ' + CAST(@FieldId AS NVARCHAR(20))
			ELSE IF @Type IN (23, 25)
				SET @Sql = @Sql +
					N'Owner$ AS Src, Id AS Dst
					FROM CmObject
					WHERE OwnFlid$ = ' + CAST(@FieldId AS NVARCHAR(20))
			ELSE IF @Type = 27
				SET @Sql = @Sql +
					N'Owner$ AS Src, Id AS Dst, OwnOrd$ AS Ord
					FROM CmObject
					WHERE OwnFlid$ = ' + CAST(@FieldId AS NVARCHAR(20))
			EXEC (@Sql)
		END --( IF BEGIN

		FETCH NEXT FROM ClassAttributes INTO @FieldId, @Type, @FieldName
	END --( WHILE
	CLOSE ClassAttributes
	DEALLOCATE ClassAttributes

	--( Rebuild referencing constraints to the object

	DECLARE OutsideRefs CURSOR FOR
		SELECT c.Name, f.Name, Type, Class, DstCls
		FROM Field$ f
		JOIN Class$ c ON c.Id = f.Class
		WHERE f.DstCls = @ClassId
			AND Class != @ClassId; --( The Field$ trigger already took care of self-referencing constraints
	OPEN OutsideRefs
	FETCH NEXT FROM OutsideRefs INTO @ClassName, @FieldName, @Type, @Class, @DstCls
	WHILE @@FETCH_STATUS = 0 BEGIN
		IF @Type = 24 AND @NewClassName != 'CmObject' BEGIN
			set @sql = 'ALTER TABLE [' + @ClassName + '] ADD CONSTRAINT [_FK_' +
				+ @ClassName + '_' + @FieldName + '] ' + CHAR(13) + CHAR(9) +
				' FOREIGN KEY ([' + @FieldName + ']) REFERENCES [' + @NewClassName + '] ([Id])'
			EXECUTE (@Sql);
		END
		--( This block is slightly modified from the Field$ insert trigger
		IF @Type = 26 OR @Type = 28 BEGIN
			set @Sql = N'ALTER TABLE [' + @ClassName + '_' + @FieldName +
				N'] ADD CONSTRAINT [_FK_' + @ClassName + N'_' + @FieldName + N'_Dst] ' +
				N'FOREIGN KEY ([Dst]) REFERENCES [' + @NewClassName + N'] ([Id])'
			exec (@sql)
		END
		FETCH NEXT FROM OutsideRefs INTO @ClassName, @FieldName, @Type, @Class, @DstCls;
	END;
	CLOSE OutsideRefs;
	DEALLOCATE OutsideRefs;

	--( Rebuild foreign keys.

	DECLARE ForeignKeys CURSOR FOR
		SELECT c.Name AS DstClsName, f.Name, Type, Class, DstCls
		FROM Field$ f
		JOIN Class$ c ON c.Id = f.DstCls
		WHERE f.Class = @ClassId
			AND DstCls = @ClassId; --( The Field$ trigger already took care of self-referencing constraints
	OPEN ForeignKeys
	FETCH NEXT FROM ForeignKeys INTO @ClassName, @FieldName, @Type, @Class, @DstCls
	WHILE @@FETCH_STATUS = 0 BEGIN
		IF @Type = 24 AND @NewClassName != 'CmObject' BEGIN
			set @sql = 'ALTER TABLE [' + @NewClassName + '] ADD CONSTRAINT [_FK_' +
				+ @NewClassName + '_' + @FieldName + '] ' + CHAR(13) + CHAR(9) +
				' FOREIGN KEY ([' + @FieldName + ']) REFERENCES [' + @ClassName + '] ([Id])'
			EXECUTE (@Sql);
		END
		--( This block is slightly modified from the Field$ insert trigger
		IF @Type = 26 OR @Type = 28 BEGIN
			set @Sql = N'ALTER TABLE [' + @NewClassName + '_' + @FieldName +
				N'] ADD CONSTRAINT [_FK_' + @NewClassName + N'_' + @FieldName + N'_Dst] ' +
				N'FOREIGN KEY ([Dst]) REFERENCES [' + @ClassName + N'] ([Id])'
			exec (@sql)
		END
		FETCH NEXT FROM ForeignKeys INTO @ClassName, @FieldName, @Type, @Class, @DstCls;
	END;
	CLOSE ForeignKeys;
	DEALLOCATE ForeignKeys;

	--( Rebuild the function fnGetRefsToObj

	EXEC CreateGetRefsToObj

	--( Rebuild CreateObject_*   Ann added condition because this sp isn't created for abstract classes
	IF @Abstract = N'0'
		EXEC DefineCreateProc$ @ClassId;

	--==( Cleanup )==--

	SET @Sql = '
		ENABLE TRIGGER TR_Field$_UpdateModel_Del ON Field$;
		ENABLE TRIGGER TR_Field$_No_Upd ON Field$

		ALTER TABLE Class$ CHECK CONSTRAINT ALL
		ALTER TABLE ClassPar$ CHECK CONSTRAINT ALL
		ALTER TABLE Field$ CHECK CONSTRAINT ALL
		ALTER TABLE CmObject CHECK CONSTRAINT ALL
		ALTER TABLE MultiStr$ CHECK CONSTRAINT ALL
		ALTER TABLE MultiBigStr$ CHECK CONSTRAINT ALL;
		ALTER TABLE MultiBigTxt$ CHECK CONSTRAINT ALL;'
	EXEC (@Sql);
GO

-------------------------------------------------------------------------------

IF OBJECT_ID('RenameField') IS NOT NULL BEGIN
	PRINT 'removing procedure RenameField';
	DROP PROCEDURE RenameField;
END
GO
PRINT 'creating procedure RenameField';
GO

CREATE PROCEDURE RenameField
	@FieldId INT,
	@NewFieldName NVARCHAR(100)
AS

	/******************************************************************
	** Warning! Do not use this procedure unless you absolutely know **
	** what you are doing! Then think about it twice. And back up    **
	** your database first.                                          **
	******************************************************************/

	--==( Setup )==--

	DECLARE
		@ClassId INT,
		@ClassName NVARCHAR(100),
		@DstClassName NVARCHAR(100),
		@Type INT,
		@OldFieldName NVARCHAR(100),
		@Sql NVARCHAR(4000);

	DECLARE @Field TABLE (
		Id INT,
		Type INT,
		Class INT,
		DstCls INT,
		--( Name NVARCHAR(100), Name not needed
		Custom TINYINT,
		CustomId UNIQUEIDENTIFIER,
		[Min] BIGINT,
		[Max] BIGINT,
		[Big] BIT,
		UserLabel NVARCHAR(100),
		HelpString NVARCHAR(100),
		ListRootId INT,
		WsSelector INT)

	SELECT
		@Type = Type,
		@ClassId = Class,
		@OldFieldName = f.Name,
		@ClassName = c.Name,
		@DstClassName = dst.Name
	FROM Field$ f
	JOIN Class$ c ON c.Id = f.Class
	LEFT OUTER JOIN Class$ dst ON dst.Id = f.DstCls
	WHERE f.Id = @FieldId;

	DISABLE TRIGGER TR_Field$_No_Upd ON Field$;

	--==============================--
	--==( 16 MultiUnicode        )==--
	--==( 26 ReferenceCollection )==--
	--==( 28 ReferenceSequence   )==--
	--==============================--

	IF @Type IN (16, 26, 28) BEGIN

		--( Save off the column info
		INSERT INTO @Field
		SELECT
			Id, Type, Class, DstCls, Custom, CustomId,
			[Min], [Max], [Big], UserLabel, HelpString, ListRootId, WsSelector
		FROM Field$
		WHERE Id = @FieldId

		--( Save off the data (of the attribute table)
		IF @Type = 16 BEGIN
			CREATE TABLE #Temp16 (Obj INT, Ws INT, Txt NVARCHAR(4000));
			SET @Sql = N'INSERT INTO #Temp16 SELECT Obj, Ws, Txt FROM ' +
				@ClassName + N'_' + @OldFieldName;
		END
--		IF @Type = 20 BEGIN --( Currently not used
--			CREATE TABLE #Temp20 (Obj INT, Ws INT, Txt NVARCHAR(4000));
--			SET @Sql = N'INSERT INTO #Temp20 SELECT Obj, Ws, Txt FROM ' +
--				@ClassName + N'_' + @OldFieldName;
--		END
		ELSE IF @Type = 26 BEGIN
			CREATE TABLE #Temp26 (Src INT, Dst INT);
			SET @Sql = N'INSERT INTO #Temp26 SELECT Src, Dst FROM ' +
				@ClassName + N'_' + @OldFieldName;
		END
		ELSE IF @Type = 28 BEGIN
			CREATE TABLE #Temp28 (Src INT, Dst INT, Ord INT);
			SET @Sql = N'INSERT INTO #Temp28 SELECT Src, Dst, Ord FROM ' +
				@ClassName + N'_' + @OldFieldName;
		END
		EXEC (@Sql);

		--( Remove the old column
		DELETE FROM Field$ WHERE Id = @FieldId

		--( Create the new column
		INSERT INTO Field$
			(Id, Type, Class, DstCls, Name, Custom, CustomId,
			[Min], [Max], [Big], UserLabel, HelpString, ListRootId, WsSelector)
		SELECT
			Id, Type, Class, DstCls, @NewFieldName, Custom, CustomId,
			[Min], [Max], [Big], UserLabel, HelpString, ListRootId, WsSelector
		FROM @Field

		--( Restore the data

		IF @Type = 16 BEGIN
			SET @Sql = N'INSERT INTO '+ @ClassName + N'_' + @NewFieldName +
				' SELECT Obj, Ws, Txt FROM #Temp16';
			EXEC (@Sql);
			DROP TABLE #Temp16;
		END
		ELSE IF @Type = 26 BEGIN
			SET @Sql = N'INSERT INTO '+ @ClassName + N'_' + @NewFieldName +
				' SELECT Src, Dst FROM #Temp26';
			EXEC (@Sql);
			DROP TABLE #Temp26;
		END
		ELSE IF @Type = 28 BEGIN
			SET @Sql = N'INSERT INTO '+ @ClassName + N'_' + @NewFieldName +
				' SELECT Src, Dst, Ord FROM #Temp28';
			EXEC (@Sql);
			DROP TABLE #Temp28;
		END;
	END;

	--===========================--
	--==( 14 MultiString      )==--
	--==( 18 MultiBigString   )==--
	--==( 23 OwningAtom       )==--
	--==( 25 OwningCollection )==--
	--==( 27 OwningSequence   )==--
	--===========================--

	ELSE IF @Type IN (14, 18, 23, 25, 27) BEGIN
		SET @Sql = N'DROP VIEW ' + @ClassName + N'_' + @OldFieldName;
		EXEC (@Sql);

		UPDATE Field$ SET Name = @NewFieldName WHERE Name = @OldFieldName

		SET @Sql = N'CREATE VIEW ' + @ClassName + N'_' + @NewFieldName + ' AS SELECT '
		IF @Type = 14
			SET @Sql = @Sql +
				N'[Obj], [Flid], [Ws], [Txt], [Fmt]
				FROM [MultiStr$]
				WHERE [Flid] = ' + CAST(@FieldId AS NVARCHAR(20))
		ELSE IF @Type = 18
			SET @Sql = @Sql +
				N'[Obj], [Flid], [Ws], [Txt], CAST(NULL AS VARBINARY) AS [Fmt]
				FROM [MultiBigStr$]
				WHERE [Flid] = ' + CAST(@FieldId AS NVARCHAR(20))
		ELSE IF @Type IN (23, 25)
			SET @Sql = @Sql +
				N'Owner$ AS Src, Id AS Dst
				FROM CmObject
				WHERE OwnFlid$ = ' + CAST(@FieldId AS NVARCHAR(20))
		ELSE IF @Type = 27
			SET @Sql = @Sql +
				N'Owner$ AS Src, Id AS Dst, OwnOrd$ AS Ord
				FROM CmObject
				WHERE OwnFlid$ = ' + CAST(@FieldId AS NVARCHAR(20))
		EXEC (@Sql);

		--==( Rebuild Stuff for the Class )==--

		--( Rebuild the class view. This should be fired in the Field$ insert last
		--( trigger TR_Field$UpdateModel_InsLast, though a lot of people add it to
		--( migration scripts:

		EXECUTE UpdateClassView$ @ClassId, 1

		--( Rebuild the function fnGetRefsToObj. Fired in the Field$ insert
		--( last trigger TR_Field$UpdateModel_InsLast:

		EXEC CreateGetRefsToObj

		--( Rebuild CreateObject_*	Fired in the Field$ insert last trigger
		--( TR_Field$UpdateModel_InsLast:

		EXEC DefineCreateProc$ @ClassId;

	END --(  @Type IN (14, 18, 23, 25, 27)

	--================================--
	--==( Attribute in class table )==--
	--==( 1 Boolean                )==--
	--==( 2 Integer                )==--
	--==( 5 Time                   )==--
	--==( 6 Guid                   )==--
	--==( 8 Gendate                )==--
	--==( 9 Binary                 )==--
	--==( 13 String                )==--
	--==( 15 Unicode               )==--
	--==( 17 BigString             )==--
	--==( 19 CompDetails           )==--
	--==( 24 ReferenceAtom         )==--
	--================================--

	ELSE BEGIN

		--( Save off the column info
		INSERT INTO @Field
		SELECT
			Id, Type, Class, DstCls, Custom, CustomId,
			[Min], [Max], [Big], UserLabel, HelpString, ListRootId, WsSelector
		FROM Field$
		WHERE Id = @FieldId

		--( Save off the data
		SET @Sql = N'CREATE TABLE #Temp' + CAST(@Type AS NVARCHAR(2))
		IF @Type = 1
			CREATE TABLE #Temp1 (Id INT, Temp BIT);
		ELSE IF @Type = 2
			CREATE TABLE #Temp2 (Id INT, Temp INT);
		ELSE IF @Type = 3 --( currently not used
			CREATE TABLE #Temp3 (Id INT, Temp INT);
		ELSE IF @Type = 4 --( currently not used
			CREATE TABLE #Temp4 (Id INT, Temp INT);
		ELSE IF @Type = 5
			CREATE TABLE #Temp5 (Id INT, Temp DATETIME);
		ELSE IF @Type = 6
			CREATE TABLE #Temp6 (Id INT, Temp UNIQUEIDENTIFIER);
		ELSE IF @Type = 7 --( currently not used
			CREATE TABLE #Temp7 (Id INT, Temp IMAGE);
		ELSE IF @Type = 8
			CREATE TABLE #Temp8 (Id INT, Temp INT);
		ELSE IF @Type = 9
			CREATE TABLE #Temp9 (Id INT, Temp VARBINARY(8000));
		ELSE IF @Type = 13
			CREATE TABLE #Temp13 (Id INT, Temp NVARCHAR(4000), Temp_Fmt VARBINARY(8000));
		ELSE IF @Type = 15
			CREATE TABLE #Temp15 (Id INT, Temp VARCHAR(4000));
		ELSE IF @Type = 17
			CREATE TABLE #Temp17 (Id INT, Temp NTEXT, Temp_Fmt IMAGE);
		ELSE IF @Type = 19
			CREATE TABLE #Temp19 (Id INT, Temp NTEXT);
		ELSE IF @Type = 24
			CREATE TABLE #Temp24 (Id INT, Temp INT);

		IF @Type NOT IN (13, 17) --( not in String and BigString
			SET @Sql = N'INSERT INTO #Temp' + CAST(@Type AS NVARCHAR(2)) +
				N' SELECT Id, ' + @OldFieldName + N' FROM ' + @ClassName;
		ELSE
			SET @Sql = N'INSERT INTO #Temp' + CAST(@Type AS NVARCHAR(2)) +
				N' SELECT Id, ' + @OldFieldName + ', ' + @OldFieldName + N'_Fmt FROM ' + @ClassName;
		EXEC (@Sql);

		--( Remove the old column
		DELETE FROM Field$ WHERE Id = @FieldId;

		--( Create the new column
		INSERT INTO Field$
			(Id, Type, Class, DstCls, Name, Custom, CustomId,
			[Min], [Max], [Big], UserLabel, HelpString, ListRootId, WsSelector)
		SELECT
			Id, Type, Class, DstCls, @NewFieldName, Custom, CustomId,
			[Min], [Max], [Big], UserLabel, HelpString, ListRootId, WsSelector
		FROM @Field;

		--( Restore the data
		IF @Type NOT IN (13, 17)
			SET @Sql = N'UPDATE '+ @ClassName + N' SET ' + @NewFieldName + N' = Temp ' +
				N'FROM ' + @ClassName + N' AS c ' +
				N'JOIN #Temp' + CAST(@Type AS NVARCHAR(2)) + N' AS t ON t.Id = c.Id';
		ELSE
			SET @Sql = N'UPDATE '+ @ClassName + N' SET ' +
					@NewFieldName + N' = Temp, ' +
					@NewFieldName + N'_Fmt = Temp_Fmt ' +
				N'FROM ' + @ClassName + N' AS c ' +
				N'JOIN #Temp' + CAST(@Type AS NVARCHAR(2)) + N' AS t ON t.Id = c.Id';
		EXEC (@Sql);

		SET @Sql = N'DROP TABLE #Temp' + CAST(@Type AS NVARCHAR(2));
		EXEC (@Sql);
	END

	--==( Clean up )==--

	GO
	ENABLE TRIGGER TR_Field$_No_Upd ON Field$;
	ALTER TABLE Field$ CHECK CONSTRAINT ALL;
GO

-------------------------------------------------------------------------------

-- FWC-10: Change class name CmDomainQuestion to CmDomainQ
--                            FsFeatureDefn	to FsFeatDefn
--		                      FsFeatureStructure to	FsFeatStruc
--                            FsFeatureStructureDisJunction to FsFeatStrucDisj
--                            FsFeatureStructureType to FsFeatStrucType
--                            FsSymbolicFeatureValue to FsSymFeatVal
--                            LanguageProject to LangProject
--                            LexicalDatabase to LexDb
--                            LexReferenceType to LexRefType
--                            MoAdhocCoProhibition to MoAdhocProhib
--                            MoAdhocCoProhibitionGroup to MoAdhocProhibGR
--                            MoAllomorphAdhocCoProhibition to MoAlloAdhocProhib
--                            MoDerivation to MoDeriv
--                            MoDerivationalAffixApp to MoDerivAffApp
--                            MoDerivationalAffixMsa to MoDerivAffMsa
--                            MoDerivationalStepMSA to MoDerivStepMsa
--                            MoDerivationTrace to MoDerivTrace
--                            MoEndocentricCompound to MoEndoCompound
--                            MoExocentricCompound to MoExoCompound
--                            MoInflectionalAffixMsa to MoInflAffMsa
--                            MoInflectionClass to MoInflClass
--                            MoMorphemeAdHocCoProhibition to MoMorphAdhocProhib
--                            MoMorphologicalData to MoMorphData  
--                            MoMorphoSyntaxAnalysis to MoMorphSynAnalysis
--                            PhPhonologicalContext to PhPhonContext
--                            PhPhonologicalData to PhPhonData
--                            PubHeaderFooterSet to PubHFSet
--                            RnGenericRecord to RnGenericRec
--                            RnResearchNotebook to RnResearchNbk
--                            RnRoledParticipants to RnRoledPartic
--                            ScrImportSettings to ScrImportSet
--                            UserAppFeatureActivated to UserAppFeatAct
--                            WordformLookupItem to WordFormLookup
--         Change Column Names CurrentAnalysisWritingSystems to CurAnalysisWss
--								CurrentPronunciationWritingSystems to CurPronunWss
--								CurrentVernacularWritingSystems to CurVernWss
--								VernacularWritingSystems to VernWss
--								InflectionalFeatures to InflectionalFeats
--								ProductivityRestrictions to ProdRestrict
--								FeatureStructureFragment to FeatStructFrag
--								FromProductivityRestrictions to FromProdRestrict
--								DerivationalAffixApps to DerivAffApp
--								InflectableFeatures to InflectableFeats
--								InherentFeatureValues to InherFeatVal
--								HeaderFooterSettings to HFSet
--								ScriptureNoteCategories to NoteCategories
--								InflectionalTemplateApps to InflTemplateApps
--								RestOfAllomorphs to RestOfAllos
--								RestOfMorphemes to RestOfMorphs
--								LexicalDatabase to LexDb
--								AnalysisWritingSystems to AnalysisWss
--								AnnotationDefinitions to AnnotationDefs


-------------------------------------------------------------------------------

EXECUTE RenameClass 67, 'CmDomainQ'
EXECUTE RenameClass 55, 'FsFeatDefn'
EXECUTE RenameClass 57, 'FsFeatStruc'
EXECUTE RenameClass 58, 'FsFeatStrucDisj'
EXECUTE RenameClass 59, 'FsFeatStrucType'
EXECUTE RenameClass 65, 'FsSymFeatVal'
EXECUTE RenameClass 6001, 'LangProject'
EXECUTE RenameClass 5005, 'LexDb'
EXECUTE RenameClass 5119, 'LexRefType'
EXECUTE RenameClass 5026, 'MoAdhocProhib'
EXECUTE RenameClass 5110, 'MoAdhocProhibGR'
EXECUTE RenameClass 5101, 'MoAlloAdhocProhib'
EXECUTE RenameClass 5100, 'MoDeriv'
EXECUTE RenameClass 5074, 'MoDerivAffApp'
EXECUTE RenameClass 5031, 'MoDerivAffMsa'
EXECUTE RenameClass 5032, 'MoDerivStepMsa'
EXECUTE RenameClass 5072, 'MoDerivTrace'
EXECUTE RenameClass 5033, 'MoEndoCompound'
EXECUTE RenameClass 5034, 'MoExoCompound'
EXECUTE RenameClass 5038, 'MoInflAffMsa'
EXECUTE RenameClass 5039, 'MoInflClass'
EXECUTE RenameClass 5102, 'MoMorphAdhocProhib'
EXECUTE RenameClass 5040, 'MoMorphData'
EXECUTE RenameClass 5041, 'MoMorphSynAnalysis'
EXECUTE RenameClass 5081, 'PhPhonContext'
EXECUTE RenameClass 5099, 'PhPhonData'
EXECUTE RenameClass 45, 'PubHFSet'
EXECUTE RenameClass 4004, 'RnGenericRec'
EXECUTE RenameClass 4001, 'RnResearchNbk'
EXECUTE RenameClass 4010, 'RnRoledPartic'
EXECUTE RenameClass 3008, 'ScrImportSet'
EXECUTE RenameClass 41, 'UserAppFeatAct'
EXECUTE RenameClass 5064, 'WordFormLookup'

EXECUTE RenameField 6001047, 'AnnotationDefs'
EXECUTE RenameField 6001017, 'AnalysisWss'
EXECUTE RenameField 6001019, 'CurAnalysisWss'
EXECUTE RenameField 6001020, 'CurPronunWss'
EXECUTE RenameField 6001018, 'CurVernWss'
EXECUTE RenameField 5078003, 'DerivAffApp'
EXECUTE RenameField 5109008, 'FeatStructFrag'
EXECUTE RenameField 5031013, 'FromProdRestrict'
EXECUTE RenameField 5038008, 'FromProdRestrict'
EXECUTE RenameField 5030008, 'ToProdRestrict'
EXECUTE RenameField 5031014, 'ToProdRestrict'
EXECUTE RenameField 43005, 'HFSet'
EXECUTE RenameField 5049009, 'InflectableFeats'
EXECUTE RenameField 5100003, 'InflectionalFeats'
EXECUTE RenameField 5059013, 'InflTemplateApps'
EXECUTE RenameField 5049001, 'InherFeatVal'
EXECUTE RenameField 6001014, 'LexDb'
EXECUTE RenameField 3001025, 'NoteCategories'
EXECUTE RenameField 5001006, 'ProdRestrict'
EXECUTE RenameField 5032006, 'ProdRestrict'
EXECUTE RenameField 5040009, 'ProdRestrict'
EXECUTE RenameField 5101003, 'RestOfAllos'
EXECUTE RenameField 5102003, 'RestOfMorphs'
EXECUTE RenameField 6001041, 'VernWss'

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200202
BEGIN
	UPDATE Version$ SET DbVer = 200203
	COMMIT TRANSACTION
	PRINT 'database updated to version 200203'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200202 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
