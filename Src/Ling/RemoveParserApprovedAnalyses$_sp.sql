/*****************************************************************************
 * Procedure: RemoveParserApprovedAnalyses$
 *
 * Description:
 *		Removes analyses for the given wordform, if they are only approved by the parser.
 *
 * Parameters:
 *		@nWfiWordFormID		ID of the wordform
 *		@nParserAgentId			ID of the parser CmAgent.
 *			If NULL it uses all non-Human agents.
 *
 * Returns:
 *		0 for success, otherwise an error code.
 *****************************************************************************/

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
