-- update database from version 200019 to 200020
BEGIN TRANSACTION  --( will be rolled back if wrong version#

-------------------------------------------------------------
-- Updated stored proc from FwCore.sql causing locks
-------------------------------------------------------------
if object_id('GetPossibilities') is not null begin
	print 'removing proc GetPossibilities'
	drop proc [GetPossibilities]
end
go
print 'creating proc GetPossibilities'
go
create proc [GetPossibilities]
	@ObjId int,
	@Ws int
as
	declare @uid uniqueidentifier,
			@retval int

	-- get all of the possibilities owned by the specified possibility list object
	declare @tblObjInfo table (
		[ObjId]		int		not null,
		[ObjClass]	int		null,
		[InheritDepth]	int		null	default(0),
		[OwnerDepth]	int		null	default(0),
		[RelObjId]	int		null,
		[RelObjClass]	int		null,
		[RelObjField]	int		null,
		[RelOrder]	int		null,
		[RelType]	int		null,
		[OrdKey]	varbinary(250)	null	default(0))

	insert into @tblObjInfo
		select * from fnGetOwnedObjects$(@ObjId, null, 176160768, 0, 0, 1, 7, 1)

	-- First return a count so that the caller can preallocate memory for the results.
	select count(*) from @tblObjInfo

	--
	--  get an ordered list of relevant writing system codes
	--
	declare @tblWs table (
		[WsId]	int not null, -- don't make unique. It shouldn't happen, but we don't want a crash if it does.
		[Ord]	int primary key clustered identity(1,1))
	--( 0xffffffff (-1) or 0xfffffffd (-3) = First string from a) ordered checked analysis
	-- writing systems b) any remaining analysis writing systems or stars if none of the above.
	if @Ws = 0xffffffff or @Ws = 0xfffffffd begin
		insert into @tblWs (WsId)
			select lws.id
			from LanguageProject_CurrentAnalysisWritingSystems caws
			join LgWritingSystem lws on caws.dst = lws.id
			order by caws.Ord
		insert into @tblWs (WsId)
			select distinct lws.id
			from LanguageProject_AnalysisWritingSystems caws
			join LgWritingSystem lws on caws.dst = lws.id and lws.id not in (select WsId from @tblWs)
	end
	--( 0xfffffffe (-2) or 0xfffffffc (-4) = First string from a) ordered checked vernacular
	-- writing systems b) any remaining vernacular writing systems or stars if none of the above.
	else if @Ws = 0xfffffffe or @Ws = 0xfffffffc begin
		insert into @tblWs (WsId)
			select lws.id
			from LanguageProject_CurrentVernacularWritingSystems cvws
			join LgWritingSystem lws on cvws.dst = lws.id
			order by cvws.Ord
		insert into @tblWs (WsId)
			select distinct lws.id
			from LanguageProject_VernacularWritingSystems cvws
			join LgWritingSystem lws on cvws.dst = lws.id and lws.id not in (select WsId from @tblWs)
	end
	--( 0xfffffffb = -5 = First string from a) ordered checked analysis writing systems
	-- b) ordered checked vernacular writing systems, c) any remaining analysis writing systems,
	-- d) any remaining vernacular writing systems or stars if none of the above.
	else if @Ws = 0xfffffffb begin
		insert into @tblWs (WsId)
			select lws.id
			from LanguageProject_CurrentAnalysisWritingSystems caws
			join LgWritingSystem lws on caws.dst = lws.id
			order by caws.Ord
		insert into @tblWs (WsId)
			select lws.id
			from LanguageProject_CurrentVernacularWritingSystems cvws
			join LgWritingSystem lws on cvws.dst = lws.id
			where lws.id not in (select WsId from @tblWs)
			order by cvws.Ord
		insert into @tblWs (WsId)
			select distinct lws.id
			from LanguageProject_AnalysisWritingSystems caws
			join LgWritingSystem lws on caws.dst = lws.id and lws.id not in (select WsId from @tblWs)
		insert into @tblWs (WsId)
			select distinct lws.id
			from LanguageProject_VernacularWritingSystems cvws
			join LgWritingSystem lws on cvws.dst = lws.id and lws.id not in (select WsId from @tblWs)
	end
	--( 0xfffffffa = -6 = First string from a) ordered checked vernacular writing systems
	-- b) ordered checked analysis writing systems, c) any remaining vernacular writing systems,
	-- d) any remaining analysis writing systems or stars if none of the above.
	else if @Ws = 0xfffffffa begin
		insert into @tblWs (WsId)
			select lws.id
			from LanguageProject_CurrentVernacularWritingSystems cvws
			join LgWritingSystem lws on cvws.dst = lws.id
			order by cvws.Ord
		insert into @tblWs (WsId)
			select lws.id
			from LanguageProject_CurrentAnalysisWritingSystems caws
			join LgWritingSystem lws on caws.dst = lws.id
			where lws.id not in (select WsId from @tblWs)
			order by caws.Ord
		insert into @tblWs (WsId)
			select distinct lws.id
			from LanguageProject_VernacularWritingSystems cvws
			join LgWritingSystem lws on cvws.dst = lws.id and lws.id not in (select WsId from @tblWs)
		insert into @tblWs (WsId)
			select distinct lws.id
			from LanguageProject_AnalysisWritingSystems caws
			join LgWritingSystem lws on caws.dst = lws.id and lws.id not in (select WsId from @tblWs)
	end
	else -- Hard coded value
		insert into @tblWs (WsId) Values(@Ws)

	-- Now that we have the desired writing systems in @tblWs, we can return the desired information.
	select
		o.ObjId,
		(select top 1 isnull(ca.[txt], '***') + ' - ' + isnull(cn.[txt], '***')
			from LgWritingSystem lws
			left outer join CmPossibility_Name cn on cn.[ws] = lws.[Id] and cn.[Obj] = o.[objId]
			left outer join CmPossibility_Abbreviation ca on ca.[ws] = lws.[Id] and ca.[Obj] = o.[objId]
			join @tblWs wstbl on wstbl.WsId = lws.id
			order by (
				select [Ord] = CASE
					WHEN cn.[txt] IS NOT NULL THEN wstbl.[ord]
					WHEN ca.[txt] IS NOT NULL THEN wstbl.[ord] + 9000
					ELSE wstbl.[Ord] + 99000
					END)),
		isnull((select top 1 lws.id
			from LgWritingSystem lws
			left outer join CmPossibility_Name cn on cn.[ws] = lws.[Id] and cn.[Obj] = o.[objId]
			left outer join CmPossibility_Abbreviation ca on ca.[ws] = lws.[Id] and ca.[Obj] = o.[objId]
			join @tblWs wstbl on wstbl.WsId = lws.id
			order by (
				select [Ord] = CASE
					WHEN cn.[txt] IS NOT NULL THEN wstbl.[ord]
					WHEN ca.[txt] IS NOT NULL THEN wstbl.[ord] + 9000
					ELSE wstbl.[Ord] + 99000
					END)
			), (select top 1 WsId from @tblws)),
		o.OrdKey, cp.ForeColor, cp.BackColor, cp.UnderColor, cp.UnderStyle
	from @tblObjInfo o
		left outer join CmPossibility cp on cp.[id] = o.[objId]
	order by o.OrdKey

	return @retval
go

-------------------------------------------------------------------------------
-- From LingSp.sql. This was corrected in 200017To200018.sql, but repeated here
-- in case someone upgraded prior to the fix.
-------------------------------------------------------------------------------
if object_id('SetAgentEval') is not null begin
	print 'removing proc SetAgentEval'
	drop proc SetAgentEval
end
go
print 'creating proc SetAgentEval'
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

-------------------------------------------------------------------------------

declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200019
begin
	update Version$ set DbVer = 200020
	COMMIT TRANSACTION
	print 'database updated to version 200020'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200019 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO