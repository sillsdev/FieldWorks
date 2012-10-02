-- Update database from version 200197 to 200198
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- FDB-188 FindOrCreateCmAgent will not work properly with the Version set to NULL
-------------------------------------------------------------------------------

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
		join LanguageProject lp On lp.Id = aa.Owner$
		where aa.Human=@isHuman and aa.Version IS NULL
	ELSE
		select @agentID=aa.Id
		from CmAgent_ aa
		join CmAgent_Name aan on aan.Obj = aa.Id and aan.Txt=@agentName
		join LanguageProject lp On lp.Id = aa.Owner$
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

	-- We don't need to wory about transactions, since the call to CreateObject_CmAgent
	-- wiil create waht is needed, and rool it back, if the creation fails.

	SELECT @wsEN=Obj
	FROM LgWritingSystem_Name
	WHERE Txt='English'

	SELECT TOP 1 @lpID=ID
	FROM LanguageProject
	ORDER BY ID

	exec @retVal = CreateObject_CmAgent
		@wsEN, @agentName,
		null,
		@isHuman,
		@version,
		@lpID,
		6001038, -- owning flid for CmAgent in LanguageProject
		null,
		@agentID out,
		@uid out

	if @retVal <> 0
	begin
		-- There was an error in CreateObject_CmAgent
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

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200197
BEGIN
	UPDATE Version$ SET DbVer = 200198
	COMMIT TRANSACTION
	PRINT 'database updated to version 200198'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200197 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
