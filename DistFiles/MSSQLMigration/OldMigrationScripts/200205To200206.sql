-- Update database from version 200205 to 200206
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

----------------------------------------------------------------------------------------
-- TE-6398: Add ScrCheckRun CheckId, RunDate, and Result
----------------------------------------------------------------------------------------

DECLARE @ClassId INT
SET @ClassId = NULL
SELECT @ClassId = Id FROM Field$ WHERE Id = 3019001
IF @ClassId IS NULL
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(3019001, 6, 3019,
			null, 'CheckId',0,Null, null, null, null)
SET @ClassId = NULL
SELECT @ClassId = Id FROM Field$ WHERE Id = 3019002
IF @ClassId IS NULL
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(3019002, 5, 3019,
			null, 'RunDate',0,Null, null, null, null)
SET @ClassId = NULL
SELECT @ClassId = Id FROM Field$ WHERE Id = 3019003
IF @ClassId IS NULL
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(3019003, 2, 3019,
			null, 'Result',0,Null, null, null, null)
go

----------------------------------------------------------------------------------------
-- Add missing SetAgentEval
----------------------------------------------------------------------------------------
if object_id('SetAgentEval') is not null begin
	drop proc SetAgentEval
end
go

CREATE PROC SetAgentEval
	@nAgentID INT,
	@nTargetID INT, --( A WfiAnalysis.ID or a WfiWordform.ID
	@nAccepted INT,
	@nvcDetails NVARCHAR(4000),
	@dtEval DATETIME
AS
	DECLARE
		@nIsNoCountOn INT,
		@nTranCount INT,
		@sysTranName SYSNAME,
		@nEvals INT,
		@nEvalId INT,
		@nNewObjId INT,
		@guidNewObj UNIQUEIDENTIFIER,
		@nNewObjTimeStamp INT,
		@nError INT,
		@nvcError NVARCHAR(100)

	SET @nError = 0

	SET @nIsNoCountOn = @@OPTIONS & 512
	IF @nIsNoCountOn = 0
		SET NOCOUNT ON

	--( Take care of transaction stuff
	SET @nTranCount = @@TRANCOUNT
	SET @sysTranName = 'SetAgentEval_tr' + CONVERT(VARCHAR(2), @@NESTLEVEL)
	IF @nTranCount = 0
		BEGIN TRAN @sysTranName
	ELSE
		SAVE TRAN @sysTranName

	--( See if we have an Agent Evaluation already
	SELECT TOP 1 @nEvalId = co.[Id]
	FROM CmAgentEvaluation cae
	JOIN CmObject co ON co.[Id] = cae.[Id]
		AND co.Owner$ = @nAgentID
	WHERE cae.Target = @nTargetID
	ORDER BY co.[Id]

	SET @nEvals = @@ROWCOUNT

	--== Remove Eval ==--

	--( If we don't know if the analysis is accepted or not,
	--( we don't really have an eval for it. And if we don't
	--( have an eval for it, we need to get rid of it.

	IF @nAccepted = 2 OR @nAccepted IS NULL BEGIN
		WHILE @nEvals > 0 BEGIN
			EXEC DeleteObj$ @nEvalId

			SELECT TOP 1 @nEvalId = co.[Id]
			FROM CmAgentEvaluation cae
			JOIN CmObject co ON co.[Id] = cae.[Id]
				AND co.Owner$ = @nAgentID
			WHERE cae.Target = @nTargetID
				AND co.[Id] > @nEvalId
			ORDER BY co.[Id]

			SET @nEvals = @@ROWCOUNT
		END
	END

	--== Create or Update Eval ==--

	--( Make sure the evaluation is set the way it should be.

	ELSE BEGIN

		--( Create a new Agent Evaluation
		IF @nEvals = 0 BEGIN

			EXEC @nError = CreateObject_CmAgentEvaluation
				@dtEval,
				@nAccepted,
				@nvcDetails,
				@nAgentId,					--(owner
				23006,	--(ownflid  23006
				NULL,						--(startobj
				@nNewObjId OUTPUT,
				@guidNewObj OUTPUT,
				0,			--(ReturnTimeStamp
				@nNewObjTimeStamp OUTPUT

			IF @nError != 0 BEGIN
				SET @nvcError = 'SetAgentEval: CreateObject_CmAgentEvaluation failed.'
				GOTO Fail
			END

			UPDATE CmAgentEvaluation  WITH (REPEATABLEREAD)
			SET Target = @nTargetID
			WHERE Id = @nNewObjId
		END

		--( Update the existing Agent Evaluation
		ELSE

			UPDATE CmAgentEvaluation WITH (REPEATABLEREAD)
			SET
				DateCreated = @dtEval,
				Accepted = @nAccepted,
				Details = @nvcDetails
			FROM CmAgentEvaluation cae
			JOIN CmObject co ON co.[Id] = cae.[Id]
				AND co.Owner$ = @nAgentID
			WHERE cae.Target = @nTargetID
		--( END
	END
	GOTO Finish

Finish:

	SET @nIsNoCountOn = @@OPTIONS & 512
	IF @nIsNoCountOn = 0
		SET NOCOUNT ON

	-- determine if a transaction or savepoint was created
	IF @nTranCount = 0
		COMMIT TRAN @sysTranName

	RETURN @nError

Fail:
	RAISERROR (@nvcError, 16, 1, @nError)
	IF @nTranCount !=0
		ROLLBACK TRAN @sysTranName

	RETURN @nError

GO

----------------------------------------------------------------------------------------
-- Delete old renamed procedures
----------------------------------------------------------------------------------------
if object_id('DisplayName_PhPhonologicalContext') is not null begin
	drop proc [DisplayName_PhPhonologicalContext]
end
go

if object_id('DisplayName_PhPhonologicalContextID') is not null begin
	drop proc [DisplayName_PhPhonologicalContextID]
end
go

----------------------------------------------------------------------------------------
-- Replace PATRString_FsFeatureStructure with PATRString_FsFeatStruc
----------------------------------------------------------------------------------------
if object_id('PATRString_FsFeatureStructure') is not null begin
	drop proc [PATRString_FsFeatureStructure]
end
go
if object_id('PATRString_FsFeatStruc') is not null begin
	drop proc [PATRString_FsFeatStruc]
end
go

create proc PATRString_FsFeatStruc
	@XMLOut bit = 0,
	@XMLIds ntext = null
as
	declare @retval int,
		@CurId int, @Txt nvarchar(4000),
		@fIsNocountOn int

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	--Table variable.
	declare @FS table (
		Id int,
		PATRTxt nvarchar(4000) )

	if @XMLIds is null begin
		-- Do all feature structures.
		insert into @FS (Id, PATRTxt)
			select	Id, '[]'
			from	FsFeatStruc_
			where OwnFlid$ != 2005001 -- Owned by FsComplexValue
				and OwnFlid$ != 2010001 -- Owned by FsFeatStrucDisj
	end
	else begin
		-- Do feature structures provided in xml string.
		declare @hdoc int
		exec sp_xml_preparedocument @hdoc output, @XMLIds
		if @@error <> 0 begin
			set @retval = 1
			goto LExit
		end
		insert into @FS (Id, PATRTxt)
			select	ol.[Id], '[]'
			from	openxml(@hdoc, '/FeatureStructures/FeatureStructure')
			with ([Id] int) as ol
			-- Remove all pretenders, since they won't 'join'.
			join CmObject cmo
				On ol.Id=cmo.Id
				and cmo.Class$=2009 -- Check for class being FsFeatStruc
				and cmo.OwnFlid$ != 2005001 -- Owned by FsComplexValue
				and cmo.OwnFlid$ != 2010001 -- Owned by FsFeatStrucDisj
		-- Turn loose of the handle
		exec sp_xml_removedocument @hdoc
		if @@error <> 0 begin
			set @retval = 1
			goto LExit
		end
	end

	-- Loop through all ids.
	select top 1 @CurId = Id
	from @FS
	order by Id
	while @@rowcount > 0 begin
		-- Call PATRString_FsAbstractStructure for each ID. It will return the PATR string.
		exec @retval = PATRString_FsAbstractStructure '', @CurId, @Txt output
		-- Note: If @retval is not 0, then we already are set to use '[]'
		-- for the string, so nothing mnore need be done.
		if @retval = 0 begin
			update @FS
			Set PATRTxt = @Txt
			where Id = @CurId
		end
		-- Try for another one.
		select top 1 @CurId = Id
		from @FS
		where Id > @CurId
		order by Id
	end

	if @XMLOut = 0
		select * from @FS
	else
		select * from @FS for xml auto
	set @retval = 0
LExit:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @retval
go

-------------------------------------------------------------------------------
-- Repair RenameClass SP
-------------------------------------------------------------------------------
IF OBJECT_ID('RenameClass') IS NOT NULL BEGIN
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
-- Regenerate all triggers affected by name changes.
-------------------------------------------------------------------------------
DECLARE @ClassId INT;
DECLARE Classes CURSOR FOR SELECT Id FROM Class$ WHERE Abstract = 0; OPEN Classes; FETCH NEXT FROM Classes INTO @ClassId; WHILE @@FETCH_STATUS = 0 BEGIN
	EXEC CreateDeleteObj @ClassId;
	FETCH NEXT FROM Classes INTO @ClassId;
END;
CLOSE Classes;
DEALLOCATE Classes;
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200205
BEGIN
	UPDATE Version$ SET DbVer = 200206
	COMMIT TRANSACTION
	PRINT 'database updated to version 200206'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200205 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
