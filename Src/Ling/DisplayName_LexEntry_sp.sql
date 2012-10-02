
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
	from LangProject_CurAnalysisWss ce
	join LgWritingSystem le On le.Id = ce.Dst
	order by ce.Src, ce.ord
	select top 1 @FormEnc=le.Id
	from LangProject_CurVernWss ce
	join LgWritingSystem le On le.Id = ce.Dst
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
			from CmObject
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
			join CmObject cmo
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
		from LexEntry_CitationForm
		where Obj=@LeId and Ws=@FormEnc
		if @@rowcount = 0 begin
			-- Lexeme form
			select top 1 @FormId=f.Obj, @Ord=0, @Flid = 5035001, @FormTxt=f.Txt,
							@FormFmt=cast(null as varbinary)
			from LexEntry_LexemeForm lf
			join MoForm_Form f On f.Obj=lf.Dst and f.Ws=@FormEnc
			where lf.Src=@LeId
			if @@rowcount = 0 begin
				-- First alternate form
				select top 1 @FormId=f.Obj, @Ord=a.Ord, @Flid=5035001, @FormTxt=f.Txt,
						@FormFmt=cast(null as varbinary)
				from LexEntry_AlternateForms a
				join MoForm_Form f On f.Obj=a.Dst and f.Ws=@FormEnc
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
		from LexEntry
		where Id=@LeId
		if @HNum > 0
			set @FullTxt = @FullTxt + '-' + cast(@HNum as nvarchar(100))

		-- Deal with conceptual model class.

		-- Deal with sense gloss.
		select top 1 @SenseId=ls.Id, @SenseGloss = isnull(g.Txt, '***'), @SenseFmt= cast(null as varbinary)
		from LexEntry_Senses mes
		left outer join LexSense ls
			On ls.Id=mes.Dst
		left outer join LexSense_Gloss g
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
