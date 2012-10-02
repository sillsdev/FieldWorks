-- update database from version 200026 to 200027
BEGIN TRANSACTION  --( will be rolled back if wrong version#

--( Added a new stored procedure for use in the new Undo/Redo code.

IF OBJECT_ID('GetUndoDelObjInfo') is not null BEGIN
	PRINT 'removing proc GetUndoDelObjInfo'
	DROP PROC GetUndoDelObjInfo
END
GO
PRINT 'creating proc GetUndoDelObjInfo'
GO
CREATE PROC GetUndoDelObjInfo
	@ObjId int=null,
	@hXMLDocObjList int=null
AS
	-- make sure that nocount is turned on
	DECLARE @fIsNocountOn INT
	SET @fIsNocountOn = @@options & 512
	IF @fIsNocountOn = 0 SET NOCOUNT ON

	CREATE TABLE  #ObjInfoForDelete (
		ObjId			INT NOT NULL,
		ObjClass		INT NULL,
		InheritDepth	INT NULL DEFAULT(0),
		OwnerDepth		INT NULL DEFAULT(0),
		Owner			INT NULL,
		OwnerClass		INT NULL,
		OwnFlid			INT NULL,
		OwnOrd			INT NULL,
		OwnPropType		INT NULL,
		OrdKey			VARBINARY(250) NULL DEFAULT(0))
	CREATE NONCLUSTERED INDEX #Ind_ObjInfo_ObjId ON dbo.#ObjInfoForDelete (ObjId)

	INSERT INTO #ObjInfoForDelete
	SELECT * FROM dbo.fnGetOwnedObjects$(
		@ObjId,			-- single objeect id
		@hXMLDocObjList,-- xml list of object ids
		null,			-- we want all owning prop types
		1,				-- we want base class records
		0,				-- but not subclasses
		1,				-- we want recursion (all owned, not just direct)
		null,			-- we want objects of any class
		0)				-- we don't need an 'order key'

	DECLARE @PropType INT
	SET @PropType = 1
	DECLARE @ClassName NVARCHAR(100)
	DECLARE @FieldName NVARCHAR(100)
	DECLARE @Flid INT

	DECLARE @props TABLE(type INT, flid INT)
	INSERT INTO @props
	SELECT DISTINCT f.type, f.id
	FROM Field$ f
	JOIN #ObjInfoForDelete oo ON oo.ObjClass = f.class

	DECLARE @sQry NVARCHAR(4000)
	DECLARE @sPropType NVARCHAR(20)
	DECLARE @sFlid NVARCHAR(20)

	SELECT TOP 1 @flid = flid, @PropType = type FROM @props ORDER BY flid
	WHILE @@rowcount > 0
	BEGIN
		SELECT @FieldName = f.Name, @Flid = f.Id, @ClassName = c.Name
		FROM Field$ f
		JOIN Class$ c ON c.Id = f.Class
		WHERE f.Type = @PropType AND f.Id = @Flid
		SET @sPropType = CONVERT(NVARCHAR(20), @PropType)
		SET @sFlid = CONVERT(NVARCHAR(20), @Flid)

		SET @sQry = 'insert into #UndoDelObjInfo select ' + @sPropType + ',Id,' + @sFlid + ', '
		IF @PropType in (1,2,8,24) BEGIN	-- Boolean, Integer, GenDate, RefAtomic
			SET @sQry = @sQry +
				'[' + @FieldName + '], null, null, null, null, null ' +
				'from ' + @ClassName + ' where Id in (select ObjId from #ObjInfoForDelete) ' +
				'and [' + @FieldName + '] is not null'
		END
		-- 3 (Numeric) and 4 (Float) are never used (as of January 2005)
		ELSE IF @PropType = 5 BEGIN		-- Time
			SET @sQry = @sQry +
				'null, [' + @FieldName + '], null, null, null, null ' +
				'from ' + @ClassName + ' where Id in (select ObjId from #ObjInfoForDelete) ' +
				'and [' + @FieldName + '] is not null'
		END
		ELSE IF @PropType = 6 BEGIN		-- Guid
			SET @sQry = @sQry +
				'null, null, [' + @FieldName + '], null, null, null ' +
				'from ' + @ClassName + ' where Id in (select ObjId from #ObjInfoForDelete) ' +
				'and [' + @FieldName + '] is not null'
		END
		ELSE IF @PropType in (7,9) BEGIN	-- Image, Binary
			SET @sQry = @sQry +
				'null, null, null, [' + @FieldName + '], null, null ' +
				'from ' + @ClassName + ' where Id in (select ObjId from #ObjInfoForDelete) ' +
				'and [' + @FieldName + '] is not null'
		END
		-- 10,11,12 are unassigned values (as of January 2005)
		ELSE IF @PropType in (13,17) BEGIN		-- String, BigString
			SET @sQry = @sQry + 'null, null, null, ' +
				@FieldName + '_Fmt, [' + @FieldName + '], null ' +
				'from ' + @ClassName + ' where Id in (select ObjId from #ObjInfoForDelete) ' +
				'and [' + @FieldName + '] is not null'
		END
		ELSE IF @PropType in (14,18) BEGIN		-- MultiString, MultiBigString
			SET @sQry = 'insert into #UndoDelObjInfo select ' + @sPropType + ',Obj,' + @sFlid +
				', Ws, null, null, Fmt, Txt, null ' +
				'from ' + @ClassName + '_' + @FieldName +
				' where Obj in (select ObjId from #ObjInfoForDelete) and Txt is not null'
		END
		ELSE IF @PropType in (15,19) BEGIN		-- Unicode, BigUnicode
			SET @sQry = @sQry +
				'null, null, null, null, [' + @FieldName + '], null ' +
				'from ' + @ClassName + ' where Id in (select ObjId from #ObjInfoForDelete) ' +
				'and [' + @FieldName + '] is not null'
		END
		ELSE IF @PropType in (16,20) BEGIN		-- MultiUnicode, MultiBigUnicode
			-- (MultiBigUnicode is unused as of January 2005)
			SET @sQry = 'insert into #UndoDelObjInfo select ' + @sPropType + ',Obj,' + @sFlid +
				', Ws, null, null, null, Txt, null ' +
				'from ' + @ClassName + '_' + @FieldName +
				' where Obj in (select ObjId from #ObjInfoForDelete) and Txt is not null'
		END
		-- 21,22 are unassigned (as of January 2005)
		-- 23,25,27 are Owning Properties, which are handled differently from Value/Reference
		--          Properties
		ELSE IF @PropType = 26 BEGIN		-- RefCollection
			SET @sQry = 'insert into #UndoDelObjInfo select ' + @sPropType + ',Src,' + @sFlid +
				', Dst, null, null, null, null, null ' +
				'from ' + @ClassName + '_' + @FieldName +
				' where Src in (select ObjId from #ObjInfoForDelete)'
		END
		ELSE IF @PropType = 28 BEGIN		-- RefSequence
			SET @sQry = 'insert into #UndoDelObjInfo select ' + @sPropType + ',Src,' + @sFlid +
				', Dst, null, null, null, null, Ord ' +
				'from ' + @ClassName + '_' + @FieldName +
				' where Src in (select ObjId from #ObjInfoForDelete) order by Ord'
		END
		ELSE BEGIN
			SET @sQry = null
		END
		IF (@sQry is not null) BEGIN
		--	PRINT @sQry
			EXEC (@sQry)
		END
		SELECT TOP 1 @flid = flid, @PropType = type FROM @props WHERE flid > @flid ORDER BY flid
	END

	-- Now do incoming references. Note that we exclude references where the SOURCE of the
	-- reference is in the deleted object collection, as those references will be reinstated
	-- by code restoring the forward ref properties.  Incoming references are marked in the
	-- table by negative values in the Type field.

	DELETE FROM @props
	INSERT INTO @props
	SELECT DISTINCT f.type, f.id
	FROM Field$ f
	JOIN #ObjInfoForDelete oo ON oo.ObjClass = f.DstCls AND f.type IN (24, 26, 28)

	SELECT TOP 1 @flid = flid, @PropType = type FROM @props ORDER BY flid
	WHILE @@rowcount > 0
	BEGIN
		SELECT @FieldName = f.Name, @Flid = f.Id, @ClassName = c.Name
		FROM Field$ f
		JOIN Class$ c ON c.Id = f.Class
		WHERE f.Type = @PropType AND f.Id = @Flid
		SET @sPropType = CONVERT(NVARCHAR(20), @PropType)
		SET @sFlid = CONVERT(NVARCHAR(20), @Flid)

		IF @PropType = 24 BEGIN				-- RefAtomic
			SET @sQry = 'insert into #UndoDelObjInfo select -' + @sPropType +
				', [' + @FieldName + '], ' + @sFlid +
				', Id, null, null, null, null, null ' +
				'from ' + @ClassName +
				' where [' + @FieldName + '] in (select ObjId from #ObjInfoForDelete)' +
				' and Id not in (select ObjId from #ObjInfoForDelete)'
		END
		ELSE IF @PropType = 26 BEGIN		-- RefCollection
			SET @sQry = 'insert into #UndoDelObjInfo select -' + @sPropType + ',Dst,' + @sFlid +
				', Src, null, null, null, null, null ' +
				'from ' + @ClassName + '_' + @FieldName +
				' where Dst in (select ObjId from #ObjInfoForDelete)' +
				' and Src not in (select ObjId from #ObjInfoForDelete)' +
				' order by Src'
		END
		ELSE IF @PropType = 28 BEGIN		-- RefSequence
			SET @sQry = 'insert into #UndoDelObjInfo select -' + @sPropType + ',Dst,' + @sFlid +
				', Src, null, null, null, null, Ord ' +
				'from ' + @ClassName + '_' + @FieldName +
				' where Dst in (select ObjId from #ObjInfoForDelete)' +
				' and Src not in (select ObjId from #ObjInfoForDelete)' +
				' order by Src, Ord'
		END
		ELSE BEGIN
			SET @sQry = null
		END
		IF (@sQry is not null) BEGIN
		--	PRINT @sQry
			EXEC (@sQry)
		END
		SELECT TOP 1 @flid = flid, @PropType = type FROM @props WHERE flid > @flid ORDER BY flid
	END

	-- if we turned on nocount, turn it off
	IF @fIsNocountOn = 0 SET NOCOUNT OFF

	RETURN @@error
GO

declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200026
begin
	update Version$ set DbVer = 200027
	COMMIT TRANSACTION
	print 'database updated to version 200027'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200026 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
