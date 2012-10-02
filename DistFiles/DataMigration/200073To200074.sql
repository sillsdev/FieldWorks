-- Update database from version 200073 to 200074
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-- Flids used in this file:
-- 5002003 = LexEntry_CitationForm
-- 5002008 = LexEntry_Allomorphs (obsolete, going away)
-- 5002010 = LexEntry_UnderlyingForm (obsolete, going away)
-- 5002029 = LexEntry_LexemeForm
-- 5002030 = LexEntry_AlternateForms
-- 5035001 = MoForm_Form
-- 5035003 = MoForm_IsAbstract

-- This was accidently omitted in the first version of 200072To200073.sql
-- Add MoForm_IsAbstract : boolean if needed
DECLARE @cfld int
SELECT @cfld=COUNT(*) FROM Field$ WHERE Id=5035003
IF @cfld = 0
BEGIN
	print 'adding the IsAbstract field to MoForm'
	insert into [Field$]
		([Id],Type, Class, DstCls,[Name],Custom,CustomId,[Min],[Max],Big)
		values(5035003, 1, 5035, null, 'IsAbstract',0, null,null,null,null)
END
GO
-- ensure other migrated class views (and subclass views) are updated properly
exec UpdateClassView$ 55, 1		--( FsFeatureDefn
exec UpdateClassView$ 59, 1		--( FsFeatureStructureType
exec UpdateClassView$ 65, 1		--( FsSymbolicFeatureValue
exec UpdateClassView$ 5002, 1		--( LexEntry
exec UpdateClassView$ 5035, 1		--( MoForm

print 'Migrating LexEntry data from UnderlyingForm and Allomorphs to LexemeForm and AlternateForms'
-- If UnderlyingForm is filled, move the MoForm to LexemeForm and set the IsAbstract Boolean
UPDATE MoForm_ SET IsAbstract=1 WHERE OwnFlid$=5002010
UPDATE MoForm_ SET OwnFlid$=5002029 WHERE OwnFlid$=5002010

-- If LexemeForm is missing, move the final MoForm from Allomorphs to LexemeForm
UPDATE MoForm_ SET OwnFlid$= 5002029, OwnOrd$=NULL
FROM MoForm_ mf
JOIN LexEntry le ON mf.Owner$ = le.Id
WHERE mf.OwnFlid$=5002008 AND
	mf.OwnOrd$ = (SELECT MAX(Ord) FROM LexEntry_Allomorphs WHERE Src = le.id) AND
	mf.Owner$ NOT IN (SELECT Owner$ FROM CmObject WHERE OwnFlid$ = 5002029)

-- Move any remaining MoForms from Allomorphs to AlternateForms.
UPDATE MoForm_ SET OwnFlid$=5002030 WHERE OwnFlid$=5002008

-- If a multiunicode alternative of the LexemeForm is identical to the corresponding
--	 CitationForm multiunicode alternative, delete the CitationForm alternative.
DELETE LexEntry_CitationForm
FROM LexEntry_CitationForm AS cf
JOIN LexEntry_LexemeForm AS lf ON lf.Src = cf.Obj
JOIN MoForm_Form AS f on f.Obj = lf.Dst
WHERE f.Ws = cf.Ws AND f.Txt = cf.Txt

--When done with the above,
--Remove the following two properties from LexEntry
--1. UnderlyingForm
--2. Allomorphs
print 'Removing Allomorphs and UnderlyingForm from LexEntry'
DELETE FROM Field$ WHERE Id = 5002008	--( LexEntry_Allomorphs
DELETE FROM Field$ WHERE Id = 5002010	--( LexEntry_UnderlyingForm
GO
print 'updating stored procedures'

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
		from LexicalDatabase ldb (readuncommitted)
		order by ldb.Id
	end

	-- Make sure we have the analysis writing system
	if @aenc is null begin
		select top 1 @aenc=Lg.Id
		from languageProject_CurrentAnalysisWritingSystems cae (readuncommitted)
		join LgWritingSystem lg (readuncommitted) On Lg.Id=cae.Dst
		order by cae.ord
	end

	-- Make sure we have the vernacular writing system
	if @vws is null begin
		select top 1 @vws=Lg.Id
		from languageProject_CurrentVernacularWritingSystems cve (readuncommitted)
		join LgWritingSystem lg (readuncommitted) On Lg.Id=cve.Dst
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
	from LexEntry_ le (readuncommitted)
	left outer join LexEntry_CitationForm cf (readuncommitted) On cf.Obj=le.Id and cf.Ws=@vws
	left outer join LexEntry_LexemeForm uf (readuncommitted) On uf.Src=le.Id
	left outer join MoForm_Form mfuf (readuncommitted) On mfuf.Obj=uf.Dst and mfuf.Ws=@vws
	left outer join LexEntry_AlternateForms a (readuncommitted) On a.Src=le.Id
	left outer join MoForm_Form mflf (readuncommitted) On mflf.Obj=a.Dst and mflf.Ws=@vws
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
	left outer join LexSense_Gloss lsg (readuncommitted) On lsg.Obj=ls.sensId and lsg.Ws=@aenc
	left outer join LexSense_Definition lsd (readuncommitted) On lsd.Obj=ls.sensId and lsd.Ws=@aenc
	order by ls.entryId, ls.sensNum

	return 0
go

/***********************************************************************************************
 * Procedure: DisplayName_MoForm
 * Description: This procedure returns display information for all subclasses of MoForm
 * Parameters:
 *    @hvo - the object ID of the MoForm, or all of them if null
 * Return: 0 if successful, otherwise 1.
***********************************************************************************************/
if object_id('DisplayName_MoForm') is not null begin
	print 'removing proc DisplayName_MoForm'
	drop proc DisplayName_MoForm
end
print 'creating proc DisplayName_MoForm'
go

create proc [DisplayName_MoForm]
	@hvo int = null
as

declare @retval int, @fIsNocountOn int,
	@DisplayName nvarchar(4000), @pfxMarker nvarchar(2), @sfxMarker nvarchar(2),
	@AlloId int, @AlloClass int, @AlloOwner int, @AlloFlid int,
	@AlloTxt nvarchar(4000), @AlloFmt int, @AlloWs int,
	@SenseId int, @SenseTxt nvarchar(4000), @SenseFmt int, @SenseWs int,
	@CfTxt nvarchar(4000), @CfFmt int, @CfWs int,
	@myCursor CURSOR

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- table variable to hold return information.
	declare @DisplayNameMoForm table (
		DisplayName nvarchar(4000), --1
		AlloId int,	-- 2
		AlloClass int,	-- 3
		AlloOwner int,	-- 4
		AlloFlid int,	-- 5
		AlloTxt nvarchar(4000),	-- 6
		AlloFmt int,	-- 7
		AlloWs int,	-- 8
		SenseId int,	--
		SenseTxt nvarchar(4000),	-- 10
		SenseFmt int,	-- 11
		SenseWs int,	-- 12
		CfTxt nvarchar(4000),	-- 13
		CfFmt int,	-- 14
		CfWs int)	-- 15

	--Note: This can't be a table variable, because we do:
	-- insert #DNLE exec DisplayName_LexEntry null
	--And that can't be done using table variables.
	create table #DNLE (
		LeId int primary key,
		Class int,
		HNum int default 0,
		--( See the notes under string tables in FwCore.sql about the
		--( COLLATE clause.
		FullTxt NVARCHAR(4000) COLLATE Latin1_General_BIN,
		FormId int default 0,
		Ord int default 0,
		Flid int default 0,
		FormTxt nvarchar(4000),
		FormFmt int,
		FormEnc int,
		SenseId int default 0,
		SenseGloss nvarchar(4000),
		SenseFmt int,
		SenseEnc int
		)
	/*
	Class ids are:
	  5027	MoAffixAllomorph
	  5045	MoStemAllomorph
	--5029	MoAffixProcess is not used at this point.

	Owner Field ids are:
	  5002029	LexEntry_LexemeForm
	  5002030	LexEntry_AlternateForms
	*/
	if @hvo is null begin
		insert #DNLE exec DisplayName_LexEntry null
		-- Do all MoForms that are owned in the LexemeForm and AlternateForms
		-- properties of LexEntry.
		set @myCursor = CURSOR FAST_FORWARD for
			select cmo.Id, cmo.Class$, cmo.OwnFlid$
			from CmObject cmo
			where cmo.Class$ IN (5027, 5045) and cmo.OwnFlid$ IN (5002029, 5002030)
			order by cmo.Id
		open @myCursor
	end
	else begin
		-- Do only the MoForm provided by @hvo.
		set @myCursor = CURSOR FAST_FORWARD for
			select cmo.Id, cmo.Class$, cmo.OwnFlid$
			from CmObject cmo
			where cmo.Id = @hvo
				and cmo.Class$ IN (5027, 5045) and OwnFlid$ IN (5002029, 5002030)
		open @myCursor
	end

	-- Loop through all ids.
	fetch next from @myCursor into @AlloId, @AlloClass, @AlloFlid
	while @@fetch_status = 0
	begin
		-- Get display name for LexEntry.
		declare @XMLLEId nvarchar(4000), @cnt int

		select @AlloOwner=Owner$
		from CmObject (readuncommitted)
		where Id=@AlloId

		if @hvo is not null begin
			set @XMLLEId = '<root><Obj Id="' + cast(@AlloOwner as nvarchar(100)) + '"/></root>'
			insert #DNLE exec DisplayName_LexEntry @XMLLEId
		end

		select @SenseId=SenseId, @SenseTxt=isnull(SenseGloss, '***'), @SenseFmt=SenseFmt,
				@SenseWs=SenseEnc, @AlloWs=FormEnc
		from #DNLE (readuncommitted)
		where LeId=@AlloOwner

		-- REVIEW (SteveMiller): MultiTxt$.fmt was always NULL. Don't understand the
		-- reason for @AlloFmt being set to Fmt. Changed to cast(null as varbinary).

		select @AlloTxt=isnull(Txt, '***'), @AlloFmt = cast(null as varbinary)
		from MoForm_Form (readuncommitted)
		where Ws=@AlloWs and Obj=@AlloId

		select @pfxMarker=isnull(mmt.Prefix, ''), @sfxMarker=isnull(mmt.Postfix, '')
		from MoForm f (readuncommitted)
		left outer join MoMorphType mmt (readuncommitted) On f.MorphType=mmt.Id
		where f.Id=@AlloId

		-- REVIEW (SteveMiller): MultiTxt$.fmt was always NULL. Don't understand the
		-- reason for @AlloFmt being set to Fmt. Changed to cast(null as varbinary).

		select @CfTxt=isnull(Txt, '***'), @CfFmt = cast(null as varbinary), @CfWs=Ws
		from LexEntry_CitationForm (readuncommitted)
		where Obj=@AlloOwner and Ws=@AlloWs

		set @DisplayName =
				@pfxMarker + @AlloTxt + @sfxMarker + ' (' + @SenseTxt + '): ' + @CfTxt

		if @hvo is not null
			truncate table #DNLE

		--Put everything in temporary table
		insert @DisplayNameMoForm (DisplayName,
			AlloId, AlloClass, AlloOwner, AlloFlid, AlloTxt, AlloFmt, AlloWs,
			SenseId, SenseTxt, SenseFmt, SenseWs,
			CfTxt, CfFmt, CfWs)
		values (@DisplayName,
			@AlloId, @AlloClass, @AlloOwner, @AlloFlid, @AlloTxt, @AlloFmt, @AlloWs,
			@SenseId, @SenseTxt, @SenseFmt, @SenseWs,
			@CfTxt, @CfFmt, @CfWs)
		-- Try for another MoForm.
		fetch next from @myCursor into @AlloId, @AlloClass, @AlloFlid
	end

	set @retval = 0
	select * from @DisplayNameMoForm order by AlloTxt

LExitWithCursor:
	close @myCursor
	deallocate @myCursor

LExitNoCursor:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	drop table #DNLE

	return @retval
go

/***********************************************************************************************
 * Procedure: DisplayName_LexEntry
 * Description: This procedure returns a variety of information about the entry:
 *	1. First of citation form, underlying form, or allomorph.
 *	2. hyphen+homographNumber, if greater than 0,
 *	3. Gloss of the first sense for major & subentries. Nothing for minor entries.
 *	4. Fmt and Ws for individual strings.
 *	5. Ids for various things for use by a view constructor.
 * Assumptions:
 *	1. The input XML is of the form: <root><Obj Id="7164"/><Obj Id="7157"/></root>
 *	2. This SP will use the first vernacular and analysis writing system, as needed for
 *	   the form and gloss, respectively.
 * Parameters:
 *    @XMLIds - Object IDs of the entry(ies), or null for all entries
 * Return: 0 if successful, otherwise 1.
***********************************************************************************************/
if object_id('DisplayName_LexEntry') is not null begin
	print 'removing proc DisplayName_LexEntry'
	drop proc DisplayName_LexEntry
end
print 'creating proc DisplayName_LexEntry'
go

create  proc [DisplayName_LexEntry]
	@XMLIds ntext = null
as

declare @retval int, @fIsNocountOn int,
	@LeId int, @Class int, @HNum int, @FullTxt nvarchar(4000),
	@FormId int, @Ord int, @Flid int, @FormTxt nvarchar(4000), @FormFmt int, @FormEnc int,
	@SenseId int, @SenseGloss nvarchar(4000), @SenseFmt int, @SenseEnc int,
	@myCursor CURSOR

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- Gather two encodings,
	select top 1 @SenseEnc=le.Id
	from LanguageProject_CurrentAnalysisWritingSystems ce (readuncommitted)
	join LgWritingSystem le (readuncommitted) On le.Id = ce.Dst
	order by ce.Src, ce.ord
	select top 1 @FormEnc=le.Id
	from LanguageProject_CurrentVernacularWritingSystems ce (readuncommitted)
	join LgWritingSystem le (readuncommitted) On le.Id = ce.Dst
	order by ce.Src, ce.ord

	--Table variable.
	declare @DisplayNameLexEntry table (
		LeId int primary key,
		Class int,
		HNum int default 0,
		--( See the notes under string tables in FwCore.sql about the
		--( COLLATE clause.
		FullTxt NVARCHAR(4000) COLLATE Latin1_General_BIN,
		FormId int default 0,
		Ord int default 0,
		Flid int default 0,
		FormTxt nvarchar(4000),
		FormFmt int,
		FormEnc int,
		SenseId int default 0,
		SenseGloss nvarchar(4000),
		SenseFmt int,
		SenseEnc int
		)

	if @XMLIds is null begin
		-- Do all lex entries.
		set @myCursor = CURSOR FAST_FORWARD for
			select Id, Class$
			from CmObject (readuncommitted)
			where Class$=5002
			order by id
		open @myCursor
	end
	else begin
		-- Do lex entries provided in xml string.
		declare @hdoc int
		exec sp_xml_preparedocument @hdoc output, @XMLIds
		if @@error <> 0 begin
			set @retval = 1
			goto LExitNoCursor
		end
		set @myCursor = CURSOR FAST_FORWARD for
			select cmo.Id, cmo.Class$
			from	openxml(@hdoc, '/root/Obj')
			with ([Id] int) as ol
			-- Remove all pretenders, since they won't 'join'.
			join CmObject cmo (readuncommitted)
				On ol.Id=cmo.Id
				and cmo.Class$=5002
			order by ol.[Id]
		open @myCursor
		-- Turn loose of the handle
		exec sp_xml_removedocument @hdoc
		if @@error <> 0 begin
			set @retval = 1
			goto LExitWithCursor
		end
	end

	-- REVIEW (SteveMiller): MultiTxt$.fmt was always NULL. Don't understand the
	-- reason for @FormFmt being set to Fmt. Changed to cast(null as varbinary).
	-- Loop through all ids.
	fetch next from @myCursor into @LeId, @Class
	while @@fetch_status = 0
	begin
		select top 1 @FormId=f.Obj, @Ord=0, @Flid = 5035001, @FormTxt=f.Txt,
						@FormFmt=cast(null as varbinary)
		from LexEntry_LexemeForm lf (readuncommitted)
		join MoForm_Form f (readuncommitted) On f.Obj=lf.Dst and f.Ws=@FormEnc
		where lf.Src=@LeId
		if @@rowcount = 0 begin
			select top 1 @FormId=0, @Ord=0, @Flid = 5002003, @FormTxt=Txt,
						@FormFmt=cast(null as varbinary)
			from LexEntry_CitationForm (readuncommitted)
			where Obj=@LeId and Ws=@FormEnc
			if @@rowcount = 0 begin
				select top 1 @FormId=f.Obj, @Ord=a.Ord, @Flid=5035001, @FormTxt=f.Txt,
						@FormFmt=cast(null as varbinary)
				from LexEntry_AlternateForms a (readuncommitted)
				join MoForm_Form f (readuncommitted) On f.Obj=a.Dst and f.Ws=@FormEnc
				where a.Src=@LeId
				if @@rowcount = 0 begin
					set @FormId = 0
					set @Ord = 0
					set @Flid = 0
					set @FormTxt = '***'
				end
			end
		end
		set @FullTxt = @FormTxt

		-- Deal with homograph number.
		select @HNum=HomographNumber
		from LexEntry (readuncommitted)
		where Id=@LeId
		if @HNum > 0
			set @FullTxt = @FullTxt + '-' + cast(@HNum as nvarchar(100))

		-- Deal with conceptual model class.

		-- Deal with sense gloss.
		select top 1 @SenseId=ls.Id, @SenseGloss = isnull(g.Txt, '***'), @SenseFmt= cast(null as varbinary)
		from LexEntry_Senses mes (readuncommitted)
		left outer join LexSense ls (readuncommitted)
			On ls.Id=mes.Dst
		left outer join LexSense_Gloss g (readuncommitted)
			On g.Obj=ls.Id and g.Ws=@SenseEnc
		where mes.Src=@LeId
		order by mes.Ord
		set @FullTxt = @FullTxt + ' : ' + @SenseGloss

		insert into @DisplayNameLexEntry (LeId, Class, HNum, FullTxt,
					FormId, Ord, Flid, FormTxt, FormFmt, FormEnc,
					SenseId, SenseGloss, SenseFmt, SenseEnc)
			values (@LeId, @Class, @HNum, @FullTxt,
					@FormId, @Ord, @Flid, @FormTxt, @FormFmt, @FormEnc,
					@SenseId, @SenseGloss, @SenseFmt, @SenseEnc)
		-- Try for another one.
		fetch next from @myCursor into @LeId, @Class
	end

	set @retval = 0
	select * from @DisplayNameLexEntry order by FullTxt

LExitWithCursor:
	close @myCursor
	deallocate @myCursor

LExitNoCursor:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @retval
go

/*****************************************************************************
 * MatchingEntries
 *
 * Description:
 *	Returns a table with zero or more rows of LexEntry objects
 *	which match in one or more of the parameters.
 * Parameters:
 *	@cf = the citation form to find.
 *	@uf = the underlying form to find.
 *	@af = the allomorphs to find.
 *	@gl = the gloss to find.
 *	@wsv = vernacular ws for matching cf, uf, af
 *	@wsa = analysis ws for matching gl
 * Returns:
 *	Result set containing table of cf, uf, af, gl for each match.
 *****************************************************************************/
if object_id('MatchingEntries') is not null begin
	print 'removing proc MatchingEntries'
	drop proc MatchingEntries
end
print 'creating proc MatchingEntries'
go

CREATE    proc [MatchingEntries]
	@cf nvarchar(4000),
	@uf nvarchar(4000),
	@af nvarchar(4000),
	@gl nvarchar(4000),
	@wsv int,
	@wsa int
AS
	declare @CFTxt nvarchar(4000), @cftext nvarchar(4000),
		@UFTxt nvarchar(4000), @uftext nvarchar(4000),
		@AFTxt nvarchar(4000), @aftext nvarchar(4000),
		@GLTxt nvarchar(4000), @gltext nvarchar(4000),
		@entryID int, @senseID int, @prevID int, @ObjId int,
		@class int
	declare @fIsNocountOn int
	-- deterimine if no count is currently set to on
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- table variable to hold return information.
	declare @MatchingEntries table (
		EntryID int primary key,		-- 1
		Class int,				-- 2
		CFTxt nvarchar(4000) default '***',	-- 3
		CFWs int,				-- 4
		UFTxt nvarchar(4000) default '***',	-- 5
		UFWs int,				-- 6
		AFTxt nvarchar(4000) default '***',	-- 7
		AFWs int,				-- 8
		GLTxt nvarchar(4000) default '***',	-- 9
		GLWs int				-- 10
		)

	--==( Citation and Underlying Forms )==--

	--( We're looking for citation forms or underlying forms that match
	--( the citation or underlying forms passed in.

	-- REVIEW (SteveMiller): LexEntry_CitationForm and MoForm_Form both take
	-- writing system IDs. If you are interested in only one writing system,
	-- the query should go faster by joining on ws as well as on obj, to make
	-- better use of the indexes. If more than one writing system can be returned,
	-- we'll have to put more thought into how to retrieve the proper Txt
	-- field from the appropriate writing system.

	insert into @MatchingEntries (EntryID, Class, CFTxt, CFWs, UFTxt, UFWs, AFWs, GLWs)
		SELECT	le.[Id], le.Class$, isnull(cf.Txt, '***'), @wsv, isnull(mff.Txt, '***'), @wsv, @wsv, @wsa
		FROM LexEntry_ le (READUNCOMMITTED)
		LEFT OUTER JOIN LexEntry_CitationForm cf (READUNCOMMITTED) ON cf.Obj = le.[Id] and cf.ws = @wsv
		LEFT OUTER JOIN LexEntry_LexemeForm uf (READUNCOMMITTED) ON uf.Src = le.[Id]
		LEFT OUTER JOIN MoForm_Form mff (readuncommitted) ON mff.Obj = uf.Dst and mff.ws = @wsv
		WHERE (LOWER(RTRIM(LTRIM(cf.Txt))) LIKE LOWER(RTRIM(LTRIM(@cf))) + '%'
			OR LOWER(RTRIM(LTRIM(mff.Txt))) LIKE LOWER(RTRIM(LTRIM(@uf))) + '%')

	--==( Alternate Allomorph Forms )==--

	-- REVIEW (SteveMiller): Cursors are notoriously slow in databases. I
	-- expect these to bog down the proc as soon as we get any quantity of
	-- data. As of this writing, we have 62 records in LexEntry.

	--( We're looking for allomorph forms that match the allomorph form
	--( passed in.

	declare @curAllos CURSOR
	set @curAllos = CURSOR FAST_FORWARD for
		select le.id, le.Class$, amf.Txt
		from LexEntry_ le (readuncommitted)
		join LexEntry_AlternateForms lea (readuncommitted) ON lea.Src = le.id
		join MoForm_Form amf (readuncommitted) ON amf.Obj = lea.Dst and amf.ws = @wsv
		where LOWER(RTRIM(LTRIM(amf.Txt))) LIKE LOWER(RTRIM(LTRIM(@af))) + '%'

	OPEN @curAllos
	FETCH NEXT FROM @curAllos INTO @entryID, @class, @aftext
	WHILE @@FETCH_STATUS = 0
	BEGIN
		if @prevID = @entryID
			set @AFTxt = @AFTxt + '; ' + @aftext
		else
			set @AFTxt = @aftext
		select @ObjId=EntryID
		from @MatchingEntries
		where EntryID=@entryID
		if @@ROWCOUNT = 0
			insert into @MatchingEntries (EntryID, Class, AFTxt, CFWs, UFWs, AFWs, GLWs)
			values (@entryID, @class, @AFTxt, @wsv, @wsv, @wsv, @wsa)
		else
			update @MatchingEntries
			set AFTxt=@AFTxt
			where EntryID=@entryID
		set @prevID = @entryID
		FETCH NEXT FROM @curAllos INTO @entryID, @class, @aftext
	END
	CLOSE @curAllos
	DEALLOCATE @curAllos

	--==( Senses )==--

	declare @curSenses CURSOR
	declare @OwnerId int, @OwnFlid int
	set @prevID = 0
	set @curSenses = CURSOR FAST_FORWARD for
		select Obj, Txt
		from LexSense_Gloss (readuncommitted)
		where LOWER(RTRIM(LTRIM(Txt))) LIKE LOWER(RTRIM(LTRIM(@gl))) + '%' and ws = @wsa

	OPEN @curSenses
	FETCH NEXT FROM @curSenses INTO @senseId, @gltext
	WHILE @@FETCH_STATUS = 0
	BEGIN
		set @OwnFlid = 0
		set @entryID = @SenseId
		-- Loop until we find an owning flid of 5002011.
		while @OwnFlid != 5002011
		begin
			select 	@OwnerId=isnull(Owner$, 0), @OwnFlid=OwnFlid$
			from	CmObject (readuncommitted)
			where	Id=@entryID
			set @entryID=@OwnerId
			if @OwnerId = 0
				return 1
		end

		select @class=class$
		from CmObject (readuncommitted)
		where id=@entryID

		if @prevID = @senseId
			set @GLTxt = @GLTxt + '; ' + @gltext
		else
			set @GLTxt = @gltext

		select @ObjId=EntryID
		from @MatchingEntries
		where EntryID=@entryID

		if @@ROWCOUNT = 0
			insert into @MatchingEntries (EntryID, Class, GLTxt, CFWs, UFWs, AFWs, GLWs)
			values (@entryID, @class, @GLTxt, @wsv, @wsv, @wsv, @wsa)
		else
			update @MatchingEntries
			set GLTxt=@GLTxt
			where EntryID=@entryID

		set @prevID = @senseId
		FETCH NEXT FROM @curSenses INTO @senseId, @gltext
	END
	CLOSE @curSenses
	DEALLOCATE @curSenses

	--==( Final Pass )==--

	-- REVIEW (SteveMiller): This "final pass" can probably be enhanced by
	-- moving the logic into the query
	--
	-- 	select * from @MatchingEntries
	--
	-- at the bottom of this proc. (This query is the true "final pass"
	-- at the data for the proc.)

	-- Try to find some kind of string for any items that have not matched,

	declare @curFinalPass CURSOR, @rowcount int, @wsFoundling int
	set @curFinalPass = CURSOR FAST_FORWARD for
		select EntryID, CFTxt, UFTxt, AFTxt, GLTxt
		from @MatchingEntries
		where	CFTxt = '***'
			or UFTxt = '***'
			or AFTxt = '***'
			or GLTxt = '***'

	OPEN @curFinalPass
	FETCH NEXT FROM @curFinalPass INTO @entryID, @cftext, @uftext, @aftext, @gltext
	WHILE @@FETCH_STATUS = 0
	BEGIN
		set @rowcount = 0
		if @cftext = '***'
		begin
			-- Try a ws other than @wsv, since it wasn't there earlier.
			select top 1 @CFTxt=Txt
			from LexEntry_CitationForm (readuncommitted)
			where Obj = @entryID
			set @rowcount = @@rowcount

			if @rowcount = 0 -- Nothing for any other ws, so try getting it from the *last* allomorph.
			begin
				-- See if the lexeme allomorph has it for @wsv.
				select @CFTxt=amf.Txt, @wsFoundling=amf.Ws
				from LexEntry_ le (readuncommitted)
				join LexEntry_LexemeForm lea (readuncommitted) ON lea.Src = le.id
				join MoForm_Form amf (readuncommitted) ON amf.Obj = lea.Dst and amf.ws = @wsv
				where le.id = @entryID
				set @rowcount = @@rowcount
			end

			if @rowcount = 0 -- Try any other ws on the lexeme allomorph
			begin
				select top 1 @CFTxt=amf.Txt, @wsFoundling=amf.Ws
				from LexEntry_ le (readuncommitted)
				join LexEntry_LexemeForm lea (readuncommitted) ON lea.Src = le.id
				join MoForm_Form amf (readuncommitted) ON amf.Obj = lea.Dst
				join LanguageProject_CurrentVernacularWritingSystems ws (readuncommitted) ON ws.Dst = amf.Ws
				where le.id = @entryID
				ORDER BY ws.ord
				set @rowcount = @@rowcount
			end

			if @rowcount > 0 -- Found one somewhere.
			begin
				update @MatchingEntries
				set CFTxt=@CFTxt, GLWs=@wsFoundling
				where EntryID=@entryID
			end
		end
		if @uftext = '***'
		begin
			select top 1 @UFTxt=uff.Txt
			from LexEntry_LexemeForm uf (readuncommitted)
			join MoForm_Form uff (readuncommitted) ON uff.Obj = uf.Dst and uff.ws = @wsv
			where uf.Src = @entryID
			set @rowcount = @@rowcount

			if @rowcount = 0 -- Nothing for the @wsv ws, so try something for any ws on the real UF.
			begin
				select top 1 @UFTxt=uff.Txt, @wsFoundling=uff.Ws
				from LexEntry_LexemeForm uf (readuncommitted)
				join MoForm_Form uff (readuncommitted) ON uff.Obj = uf.Dst
				join LanguageProject_CurrentVernacularWritingSystems ws (readuncommitted) ON ws.Dst = uff.Ws
				where uf.Src = @entryID
				ORDER BY ws.Ord
				set @rowcount = @@rowcount
			end

--			if @rowcount = 0 -- Try @wsv on the *last* allomorph
--			begin
--				select top 1 @UFTxt=amf.Txt, @wsFoundling=amf.Ws
--				from LexEntry_ le (readuncommitted)
--				join LexEntry_Allomorphs lea (readuncommitted) ON lea.Src = le.id
--				join MoForm_Form amf (readuncommitted) ON amf.Obj = lea.Dst and amf.ws = @wsv
--				where le.id = @entryID
--				ORDER BY lea.Ord DESC
--				set @rowcount = @@rowcount
--			end
--
--			if @rowcount = 0 -- Try any other ws on the *last* allomorph
--			begin
--				select top 1 @UFTxt=amf.Txt, @wsFoundling=amf.Ws
--				from LexEntry_ le (readuncommitted)
--				join LexEntry_Allomorphs lea (readuncommitted) ON lea.Src = le.id
--				join MoForm_Form amf (readuncommitted) ON amf.Obj = lea.Dst
--				join LanguageProject_CurrentVernacularWritingSystems ws (readuncommitted) ON ws.Dst = amf.Ws
--				where le.id = @entryID
--				ORDER BY lea.Ord DESC, ws.ord
--				set @rowcount = @@rowcount
--			end

			if @rowcount > 0 -- Found one somewhere.
			begin
				update @MatchingEntries
				set UFTxt=@UFTxt, GLWs=@wsFoundling
				where EntryID=@entryID
			end
		end
		if @aftext = '***'
		begin
			select top 1 @AFTxt=amf.Txt
			from LexEntry_ le (readuncommitted)
			join LexEntry_AlternateForms lea (readuncommitted) ON lea.Src = le.id
			join MoForm_Form amf (readuncommitted) ON amf.Obj = lea.Dst and amf.ws = @wsv
			where le.id = @entryID
			set @rowcount = @@rowcount

			if @rowcount = 0 -- Nothing for the @wsv ws, so try all of them.
			begin
				SELECT top 1 @AFTxt=amf.Txt, @wsFoundling=amf.Ws
				from LexEntry_ le (readuncommitted)
				join LexEntry_AlternateForms lea (readuncommitted) ON lea.Src = le.id
				join MoForm_Form amf (readuncommitted) ON amf.Obj = lea.Dst
				join LanguageProject_CurrentVernacularWritingSystems ws (readuncommitted) ON ws.Dst = amf.Ws
				where le.id = @entryID
				ORDER BY ws.Ord
				set @rowcount = @@rowcount
			end

			if @rowcount > 0 -- Found one somewhere.
			begin
				update @MatchingEntries
				set AFTxt=@AFTxt, GLWs=@wsFoundling
				where EntryID=@entryID
			end
		end
		if @gltext = '***'
		begin
			SELECT top 1 @GLTxt=lsg.Txt, @wsFoundling=lsg.Ws
			FROM dbo.fnGetSensesInEntry$(@entryID)
			join LexSense_Gloss lsg (readuncommitted) On lsg.Obj=SenseId and lsg.ws = @wsa
			set @rowcount = @@rowcount

			if @rowcount = 0 -- Nothing for the @wsa ws, so try all of them.
			begin
				SELECT top 1 @GLTxt=lsg.Txt, @wsFoundling=lsg.Ws
				FROM dbo.fnGetSensesInEntry$(@entryID)
				join LexSense_Gloss lsg (readuncommitted) On lsg.Obj=SenseId
				join LanguageProject_CurrentAnalysisWritingSystems ws (readuncommitted) ON ws.Dst = lsg.Ws
				ORDER BY ws.Ord
				set @rowcount = @@rowcount
			end

			if @rowcount > 0 -- Found one somewhere.
			begin
				update @MatchingEntries
				set GLTxt=@GLTxt, GLWs=@wsFoundling
				where EntryID=@entryID
			end
		end
		FETCH NEXT FROM @curFinalPass INTO @entryID, @cftext, @uftext, @aftext, @gltext
	END
	CLOSE @curFinalPass
	DEALLOCATE @curFinalPass

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	select *
	from @MatchingEntries

go

/***********************************************************************************************
 * Function: GetHeadwordsForEntriesOrSenses
 *
 * Description:
 *	Generate the "headword" string for all LexEntry or LexSense objects that are the targets
 * 	of either LexReference_Targets or LexEntry_MainEntriesOrSenses.
 *
 * Parameters:
 * 	None.
 * Returns:
 *	A table containing headword strings for LexEntry and LexSense objects which are
 * 	targets of cross references in the lexicon. The table also contains the object id and class
 *  id for the LexEntry and LexSense objects.
 **********************************************************************************************/
if object_id('GetHeadwordsForEntriesOrSenses') is not null begin
	print 'removing function GetHeadwordsForEntriesOrSenses'
	drop function [GetHeadwordsForEntriesOrSenses]
end
print 'creating function GetHeadwordsForEntriesOrSenses'
go

CREATE FUNCTION dbo.GetHeadwordsForEntriesOrSenses ()
RETURNS @ObjIdInfo TABLE (
	ObjId int,
	ClassId int,
	Headword nvarchar(4000))
AS
BEGIN

DECLARE @nvcAllo nvarchar(4000), @nHomograph int, @nvcPostfix nvarchar(4000),
		@nvcPrefix nvarchar(4000), @nvcHeadword nvarchar(4000)
DECLARE @objId int, @objOwner int, @objOwnFlid int, @objClass int, @hvoEntry int,
	@nvcSenseNum nvarchar(4000), @objId2 int
INSERT INTO @ObjIdInfo (ObjId, ClassId, Headword)
	SELECT Dst, NULL, NULL FROM LexReference_Targets
	UNION
	SELECT Dst, NULL, NULL FROM LexEntry_MainEntriesOrSenses
DECLARE cur CURSOR local static forward_only read_only FOR
	SELECT id, Class$, Owner$, OwnFlid$
		FROM CmObject
		WHERE Id in (SELECT ObjId FROM @ObjIdInfo)
OPEN cur
FETCH NEXT FROM cur INTO @objId, @objClass, @objOwner, @objOwnFlid
WHILE @@FETCH_STATUS = 0
BEGIN
	IF @objClass = 5002 BEGIN -- LexEntry
		SET @hvoEntry=@objId
	END
	ELSE BEGIN
		IF @objOwnFlid = 5002011 BEGIN -- LexEntry_Senses
			SET @hvoEntry=@objOwner
		END
		ELSE BEGIN
			while @objOwnFlid != 5002011
			begin
				set @objId2=@objOwner
				select 	@objOwner=isnull(Owner$, 0), @objOwnFlid=OwnFlid$
				from	CmObject (readuncommitted)
				where	Id=@objId2
				if @objOwner = 0 begin
					SET @objOwnFlid = 5002011
				end
			end
			SET @hvoEntry=@objOwner
		END
	END

	SELECT @nvcAllo=f.Txt, @nHomograph=le.HomographNumber, @nvcPostfix=t.Postfix,
			@nvcPrefix=t.Prefix, @nvcSenseNum=s.SenseNum
		FROM LexEntry le
		LEFT OUTER JOIN LexEntry_LexemeForm a on a.Src=le.id
		LEFT OUTER JOIN MoForm_Form f on f.Obj=a.Dst
		LEFT OUTER JOIN MoForm mf on mf.Id=a.Dst
		LEFT OUTER JOIN MoMorphType t on t.Id=mf.MorphType
		LEFT OUTER JOIN dbo.fnGetSensesInEntry$ (@hvoEntry) s on s.SenseId=@objId
		WHERE le.Id = @hvoEntry

	IF @nvcPrefix is null SET @nvcHeadword=@nvcAllo
	ELSE SET @nvcHeadword=@nvcPrefix+@nvcAllo
	IF @nvcPostfix is not null SET @nvcHeadword=@nvcHeadword+@nvcPostfix
	IF @nHomograph <> 0 SET @nvcHeadword=@nvcHeadword+CONVERT(nvarchar(20), @nHomograph)
	IF @nvcSenseNum is not null SET @nvcHeadword=@nvcHeadword+' '+@nvcSenseNum
	UPDATE @ObjIdInfo SET Headword=@nvcHeadword, ClassId=@objClass
		WHERE ObjId=@objId

	FETCH NEXT FROM cur INTO @objId, @objClass, @objOwner, @objOwnFlid
END
CLOSE cur
DEALLOCATE cur

RETURN
END
go

--( FDB-98 -- Fix insert field$ trigger
IF OBJECT_ID('TR_Field$_UpdateModel_InsLast') IS NOT NULL BEGIN
	PRINT 'removing trigger TR_Field$_UpdateModel_InsLast'
	DROP TRIGGER TR_Field$_UpdateModel_InsLast
END
GO
PRINT 'creating trigger TR_Field$_UpdateModel_InsLast'
GO

CREATE TRIGGER TR_Field$_UpdateModel_InsLast ON Field$ FOR INSERT
AS
	DECLARE
		@nErr INT,
		@nClassid INT,
		@nAbstract BIT,
		@nLoopLevel TINYINT,
		@fExit BIT

	DECLARE @tblSubclasses TABLE (ClassId INT, Abstract BIT, ClassLevel TINYINT)

	SELECT @nClassId = Class FROM inserted
	SET @nLoopLevel = 1

	--==( Outer loop: all the classes for the level )==--

	--( This insert is necessary for any subclasses. It also
	--( gets Class$.Abstract for updating the CreateObject_*
	--( stored procedure.

	INSERT INTO @tblSubclasses
	SELECT @nClassId, c.Abstract, @nLoopLevel
	FROM Class$ c
	WHERE c.Id = @nClassId

	--( Rebuild CreateObject_*

	SELECT @nAbstract = Abstract FROM @tblSubClasses

	IF @nAbstract != 1 BEGIN
		EXEC @nErr = DefineCreateProc$ @nClassId
		IF @nErr <> 0 GOTO LFail
	END

	SET @fExit = 0
	WHILE @fExit = 0 BEGIN

		--( Inner loop: update all classes subclassed from the previous
		--( set of classes.

		SELECT TOP 1 @nClassId = ClassId, @nAbstract = Abstract
		FROM @tblSubclasses
		WHERE ClassLevel = @nLoopLevel
		ORDER BY ClassId

		WHILE @@ROWCOUNT > 0 BEGIN

			--( Update the view

			EXEC @nErr = UpdateClassView$ @nClassId, 1
			IF @nErr <> 0 GOTO LFail

			--( Get next class

			SELECT TOP 1 @nClassId = ClassId, @nAbstract = Abstract
			FROM @tblSubclasses
			WHERE ClassLevel = @nLoopLevel AND ClassId > @nClassId
			ORDER BY ClassId
		END

		--( Load outer loop with next level
		SET @nLoopLevel = @nLoopLevel + 1

		INSERT INTO @tblSubclasses
		SELECT c.Id, c.Abstract, @nLoopLevel
		FROM @tblSubClasses sc
		JOIN Class$ c ON c.Base = sc.ClassId
		WHERE sc.ClassLevel = @nLoopLevel - 1

		IF @@ROWCOUNT = 0
			SET @fExit = 1
	END

	RETURN

LFail:
	ROLLBACK TRANSACTION
	RETURN

GO

EXEC sp_settriggerorder 'TR_Field$_UpdateModel_InsLast', 'last', 'INSERT'
GO

---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200073
begin
	update Version$ set DbVer = 200074
	COMMIT TRANSACTION
	print 'database updated to version 200074'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200073 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
