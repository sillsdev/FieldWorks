/*****************************************************************************
 * GetEntriesAndSenses$
 *
 * Description:
 *	Returns a table of all of the entries in the LDB.
 *	The table contains the information needed by the
 *	two Insert/Go to and Link to dlgs.
 * Parameters:
 *	@LdbId=the ID of the lexical database.
 *	@aenc=the analysis writing system.
 *	@vws=the vernacular writing system.
 * Returns:
 *	0 if successful, otherwise 1
 *****************************************************************************/
if object_id('GetEntriesAndSenses$') is not null begin
	print 'removing proc GetEntriesAndSenses$'
	drop proc GetEntriesAndSenses$
end
print 'creating proc GetEntriesAndSenses$'
go

create proc [GetEntriesAndSenses$]
	@LdbId as integer = null,
	@aenc as integer = null,
	@vws as integer = null
as
	declare @fIsNocountOn int

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- Make sure we have the LDB id.
	if @LdbId is null begin
		select top 1 @LdbId=ldb.Id
		from LexDb ldb
		order by ldb.Id
	end

	-- Make sure we have the analysis writing system
	if @aenc is null begin
		select top 1 @aenc=Lg.Id
		from LangProject_CurAnalysisWss cae
		join LgWritingSystem lg On Lg.Id=cae.Dst
		order by cae.ord
	end

	-- Make sure we have the vernacular writing system
	if @vws is null begin
		select top 1 @vws=Lg.Id
		from LangProject_CurVernWss cve
		join LgWritingSystem lg On Lg.Id=cve.Dst
		order by cve.ord
	end

	DECLARE @tblSenses TABLE (
		entryId int,
		ownrId int,
		sensId int,
		ord int,
		depth int,
		sensNum nvarchar(1000)	)

	declare @leId as int
	SET @leId = NULL --( NULL gets all entries in fnGetSensesInEntry$

	INSERT INTO @tblSenses
		SELECT
			EntryId,
			OwnerId,
			SenseId,
			Ord,
			Depth,
			SenseNum
		FROM dbo.fnGetSensesInEntry$(@leId)

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	-- REVIEW (SteveMiller): MultiTxt$.fmt was always NULL. Don't understand the reason
	-- for them being selected here.

	-- Select entry information
	select le.Id, le.Class$, le.HomographNumber,
		isnull(cf.Txt, 'N/F') As CitationForm,
		cast(null as varbinary) As CitationFormFmt,
		isnull(mfuf.Txt, 'N/F') As UnderlyingForm,
		cast(null as varbinary) As UnderlyingFormFmt,
		isnull(mflf.Txt, 'no form') As LexicalForm,
		cast(null as varbinary) As LexicalFormFmt
	from LexEntry_ le
	left outer join LexEntry_CitationForm cf On cf.Obj=le.Id and cf.Ws=@vws
	left outer join LexEntry_LexemeForm uf On uf.Src=le.Id
	left outer join MoForm_Form mfuf On mfuf.Obj=uf.Dst and mfuf.Ws=@vws
	left outer join LexEntry_AlternateForms a On a.Src=le.Id
	left outer join MoForm_Form mflf On mflf.Obj=a.Dst and mflf.Ws=@vws
	where @ldbId=le.Owner$
	order by le.Id

	-- REVIEW (SteveMiller): MultiTxt$.fmt was always NULL. Don't understand the reason
	-- for them being selected here.

	-- Select sense information in another rowset
	select ls.entryId As EntryId,
		isnull(ls.sensId, 0) As SenseID,
		ls.sensNum As SenseNum,
		isnull(lsg.Txt, 'no gloss') As Gloss,
		cast(null as varbinary) As GlossFmt,
		isnull(lsd.Txt, 'no def') As Definition,
		cast(null as varbinary) As DefinitionFmt
	from @tblSenses ls
	left outer join LexSense_Gloss lsg On lsg.Obj=ls.sensId and lsg.Ws=@aenc
	left outer join LexSense_Definition lsd On lsd.Obj=ls.sensId and lsd.Ws=@aenc
	order by ls.entryId, ls.sensNum

	return 0
go
