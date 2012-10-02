if object_id('CreateParserProblemAnnotation') is not null begin
	print 'removing proc CreateParserProblemAnnotation'
	drop proc CreateParserProblemAnnotation
end
print 'creating proc CreateParserProblemAnnotation'
go
/*****************************************************************************
 * CreateParserProblemAnnotation
 *
 * Description:
 *	Creates a CmBaseAnnotation object for a wordform that had problems.
 * Parameters:
 *	@CompDetails = Value for the CompDetails property of the new annotation.
 *	@BeginObject_WordformID = Value for the BeginObject property of the new annotation.
 *	@Source_AgentID = Value for the Source property of the new annotation.
 *	@AnnotationType_AnnDefID = Value for the AnnotationType property of the new annotation.
 * Returns:
 *	0, if successful, otherwise:
 *		1 - could not create annotation
 *		2 - could not update data for CmAnnotation
 *		3 - could not update data for CmBaseAnnotation
 *****************************************************************************/
create proc [CreateParserProblemAnnotation]
	@CompDetails ntext,
	@BeginObject_WordformID int,
	@Source_AgentID int,
	@AnnotationType_AnnDefID int
AS
	DECLARE
		@retVal INT,
		@fIsNocountOn INT,
		@lpid INT,
		@nTrnCnt INT,
		@sTranName VARCHAR(50),
		@uid uniqueidentifier,
		@annID INT

	-- determine if NO COUNT is currently set to ON
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- @lpid will be the annotation's owner.
	SELECT TOP 1 @lpID=ID
	FROM LangProject
	ORDER BY ID

	-- Determine if a transaction already exists.
	-- If one does then create a savepoint, otherwise create a transaction.
	set @nTrnCnt = @@trancount
	set @sTranName = 'CreateParserProblemAnnotation_tr' + convert(varchar(8), @@nestlevel)
	if @nTrnCnt = 0 begin tran @sTranName
	else save tran @sTranName

	-- Create a new CmBaseAnnotation, and add it to the LangProject
	set @uid = null
	exec @retVal = CreateOwnedObject$
		37, -- kclidCmBaseAnnotation
		@annID output,
		null,
		@lpid,
		6001044, -- kflidLangProject_Annotations
		25, --kcptOwningCollection
		null,
		0,
		1,
		@uid output

	if @retVal <> 0
	begin
		-- There was an error in CreateOwnedObject
		set @retVal = 1
		GOTO FinishRollback
	end

	-- Update values.
	UPDATE CmAnnotation
	SET CompDetails=@CompDetails,
		Source=@Source_AgentID,
		AnnotationType=@AnnotationType_AnnDefID
	WHERE ID = @annID
	if @@error <> 0
	begin
		-- Couldn't update CmAnnotation data.
		set @retVal = 2
		goto FinishRollback
	end
	UPDATE CmBaseAnnotation
	SET BeginObject=@BeginObject_WordformID
	WHERE ID = @annID
	if @@error <> 0
	begin
		-- Couldn't update CmBaseAnnotation data.
		set @retVal = 3
		goto FinishRollback
	end

	if @nTrnCnt = 0 commit tran @sTranName
	SET @retVal = 0
	GOTO FinishFinal

FinishRollback:
	if @nTrnCnt = 0 rollback tran @sTranName
	GOTO FinishFinal

FinishFinal:
	if @fIsNocountOn = 0 set nocount off
	return @retval
go
