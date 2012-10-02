/*****************************************************************************
 * Procedure: SetAgentEval
 *
 * Description:
 *		Updates an agent evaluation with the latest acceptance, details, and
 *		date-time of the evaluation. Creates a new agent evaluation if one
 *		doesn't already exist. If the Agent doesn't know, the accepted flag
 *		is set to 2 (or optionally NULL), and the associated evaluations will
 *		be deleted.
 *
 * Parameters:
 * 		@nAgentID	ID of the agent
 *      @nTargetID	ID of the thing analyzed, whether a WfiAnalysis or a
 *						WfiWordform.
 *		@nAccepted	Has the agent evaluation been accepted by Agent?
 *						0 = not accepted
 *						1 = accepted
 *						2 = don't know
 *						NULL = don't know
 *		@nvcDetails	Additional detail
 *		@dtEval		Date-time of the evaluation
 *
 * Returns:
 *		0 for success, otherwise an error code.
 *****************************************************************************/

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
		@nvcError NVARCHAR(100),
		@StrId NVARCHAR(20);

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
			SET @StrId = CONVERT(NVARCHAR(20), @nEvalId);
			EXEC DeleteObjects @StrId;

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

			EXEC @nError = MakeObj_CmAgentEvaluation
				@dtEval,
				@nAccepted,
				@nvcDetails,
				@nAgentId,					--(owner
				23006,						--(ownflid  23006
				NULL,						--(startobj
				@nNewObjId OUTPUT,
				@guidNewObj OUTPUT,
				0,			--(ReturnTimeStamp
				@nNewObjTimeStamp OUTPUT

			IF @nError != 0 BEGIN
				SET @nvcError = 'SetAgentEval: MakeObj_CmAgentEvaluation failed.'
				GOTO Fail
			END

			UPDATE CmAgentEvaluation
			SET Target = @nTargetID
			WHERE Id = @nNewObjId
		END

		--( Update the existing Agent Evaluation
		ELSE

			UPDATE CmAgentEvaluation
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
