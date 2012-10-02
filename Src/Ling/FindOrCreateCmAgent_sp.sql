/*****************************************************************************
 * Procedure: FindOrCreateCmAgent
 *
 * Description:
 *	Finds (creates new one, if needed) a CmAgent that
 *	matches the agent name, Version, and humanity that are provided.
 * Parameters:
 *      @agentName	name of the agent
 *      @isHuman	1 for a human agent, otherwise 0
 *	@version	Version of the agent
 * Assumptions:
 *	It will select the ID of the agent, or 0, if there was an error.
 * Returns:
 *	0 for success, otherwise an error code from 1 to 10.
 *	(See code for error code details.)
 *****************************************************************************/

if object_id('FindOrCreateCmAgent') is not null
	drop proc FindOrCreateCmAgent
go
print 'creating proc FindOrCreateCmAgent'
go

create proc FindOrCreateCmAgent
	@agentName nvarchar(4000),
	@isHuman bit,
	@version  nvarchar(4000)
as
	DECLARE
		@retVal INT,
		@fIsNocountOn INT,
		@agentID int

	set @agentID = null

	-- determine if NO COUNT is currently set to ON
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	IF @version IS NULL
		select @agentID=aa.Id
		from CmAgent_ aa
		join CmAgent_Name aan on aan.Obj = aa.Id and aan.Txt=@agentName
		join LangProject lp On lp.Id = aa.Owner$
		where aa.Human=@isHuman and aa.Version IS NULL
	ELSE
		select @agentID=aa.Id
		from CmAgent_ aa
		join CmAgent_Name aan on aan.Obj = aa.Id and aan.Txt=@agentName
		join LangProject lp On lp.Id = aa.Owner$
		where aa.Human=@isHuman and aa.Version=@version

	-- Found extant one, so return it.
	if @agentID is not null
	begin
		set @retVal = 0
		goto FinishFinal
	end

	--== Need to make a new one ==--
	DECLARE @uid uniqueidentifier,
		@nTrnCnt INT,
		@sTranName VARCHAR(50),
		@wsEN int,
		@lpID int

	-- We don't need to wory about transactions, since the call to MakeObj_CmAgent
	-- wiil create waht is needed, and rool it back, if the creation fails.

	SELECT @wsEN=Obj
	FROM LgWritingSystem_Name
	WHERE Txt='English'

	SELECT TOP 1 @lpID=ID
	FROM LangProject
	ORDER BY ID

	exec @retVal = MakeObj_CmAgent
		@wsEN, @agentName,
		null,
		@isHuman,
		@version,
		@lpID,
		6001038, -- owning flid for CmAgent in LangProject
		null,
		@agentID out,
		@uid out

	if @retVal <> 0
	begin
		-- There was an error in MakeObj_CmAgent
		set @retVal = 1
		GOTO FinishClearID
	end

	SET @retVal = 0
	GOTO FinishFinal

FinishClearID:
	set @agentID = 0
	GOTO FinishFinal

FinishFinal:
	if @fIsNocountOn = 0 set nocount off
	select @agentID
	return @retVal

go
