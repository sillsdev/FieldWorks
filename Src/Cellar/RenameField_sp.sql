/******************************************************************************
* Procedure: RenameField
*
* Description:
*	RenameField changes a column/field name.
*
* Parameters:
*	@FieldId
*	@NewFieldName
*
* Returns:
*	Nothing
*
* Calling sample:
*	EXECUTE RenameField
*
* Warning:
*	See warning message below in the code.
*
* Notes:
*	This is a utility procedure, built to shorten a bunch of field names
*	for the Firebird port. It is not called by anything in the system, but
*	included as	a useful tool.
******************************************************************************/

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

		--( Rebuild MakeObj_*	Fired in the Field$ insert last trigger
		--( TR_Field$UpdateModel_InsLast:

		EXEC GenMakeObjProc @ClassId;

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
