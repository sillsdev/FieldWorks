-- Update database from version 200075 to 200076
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

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
	-- ( Class ids are:
	-- (   5027	MoAffixAllomorph
	-- (   5045	MoStemAllomorph
	-- ( --5029	MoAffixProcess is not used at this point.
	-- ( Owner Field ids are:
	-- (   5002029	LexEntry_LexemeForm
	-- (   5002030	LexEntry_AlternateForms
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
				@SenseWs=SenseEnc, @AlloWs=FormEnc,
				@CfTxt=FormTxt, @CfFmt=FormFmt, @CfWs=FormEnc
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
		-- ( We will try to get @FormTxt from objects in this order:
		-- ( 1. Citation form
		-- ( 2. Lexeme form
		-- ( 3. Last alternate form
		-- ( 4. 'no form available'
		-- Citation form
		select top 1 @FormId=0, @Ord=0, @Flid = 5002003, @FormTxt=Txt,
					@FormFmt=cast(null as varbinary)
		from LexEntry_CitationForm (readuncommitted)
		where Obj=@LeId and Ws=@FormEnc
		if @@rowcount = 0 begin
			-- Lexeme form
			select top 1 @FormId=f.Obj, @Ord=0, @Flid = 5035001, @FormTxt=f.Txt,
							@FormFmt=cast(null as varbinary)
			from LexEntry_LexemeForm lf (readuncommitted)
			join MoForm_Form f (readuncommitted) On f.Obj=lf.Dst and f.Ws=@FormEnc
			where lf.Src=@LeId
			if @@rowcount = 0 begin
				-- First alternate form
				select top 1 @FormId=f.Obj, @Ord=a.Ord, @Flid=5035001, @FormTxt=f.Txt,
						@FormFmt=cast(null as varbinary)
				from LexEntry_AlternateForms a (readuncommitted)
				join MoForm_Form f (readuncommitted) On f.Obj=a.Dst and f.Ws=@FormEnc
				where a.Src=@LeId
				ORDER BY a.Ord
				if @@rowcount = 0 begin
					-- ( Give up.
					set @FormId = 0
					set @Ord = 0
					set @Flid = 0
					set @FormTxt = 'no form available'
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

---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200075
begin
	update Version$ set DbVer = 200076
	COMMIT TRANSACTION
	print 'database updated to version 200075'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200075 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
