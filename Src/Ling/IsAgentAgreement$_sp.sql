/********************************************************************
 * Procedure: IsAgentAgreement$
 *
 * Description:
 *	Returns whether agents agree in "evaluations of analyses. They
 *	are in agreement if the set of  evaluations by the parser
 *	(nonhuman agent), for the word of interest, is exactly the same
 *	as the set of corresponding, 'accepted' evaluations from the
 *	human agent on that word. Note: all non human evaluations these
 *	will be accepted; there is no such thing as a rejected
 * 	machine evaluation." (John Hatton)
 *
 * Parameters:
 *	@nWfiWordFormId INT = object ID for the word of interest
 *	@nAgentId1 INT = object ID for the first agent
 *	@nAgentId2 INT = object ID for the second agent
 *	@fAgree BIT = 0 not agree, 1 agree; output parameter
 *
 * Returns:
 *	1 if the two agents agree, 0 if they don't
 *
 * Note:
 *	This was a stored function, but according to error messages,
 *	"select statements included in a function cannot return data
 *	to a client" in SQL Server. <sigh>
 *
 * Sample Call:
 *	DECLARE @fAgree BIT
 *	EXEC IsAgentAgreement$ 11588, 11589, 11594, @fAgree OUTPUT
 *******************************************************************/

IF OBJECT_ID('IsAgentAgreement$') IS NOT NULL BEGIN
	PRINT 'removing procedure IsAgentAgreement$'
	DROP PROCEDURE IsAgentAgreement$
END
GO
PRINT 'creating procedure IsAgentAgreement$'
GO

CREATE PROCEDURE [IsAgentAgreement$] (
	@nWfiWordFormId INT,
	@nAgentId1 INT,
	@nAgentId2 INT,
	@fAgreement BIT OUTPUT)
AS
BEGIN

	DECLARE @tblAgentEvals1 TABLE ([Id] INT, Target INT, [Accepted] BIT)
	DECLARE @tblAgentEvals2 TABLE ([Id] INT, Target INT, [Accepted] BIT)

	DECLARE
		@nCount1 INT,
		@nCount2 INT

	SET @fAgreement = 1

	INSERT INTO @tblAgentEvals1
	SELECT ae.[Id], ae.Target, ae.[Accepted]
	FROM CmAgentEvaluation ae
	JOIN CmObject oAgentEval ON oAgentEval.[Id] = ae.[Id]
		AND oAgentEval.Owner$ = @nAgentId1
	JOIN CmObject oWordAnal ON oWordAnal.[Id] = ae.Target
		AND oWordAnal.Owner$ = @nWfiWordFormId

	INSERT INTO @tblAgentEvals2
	SELECT ae.[Id], ae.Target, ae.[Accepted]
	FROM CmAgentEvaluation ae
	JOIN CmObject oAgentEval ON oAgentEval.[Id] = ae.[Id]
		AND oAgentEval.Owner$ = @nAgentId2
	JOIN CmObject oWordAnal ON oWordAnal.[Id] = ae.Target
		AND oWordAnal.Owner$ = @nWfiWordFormId

	--( Make sure all are accepted

	SELECT @nCount1 = COUNT(*) FROM @tblAgentEvals1 WHERE [Accepted] = 0
	SELECT @nCount2 = COUNT(*) FROM @tblAgentEvals2 WHERE [Accepted] = 0

	IF @nCount1 + @nCount2 > 0
		SET @fAgreement = 0

	--( All evaluations are marked accepted. Make sure the analyses
	--( from the two different agents line up

	ELSE BEGIN
		SET @nCount1 = 0

		SELECT @nCount1 = COUNT(*)
		FROM @tblAgentEvals1 a1
		RIGHT OUTER JOIN @tblAgentEvals2 a2 ON a2.Target = a1.Target
		WHERE a1.[Id] IS NULL

		IF @nCount1 > 0
			SET @fAgreement = 0

		ELSE BEGIN
			SET @nCount1 = 0

			SELECT @nCount1 = COUNT(*)
			FROM @tblAgentEvals2 a2
			RIGHT OUTER JOIN @tblAgentEvals1 a1 ON a1.Target = a2.Target
			WHERE a2.[Id] IS NULL

			IF @nCount1 > 0
				SET @fAgreement = 0
		END
	END

	RETURN @fAgreement
END
GO
