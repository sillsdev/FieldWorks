-- update database FROM version 200124 to 200125
BEGIN TRANSACTION  --( will be rolled back if wrong version#

-- (LT-3718) Use WfiWordform.Checksum instead of checksum file
if object_id('RemoveParserApprovedAnalyses$') is not null begin
	print 'removing proc RemoveParserApprovedAnalyses$'
	drop proc [RemoveParserApprovedAnalyses$]
end
go
print 'creating proc RemoveParserApprovedAnalyses$'
go

/*****************************************************************************
 * Procedure: RemoveParserApprovedAnalyses$
 *
 * Description:
 *		Removes analyses for the given wordform, if they are only approved by the parser.
 *
 * Parameters:
 *		@nWfiWordFormID		ID of the wordform
 *
 * Returns:
 *		0 for success, otherwise an error code.
 *****************************************************************************/

CREATE PROC [RemoveParserApprovedAnalyses$]
	@nWfiWordFormID INT
AS
	DECLARE
		@nIsNoCountOn INT,
		@nGonnerId INT,
		@nParserAgentId INT,
		@humanAgentId INT,
		@nError INT

	SET @nIsNoCountOn = @@OPTIONS & 512
	IF @nIsNoCountOn = 0
		SET NOCOUNT ON

	SET @nError = 0

	-- Set checksum to zero
	UPDATE WfiWordform SET Checksum=0 WHERE Id=@nWfiWordFormID

	--( Get the parser agent id
	SELECT TOP 1 @nParserAgentId = Obj
	FROM CmAgent_Name (READUNCOMMITTED)
	WHERE Txt = N'M3Parser'
	-- Get Id of the 'default user' agent
	SELECT TOP 1 @humanAgentId = Obj
	FROM CmAgent_Name nme (READUNCOMMITTED)
	WHERE Txt = N'default user'

	--== Delete all parser evaluations that reference analyses owned by the @nWfiWordFormID wordform. ==--
	SELECT TOP 1 @nGonnerId = ae.[Id]
	FROM CmAgentEvaluation ae (READUNCOMMITTED)
	JOIN CmObject objae (READUNCOMMITTED)
		ON objae.[Id] = ae.[Id] AND objae.Owner$ = @nParserAgentId
	JOIN CmObject objanalysis (READUNCOMMITTED)
		ON objanalysis.[Id] = ae.Target
		AND objanalysis.Class$ = 5059 -- WfiAnalysis objects
		AND objanalysis.Owner$ = @nWfiWordFormID
	ORDER BY ae.[Id]
	WHILE @@ROWCOUNT != 0 BEGIN
		EXEC @nError = DeleteObj$ @nGonnerId
		IF @nError != 0
			GOTO Finish

		SELECT TOP 1 @nGonnerId = ae.[Id]
		FROM CmAgentEvaluation ae (READUNCOMMITTED)
		JOIN CmObject objae (READUNCOMMITTED)
			ON objae.[Id] = ae.[Id] AND objae.Owner$ = @nParserAgentId
		JOIN CmObject objanalysis (READUNCOMMITTED)
			ON objanalysis.[Id] = ae.Target
			AND objanalysis.Class$ = 5059 -- WfiAnalysis objects
			AND objanalysis.Owner$ = @nWfiWordFormID
		WHERE ae.[Id] > @nGonnerId
		ORDER BY ae.[Id]
	END

	--== Delete orphan analyses owned by the given @nWfiWordFormID wordform. ==--
	--== 'Orphan' means they have no evaluations ==--
	SELECT TOP 1 @nGonnerId = analysis.[Id]
	FROM CmObject analysis (READUNCOMMITTED)
	LEFT OUTER JOIN cmAgentEvaluation cae (READUNCOMMITTED)
		ON cae.Target = analysis.[Id]
	WHERE cae.Target IS NULL
		AND analysis.OwnFlid$ = 5062002		-- 5062002
		AND analysis.Owner$ = @nWfiWordFormID
	ORDER BY analysis.[Id]
	WHILE @@ROWCOUNT != 0 BEGIN
		EXEC @nError = DeleteObj$ @nGonnerId
		IF @nError != 0
			GOTO Finish

		SELECT TOP 1 @nGonnerId = analysis.[Id]
		FROM CmObject analysis (READUNCOMMITTED)
		LEFT OUTER JOIN cmAgentEvaluation cae (READUNCOMMITTED)
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

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200124
begin
	UPDATE Version$ SET DbVer = 200125
	COMMIT TRANSACTION
	print 'database updated to version 200125'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200124 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO