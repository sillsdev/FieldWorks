-- Update database from version 200208 to 200209
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- FWNX-47: Create missing Foreign Keys for FsFeatDefn, MoDerivTrace,
-- MoMorphSynAnalysis, PhPhonContext, RnGenericRec
-------------------------------------------------------------------------------

IF OBJECT_ID('TempCreateKeys') IS NOT NULL BEGIN
	PRINT 'removing procedure TempCreateKeys';
	DROP PROCEDURE TempCreateKeys;
END
GO
PRINT 'creating procedure TempCreateKeys';
GO

CREATE PROCEDURE TempCreateKeys
	@ClassId INT,
	@NewClassName NVARCHAR(100)
AS

	--==( Setup )==--

	DECLARE
		@ForeignKey NVARCHAR(100),
		@ClassName NVARCHAR(100),
		@Sql NVARCHAR(4000),
		@Debug BIT

	SET @Debug = 0

	--( Rebuild subclass referencing constraints

	SET @sql = N' '
	DECLARE Subclasses CURSOR FOR SELECT Name FROM Class$ WHERE Base = @ClassId;
	OPEN Subclasses
	FETCH NEXT FROM Subclasses INTO @ClassName
	WHILE @@FETCH_STATUS = 0 BEGIN
		SET @ForeignKey = N'_FK_' + @ClassName + N'_Id';
		IF object_id(@ForeignKey, N'F') is null BEGIN
			SET @sql = @Sql + N'ALTER TABLE ' + @ClassName + N' WITH CHECK ADD CONSTRAINT ' + @ForeignKey +
					   N' FOREIGN KEY ([Id]) REFERENCES ' + @NewClassName + N' ([Id]);';

			If @Debug = 1
				Print 'SQL: ' + @sql;

			EXECUTE sp_executesql @sql;
			SET @sql = N' '
		END

		FETCH NEXT FROM Subclasses INTO @ClassName
	END
	CLOSE Subclasses
	DEALLOCATE Subclasses
GO

-------------------------------------------------------------------

EXECUTE TempCreateKeys 67, 'CmDomainQ'
EXECUTE TempCreateKeys 55, 'FsFeatDefn'
EXECUTE TempCreateKeys 57, 'FsFeatStruc'
EXECUTE TempCreateKeys 58, 'FsFeatStrucDisj'
EXECUTE TempCreateKeys 59, 'FsFeatStrucType'
EXECUTE TempCreateKeys 65, 'FsSymFeatVal'
EXECUTE TempCreateKeys 6001, 'LangProject'
EXECUTE TempCreateKeys 5005, 'LexDb'
EXECUTE TempCreateKeys 5119, 'LexRefType'
EXECUTE TempCreateKeys 5026, 'MoAdhocProhib'
EXECUTE TempCreateKeys 5110, 'MoAdhocProhibGR'
EXECUTE TempCreateKeys 5101, 'MoAlloAdhocProhib'
EXECUTE TempCreateKeys 5100, 'MoDeriv'
EXECUTE TempCreateKeys 5074, 'MoDerivAffApp'
EXECUTE TempCreateKeys 5031, 'MoDerivAffMsa'
EXECUTE TempCreateKeys 5032, 'MoDerivStepMsa'
EXECUTE TempCreateKeys 5072, 'MoDerivTrace'
EXECUTE TempCreateKeys 5033, 'MoEndoCompound'
EXECUTE TempCreateKeys 5034, 'MoExoCompound'
EXECUTE TempCreateKeys 5038, 'MoInflAffMsa'
EXECUTE TempCreateKeys 5039, 'MoInflClass'
EXECUTE TempCreateKeys 5102, 'MoMorphAdhocProhib'
EXECUTE TempCreateKeys 5040, 'MoMorphData'
EXECUTE TempCreateKeys 5041, 'MoMorphSynAnalysis'
EXECUTE TempCreateKeys 5081, 'PhPhonContext'
EXECUTE TempCreateKeys 5099, 'PhPhonData'
EXECUTE TempCreateKeys 45, 'PubHFSet'
EXECUTE TempCreateKeys 4004, 'RnGenericRec'
EXECUTE TempCreateKeys 4001, 'RnResearchNbk'
EXECUTE TempCreateKeys 4010, 'RnRoledPartic'
EXECUTE TempCreateKeys 3008, 'ScrImportSet'
EXECUTE TempCreateKeys 41, 'UserAppFeatAct'
EXECUTE TempCreateKeys 5064, 'WordFormLookup'

IF OBJECT_ID('TempCreateKeys') IS NOT NULL BEGIN
	PRINT 'removing procedure TempCreateKeys';
	DROP PROCEDURE TempCreateKeys;
END
GO

---------------------------------------------------------------------------------

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
		@ForeignKey NVARCHAR(100),
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
	SELECT @OldClassName = Name, @Abstract = Abstract  FROM Class$ WHERE Id = @ClassId

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

	-- This sp doesn't exist for abstract classes
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

	--( Rebuild subclass referencing constraints

	SET @sql = N' '
	DECLARE Subclasses CURSOR FOR SELECT Name FROM Class$ WHERE Base = @ClassId;
	OPEN Subclasses
	FETCH NEXT FROM Subclasses INTO @ClassName
	WHILE @@FETCH_STATUS = 0 BEGIN
		SET @ForeignKey = N'_FK_' + @ClassName + N'_Id';
		IF object_id(@ForeignKey, N'F') is null BEGIN
			SET @sql = @Sql + N'ALTER TABLE ' + @ClassName + N' WITH CHECK ADD CONSTRAINT ' + @ForeignKey +
					   N' FOREIGN KEY ([Id]) REFERENCES ' + @NewClassName + N' ([Id]);';

			If @Debug = 1
				Print 'SQL: ' + @sql;

			EXECUTE sp_executesql @sql;
			SET @sql = N' '
		END

		FETCH NEXT FROM Subclasses INTO @ClassName
	END
	CLOSE Subclasses
	DEALLOCATE Subclasses

	--( Rebuild the function fnGetRefsToObj

	EXEC CreateGetRefsToObj;

	IF @Abstract = N'0' BEGIN

		--( Rebuild CreateObject_*

		EXEC DefineCreateProc$ @ClassId;

		--( Rebuild the delete object trigger. It was regenerated once already,
		--( but it picks up collection and sequence references from the dummy
		--( class gets picked up.

		EXEC CreateDeleteObj @ClassId;
	END

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
		ALTER TABLE MultiBigTxt$ CHECK CONSTRAINT ALL;';
	EXEC (@Sql);
GO

-------------------------------------------------------------------------------
-- FWNX-44: Shorten ConvertCVNumsToEuropeanDigitsOnExport
-------------------------------------------------------------------------------

EXECUTE RenameField 3001018, 'ConvertCVDigitsOnExport'

-------------------------------------------------------------------------------
-- FWNX-54: View Names too Long
-------------------------------------------------------------------------------

EXECUTE RenameField 5032003, 'InflFeats'
EXECUTE RenameField 5038001, 'InflFeats'
EXECUTE RenameField 5124001, 'ConstChartTempl'

-------------------------------------------------------------------------------
-- FWNX-50: Length of Names Still too Long
-------------------------------------------------------------------------------

EXECUTE RenameField 55006, 'RightGlossSep'
EXECUTE RenameField 65005, 'RightGlossSep'
EXECUTE RenameField 5086003, 'MinusContr'
EXECUTE RenameField 5086002, 'PlusContr'
EXECUTE RenameField 41001, 'UserConfigAcct'

EXECUTE RenameClass 40, 'UserConfigAcct' --( for consistency

GO

---------------------------------------------------------------------------------
---- Finish or roll back transaction as applicable
---------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200208
BEGIN
	UPDATE Version$ SET DbVer = 200209
	COMMIT TRANSACTION
	PRINT 'database updated to version 200209'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200208 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
