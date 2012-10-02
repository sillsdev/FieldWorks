-- update database from version 200012 to 200013
BEGIN TRANSACTION

if object_id('RemoveUnusedAnalyses$') is not null begin
	if (select DbVer from Version$) = 200012
		print 'removing procedure RemoveUnusedAnalyses$'
	drop proc [RemoveUnusedAnalyses$]
end
go
if (select DbVer from Version$) = 200012
	print 'creating procedure RemoveUnusedAnalyses$'
go

/*****************************************************************************
 *	Procedure: RemoveUnusedAnalyses$
 *
 *	Description:
 *		Performs 2 tasks:
 *		1) Deletes any "stale" evaluations, those that have an old date that
 *			are no longer valid.
 *		2) Removes Word form Analyses that are not referenced by an
 *			evaluation.
 *
 *	Parameters:
 * 		@nAgentID			ID of the agent
 *		@nWfiWordFormID		ID of the wordform
 *		@dtEval			Date-time of the evaluation
 *
 *  Selects:
 *		0 if everything has been deleted (or an error has occurred), 1 if there
 *		is more stuff remaining to delete (deletes a maximum of 16 objects at a
 *		 time).
 *	Returns:
 *		0 for success, otherwise the error code returned by DeleteObj$
 *****************************************************************************/

-- TODO (SteveMiller/RandyR): Determine if the orphaned records should really
--							be deleted by a trigger.

CREATE PROCEDURE RemoveUnusedAnalyses$
	@nAgentId INT,
	@nWfiWordFormID INT,
	@dtEval DATETIME
AS
	DECLARE
		@nGonnerID INT,
		@nError INT,
		@nCountDeleted INT,
		@fMoreToDelete INT

	SET @nGonnerId = NULL
	SET @nError = 0
	SET @nCountDeleted = 0
	SET @fMoreToDelete = 0

	--== Delete stale evaluations on analyses ==--

	SELECT TOP 1 @nGonnerId = ae.[Id]
	FROM CmAgentEvaluation ae (READUNCOMMITTED)
	JOIN CmObject objae (READUNCOMMITTED)
		ON objae.[Id] = ae.[Id] AND objae.Owner$ = @nAgentId
	JOIN CmObject objanalysis (READUNCOMMITTED)
		ON objanalysis.[Id] = ae.Target
		AND objanalysis.Class$ = 5059 -- WfiAnalysis objects
		AND objanalysis.Owner$ = @nWfiWordFormID
	WHERE ae.DateCreated < @dtEval
	ORDER BY ae.[Id]

	WHILE @@ROWCOUNT != 0 BEGIN
		IF @nCountDeleted >= 16
		BEGIN
			SET @fMoreToDelete = 1
			GOTO Finish
		END

		EXEC @nError = DeleteObj$ @nGonnerId
		SET @nCountDeleted = @nCountDeleted + 1

		IF @nError != 0
			GOTO Finish

		SELECT TOP 1 @nGonnerId = ae.[Id]
		FROM CmAgentEvaluation ae (READUNCOMMITTED)
		JOIN CmObject objae (READUNCOMMITTED)
			ON objae.[Id] = ae.[Id] AND objae.Owner$ = @nAgentId
		JOIN CmObject objanalysis (READUNCOMMITTED)
			ON objanalysis.[Id] = ae.Target
			AND objanalysis.Class$ = 5059 -- WfiAnalysis objects
			AND objanalysis.Owner$ = @nWfiWordFormID
		WHERE ae.[Id] > @nGonnerId AND ae.DateCreated < @dtEval
		ORDER BY ae.[Id]
	END

	--== Delete orphan analyses, which have no evaluations ==--

	SELECT TOP 1 @nGonnerId = analysis.[Id]
	FROM CmObject analysis (READUNCOMMITTED)
	LEFT OUTER JOIN cmAgentEvaluation cae (READUNCOMMITTED)
		ON cae.Target = analysis.[Id]
	WHERE cae.Target IS NULL
		AND analysis.OwnFlid$ = 5062002		-- kflidWfiWordform_Analyses
		AND analysis.Owner$ = @nWfiWordFormID
	ORDER BY analysis.[Id]

	WHILE @@ROWCOUNT != 0 BEGIN
		IF @nCountDeleted >= 16
		BEGIN
			SET @fMoreToDelete = 1
			GOTO Finish
		END

		EXEC @nError = DeleteObj$ @nGonnerId

		IF @nError != 0
			GOTO Finish
		SET @nCountDeleted = @nCountDeleted + 1

		SELECT TOP 1 @nGonnerId = analysis.[Id]
		FROM CmObject analysis (READUNCOMMITTED)
		LEFT OUTER JOIN cmAgentEvaluation cae (READUNCOMMITTED)
			ON cae.Target = analysis.[Id]
		WHERE cae.Target IS NULL
			AND analysis.[Id] > @nGonnerId
			AND analysis.OwnFlid$ = 5062002		-- kflidWfiWordform_Analyses
			AND analysis.Owner$ = @nWfiWordFormID
		ORDER BY analysis.[Id]
	END

	GOTO Finish

Finish:
	SELECT @fMoreToDelete
	RETURN @nError

GO


declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200012
begin
	update Version$ set DbVer = 200013
	COMMIT TRANSACTION
	print 'database updated to version 200013'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200012 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
