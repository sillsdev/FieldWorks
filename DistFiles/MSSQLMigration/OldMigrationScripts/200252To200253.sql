-- Update database from version 200252 to 200253
BEGIN TRANSACTION  --( will be rolled back if wrong version #)
-------------------------------------------------------------------------------
-- Fixed LT-9500 and modified stored proc to allow selection of what parses are deleted
-- based on the kind of parser being used.
-------------------------------------------------------------------------------

-- Fix trigger so RenameField will work.
if object_id('TR_Field$_UpdateModel_Del') is not null begin
	print 'removing trigger TR_Field$_UpdateModel_Del'
	drop trigger TR_Field$_UpdateModel_Del
end
go
print 'creating trigger TR_Field$_UpdateModel_Del'
go
create trigger TR_Field$_UpdateModel_Del on Field$ for delete
as
	declare @Clid INT
	declare @DstCls INT
	declare @sName VARCHAR(100)
	declare @sClass VARCHAR(100)
	declare @sFlid VARCHAR(20)
	declare @Type INT
	DECLARE @nAbstract INT

	declare @Err INT
	declare @fIsNocountOn INT
	declare @sql VARCHAR(1000)

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- get the first custom field to process
	Select @sFlid= min([id]) from deleted

	-- loop through all of the custom fields to be deleted
	while @sFlid is not null begin

		-- get deleted fields
		select 	@Type = [Type], @Clid = [Class], @sName = [Name], @DstCls = [DstCls]
		from	deleted
		where	[Id] = @sFlid

		-- get class name
		select 	@sClass = [Name], @nAbstract = Abstract  from class$  where [Id] = @Clid

		if @type IN (14,16,18,20) begin
			-- Remove any data stored for this multilingual custom field.
			declare @sTable VARCHAR(20)
			set @sTable = case @type
				when 14 then 'MultiStr$'
				when 16 then 'MultiTxt$ (No Longer Exists)'
				when 18 then 'MultiBigStr$'
				when 20 then 'MultiBigTxt$'
				end
			--( NOTE (SteveMiller/JohnS): The MultiStr$, MultiBigStr$, and
			--( MultiBigTxt$ tables have foreign key constraints on Field$.ID.
			--( This means that the tables in these Multi* tables must be deleted
			--( before the Field$ row can be deleted. That means the delete
			--( command below probably never deletes anything. It's not bad to
			--( leave the code in, just in case something really weird happnes in
			--( the wild.
			IF @type != 16  -- MultiTxt$ data will be deleted when the table is dropped
			BEGIN
				set @sql = 'DELETE FROM [' + @sTable + '] WHERE [Flid] = ' + @sFlid
				exec (@sql)
				set @Err = @@error
				if @Err <> 0 goto LFail
			END

			-- Remove the view created for this multilingual custom field.
			IF @type != 16
				set @sql = 'DROP VIEW [' + @sClass + '_' + @sName + ']'
			ELSE
				SET @sql = 'DROP TABLE [' + @sClass + '_' + @sName + ']'
			exec (@sql)
			set @Err = @@error
			if @Err <> 0 goto LFail
		end
		else if @type IN (23,25,27) begin
			-- Remove the view created for this custom OwningAtom/Collection/Sequence field.
			set @sql = 'DROP VIEW [' + @sClass + '_' + @sName + ']'
			exec (@sql)
			set @Err = @@error
			if @Err <> 0 goto LFail
			-- Check for any objects stored for this custom OwningAtom/Collection/Sequence field.
			declare @DelId INT
			select @DelId = [Id] FROM CmObject WHERE [OwnFlid$] = @sFlid
			set @Err = @@error
			if @Err <> 0 goto LFail
			if @DelId is not null begin
				raiserror('TR_Field$_UpdateModel_Del: Unable to remove %s field until corresponding objects are deleted',
						16, 1, @sName)
				goto LFail
			end
		end
		else if @type IN (26,28) begin
			-- Remove the table created for this custom ReferenceCollection/Sequence field.
			set @sql = 'DROP TABLE [' + @sClass + '_' + @sName + ']'
			exec (@sql)
			set @Err = @@error
			if @Err <> 0 goto LFail

			-- Remove the procedure that handles reference collections or sequences for
			-- the dropped table
			set @sql = N'
				IF OBJECT_ID(''ReplRC_' +
					SUBSTRING(@sClass, 1, 11) +  '_' + SUBSTRING(@sName, 1, 11) +
					''') IS NOT NULL
					DROP PROCEDURE [ReplRC_' + SUBSTRING(@sClass, 1, 11) + '_' + SUBSTRING(@sName, 1, 11) + ']
				IF OBJECT_ID(''ReplRS_' +
					SUBSTRING(@sClass, 1, 11) +  '_' + SUBSTRING(@sName, 1, 11) +
					''') IS NOT NULL
					DROP PROCEDURE [ReplRS_' + SUBSTRING(@sClass, 1, 11) + '_' + SUBSTRING(@sName, 1, 11) + ']'
			exec (@sql)
			set @Err = @@error
			if @Err <> 0 goto LFail
		end
		else begin
			-- Remove the format column created if this was a custom String field.
			if @type in (13,17) begin
				set @sql = 'ALTER TABLE [' + @sClass + '] DROP COLUMN [' + @sName + '_Fmt]'
				exec (@sql)
				set @Err = @@error
				if @Err <> 0 goto LFail
			end
			-- Remove the constraint created if this was a custom ReferenceAtom field.
			-- Not necessary for CmObject : Foreign Key constraints are not created agains CmObject
			if @type = 24 begin
				declare @sTarget VARCHAR(100)
				select @sTarget = [Name] FROM [Class$] WHERE [Id] = @DstCls
				set @Err = @@error
				if @Err <> 0 goto LFail
				if @sTarget != 'CmObject' begin
					set @sql = 'ALTER TABLE [' + @sClass + '] DROP CONSTRAINT [' +
						'_FK_' + @sClass + '_' + @sName + ']'
					exec (@sql)
					set @Err = @@error
					if @Err <> 0 goto LFail
				end
			end
			-- Remove Default Constraint from Numeric or Date fields before dropping the column
			If @type in (1,2,3,4,5,8) begin
				select @sql = 'ALTER TABLE [' + @sClass + '] DROP CONSTRAINT [' + so.name + ']'
				from sysconstraints sc
					join sysobjects so on so.id = sc.constid and so.name like 'DF[_]%'
					join sysobjects so2 on so2.id = sc.id
					join syscolumns sco on sco.id = sc.id and sco.colid = sc.colid
				where so2.name = @sClass   -- Tablename
				and   sco.name = @sName    -- Fieldname
				and   so2.type = 'U'	   -- Userdefined table
				exec (@sql)
				set @Err = @@error
				if @Err <> 0 goto LFail
			end
			-- Remove Check Constraint from Numeric fields before dropping the column
			If @type = 2 begin
				select @sql = 'ALTER TABLE [' + @sClass + '] DROP CONSTRAINT [' + so.name + ']'
				from sysconstraints sc
					join sysobjects so on so.id = sc.constid and so.name like '_CK_%'
					join sysobjects so2 on so2.id = sc.id
					join syscolumns sco on sco.id = sc.id and sco.colid = sc.colid
				where so2.name = @sClass   -- Tablename
				and   sco.name = @sName    -- Fieldname
				and   so2.type = 'U'	   -- Userdefined table
				exec (@sql)
				set @Err = @@error
				if @Err <> 0 goto LFail
			end

			-- Remove the column created for this custom field.
			set @sql = 'ALTER TABLE [' + @sClass + '] DROP COLUMN [' + @sName + ']'
			exec (@sql)
			set @Err = @@error
			if @Err <> 0 goto LFail

			-- fix the view associated with this class.
			exec @Err = UpdateClassView$ @Clid, 1
			if @Err <> 0 goto LFail
		end

		--( Rebuild the delete trigger

		EXEC @Err = CreateDeleteObj @Clid
		IF @Err <> 0 GOTO LFail

		--( Rebuild MakeObj_*

		IF @nAbstract != 1 BEGIN
			EXEC @Err = GenMakeObjProc @Clid
			IF @Err <> 0 GOTO LFail
		END

		-- get the next custom field to process
		Select @sFlid= min([id]) from deleted  where [Id] > @sFlid

	end -- While loop

	--( Rebuild the stored function fnGetRefsToObj
	EXEC @Err = CreateGetRefsToObj
	IF @Err <> 0 GOTO LFail

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return

LFail:
	rollback tran
	return
go

-- Now fix LT-9500 with correct spelling.
EXECUTE RenameField 5086003, 'MinusConstr'
EXECUTE RenameField 5086002, 'PlusConstr'
go

-- Fix stored procedure to handle more than one computer parser.
if object_id('RemoveParserApprovedAnalyses$') is not null begin
	print 'removing proc RemoveParserApprovedAnalyses$'
	drop proc [RemoveParserApprovedAnalyses$]
end
go
print 'creating proc RemoveParserApprovedAnalyses$'
go

CREATE PROC [RemoveParserApprovedAnalyses$]
	@nWfiWordFormID INT,
	@nParserAgentId INT = null
AS
	DECLARE
		@nIsNoCountOn INT,
		@nGonnerId INT,
		@humanAgentId INT,
		@nError INT,
		@StrId NVARCHAR(20);

	SET @nIsNoCountOn = @@OPTIONS & 512
	IF @nIsNoCountOn = 0
		SET NOCOUNT ON

	SET @nError = 0

	-- table variable to hold non-human CmAgents
	declare @ComputerAgents table (
		id int primary key
		)

	if @nParserAgentId is null begin
		insert into @ComputerAgents (id)
		select id from CmAgent where Human = 0
	end
	else begin
		insert into @ComputerAgents (id)
		select @nParserAgentId
	end

	-- Set checksum to zero
	UPDATE WfiWordform SET Checksum=0 WHERE Id=@nWfiWordFormID

	-- Get Id of the 'default user' agent
	SELECT TOP 1 @humanAgentId = Obj
	FROM CmAgent_Name nme
	WHERE Txt = N'default user'

	--== Delete all parser evaluations that reference analyses owned by the @nWfiWordFormID wordform. ==--
	SELECT TOP 1 @nGonnerId = ae.[Id]
	FROM CmAgentEvaluation ae
	JOIN CmObject objae
		ON objae.[Id] = ae.[Id] AND objae.Owner$ in (select id from @ComputerAgents)
	JOIN CmObject objanalysis
		ON objanalysis.[Id] = ae.Target
		AND objanalysis.Class$ = 5059 -- WfiAnalysis objects
		AND objanalysis.Owner$ = @nWfiWordFormID
	ORDER BY ae.[Id]
	WHILE @@ROWCOUNT != 0 BEGIN
		SET @StrId = CONVERT(NVARCHAR(20), @nGonnerId);
		EXEC @nError = DeleteObjects @StrId;
		IF @nError != 0
			GOTO Finish

		SELECT TOP 1 @nGonnerId = ae.[Id]
		FROM CmAgentEvaluation ae
		JOIN CmObject objae
			ON objae.[Id] = ae.[Id] AND objae.Owner$ in (select id from @ComputerAgents)
		JOIN CmObject objanalysis
			ON objanalysis.[Id] = ae.Target
			AND objanalysis.Class$ = 5059 -- WfiAnalysis objects
			AND objanalysis.Owner$ = @nWfiWordFormID
		WHERE ae.[Id] > @nGonnerId
		ORDER BY ae.[Id]
	END

	--== Delete orphan analyses owned by the given @nWfiWordFormID wordform. ==--
	--== 'Orphan' means they have no evaluations ==--
	SELECT TOP 1 @nGonnerId = analysis.[Id]
	FROM CmObject analysis
	LEFT OUTER JOIN cmAgentEvaluation cae
		ON cae.Target = analysis.[Id]
	WHERE cae.Target IS NULL
		AND analysis.OwnFlid$ = 5062002		-- 5062002
		AND analysis.Owner$ = @nWfiWordFormID
	ORDER BY analysis.[Id]
	WHILE @@ROWCOUNT != 0 BEGIN
		SET @StrId = CONVERT(NVARCHAR(20), @nGonnerId);
		EXEC @nError = DeleteObjects @StrId;
		IF @nError != 0
			GOTO Finish

		SELECT TOP 1 @nGonnerId = analysis.[Id]
		FROM CmObject analysis
		LEFT OUTER JOIN cmAgentEvaluation cae
			ON cae.Target = analysis.[Id]
		WHERE cae.Target IS NULL
			AND analysis.[Id] > @nGonnerId
			AND analysis.OwnFlid$ = 5062002		-- 5062002
			AND analysis.Owner$ = @nWfiWordFormID
		ORDER BY analysis.[Id]
	END

Finish:
	IF @nIsNocountOn = 0 SET NOCOUNT OFF
	RETURN @nError

GO


-----------------------------------------------------------------------------------
------ Finish or roll back transaction as applicable
-----------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200252
BEGIN
	UPDATE Version$ SET DbVer = 200253
	COMMIT TRANSACTION
	PRINT 'database updated to version 200253'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200252 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
