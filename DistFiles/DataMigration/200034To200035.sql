-- update database from version 200034 to 200035
BEGIN TRANSACTION


IF OBJECT_ID('fnGetDefaultAnalysisGloss') IS NOT NULL BEGIN
	if (select DbVer from Version$) = 200005
		PRINT 'removing function fnGetDefaultAnalysisGloss'
	DROP FUNCTION fnGetDefaultAnalysisGloss
END
GO
if (select DbVer from Version$) = 200005
	PRINT 'creating function fnGetDefaultAnalysisGloss'
GO

CREATE FUNCTION fnGetDefaultAnalysisGloss (
	@nWfiWordFormId INT)
RETURNS @tblScore TABLE (
	AnalysisId INT,
	GlossId INT,
	[Score] INT)
AS BEGIN

	INSERT INTO @tblScore
		--( wfiGloss is an InstanceOf
		SELECT
			oanalysis.[Id],
			ogloss.[Id],
			(COUNT(ann.InstanceOf) + 10000) --( needs higher # than wfiAnalsys
		FROM CmAnnotation ann (READUNCOMMITTED)
		JOIN WfiGloss g  (READUNCOMMITTED) ON g.[Id] = ann.InstanceOf
		JOIN CmObject ogloss (READUNCOMMITTED) ON ogloss.[Id] = g.[Id]
		JOIN CmObject oanalysis (READUNCOMMITTED) ON oanalysis.[Id] = ogloss.Owner$
			AND oanalysis.Owner$ = @nWfiWordFormId
		JOIN WfiAnalysis a (READUNCOMMITTED) ON a.[Id] = oanalysis.[Id]
		GROUP BY oanalysis.[Id], ogloss.[Id]
	UNION ALL
		--( wfiAnnotation is an InstanceOf
		SELECT
			oanalysis.[Id],
			NULL,
			COUNT(ann.InstanceOf)
		FROM CmAnnotation ann (READUNCOMMITTED)
		JOIN CmObject oanalysis (READUNCOMMITTED) ON oanalysis.[Id] = ann.InstanceOf
			AND oanalysis.Owner$ = @nWfiWordFormId
		JOIN WfiAnalysis a (READUNCOMMITTED) ON a.[Id] = oanalysis.[Id]
		-- this is a tricky way of eliminating analyses where there exists
		-- a negative evaluation by a human agent.
		LEFT OUTER JOIN (
				SELECT ae.Target
				FROM CmAgentEvaluation_ ae
				JOIN CmAgent a ON a.Id = ae.Owner$ AND Human = 1
				WHERE ae.Accepted = 0)
					aae ON aae.Target = a.Id
			WHERE aae.Target IS NULL
		GROUP BY oanalysis.[Id]

	--( If the gloss and analysis ID are all null, there
	--( are no annotations, but an analysis (and, possibly, a gloss) still might exist.

	IF @@ROWCOUNT = 0

		INSERT INTO @tblScore
		SELECT TOP 1
			oanalysis.[Id],
			wg.id,
			0
		FROM CmObject oanalysis (READUNCOMMITTED)
		left outer join WfiGloss_ wg (readuncommitted) on wg.owner$ = oanalysis.id
		LEFT OUTER JOIN (
				SELECT ae.Target
				FROM CmAgentEvaluation_ ae
				JOIN CmAgent a ON a.Id = ae.Owner$ AND Human = 1
				WHERE ae.Accepted = 0)
					aae ON aae.Target = oanalysis.Id
		WHERE oanalysis.Owner$ = @nWfiWordFormId and aae.Target IS NULL

	RETURN
END
GO


declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200034
begin
	update Version$ set DbVer = 200035
	COMMIT TRANSACTION
	print 'database updated to version 200035'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200034 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
