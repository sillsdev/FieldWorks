-- Update database from version 200204 to 200205
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- FWC-10: Reload stored procedure CreateCmBaseAnnotation
-------------------------------------------------------------------------------

if object_id('CreateCmBaseAnnotation') is not null begin
	print 'removing proc CreateCmBaseAnnotation'
	drop proc [CreateCmBaseAnnotation]
end
go


----------------------------------------------------------------
-- This stored procedure provides an efficient way to create annotations.
-- It does no checking!
----------------------------------------------------------------

CREATE  proc [CreateCmBaseAnnotation]	@Owner int = null,
	@annotationType int = null,
	@InstanceOf int = null,
	@BeginObject int = null,
	@CmBaseAnnotation_Flid int = 0,	@CmBaseAnnotation_BeginOffset int = 0,	@CmBaseAnnotation_EndOffset int = 0as
	declare @fIsNocountOn int, @Err int
	declare @ObjId int, @guid uniqueidentifier

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on
	if @InstanceOf = 0 set @instanceOf = null

	set @guid = newid()
	-- leave ownord$ null for owning collection.
	-- 6001044 is LexDb_Annotations
	-- 37 is the class of CmBaseAnnotation.
	insert into [CmObject] with (rowlock) ([Guid$], [Class$], [Owner$], [OwnFlid$])
		values (@guid, 37, @Owner, 6001044)
	set @Err = @@error
	set @ObjId = @@identity
	if @Err <> 0 begin
		raiserror('SQL Error %d: Unable to create the new object', 16, 1, @Err)
		goto LCleanUp
	end
	insert into [CmAnnotation] ([Id], [AnnotationType], [InstanceOf]) 		values (@ObjId, @annotationType, @InstanceOf)	set @Err = @@error	if @Err <> 0 goto LCleanUp	insert into [CmBaseAnnotation] ([Id],[BeginOffset],[Flid],[EndOffset],[BeginObject],[EndObject]) 		values (@ObjId, @CmBaseAnnotation_BeginOffset, @CmBaseAnnotation_Flid, @CmBaseAnnotation_EndOffset,  @BeginObject, @BeginObject)	set @Err = @@error
LCleanUp:

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on


	return @Err

GO

-------------------------------------------------------------------------------
-- FWC-10: Reload stored procedure CreateParserProblemAnnotation
-------------------------------------------------------------------------------
if object_id('CreateParserProblemAnnotation') is not null begin
	print 'removing proc CreateParserProblemAnnotation'
	drop proc CreateParserProblemAnnotation
end
print 'creating proc CreateParserProblemAnnotation'
go

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
		37, -- 37
		@annID output,
		null,
		@lpid,
		6001044, -- kflidLangProject_Annotations
		25, --25
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


if object_id('SetAgentEval') is not null begin
	print 'removing proc SetAgentEval'
	drop proc SetAgentEval
end
go
print 'creating proc SetAgentEval'
go

-------------------------------------------------------------------------------
-- FWC-10: Reload stored procedure DisplayName_LexEntry
-------------------------------------------------------------------------------
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
-------------------------------------------------------------------------------
-- FWC-10: Reload stored procedure DisplayName_Msa
-------------------------------------------------------------------------------
if object_id('DisplayName_Msa') is not null begin
	drop proc DisplayName_MSA
end
go
print 'creating proc DisplayName_Msa'
go










create proc [DisplayName_Msa]
	@XMLIds ntext = null, @ShowForm bit = 1
as

declare @retval int, @fIsNocountOn int,
	@MsaId int, @MsaClass int, @MsaForm nvarchar(4000),
	@FormId int, @FormClass int, @FormOwner int, @FormFlid int,
		@FormTxt nvarchar(4000), @FormFmt int, @FormEnc int,
	@SenseId int, @SenseTxt nvarchar(4000), @SenseFmt int, @SenseEnc int,
	@POSaID int, @POSaTxt nvarchar(4000), @POSaFmt int, @POSaEnc int,
	@POSbID int, @POSbTxt nvarchar(4000), @POSbFmt int, @POSbEnc int,
	@SlotTxt nvarchar(4000), @SlotsTxt nvarchar(4000), @rowCnt int,
	@myCursor CURSOR

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- table variable to hold return information.
	declare @DisplayNameMsa table (
		MsaId int,	-- 1
		MsaClass int,	-- 2
		MsaForm nvarchar(4000),	-- 3
		FormId int,	-- 4
		FormClass int,	-- 5
		FormOwner int,	-- 6
		FormFlid int,	-- 7
		--( See the notes under string tables in FwCore.sql about the
		--( COLLATE clause.
		FormTxt NVARCHAR(4000) COLLATE Latin1_General_BIN, -- 8
		FormFmt int,	-- 9
		FormEnc int,	-- 10
		SenseId int,	-- 11
		--( See the notes under string tables in FwCore.sql about the
		--( COLLATE clause.
		SenseTxt NVARCHAR(4000) COLLATE Latin1_General_BIN, -- 12
		SenseFmt int,	-- 13
		SenseEnc int,	-- 14
		POSaID int,	-- 15
		--( See the notes under string tables in FwCore.sql about the
		--( COLLATE clause.
		POSaTxt NVARCHAR(4000) COLLATE Latin1_General_BIN, --16
		POSaFmt int,	-- 17
		POSaEnc int,	-- 18
		POSbID int,	-- 19
		--( See the notes under string tables in FwCore.sql about the
		--( COLLATE clause.
		POSbTxt NVARCHAR(4000) COLLATE Latin1_General_BIN, --20
		POSbFmt int,	-- 21
		POSbEnc int	-- 22
		)

	--( Need to deal with: @FormClass.

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
		--( See the notes under string tables in FwCore.sql about the
		--( COLLATE clause.
		FormTxt NVARCHAR(4000) COLLATE Latin1_General_BIN,
		FormFmt int,
		FormEnc int,
		SenseId int default 0,
		SenseGloss nvarchar(4000),
		SenseFmt int,
		SenseEnc int
		)

	--( class ids are:
	--( 5001	MoStemMsa
	--( 5031	MoDerivAffMsa
	--( 5032	MoDerivStepMsa
	--( 5038	MoInflAffMsa
	--( 5117	MoUnclassifiedAffixMsa

	if @XMLIds is null begin
		insert #DNLE exec DisplayName_LexEntry null
		-- Do all MSAes.
		set @myCursor = CURSOR FAST_FORWARD for
			select Id, Class$
			from CmObject
			where Class$ IN (5001, 5031, 5032, 5038, 5117)
			order by Id
		open @myCursor
	end
	else begin
		-- Do MSAes provided in xml string.
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
				and cmo.Class$ IN (5001, 5031, 5032, 5038, 5117)
			order by ol.[Id]
		open @myCursor
		-- Turn loose of the handle
		exec sp_xml_removedocument @hdoc
		if @@error <> 0 begin
			set @retval = 1
			goto LExitWithCursor
		end
	end

	-- Loop through all ids.
	fetch next from @myCursor into @MsaId, @MsaClass
	while @@fetch_status = 0
	begin
		-- Get display name for LexEntry.
		declare @LeId int, @XMLLEId nvarchar(4000), @cnt int

		select @LeId=Owner$
		from CmObject
		where Id=@MsaId

		set @XMLLEId = '<root><Obj Id="' + cast(@LeId as nvarchar(100)) + '"/></root>'

		if @XMLIds is not null
			insert #DNLE exec DisplayName_LexEntry @XMLLEId
		select @MsaForm=FullTxt,
			@FormId=FormId, @FormFlid=Flid, @FormTxt=FormTxt, @FormFmt=FormFmt, @FormEnc=FormEnc,
			@SenseId=SenseId, @SenseTxt=SenseGloss, @SenseFmt=SenseFmt, @SenseEnc=SenseEnc
		from #DNLE
		where LeId=@LeId
		if @ShowForm = 0
			set @MsaForm = ''
		else
			set @MsaForm = @MsaForm + ' '
		if @FormId = 0
			set @FormOwner = @LeId
		else
			set @FormOwner = @FormId
		if @XMLIds is not null
			truncate table #DNLE

		-- REVIEW (SteveMiller): MultiTxt$.fmt was always NULL. Don't understand the
		-- reason for @POSaFmt being set to Fmt. Changed to cast(null as varbinary).

		if @MsaClass=5001 begin		--MoStemMsa
			select top 1 @POSaID=pos.Id, @POSaTxt=isnull(nm.Txt, '***'),
					@POSaFmt=cast(null as varbinary), @POSaEnc=nm.Ws
			from MoStemMsa msa
			left outer join PartOfSpeech pos On pos.Id=msa.PartOfSpeech
			left outer join CmPossibility_Name nm On nm.Obj=pos.Id and nm.Ws=@SenseEnc
			where msa.Id=@MsaId
			set @MsaForm = @MsaForm + 'stem/root: ' + @POSaTxt
		end
		else if @MsaClass=5038 begin --MoInflAffMsa
			select top 1 @POSaID=pos.Id, @POSaTxt=isnull(nm.Txt, '***'),
					@POSaFmt=cast(null as varbinary), @POSaEnc=nm.Ws
			from MoInflAffMsa msa
			left outer join PartOfSpeech pos On pos.Id=msa.PartOfSpeech
			left outer join CmPossibility_Name nm On nm.Obj=pos.Id and nm.Ws=@SenseEnc
			where msa.Id=@MsaId

			SET @SlotsTxt=''

			select top 1 @SlotTxt=slot_nm.Txt
			from MoInflAffMsa_Slots msa_as
			join MoInflAffixSlot slot On slot.Id=msa_as.Dst
			join MoInflAffixSlot_Name slot_nm On slot_nm.Obj=slot.Id and slot_nm.Ws=@SenseEnc
			where @MsaId=msa_as.Src
			ORDER BY slot_nm.Txt
			SET @cnt = @@rowcount

			while (@cnt > 0)
			BEGIN
				IF @SlotsTxt=''
					SET @SlotsTxt=@SlotTxt
				ELSE
					SET @SlotsTxt=@SlotsTxt + '/' + @SlotTxt

				select top 1 @SlotTxt=slot_nm.Txt
				from MoInflAffMsa_Slots msa_as
				join MoInflAffixSlot slot On slot.Id=msa_as.Dst
				join MoInflAffixSlot_Name slot_nm On slot_nm.Obj=slot.Id and slot_nm.Ws=@SenseEnc
				where @MsaId=msa_as.Src AND slot_nm.Txt > @SlotTxt
				ORDER BY slot_nm.Txt
				SET @cnt = @@rowcount
			END

			if @SlotsTxt='' SET @SlotsTxt=null
			set @MsaForm = @MsaForm + 'inflectional: ' + @POSaTxt + ':(' + isnull(@SlotsTxt, '***') + ')'
		end
		else if @MsaClass=5031 begin	--MoDerivAffMsa
			-- FromPartOfSpeech
			select top 1 @POSaID=pos.Id, @POSaTxt=isnull(nm.Txt, '***'),
					@POSaFmt=cast(null as varbinary), @POSaEnc=nm.Ws
			from MoDerivAffMsa msa
			left outer join PartOfSpeech pos On pos.Id=msa.FromPartOfSpeech
			left outer join CmPossibility_Name nm On nm.Obj=pos.Id and nm.Ws=@SenseEnc
			where msa.Id=@MsaId
			-- ToPartOfSpeech
			select top 1 @POSbID=pos.Id, @POSbTxt=isnull(nm.Txt, '***'),
					@POSbFmt=cast(null as varbinary), @POSbEnc=nm.Ws
			from MoDerivAffMsa msa
			left outer join PartOfSpeech pos On pos.Id=msa.ToPartOfSpeech
			left outer join CmPossibility_Name nm On nm.Obj=pos.Id and nm.Ws=@SenseEnc
			where msa.Id=@MsaId
			set @MsaForm = @MsaForm + 'derivational: ' + @POSaTxt + ' to ' + @POSbTxt
		end
		else if @MsaClass=5117 begin	--MoUnclassifiedAffixMsa
			select top 1 @POSaID=pos.Id, @POSaTxt=isnull(nm.Txt, '***'),
					@POSaFmt=cast(null as varbinary), @POSaEnc=nm.Ws
			from MoUnclassifiedAffixMsa msa
			left outer join PartOfSpeech pos On pos.Id=msa.PartOfSpeech
			left outer join CmPossibility_Name nm On nm.Obj=pos.Id and nm.Ws=@SenseEnc
			where msa.Id=@MsaId
			set @MsaForm = @MsaForm + 'unclassified: ' + @POSaTxt
		end
		else if @MsaClass=5032 begin	--MoDerivStepMsa
			select top 1 @POSaID=pos.Id, @POSaTxt=isnull(nm.Txt, '***'),
					@POSaFmt=cast(null as varbinary), @POSaEnc=nm.Ws
			from MoDerivStepMsa msa
			left outer join PartOfSpeech pos On pos.Id=msa.PartOfSpeech
			left outer join CmPossibility_Name nm On nm.Obj=pos.Id and nm.Ws=@SenseEnc
			where msa.Id=@MsaId
			set @MsaForm = @MsaForm + ' : ' + @POSaTxt
		end

		--Put everything in temporary table
		insert @DisplayNameMsa (MsaId, MsaClass,
			MsaForm, FormId, FormClass, FormOwner, FormFlid, FormTxt, FormFmt, FormEnc,
			SenseId, SenseTxt, SenseFmt, SenseEnc,
			POSaID, POSaTxt, POSaFmt, POSaEnc,
			POSbID, POSbTxt, POSbFmt, POSbEnc)
		values (@MsaId, @MsaClass, @MsaForm,
			@FormId, @FormClass, @FormOwner, @FormFlid, @FormTxt, @FormFmt, @FormEnc,
			@SenseId, @SenseTxt, @SenseFmt, @SenseEnc,
			@POSaID, @POSaTxt, @POSaFmt, @POSaEnc,
			@POSbID, @POSbTxt, @POSbFmt, @POSbEnc)
		-- Try for another MSA.
		fetch next from @myCursor into @MsaId, @MsaClass
	end

	set @retval = 0
	select * from @DisplayNameMsa order by MsaForm

LExitWithCursor:
	close @myCursor
	deallocate @myCursor

LExitNoCursor:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	drop table #DNLE

	return @retval
go

-------------------------------------------------------------------------------
-- FWC-10: Reload stored procedure DisplayName_PhPhonContext
-------------------------------------------------------------------------------
if object_id('DisplayName_PhPhonContext') is not null begin
	drop proc DisplayName_PhPhonContext
end
go
print 'creating proc DisplayName_PhPhonContext'
go

create proc DisplayName_PhPhonContext
	@XMLIds ntext = null
as
	declare @retval int, @fIsNocountOn int,
		@CtxId int, @CtxForm nvarchar(4000),
		@myCursor CURSOR

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	--Table variable.
	declare @DisplayNamePhPhonContext table (
		CtxId int primary key,
		CtxForm nvarchar(4000)
		)

	if @XMLIds is null begin
		-- Do all contexts.
		set @myCursor = CURSOR FAST_FORWARD for
			select Id
			from PhPhonContext
			order by id
		open @myCursor
	end
	else begin
		-- Do contexts provided in xml string.
		declare @hdoc int
		exec sp_xml_preparedocument @hdoc output, @XMLIds
		if @@error <> 0 begin
			set @retval = 1
			goto LExitNoCursor
		end
		set @myCursor = CURSOR FAST_FORWARD for
			select cmo.Id
			from	openxml(@hdoc, '/root/Obj')
			with ([Id] int) as ol
			-- Remove all pretenders, since they won't 'join'.
			join CmObject cmo
				On ol.Id=cmo.Id
				and cmo.Class$ IN (5082, 5083, 5085, 5086, 5087)
			order by ol.[Id]
		open @myCursor
		-- Turn loose of the handle
		exec sp_xml_removedocument @hdoc
		if @@error <> 0 begin
			set @retval = 1
			goto LExitWithCursor
		end
	end

	-- Loop through all ids.
	fetch next from @myCursor into @CtxId
	while @@fetch_status = 0
	begin
		exec @retval = DisplayName_PhPhonContextID @CtxId, @CtxForm output
		if @retval > 0 begin
			delete @DisplayNamePhPhonContext
			goto LExitWithCursor
		end
		-- Update the temporary table
		insert @DisplayNamePhPhonContext (CtxId, CtxForm)
		values (@CtxId, @CtxForm)

		-- Try for another one.
		fetch next from @myCursor into @CtxId
	end

	select * from @DisplayNamePhPhonContext
	set @retval = 0

LExitWithCursor:
	close @myCursor
	deallocate @myCursor

LExitNoCursor:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	return @retval
go

-------------------------------------------------------------------------------
-- FWC-10: Reload stored procedure DisplayName_PhPhonContextID
-------------------------------------------------------------------------------
if object_id('DisplayName_PhPhonContextID') is not null begin
	drop proc DisplayName_PhPhonContextID
end
go
print 'creating proc DisplayName_PhPhonContextID'
go
create proc DisplayName_PhPhonContextID
	@ContextId int,
	@ContextString nvarchar(4000) output
as
	return 0
go
print 'altering proc DisplayName_PhPhonContextID'
go

alter proc DisplayName_PhPhonContextID
	@ContextId int,
	@ContextString nvarchar(4000) output
as
	declare @retval int,
		@CurId int, @Txt nvarchar(4000),
		@class int,
		@CurSeqId int, @SeqTxt nvarchar(4000), @wantSpace bit, @CurOrd int,
		@fIsNocountOn int

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	set @ContextString = ''

	-- Check for legal class.
	select @CurId = isnull(Id, 0)
	from CmObject cmo
	where cmo.Id = @ContextId
		 -- Check for class being a subclass of PhPhonContext
		and cmo.Class$ IN (5082, 5083, 5085, 5086, 5087)

	if @CurId > 0 begin
		select @class = Class$
		from CmObject
		where Id = @CurId

		-- Deal with subclass specific contexts.
		if @class = 5082 begin	-- PhIterationContext
			select @CurSeqId = isnull(Member, 0)
			from PhIterationContext mem
			where mem.Id = @ContextId
			if @CurSeqId = 0 begin
				set @ContextString = '(***)'
				set @retval = 1
				goto LExit
			end
			exec @retval = DisplayName_PhPhonContextID @CurSeqId, @Txt output
			if @retval != 0 begin
				set @ContextString = '(***)'
				goto LExit
			end
			set @ContextString = '(' + @Txt + ')'
		end
		else if @class = 5083 begin	-- PhSequenceContext
			set @wantSpace = 0
			select top 1 @CurSeqId = Dst, @CurOrd = mem.ord
			from PhSequenceContext_Members mem
			where mem.Src = @ContextId
			order by mem.Ord
			while @@rowcount > 0 begin
				set @SeqTxt = '***'
				exec @retval = DisplayName_PhPhonContextID @CurSeqId, @SeqTxt output
				if @retval != 0 begin
					set @ContextString = '***'
					goto LExit
				end
				if @wantSpace = 1
					set @ContextString = @ContextString + ' '
				set @wantSpace = 1
				set @ContextString = @ContextString + @SeqTxt
				-- Try to get next one
				select top 1 @CurSeqId = Dst, @CurOrd = mem.ord
				from PhSequenceContext_Members mem
				where mem.Src = @ContextId and mem.Ord > @CurOrd
				order by mem.Ord
			end
			--set @ContextString = 'PhSequenceContext'
		end
		else if @class = 5085 begin	-- PhSimpleContextBdry
			select top 1 @Txt = isnull(nm.Txt, '***')
			from PhSimpleContextBdry ctx
			join PhTerminalUnit tu On tu.Id = ctx.FeatureStructure
			join PhTerminalUnit_Codes cds On cds.Src = tu.Id
			join PhCode_Representation nm On nm.Obj = cds.Dst
			where ctx.Id = @CurId
			order by cds.Ord, nm.Ws
			set @ContextString = @Txt
		end
		else if @class = 5086 begin	-- PhSimpleContextNC
			select top 1 @Txt = isnull(nm.Txt, '***')
			from PhSimpleContextNC ctx
			join PhNaturalClass_Name nm On nm.Obj = ctx.FeatureStructure
			where ctx.Id = @CurId
			order by nm.Ws
			set @ContextString = '[' + @Txt + ']'
		end
		else if @class = 5087 begin	-- PhSimpleContextSeg
			select top 1 @Txt = isnull(nm.Txt, '***')
			from PhSimpleContextSeg ctx
			join PhTerminalUnit tu On tu.Id = ctx.FeatureStructure
			join PhTerminalUnit_Codes cds On cds.Src = tu.Id
			join PhCode_Representation nm On nm.Obj = cds.Dst
			where ctx.Id = @CurId
			order by cds.Ord, nm.Ws
			set @ContextString = @Txt
		end
		else begin
			set @ContextString = '***'
			set @retval = 1
			goto LExit
		end
	end
	else begin
		set @ContextString = '***'
		set @retval = 1
		goto LExit
	end
	set @retval = 0
LExit:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @retval
go
-------------------------------------------------------------------------------
-- FWC-10: Reload stored procedure FindOrCreateCmAgent
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

	-- We don't need to wory about transactions, since the call to CreateObject_CmAgent
	-- wiil create waht is needed, and rool it back, if the creation fails.

	SELECT @wsEN=Obj
	FROM LgWritingSystem_Name
	WHERE Txt='English'

	SELECT TOP 1 @lpID=ID
	FROM LangProject
	ORDER BY ID

	exec @retVal = CreateObject_CmAgent
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
-- FWC-10: Reload stored procedure fnConcordForPartOfSpeech
-------------------------------------------------------------------------------
IF OBJECT_ID('fnConcordForPartOfSpeech') IS NOT NULL BEGIN
	PRINT 'removing function fnConcordForPartOfSpeech'
	DROP FUNCTION fnConcordForPartOfSpeech
END
GO
PRINT 'creating function fnConcordForPartOfSpeech'
GO

CREATE FUNCTION [dbo].[fnConcordForPartOfSpeech](
	@nOwnFlid INT,
	@hvoPOS INT,
	@ntIds NTEXT)
RETURNS @tblTextAnnotations TABLE (
	BeginObject INT,
	BeginOffset INT,
	AnnotationId INT)
AS
BEGIN
	DECLARE @tblIds TABLE (Id INT)

	INSERT INTO @tblIds SELECT f.ID FROM dbo.fnGetIdsFromString(@ntIds, NULL) AS f

	INSERT INTO @tblTextAnnotations
	SELECT a.BeginObject, a.BeginOffset, a.Id AS AnnotationId
	FROM CmBaseAnnotation_ a
	JOIN WfiAnalysis wa ON wa.Id = a.InstanceOf
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst
	LEFT OUTER JOIN MoStemMsa msm ON msm.Id= wmb.Msa
	LEFT OUTER JOIN MoInflAffMsa miam ON miam.Id= wmb.Msa
	LEFT OUTER JOIN MoDerivStepMsa mdsm ON mdsm.Id= wmb.Msa
	LEFT OUTER JOIN MoDerivAffMsa mdam ON mdam.Id= wmb.Msa
	LEFT OUTER JOIN MoUnclassifiedAffixMsa muam ON muam.Id= wmb.Msa
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN @tblIds i ON i.Id = t.Id
	WHERE ((t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL) AND
		   (wa.Category=@hvoPOS OR
			msm.PartOfSpeech=@hvoPOS OR
			miam.PartOfSpeech=@hvoPOS OR
			mdsm.PartOfSpeech=@hvoPOS OR
			mdam.ToPartOfSpeech=@hvoPOS OR
			muam.PartOfSpeech=@hvoPOS))
	UNION
	SELECT a.BeginObject, a.BeginOffset, a.Id AS AnnotationId
	FROM CmBaseAnnotation_ a
	JOIN WfiGloss_ wg ON wg.Id = a.InstanceOf
	JOIN WfiAnalysis wa ON wa.Id = wg.Owner$
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst
	LEFT OUTER JOIN MoStemMsa msm ON msm.Id= wmb.Msa
	LEFT OUTER JOIN MoInflAffMsa miam ON miam.Id= wmb.Msa
	LEFT OUTER JOIN MoDerivStepMsa mdsm ON mdsm.Id= wmb.Msa
	LEFT OUTER JOIN MoDerivAffMsa mdam ON mdam.Id= wmb.Msa
	LEFT OUTER JOIN MoUnclassifiedAffixMsa muam ON muam.Id= wmb.Msa
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN @tblIds i ON i.Id = t.Id
	WHERE ((t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL) AND
		   (wa.Category=@hvoPOS OR
			msm.PartOfSpeech=@hvoPOS OR
			miam.PartOfSpeech=@hvoPOS OR
			mdsm.PartOfSpeech=@hvoPOS OR
			mdam.ToPartOfSpeech=@hvoPOS OR
			muam.PartOfSpeech=@hvoPOS))
	ORDER BY a.BeginObject, a.BeginOffset, a.Id

	RETURN
END

GO

-------------------------------------------------------------------------------
-- FWC-10: Reload stored procedure fnGetTextAnnotations
-------------------------------------------------------------------------------
IF OBJECT_ID('fnGetTextAnnotations') IS NOT NULL BEGIN
	PRINT 'removing function fnGetTextAnnotations'
	DROP FUNCTION fnGetTextAnnotations
END
GO
PRINT 'creating function fnGetTextAnnotations'
GO

CREATE FUNCTION dbo.fnGetTextAnnotations(
	@nvcTextName NVARCHAR(4000),
	@nVernacularWs INT = NULL,
	@nAnalysisWS INT = NULL)
RETURNS @tblTextAnnotations TABLE (
	TextId INT,
	TextName NVARCHAR(4000),
	Paragraph INT,
	StTxtParaId INT,
	BeginOffset INT,
	EndOffset INT,
	AnnotationId INT,
	WordFormId INT,
	Wordform NVARCHAR(4000),
	AnalysisId INT,
	GlossId INT,
	Gloss NVARCHAR(4000))
AS
BEGIN
	DECLARE @nAnnotationDefnPIC INT

	SELECT @nAnnotationDefnPIC = Obj
	FROM CmPossibility_Name
	WHERE Txt = 'Punctuation In Context'

	IF @nAnalysisWS IS NULL
		SELECT TOP 1 @nAnalysisWS = Dst
		FROM LangProject_CurAnalysisWss ORDER BY Ord
	IF @nVernacularWS IS NULL
		SELECT TOP 1 @nVernacularWS = dst
		FROM LangProject_CurVernWss ORDER BY Ord

	-- REVIEW (SteveMiller): Most of these queries (joined together by the
	-- UNIONs) can be optimized by dropping out some tables. Since this is
	-- utility function, and it moves pretty fast already, I didn't take
	-- the time to tweak it anymore.

	-- REVIEW (SteveMiller): Text segment queries are still not being
	-- picked up. If those are desired, another query will be needed,
	-- UNIONed with the rest of them.

	--== Annotation is not an InstanceOf anything ==--
	INSERT INTO @tblTextAnnotations
	SELECT
		cmon.Obj AS TextId,
		cmon.Txt AS TextName,
		tp.Ord AS Paragraph,
		stp.Id AS StTxtParaId,
		cba.BeginOffset,
		cba.EndOffset,
		cba.Id AS AnnotationId,
		NULL AS WordFormId,
		SUBSTRING(stp.Contents, cba.BeginOffset + 1, cba.EndOffset - cba.BeginOffset)
			COLLATE SQL_Latin1_General_CP1_CI_AS AS WordForm, --( avoids collate mismatch
		NULL AS AnalysisId,
		NULL AS GlossId,
		NULL AS Gloss
	FROM CmMajorObject_Name cmon
	JOIN Text_Contents tc ON tc.Src = cmon.Obj
	JOIN StText st ON st.Id = tc.Dst
	JOIN StText_Paragraphs tp ON tp.Src = st.Id
	JOIN StTxtPara stp ON stp.Id = tp.Dst
	JOIN CmBaseAnnotation cba ON cba.BeginObject = stp.Id
	JOIN CmAnnotation ca ON ca.Id = cba.Id
	WHERE ca.InstanceOf IS NULL
		AND cmon.Txt = @nvcTextName
		AND ca.AnnotationType = @nAnnotationDefnPIC
	--== Annotation is an InstanceOf Wordform ==--
	UNION
	SELECT
		cmon.Obj AS TextId,
		cmon.Txt AS TextName,
		tp.Ord AS Paragraph,
		stp.Id AS StTxtParaId,
		cba.BeginOffset,
		cba.EndOffset,
		cba.Id AS AnnotationId,
		wwff.Obj AS WordFormId,
		wwff.Txt AS WordForm,
		NULL AS AnalysisId,
		NULL AS GlossId,
		NULL AS Gloss
	FROM CmMajorObject_Name cmon
	JOIN Text_Contents tc ON tc.Src = cmon.Obj
	JOIN StText st ON st.Id = tc.Dst
	JOIN StText_Paragraphs tp ON tp.Src = st.Id
	JOIN StTxtPara stp ON stp.Id = tp.Dst
	JOIN CmBaseAnnotation cba ON cba.BeginObject = stp.Id
	JOIN CmAnnotation ca ON ca.Id = cba.Id
	JOIN WfiWordForm_Form wwff ON wwff.Obj = ca.InstanceOf AND wwff.WS = @nVernacularWS
	WHERE cmon.Txt = @nvcTextName
	--== Annotation is an InstanceOf Annotation ==--
	UNION
	SELECT
		cmon.Obj AS TextId,
		cmon.Txt AS TextName,
		tp.Ord AS Paragraph,
		stp.Id AS StTxtParaId,
		cba.BeginOffset,
		cba.EndOffset,
		cba.Id AS AnnotationId,
		wwff.Obj AS WordFormId,
		wwff.Txt AS WordForm,
		wa.Id AS AnalysisId,
		NULL AS GlossId,
		NULL AS Gloss
	FROM CmMajorObject_Name cmon
	JOIN Text_Contents tc ON tc.Src = cmon.Obj
	JOIN StText st ON st.Id = tc.Dst
	JOIN StText_Paragraphs tp ON tp.Src = st.Id
	JOIN StTxtPara stp ON stp.Id = tp.Dst
	JOIN CmBaseAnnotation cba ON cba.BeginObject = stp.Id
	JOIN CmAnnotation ca ON ca.Id = cba.Id
	JOIN WfiAnalysis wa ON wa.Id = ca.InstanceOf
	LEFT OUTER JOIN WfiWordForm_Analyses wwfa ON wwfa.Dst = wa.Id
	LEFT OUTER JOIN WfiWordForm_Form wwff ON wwff.Obj = wwfa.Src AND wwff.WS = @nVernacularWS
	WHERE cmon.Txt = @nvcTextName
	--== Annotation is an InstanceOf Gloss ==--
	UNION
	SELECT
		cmon.Obj AS TextId,
		cmon.Txt AS TextName,
		tp.Ord AS Paragraph,
		stp.Id AS StTxtParaId,
		cba.BeginOffset,
		cba.EndOffset,
		cba.Id AS AnnotationId,
		wwff.Obj AS WordFormId,
		wwff.Txt AS WordForm,
		wa.Id AS AnalysisId,
		wgf.Obj AS GlossId,
		wgf.Txt AS Gloss
	FROM CmMajorObject_Name cmon
	JOIN Text_Contents tc ON tc.Src = cmon.Obj
	JOIN StText st ON st.Id = tc.Dst
	JOIN StText_Paragraphs tp ON tp.Src = st.Id
	JOIN StTxtPara stp ON stp.Id = tp.Dst
	JOIN CmBaseAnnotation cba ON cba.BeginObject = stp.Id
	JOIN CmAnnotation ca ON ca.Id = cba.Id
	JOIN WfiGloss_Form wgf ON wgf.Obj = ca.InstanceOf AND wgf.WS = @nAnalysisWS
	LEFT OUTER JOIN WfiAnalysis_Meanings wam ON wam.Dst = wgf.Obj
	LEFT OUTER JOIN WfiAnalysis wa ON wa.Id = wam.Src
	LEFT OUTER JOIN WfiWordForm_Analyses wwfa ON wwfa.Dst = wa.Id
	LEFT OUTER JOIN WfiWordForm_Form wwff ON wwff.Obj = wwfa.Src AND wwff.WS = @nVernacularWS
	WHERE cmon.Txt = @nvcTextName
	ORDER BY tp.Ord, cba.BeginOffset

	RETURN
END
GO
-------------------------------------------------------------------------------
-- FWC-10: Reload stored procedure fnMatchEntries
-------------------------------------------------------------------------------
IF OBJECT_ID('dbo.fnMatchEntries') IS NOT NULL BEGIN
	PRINT 'removing function fnMatchEntries'
	DROP FUNCTION dbo.fnMatchEntries
END
PRINT 'creating function fnMatchEntries'
GO

CREATE  FUNCTION [dbo].[fnMatchEntries](
	@nExactMatch BIT = 0,
	@nvcLexForm NVARCHAR(900),
	@nvcCitForm NVARCHAR(900),
	@nvcAltForm	NVARCHAR(900),
	@nvcGloss NVARCHAR(900),
	@nVernWS INT,
	@nAnalysisWS INT,
	@nMaxSize INT)
RETURNS @tblMatchingEntries TABLE (
		EntryId INT PRIMARY KEY,
		LexicalForm NVARCHAR(900),
		LexicalFormWS INT,
		CitationForm NVARCHAR(900),
		CitationFormWS INT,
		AlternateForm NVARCHAR(900),
		AlternateFormWS INT,
		Gloss NVARCHAR(900),
		GlossWS INT)
AS
BEGIN
	DECLARE
		@nEntryId INT,
		@nCount INT

	-- Make sure we use a valid nMaxSize: allowing the caller to set it
	-- to a value between 1 and 900

	IF @nMaxSize IS NULL OR @nMaxSize > 900 OR @nMaxSize <= 0 BEGIN
		SET @nMaxSize = 900
	END

	SET @nvcLexForm = SUBSTRING(RTRIM(LTRIM(LOWER(@nvcLexForm))),1, @nMaxSize)
	SET @nvcCitForm = SUBSTRING(RTRIM(LTRIM(LOWER(@nvcCitForm))),1, @nMaxSize)
	SET @nvcAltForm = SUBSTRING(RTRIM(LTRIM(LOWER(@nvcAltForm))),1, @nMaxSize)
	SET @nvcGloss = SUBSTRING(RTRIM(LTRIM(LOWER(@nvcGloss))),1, @nMaxSize)

	--==( Get EntryIDs )==--

	--( This block is for searches on any lexical, citation, or alternate form
	IF @nvcLexForm != N'!' OR @nvcCitForm != '!' OR @nvcAltForm != '!' BEGIN
		IF @nExactMatch = 0 BEGIN
			INSERT INTO @tblMatchingEntries (EntryId)
			--( matching lexeme forms
			SELECT DISTINCT lf.Src AS EntryID
			FROM LexEntry_LexemeForm lf
			JOIN MoForm_Form mff_lf ON mff_lf.Obj = lf.Dst AND mff_lf.WS = @nVernWS
			WHERE @nvcLexForm != N'!'
				AND SUBSTRING(RTRIM(LTRIM(LOWER(mff_lf.Txt))), 1, @nMaxSize) LIKE @nvcLexForm + N'%'
			--( matching citation forms
			UNION
			SELECT DISTINCT cf.Obj AS EntryID
			FROM LexEntry_CitationForm cf
			WHERE @nvcCitForm != N'!'
				AND SUBSTRING(RTRIM(LTRIM(LOWER(cf.Txt))), 1, @nMaxSize) LIKE @nvcCitForm + N'%'
				AND cf.WS = @nVernWS
			--( matching alternate forms
			UNION
			SELECT DISTINCT af.Src AS EntryID
			FROM LexEntry_AlternateForms af
			JOIN MoForm_Form mff_af ON mff_af.Obj = af.Dst AND mff_af.WS = @nVernWS
			WHERE @nvcAltForm != N'!'
				AND SUBSTRING(RTRIM(LTRIM(LOWER(mff_af.Txt))), 1, @nMaxSize) LIKE @nvcAltForm + N'%'
			--( matching glosses
			UNION
			SELECT DISTINCT o.Owner$ AS EntryID
			FROM LexSense_Gloss lsg
			JOIN CmObject o ON o.Id = lsg.Obj
			JOIN LexEntry le ON le.Id = o.Owner$
			WHERE @nvcGloss != N'!' AND lsg.Ws = @nAnalysisWS
				AND SUBSTRING(RTRIM(LTRIM(LOWER(lsg.Txt))), 1, @nMaxSize) LIKE @nvcGloss  + N'%'
		END
		ELSE BEGIN --( IF ! @nExactMatch = 0
			INSERT INTO @tblMatchingEntries (EntryId)
			--( matching lexeme forms
			SELECT DISTINCT lf.Src AS EntryID
			FROM LexEntry_LexemeForm lf
			JOIN MoForm_Form mff_lf ON mff_lf.Obj = lf.Dst AND mff_lf.WS = @nVernWS
			WHERE @nvcLexForm != N'!'
				AND SUBSTRING(RTRIM(LTRIM(mff_lf.Txt)), 1, @nMaxSize) = @nvcLexForm
			--( matching citation forms
			UNION
			SELECT DISTINCT lf.Src AS EntryID
			FROM LexEntry_LexemeForm lf
			JOIN MoForm_Form mff_lf ON mff_lf.Obj = lf.Dst
			JOIN LexEntry_CitationForm cf ON cf.Obj = lf.Src
			WHERE @nvcCitForm != N'!'
				AND SUBSTRING(RTRIM(LTRIM(cf.Txt)), 1, @nMaxSize) = @nvcCitForm
				AND cf.WS = @nVernWS
			--( matching alternate forms
			UNION
			SELECT DISTINCT af.Src AS EntryID
			FROM LexEntry_AlternateForms af
			JOIN MoForm_Form mff_af ON mff_af.Obj = af.Dst AND mff_af.WS = @nVernWS
			WHERE @nvcAltForm != N'!'
				AND SUBSTRING(RTRIM(LTRIM(mff_af.Txt)), 1, @nMaxSize) = @nvcAltForm
			--( matching glosses
			UNION
			SELECT DISTINCT o.Owner$ AS EntryID
			FROM LexSense_Gloss lsg
			JOIN CmObject o ON o.Id = lsg.Obj
			JOIN LexEntry le ON le.Id = o.Owner$
			WHERE @nvcGloss != N'!'
				AND lsg.Ws = @nAnalysisWS
				AND SUBSTRING(RTRIM(LTRIM(lsg.Txt)), 1, @nMaxSize) = @nvcGloss
		END
	END --( IF @nvcLexForm != N'!' OR @nvcCitForm != '!' OR @nvcAltForm != '!'

	ELSE BEGIN --( IF @nvcGloss != '!' BEGIN
		IF @nExactMatch = 0 BEGIN
			INSERT INTO @tblMatchingEntries (EntryId)
			SELECT DISTINCT lf.Src AS EntryID
			FROM LexEntry_LexemeForm lf
			JOIN MoForm_Form mff_lf ON mff_lf.Obj = lf.Dst AND mff_lf.WS = @nVernWS
			JOIN CmObject o ON o.Owner$ = lf.Src
			JOIN LexSense_Gloss lsg ON lsg.Obj = o.Id AND lsg.Ws = @nAnalysisWS
			WHERE @nvcGloss != N'!'
				AND SUBSTRING(RTRIM(LTRIM(LOWER(lsg.Txt))), 1, @nMaxSize) LIKE @nvcGloss + N'%'
		END
		ELSE BEGIN --( IF ! @nExactMatch = 0
			INSERT INTO @tblMatchingEntries (EntryId)
			SELECT DISTINCT lf.Src AS EntryID
			FROM LexEntry_LexemeForm lf
			JOIN MoForm_Form mff_lf ON mff_lf.Obj = lf.Dst AND mff_lf.WS = @nVernWS
			JOIN CmObject o ON o.Owner$ = lf.Src
			JOIN LexSense_Gloss lsg ON lsg.Obj = o.Id AND lsg.Ws = @nAnalysisWS
			WHERE @nvcGloss != N'!'
				AND SUBSTRING(RTRIM(LTRIM(lsg.Txt)), 1, @nMaxSize) = @nvcGloss
		END
	END

	--==( Fill in Info for Entry IDs )==--

	--( Get forms and glosses for each of the hits from the above query.
	DECLARE curEntries CURSOR FOR
		SELECT EntryId FROM @tblMatchingEntries me ORDER BY EntryId
	OPEN curEntries
	FETCH NEXT FROM curEntries INTO @nEntryId
	WHILE @@FETCH_STATUS = 0 BEGIN

		UPDATE @tblMatchingEntries SET
			LexicalForm = mff_lf.Txt,
			LexicalFormWS = mff_lf.WS,
			CitationForm = cf.Txt,
			CitationFormWS = cf.WS,
			AlternateForm = (
				SELECT SUBSTRING(RTRIM(LTRIM(AltForm)), 1, @nMaxSize)
				FROM dbo.fnGetEntryAltForms(@nEntryId, @nVernWS)),
			AlternateFormWS = @nVernWS,
			Gloss = (
				SELECT SUBSTRING(RTRIM(LTRIM(Gloss)), 1, @nMaxSize)
				FROM dbo.fnGetEntryGlosses(@nEntryId, @nAnalysisWS)),
			GlossWS = @nAnalysisWS
		FROM @tblMatchingEntries me
		LEFT OUTER JOIN LexEntry_LexemeForm lf ON lf.Src = me.EntryID
		LEFT OUTER JOIN MoForm_Form mff_lf ON mff_lf.Obj = lf.Dst AND mff_lf.WS = @nVernWS
		LEFT OUTER JOIN LexEntry_CitationForm cf ON cf.Obj = me.EntryID AND cf.WS = @nVernWS
		WHERE EntryId = @nEntryId

		FETCH NEXT FROM curEntries INTO @nEntryId
	END
	CLOSE curEntries
	DEALLOCATE curEntries

	--==( More getting glosses )==--

	--( If we can't get a gloss in the selected writing system, try for one in
	--( the other writing systems. If there is only one analysis writing system,
	--( never mind.

	SELECT @nCount = COUNT(*) FROM LangProject_CurAnalysisWss
	IF @nCount > 1 BEGIN

		UPDATE @tblMatchingEntries
		SET Gloss = lsg.Txt, GlossWS = lsg.WS
		FROM @tblMatchingEntries me
		JOIN CmObject o ON o.Owner$ = me.EntryId
		JOIN LexSense_Gloss lsg ON lsg.Obj = o.Id AND lsg.WS != @nAnalysisWS
		JOIN (
			SELECT TOP 1 Dst
			FROM LangProject_CurAnalysisWss
			WHERE Dst != @nAnalysisWS
			ORDER BY Ord
			) caw ON caw.Dst = lsg.WS
		WHERE me.Gloss IS NULL

	END
	RETURN
END
GO
-------------------------------------------------------------------------------
-- FWC-10: Reload stored procedure GetEntriesAndSenses$
-------------------------------------------------------------------------------
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

-------------------------------------------------------------------------------
-- FWC-10: Reload stored procedure PATRString_sp
-------------------------------------------------------------------------------
/***********************************************************************************************
 * The following are three inter-related stored procedures. Since some of them call each other,
 * they must be created, and then altered, in order to avoid some SP creation error messages.
 * Together, they serve to return a PC-PATR suitable string representation of one or more
 * FsFeatStruc objects.
***********************************************************************************************/
-- First, delete them all, if they exist.

if object_id('PATRString_FsFeatStruc') is not null begin
	drop proc PATRString_FsFeatStruc
end
go
if object_id('PATRString_FsAbstractStructure') is not null begin
	drop proc PATRString_FsAbstractStructure
end
go
if object_id('PATRString_FsFeatureSpecification') is not null begin
	drop proc PATRString_FsFeatureSpecification
end
go

-- Create to 'empty' SPs.
print 'creating proc PATRString_FsFeatureSpecification'
go
create proc PATRString_FsFeatureSpecification
	@Def nvarchar(1),
	@Id int,
	@PATRString nvarchar(4000) output
as
	return 1
go
print 'creating proc PATRString_FsAbstractStructure'
go
create proc PATRString_FsAbstractStructure
	@Def nvarchar(1),
	@Id int,
	@PATRString nvarchar(4000) output
as
	return 1
go


-- Create real top-level SPs.
print 'altering proc PATRString_FsAbstractStructure'
go

alter proc PATRString_FsAbstractStructure
	@Def nvarchar(1),
	@Id int,
	@PATRString nvarchar(4000) output
as
	declare @fIsNocountOn int, @retval int,
		@fNeedSpace bit, @CurDstId int,
		@Txt NVARCHAR(4000),
		@Class int,
		@fNeedSlash bit

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- Get class info.
	select @Class = Class$
	from CmObject
	where Id = @Id

	if @Class = 2009 begin	-- FsFeatStruc
		set @PATRString = '['

		-- Handle Disjunctions, if any
		select top 1 @CurDstId = Dst
		from FsFeatStruc_FeatureDisjs
		where Src = @Id
		order by Dst
		while @@rowcount > 0 begin
			exec @retval = PATRString_FsAbstractStructure @Def, @CurDstId, @Txt output
			if @retval != 0 begin
				set @PATRString = '[]'
				goto LFail
			end
			set @PATRString = @PATRString + @Txt
			-- Try getting another one
			select top 1 @CurDstId = Dst
			from FsFeatStruc_FeatureDisjs
			where Src = @Id and Dst > @CurDstId
			order by Dst
		end

		-- Handle FeatureSpecs, if any
		set @fNeedSpace = 0
		select top 1 @CurDstId = Dst
		from FsFeatStruc_FeatureSpecs
		where Src = @Id
		order by Dst
		while @@rowcount > 0 begin
			exec @retval = PATRString_FsFeatureSpecification @Def, @CurDstId, @Txt output
			if @retval != 0 begin
				set @PATRString = '[]'
				goto LFail
			end
			if @fNeedSpace = 1 set @PATRString = @PATRString + ' '
			else set @fNeedSpace = 1
			set @PATRString = @PATRString + @Txt
			-- Try getting another one
			select top 1 @CurDstId = Dst
			from FsFeatStruc_FeatureSpecs
			where Src = @Id and Dst > @CurDstId
			order by Dst
		end

		set @PATRString = @PATRString + ']'
	end
	else if @Class = 2010 begin	-- FsFeatStrucDisj
		set @PATRString = '{'
		-- Handle contents, if any
		set @fNeedSlash = 0
		select top 1 @CurDstId = Dst
		from FsFeatStrucDisj_Contents
		where Src = @Id
		order by Dst
		while @@rowcount > 0 begin
			exec @retval = PATRString_FsAbstractStructure @Def, @CurDstId, @Txt output
			if @retval != 0 begin
				set @PATRString = ''
				goto LFail
			end
			if @fNeedSlash = 1 set @PATRString = @PATRString + ' '
			else set @fNeedSlash = 1
			set @PATRString = @PATRString + @Txt
			-- Try getting another one
			select top 1 @CurDstId = Dst
			from FsFeatStrucDisj_Contents
			where Src = @Id and Dst > @CurDstId
			order by Dst
		end
		set @PATRString = @PATRString + '}'
	end
	else begin	-- unknown class.
		set @retval = 1
		set @PATRString = '[]'
		goto LFail
	end
	set @retval = 0
LFail:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @retval
go

-------------------------------------------------------------------------------
-- FWC-10: Reload stored procedure WasParsingDataModified
-------------------------------------------------------------------------------
if object_id('WasParsingDataModified') is not null begin
	print 'removing proc WasParsingDataModified'
	drop proc WasParsingDataModified
end
print 'creating proc WasParsingDataModified'
go

CREATE PROC [WasParsingDataModified]
			@stampCompare TIMESTAMP
AS
	SELECT TOP 1 Id
	FROM CmObject co
	WHERE co.UpdStmp > @stampCompare
		AND (co.Class$ BETWEEN 5026 AND 5045
			OR co.Class$ IN
			(4, -- FsComplexFeature 4
			49, -- FsFeatureSystem 49
			50, -- FsClosedFeature 50
			51, -- FsClosedValue 51
			53, -- FsComplexValue 53
			57, -- FsFeatureStructure 57
			59, -- FsFeatureStructureType 59
			65, -- FsSymFeatVal 65
			5001, -- MoStemMsa 5001
			5002, -- LexEntry 5002
			5005, -- LexDb 5005
			5049, -- PartOfSpeech 5049
			5092, -- PhPhoneme 5092
			5095, -- PhNCSegments 5095
			5097, -- PhEnvironment 5097
			5098, -- PhCode 5098
			5099, -- PhPhonData 5099
			5101, -- MoAlloAdhocProhib 5101
			5102, -- MoMorphAdhocProhib 5102
			5110, -- MoAdhocProhibGr 5110
			5117 -- MoUnclassifiedAffixMsa 5117
			))
GO

-------------------------------------------------------------------------------
-- FWC-10: Reload stored procedure WasParsingGrammarDataModified
-------------------------------------------------------------------------------
if object_id('WasParsingGrammarDataModified') is not null begin
	print 'removing proc WasParsingGrammarDataModified'
	drop proc WasParsingGrammarDataModified
end
print 'creating proc WasParsingGrammarDataModified'
go

CREATE PROC [WasParsingGrammarDataModified]
			@stampCompare TIMESTAMP
AS
	SELECT TOP 1 Id
	FROM CmObject co
	WHERE co.UpdStmp > @stampCompare
		AND (co.Class$ IN
			(4, -- FsComplexFeature 4
			49, -- FsFeatureSystem 49
			50, -- FsClosedFeature 50
			51, -- FsClosedValue 51
			53, -- FsComplexValue 53
			57, -- FsFeatureStructure 57
			59, -- FsFeatureStructureType 59
			65, -- FsSymFeatVal 65
			5001, -- MoStemMsa 5001
			5026, -- MoAdhocProhib 5026
			5027, -- MoAffixAllomorph 5027 (Actually only want MoAffixForm, but it doesn't work)
			--5028, -- MoAffixForm 5028
			5030, -- MoCompoundRule 5030
			5032, -- MoDerivStepMsa 5031
			5033, -- MoEndoCompound 5033
			5034, -- MoExoCompound 5034
			5036, -- MoInflAffixSlot 5036
			5037, -- MoInflAffixTemplate 5037
			5038, -- MoInflectionalAffixMsa 5038
			5039, -- MoInflClass 5039
			5040, -- MoMorphData 5040
			5041, -- MoMorphSynAnalysis 5041
			5042, -- MoMorphType 5042
			5049, -- PartOfSpeech 5049
			5092, -- PhPhoneme 5092
			5095, -- PhNCSegments 5095
			5097, -- PhEnvironment 5097
			5098, -- PhCode 5098
			5099, -- PhPhonData 5099
			5101, -- MoAlloAdhocProhib 5101
			5102, -- MoMorphAdhocProhib 5102
			5110, -- MoAdhocProhibGr 5110
			5117 -- MoUnclassifiedAffixMsa 5117
			))
GO

-------------------------------------------------------------------------------
-- FWC-10: Reload stored procedure WasParsingLexiconDataModified
-------------------------------------------------------------------------------

if object_id('WasParsingLexiconDataModified') is not null begin
	print 'removing proc WasParsingLexiconDataModified'
	drop proc WasParsingLexiconDataModified
end
print 'creating proc WasParsingLexiconDataModified'
go

CREATE PROC [WasParsingLexiconDataModified]
			@stampCompare TIMESTAMP
AS
	SELECT TOP 1 Id
	FROM CmObject co
	WHERE co.UpdStmp > @stampCompare
		AND (co.Class$ IN
			(
			51, -- FsClosedValue 51
			53, -- FsComplexValue 53
			57, -- FsFeatureStructure 57
			59, -- FsFeatureStructureType 59
			5001, -- MoStemMsa 5001
			5002, -- LexEntry 5002
			5005, -- LexDb 5005
			5027, -- MoAffixAllomorph 5027
			5028, -- MoAffixForm 5028
			5031, -- MoDerivAffMsa 5031
			5035, -- MoForm 5035
			5038, -- MoInflAffMsa 5038
			5045, -- MoStemAllomorph 5045
			5117 -- MoUnclassifiedAffixMsa 5117
			))
GO
-------------------------------------------------------------------------------
-- FWC-10: Reload stored procedures in  NotebkSP.sql
-------------------------------------------------------------------------------
print '**************************** Loading NotebkSP.sql ****************************'
go

if object_id('fnGetAddedNotebookObjects$') is not null begin
	print 'removing function fnGetAddedNotebookObjects$'
	drop function [fnGetAddedNotebookObjects$]
end
go
print 'creating function fnGetAddedNotebookObjects$'
go
create function [fnGetAddedNotebookObjects$] ()
returns @DelList table ([ObjId] int not null)
as
begin
	declare @nRowCnt int
	declare @nOwnerDepth int
	declare @Err int
	set @nOwnerDepth = 1
	set @Err = 0

	insert into @DelList
	select [Id] from CmObject
	where OwnFlid$ = 4001001

	-- use a table variable to hold the possibility list item object ids
	declare @PossItems table (
		[ObjId] int primary key not null,
		[OwnerDepth] int null,
		[DateCreated] datetime null
	)

	-- Get the object ids for all of the possibility items in the lists used by Data Notebook
	-- (except for the Anthropology List, which is loaded separately).
	-- Note the hard-wired sets of possibility list flids.
	-- First, get the top-level possibility items from the standard data notebook lists.

	insert into @PossItems (ObjId, OwnerDepth, DateCreated)
	select co.[Id], @nOwnerDepth, cp.DateCreated
	from [CmObject] co
	join [CmPossibility] cp on cp.[id] = co.[id]
	join CmObject co2 on co2.[id] = co.Owner$ and co2.OwnFlid$ in (
			4001003,
			6001025,
			6001026,
			6001027,
			6001028,
			6001029,
			6001030,
			6001031,
			6001032,
			6001033,
			6001036
			)

	if @@error <> 0 goto LFail
	set @nRowCnt=@@rowcount

	-- Repeatedly get the list items owned at the next depth.

	while @nRowCnt > 0 begin
		set @nOwnerDepth = @nOwnerDepth + 1

		insert into @PossItems (ObjId, OwnerDepth, DateCreated)
		select co.[id], @nOwnerDepth, cp.DateCreated
		from [CmObject] co
		join [CmPossibility] cp on cp.[id] = co.[id]
		join @PossItems pi on pi.[ObjId] = co.[Owner$] and pi.[OwnerDepth] = @nOwnerDepth - 1

		if @@error <> 0 goto LFail
		set @nRowCnt=@@rowcount
	end

	-- Extract all the items which are newer than the language project, ie, which cannot be
	-- factory list items.
	-- Omit list items which are owned by other non-factory list items, since they will be
	-- deleted by deleting their owner.

	insert into @DelList
	select pi.ObjId
	from @PossItems pi
	join CmObject co on co.[id] = pi.ObjId
	where pi.DateCreated > (select top 1 DateCreated from CmProject order by DateCreated DESC)

	delete from @PossItems

	-- Get the object ids for all of the possibility items in the anthropology list.
	-- First, get the top-level possibility items from the anthropology list.

	set @nOwnerDepth = 1

	insert into @PossItems (ObjId, OwnerDepth, DateCreated)
	select co.[Id], @nOwnerDepth, cp.DateCreated
	from [CmObject] co
	join [CmPossibility] cp on cp.[id] = co.[id]
	where co.[Owner$] in (select id from CmObject where OwnFlid$ = 6001012)

	set @nRowCnt=@@rowcount
	if @@error <> 0 goto LFail

	-- Repeatedly get the anthropology list items owned at the next depth.

	while @nRowCnt > 0 begin
		set @nOwnerDepth = @nOwnerDepth + 1

		insert into @PossItems (ObjId, OwnerDepth, DateCreated)
		select co.[id], @nOwnerDepth, cp.DateCreated
		from [CmObject] co
		join [CmPossibility] cp on cp.[id] = co.[id]
		join @PossItems pi on pi.[ObjId] = co.[Owner$] and pi.[OwnerDepth] = @nOwnerDepth - 1

		if @@error <> 0 goto LFail
		set @nRowCnt=@@rowcount
	end

	declare @cAnthro int
	declare @cTimes int
	select @cAnthro = COUNT(*) from @PossItems
	select @cTimes = COUNT(distinct DateCreated) from @PossItems

	if @cTimes = @cAnthro begin
		-- Assume that none of them are factory if they all have different creation
		-- times.  This is true even if there's only one item.
		insert into @DelList
		select pi.ObjId
		from @PossItems pi
		where pi.OwnerDepth = 1
	end
	else if @cTimes != 1 begin
		-- assume that the oldest items are factory, the rest aren't
		insert into @DelList
		select pi.ObjId
		from @PossItems pi
		where pi.DateCreated > (select top 1 DateCreated from @PossItems order by DateCreated)
	end

return

LFail:
	delete from @DelList
	return
end
go

if object_id('DeleteAddedNotebookObjects$') is not null begin
	print 'removing proc DeleteAddedNotebookObjects$'
	drop proc [DeleteAddedNotebookObjects$]
end
go
print 'creating proc DeleteAddedNotebookObjects$'
go
create proc [DeleteAddedNotebookObjects$]
as
	declare @Err int
	set @Err = 0

	-- determine if the procedure was called within a transaction;
	-- if yes then create a savepoint, otherwise create a transaction
	declare @nTrnCnt int
	set @nTrnCnt = @@trancount
	if @nTrnCnt = 0 begin tran DelObj$_Tran
	else save tran DelObj$_Tran
	set @Err = @@error
	if @Err <> 0 begin
		raiserror ('DeleteAddedNotebookObjects$: SQL Error %d; Unable to create a transaction.', 16, 1, @Err)
		goto LFail
	end

	-- delete the objects (records and list items) added to the data notebook
	-- first, build an XML string containing all of the object ids

	declare @ObjId int
	declare @vcXml varchar(8000)
	declare @cObj int
	set @vcXml = '<root>'
	set @cObj = 0
	declare @hdoc int

	DECLARE curObj CURSOR FOR SELECT [ObjId] FROM dbo.fnGetAddedNotebookObjects$()
	OPEN curObj
	FETCH NEXT FROM curObj INTO @ObjId
	WHILE @@FETCH_STATUS = 0
	BEGIN
		set @vcXml = @vcXml + '<Obj Id="'+ cast(@ObjId as varchar(10)) + '"/>'
		set @cObj = @cObj + 1
		if len(@vcXml) > 7970 begin
			-- we are close to filling the string, so convert the string into an "XML document",
			-- and delete all the objects (in one swell foop).
			set @vcXml = @vcXml + '</root>'
			exec sp_xml_preparedocument @hdoc output, @vcXml
			exec @Err = DeleteObj$ null,@hdoc
			exec sp_xml_removedocument @hdoc
			set @vcXml = '<root>'
			set @cObj = 0
			if @Err <> 0 goto LFail
		end
		FETCH NEXT FROM curObj INTO @ObjId
	END
	CLOSE curObj
	DEALLOCATE curObj

	if @cObj <> 0 begin
		set @vcXml = @vcXml + '</root>'
		-- now, convert the string into an "XML document", and delete all the objects
		-- (in one swell foop).
		exec sp_xml_preparedocument @hdoc output, @vcXml
		exec @Err = DeleteObj$ null,@hdoc
		exec sp_xml_removedocument @hdoc
		if @Err <> 0 goto LFail
	end

	if @nTrnCnt = 0 commit tran DelObj$_Tran

	return 0

LFail:
	rollback tran DelObj$_Tran
	return @Err
go

print '*********************** Finished loading NotebkSP.sql ************************'
go

-------------------------------------------------------------------------------
-- FWC-10: Reload stored procedure GetOrderedMultiTxt
-------------------------------------------------------------------------------
if exists (select * from sysobjects where name = 'GetOrderedMultiTxt')
	drop proc GetOrderedMultiTxt
go
print 'creating proc GetOrderedMultiTxt'
go

create proc GetOrderedMultiTxt
	@id int,
	@flid int,
	@anal tinyint = 1
as

	declare @fIsNocountOn int
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	declare
		@iFieldType int,
		@nvcTable NVARCHAR(60),
		@nvcSql NVARCHAR(4000)

	select @iFieldType = [Type] from Field$ where [Id] = @flid
	EXEC GetMultiTableName @flid, @nvcTable OUTPUT

	--== Analysis WritingSystems ==--

	if @anal = 1
	begin

		-- MultiStr$ --
		if @iFieldType = 14
			select
				isnull(ms.[txt], '***') txt,
				ms.[ws],
				isnull(lpcae.[ord], 99998) [ord]
			from MultiStr$ ms
			left outer join LgWritingSystem le on le.[Id] = ms.[ws]
			left outer join LangProject_AnalysisWss lpae on lpae.[dst] = le.[id]
			left outer join LangProject_CurAnalysisWss lpcae on lpcae.[dst] = lpae.[dst]
			where ms.[obj] = @id and ms.[flid] = @flid
			union all
			select '***', 0, 99999
			order by isnull([ord], 99998)

		-- MultiBigStr$ --
		else if @iFieldType = 18
		begin
			--( See note 2 in the header
			declare @tblMultiBigStrAnalysis table (
				--( See the notes under string tables in FwCore.sql about the
				--( COLLATE clause.
				Txt NTEXT COLLATE Latin1_General_BIN,
				[ws] int,
				[ord] int primary key)

			insert into @tblMultiBigStrAnalysis
			select
				isnull(mbs.[txt], '***') txt,
				mbs.[ws],
				isnull(lpcae.[ord], 99998) [ord]
			from MultiBigStr$ mbs
			left outer join LgWritingSystem le on le.[Id] = mbs.[ws]
			left outer join LangProject_AnalysisWss lpae on lpae.[dst] = le.[id]
			left outer join LangProject_CurAnalysisWss lpcae on lpcae.[dst] = lpae.[dst]
			where mbs.[obj] = @id and mbs.[flid] = @flid
			order by isnull([ord], 99998)

			insert into @tblMultiBigStrAnalysis
			select convert(ntext, '***') [txt], 0 [ws], 99999 [ord]

			select * from @tblMultiBigStrAnalysis order by [ord]
		end

		-- MultiBigTxt$ --
		else if @iFieldType = 20
		begin
			--( See note 2 in the header
			declare @tblMultiBigTxtAnalysis table (
				--( See the notes under string tables in FwCore.sql about the
				--( COLLATE clause.
				Txt NTEXT COLLATE Latin1_General_BIN,
				[ws] int,
				[ord] int primary key)

			insert into @tblMultiBigTxtAnalysis
			select
				isnull(mbt.[txt], '***') txt,
				mbt.[ws],
				isnull(lpcae.[ord], 99998) [ord]
			from MultiBigTxt$ mbt
			left outer join LgWritingSystem le on le.[Id] = mbt.[ws]
			left outer join LangProject_AnalysisWss lpae on lpae.[dst] = le.[id]
			left outer join LangProject_CurAnalysisWss lpcae on lpcae.[dst] = lpae.[dst]
			where mbt.[obj] = @id and mbt.[flid] = @flid
			order by isnull([ord], 99998)

			insert into @tblMultiBigTxtAnalysis
			select convert(ntext, '***') [txt], 0 [ws], 99999 [ord]

			select * from @tblMultiBigTxtAnalysis order by [ord]
		end

		-- MultiTxt$ --
		else if @iFieldType = 16 BEGIN
			SET @nvcSql =
				N'select ' + CHAR(13) +
					N'isnull(mt.[txt], ''***'') txt, ' + CHAR(13) +
					N'mt.[ws], ' + CHAR(13) +
					N'isnull(lpcae.[ord], 99998) [ord] ' + CHAR(13) +
				N'from ' + @nvcTable + N' mt ' + CHAR(13) +
				N'left outer join LgWritingSystem le on le.[Id] = mt.[ws] ' + CHAR(13) +
				N'left outer join LangProject_AnalysisWss lpae ' +
					N'on lpae.[dst] = le.[id] ' + CHAR(13) +
				N'left outer join LangProject_CurAnalysisWss lpcae ' +
					N'on lpcae.[dst] = lpae.[dst] ' + CHAR(13) +
				N'where mt.[obj] = @id ' + CHAR(13) +
				N'union all ' + CHAR(13) +
				N'select ''***'', 0, 99999 ' + CHAR(13) +
				N'order by isnull([ord], 99998) '

			EXEC sp_executesql @nvcSql, N'@id INT', @id
		END

	end

	--== Vernacular WritingSystems ==--

	else if @anal = 0
	begin

		-- MultiStr$ --
		if @iFieldType = 14
			select
				isnull(ms.[txt], '***') txt,
				ms.[ws],
				isnull(lpcve.[ord], 99998) [ord]
			from MultiStr$ ms
			left outer join LgWritingSystem le on le.[Id] = ms.[ws]
			left outer join LangProject_VernWss lpve on lpve.[dst] = le.[id]
			left outer join LangProject_CurVernWss lpcve on lpcve.[dst] = lpve.[dst]
			where ms.[obj] = @id and ms.[flid] = @flid
			union all
			select '***', 0, 99999
			order by isnull([ord], 99998)

		-- MultiBigStr$ --
		else if @iFieldType = 18
		begin
			--( See note 2 in the header
			declare @tblMultiBigStrVernacular table (
				--( See the notes under string tables in FwCore.sql about the
				--( COLLATE clause.
				Txt NTEXT COLLATE Latin1_General_BIN,
				[ws] int,
				[ord] int primary key)

			insert into @tblMultiBigStrVernacular
			select
				isnull(mbs.[txt], '***') txt,
				mbs.[ws],
				isnull(lpcve.[ord], 99998) [ord]
			from MultiBigStr$ mbs
			left outer join LgWritingSystem le on le.[Id] = mbs.[ws]
			left outer join LangProject_VernWss lpve on lpve.[dst] = le.[id]
			left outer join LangProject_CurVernWss lpcve on lpcve.[dst] = lpve.[dst]
			where mbs.[obj] = @id and mbs.[flid] = @flid
			order by isnull([ord], 99998)

			insert into @tblMultiBigStrVernacular
			select convert(ntext, '***') [txt], 0 [ws], 99999 [ord]

			select * from @tblMultiBigStrVernacular order by [ord]
		end

		-- MultiBigTxt$ --
		else if @iFieldType = 20
		begin
			--( See note 2 in the header
			declare @tblMultiBigTxtVernacular table (
				--( See the notes under string tables in FwCore.sql about the
				--( COLLATE clause.
				Txt NTEXT COLLATE Latin1_General_BIN,
				[ws] int,
				[ord] int primary key)

			insert into @tblMultiBigTxtVernacular
			select
				isnull(mbt.[txt], '***') txt,
				mbt.[ws],
				isnull(lpcve.[ord], 99998) [ord]
			from MultiBigTxt$ mbt
			left outer join LgWritingSystem le on le.[Id] = mbt.[ws]
			left outer join LangProject_VernWss lpve on lpve.[dst] = le.[id]
			left outer join LangProject_CurVernWss lpcve on lpcve.[dst] = lpve.[dst]
			where mbt.[obj] = @id and mbt.[flid] = @flid
			order by isnull([ord], 99998)

			insert into @tblMultiBigTxtVernacular
			select convert(ntext, '***') [txt], 0 [ws], 99999 [ord]

			select * from @tblMultiBigTxtVernacular order by [ord]
		end

		-- MultiTxt$ --
		else if @iFieldType = 16 BEGIN
			SET @nvcSql =
				N' select ' + CHAR(13) +
					N'isnull(mt.[txt], ''***'') txt, ' + CHAR(13) +
					N'mt.[ws], ' + CHAR(13) +
					N'isnull(lpcve.[ord], 99998) ord ' + CHAR(13) +
				N'from ' + @nvcTable + N' mt ' + CHAR(13) +
				N'left outer join LgWritingSystem le on le.[Id] = mt.[ws] ' + CHAR(13) +
				N'left outer join LangProject_VernWss lpve ' +
					N'on lpve.[dst] = le.[id] ' + CHAR(13) +
				N'left outer join LangProject_CurVernWss lpcve ' +
					N'on lpcve.[dst] = lpve.[dst] ' + CHAR(13) +
				N'where mt.[obj] = @id ' + CHAR(13) +
				N'union all ' + CHAR(13) +
				N'select ''***'', 0, 99999 ' + CHAR(13) +
				N'order by isnull([ord], 99998) '

			EXEC sp_executesql @nvcSql, N'@id INT', @id
		END
	end
	else
		raiserror('@anal flag not set correctly', 16, 1)
		goto LFail

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

LFail:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	go

-------------------------------------------------------------------------------
-- FWC-10: Reload stored procedure GetOrderedMultiTxtXml$
-------------------------------------------------------------------------------
if object_id('GetOrderedMultiTxtXml$') is not null begin
	print 'removing procedure GetOrderedMultiTxtXml$'
	drop proc [GetOrderedMultiTxtXml$]
end
go
print 'creating proc GetOrderedMultiTxtXml$'
go

create proc GetOrderedMultiTxtXml$
	@hXMLDocObjList int = null,
	@iFlid int,
	@tiAnal tinyint = 1
as

	declare @fIsNocountOn int
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	DECLARE
		@nvcTable NVARCHAR(60),
		@nvcSql NVARCHAR(4000)

	EXEC GetMultiTableName @iflid, @nvcTable OUTPUT

	if @tiAnal = 1 BEGIN

		SET @nvcSql = N'select ' + CHAR(13) +
			N'ids.[Id], ' + CHAR(13) +
			N'isnull((select top 1 isnull(mt.[txt], ''***'') ' + CHAR(13) +
				N'from ' + @nvcTable + N' mt ' + CHAR(13) +
				N'left outer join LgWritingSystem le on le.[Id] = mt.[ws] ' + CHAR(13) +
				N'left outer join LangProject_AnalysisWss lpae ' +
					N'on lpae.[dst] = le.[id] ' + CHAR(13) +
				N'left outer join LangProject_CurAnalysisWss lpcae ' +
					N'on lpcae.[dst] = lpae.[dst] ' + CHAR(13) +
				N'where mt.[obj] = ids.[Id] ' + CHAR(13) +
				N'order by isnull(lpcae.[ord], 99999)), ''***'') as [txt] , ' + CHAR(13) +
			N'isnull((select top 1 isnull(mt.[ws], 0) ' + CHAR(13) +
				N'from ' + @nvcTable + N' mt ' + CHAR(13) +
				N'left outer join LgWritingSystem le on le.[Id] = mt.[ws] ' + CHAR(13) +
				N'left outer join LangProject_AnalysisWss lpae ' +
					N' on lpae.[dst] = le.[id] ' + CHAR(13) +
				N'left outer join LangProject_CurAnalysisWss lpcae ' +
					N'on lpcae.[dst] = lpae.[dst] ' + CHAR(13) +
				N'where mt.[obj] = ids.[Id] ' + CHAR(13) +
				N'order by isnull(lpcae.[ord], 99999)), 0) as [ws] ' + CHAR(13) +
			N'from openxml (@hXMLDocObjList, ''/root/Obj'') with ([Id] int) ids '

		EXEC sp_executesql @nvcSql, N'@hXMLDocObjList INT', @hXMLDocObjList
	END
	else if @tiAnal = 0 BEGIN

		SET @nvcSql = N'select ' + CHAR(13) +
			N'ids.[Id], ' + CHAR(13) +
			N'isnull((select top 1 isnull(mt.[txt], ''***'') as [txt] ' + CHAR(13) +
				N'from ' + @nvcTable + N' mt ' + CHAR(13) +
				N'left outer join LgWritingSystem le on le.[Id] = mt.[ws] ' + CHAR(13) +
				N'left outer join LangProject_VernWss lpve ' +
					N'on lpve.[dst] = le.[id] ' + CHAR(13) +
				N'left outer join LangProject_CurVernWss lpcve ' +
					N'on lpcve.[dst] = lpve.[dst] ' + CHAR(13) +
				N'where mt.[obj] = ids.[Id] ' + CHAR(13) +
				N'order by isnull(lpcve.[ord], 99999)), ''***'') , ' + CHAR(13) +
			N'isnull((select top 1 isnull(mt.[ws], 0) as [ws] ' + CHAR(13) +
				N'from ' + @nvcTable + N' mt ' + CHAR(13) +
				N'left outer join LgWritingSystem le on le.[Id] = mt.[ws] ' + CHAR(13) +
				N'left outer join LangProject_VernWss lpve ' +
					N'on lpve.[dst] = le.[id] ' + CHAR(13) +
				N'left outer join LangProject_CurVernWss lpcve ' +
					N'on lpcve.[dst] = lpve.[dst] ' + CHAR(13) +
				N'where mt.[obj] = ids.[Id] ' + CHAR(13) +
				N'order by isnull(lpcve.[ord], 99999)), 0) ' + CHAR(13) +
			N'from openxml (@hXMLDocObjList, ''/root/Obj'') with ([Id] int) ids '

		EXEC sp_executesql @nvcSql, N'@hXMLDocObjList INT', @hXMLDocObjList
	END
	else begin
		raiserror('@tiAnal flag not set correctly', 16, 1)
		goto LFail
	end

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

LFail:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

go
-------------------------------------------------------------------------------
-- FWC-10: Reload stored procedure NoteInterlinProcessTime
-------------------------------------------------------------------------------
if object_id('NoteInterlinProcessTime') is not null begin
	print 'removing proc NoteInterlinProcessTime'
	drop proc [NoteInterlinProcessTime]
end
go
print 'creating proc NoteInterlinProcessTime'
go

create proc NoteInterlinProcessTime
	@atid INT, @stid INT,
	@nvNew nvarchar(4000) output
AS BEGIN

	declare @lpid int
	select top 1 @lpid = id from LangProject

	set @nvNew = ''

	declare MakeAnnCursor cursor local static forward_only read_only for
	select tp.id from StTxtPara_ tp
	left outer join CmBaseAnnotation_ cb
					on cb.BeginObject = tp.id and cb.AnnotationType = @atid
	where tp.owner$ = @stid and cb.id is null

	declare @tpid int,
		@NewObjGuid uniqueidentifier,
		@cbaId int
	open MakeAnnCursor
		fetch MakeAnnCursor into @tpid
		while @@fetch_status = 0 begin
			exec CreateOwnedObject$ 37, @cbaId out, @NewObjGuid out, @lpid, 6001044, 25
			set @nvNew = @nvNew + ',' + cast(@cbaId as nvarchar(8))
			update CmBaseAnnotation set BeginObject = @tpid where id = @cbaId
			update CmAnnotation set AnnotationType = @atid where id = @cbaId
			set @cbaId = null
			set @NewObjGuid = null
			fetch MakeAnnCursor into @tpid
		end
	close MakeAnnCursor
	deallocate MakeAnnCursor

	update CmBaseAnnotation_
	set CompDetails = cast(cast(tp.UpdStmp as bigint) as NVARCHAR(20))
	from CmBaseAnnotation_ cba
	join StTxtPara_ tp on cba.BeginObject = tp.id and tp.owner$ = @stid
	where cba.AnnotationType = @atid

	return @@ERROR
END
GO
-------------------------------------------------------------------------------
-- FWC-10: Reload stored procedure CreateFlidCollations
-------------------------------------------------------------------------------
IF OBJECT_ID('CreateFlidCollations') IS NOT NULL BEGIN
	PRINT 'removing procedure CreateFlidCollations'
	DROP PROCEDURE [CreateFlidCollations]
END
GO
PRINT 'creating procedure CreateFlidCollations'
GO
CREATE PROCEDURE [CreateFlidCollations]
	@nEncID INT,
	@nCollationID INT,
	@nFlid INT
AS

	-- TODO (SteveMi): Only certain strings should have FlidCollation
	-- and MultiTxtSortKey$

	IF @nEncId IS NOT NULL
		PRINT 'todo'
		-- TODO (SteveMi): this chunk

	ELSE IF @nCollationID IS NOT NULL
		PRINT 'todo'
		-- TODO (SteveMi): this chunk

	ELSE IF @nFlid IS NOT NULL

		--( We want to create flidcollations only for current writing systems.
		--( We also don't want to duplicate any Writing System/Collation/Flid
		--( combination.

		INSERT INTO FlidCollation$ ([Ws], [CollationId], [Flid])
		SELECT curwritingsys.[Ws], wsc.[Dst] AS [CollationID], @nFlid AS [Flid]
		FROM (
			SELECT [Dst] AS [Ws] FROM LangProject_CurAnalysisWss
			UNION
			SELECT [Dst] AS [Ws] FROM LangProject_CurVernWss) curwritingsys
		JOIN LgWritingSystem_Collations wsc ON wsc.[Src] = curwritingsys.[Ws]
		LEFT OUTER JOIN FlidCollation$ fc ON
			fc.[Ws] = curwritingsys.[Ws] AND
			fc.[CollationID] = wsc.[Dst] AND
			fc.[Flid] = @nFlid
		WHERE (
			fc.[Ws] IS NULL OR
			fc.[CollationID] IS NULL OR
			fc.[Flid] IS NULL)

	--( The insert into FlidCollation$ should trigger an insert trigger
	--( sort key creation in MultiTxtSortKey$

	-- TODO (SteveMi): above comment.

GO


-------------------------------------------------------------------------------
-- FWC-10: Reload stored procedure DeleteFlidCollations
-------------------------------------------------------------------------------

IF OBJECT_ID('DeleteFlidCollations') IS NOT NULL BEGIN
	PRINT 'removing procedure DeleteFlidCollations'
	DROP PROCEDURE [DeleteFlidCollations]
END
GO
PRINT 'creating procedure DeleteFlidCollations'
GO
CREATE PROCEDURE [DeleteFlidCollations]
	@nEncID INT,
	@nCollationID INT,
	@nFlid INT
AS

	-- TODO (SteveMi): Only certain strings should have FlidCollation
	-- and MultiTxtSortKey$

	IF @nEncId IS NOT NULL
		PRINT 'todo'
		-- TODO (SteveMi): this chunk

	ELSE IF @nCollationID IS NOT NULL
		PRINT 'todo'
		-- TODO (SteveMi): this chunk

	ELSE IF @nFlid IS NOT NULL


		INSERT INTO FlidCollation$ ([Ws], [CollationId], [Flid])
		SELECT e.[Ws], wsc.[Dst] AS [CollationID], @nFlid AS [Flid]
		FROM (
			SELECT [Dst] AS [Ws] FROM LangProject_CurAnalysisWss
			UNION
			SELECT [Dst] AS [Ws] FROM LangProject_CurVernWss) e
		JOIN LgWritingSystem_Collations wsc ON wsc.[Src] = e.[Ws]
		LEFT OUTER JOIN FlidCollation$ fc ON
			fc.[Ws] = e.[Ws] AND
			fc.[CollationID] = wsc.[Dst] AND
			fc.[Flid] = @nFlid
		WHERE (
			fc.[Ws] IS NULL OR
			fc.[CollationID] IS NULL OR
			fc.[Flid] IS NULL)

	--( The delete into FlidCollation$ should trigger an delete trigger
	--( sort key creation in MultiTxtSortKey$

	-- TODO (SteveMi): above comment.

GO
-------------------------------------------------------------------------------
-- FWC-10: Reload stored procedure GetPossibilities
-------------------------------------------------------------------------------
if object_id('GetPossibilities') is not null begin
	print 'removing proc GetPossibilities'
	drop proc [GetPossibilities]
end
go
print 'creating proc GetPossibilities'
go
create proc [GetPossibilities]
	@ObjId int,
	@Ws int
as
	declare @uid uniqueidentifier,
			@retval int

	-- get all of the possibilities owned by the specified possibility list object
	declare @tblObjInfo table (
		[ObjId]		int		not null,
		[ObjClass]	int		null,
		[InheritDepth]	int		null	default(0),
		[OwnerDepth]	int		null	default(0),
		[RelObjId]	int		null,
		[RelObjClass]	int		null,
		[RelObjField]	int		null,
		[RelOrder]	int		null,
		[RelType]	int		null,
		[OrdKey]	varbinary(250)	null	default(0))

	insert into @tblObjInfo
		select * from fnGetOwnedObjects$(@ObjId, null, 176160768, 0, 0, 1, 7, 0)

	-- First return a count so that the caller can preallocate memory for the results.
	select count(*) from @tblObjInfo

	--
	--  get an ordered list of relevant writing system codes
	--
	declare @tblWs table (
		[WsId]	int not null, -- don't make unique. It shouldn't happen, but we don't want a crash if it does.
		[Ord]	int primary key clustered identity(1,1))
	--( 0xffffffff (-1) or 0xfffffffd (-3) = First string from a) ordered checked analysis
	-- writing systems b) any remaining analysis writing systems or stars if none of the above.
	if @Ws = 0xffffffff or @Ws = 0xfffffffd begin
		insert into @tblWs (WsId)
			select lws.id
			from LangProject_CurAnalysisWss caws
			join LgWritingSystem lws on caws.dst = lws.id
			order by caws.Ord
		insert into @tblWs (WsId)
			select distinct lws.id
			from LangProject_AnalysisWss caws
			join LgWritingSystem lws on caws.dst = lws.id and lws.id not in (select WsId from @tblWs)
	end
	--( 0xfffffffe (-2) or 0xfffffffc (-4) = First string from a) ordered checked vernacular
	-- writing systems b) any remaining vernacular writing systems or stars if none of the above.
	else if @Ws = 0xfffffffe or @Ws = 0xfffffffc begin
		insert into @tblWs (WsId)
			select lws.id
			from LangProject_CurVernWss cvws
			join LgWritingSystem lws on cvws.dst = lws.id
			order by cvws.Ord
		insert into @tblWs (WsId)
			select distinct lws.id
			from LangProject_VernWss cvws
			join LgWritingSystem lws on cvws.dst = lws.id and lws.id not in (select WsId from @tblWs)
	end
	--( 0xfffffffb = -5 = First string from a) ordered checked analysis writing systems
	-- b) ordered checked vernacular writing systems, c) any remaining analysis writing systems,
	-- d) any remaining vernacular writing systems or stars if none of the above.
	else if @Ws = 0xfffffffb begin
		insert into @tblWs (WsId)
			select lws.id
			from LangProject_CurAnalysisWss caws
			join LgWritingSystem lws on caws.dst = lws.id
			order by caws.Ord
		insert into @tblWs (WsId)
			select lws.id
			from LangProject_CurVernWss cvws
			join LgWritingSystem lws on cvws.dst = lws.id
			where lws.id not in (select WsId from @tblWs)
			order by cvws.Ord
		insert into @tblWs (WsId)
			select distinct lws.id
			from LangProject_AnalysisWss caws
			join LgWritingSystem lws on caws.dst = lws.id and lws.id not in (select WsId from @tblWs)
		insert into @tblWs (WsId)
			select distinct lws.id
			from LangProject_VernWss cvws
			join LgWritingSystem lws on cvws.dst = lws.id and lws.id not in (select WsId from @tblWs)
	end
	--( 0xfffffffa = -6 = First string from a) ordered checked vernacular writing systems
	-- b) ordered checked analysis writing systems, c) any remaining vernacular writing systems,
	-- d) any remaining analysis writing systems or stars if none of the above.
	else if @Ws = 0xfffffffa begin
		insert into @tblWs (WsId)
			select lws.id
			from LangProject_CurVernWss cvws
			join LgWritingSystem lws on cvws.dst = lws.id
			order by cvws.Ord
		insert into @tblWs (WsId)
			select lws.id
			from LangProject_CurAnalysisWss caws
			join LgWritingSystem lws on caws.dst = lws.id
			where lws.id not in (select WsId from @tblWs)
			order by caws.Ord
		insert into @tblWs (WsId)
			select distinct lws.id
			from LangProject_VernWss cvws
			join LgWritingSystem lws on cvws.dst = lws.id and lws.id not in (select WsId from @tblWs)
		insert into @tblWs (WsId)
			select distinct lws.id
			from LangProject_AnalysisWss caws
			join LgWritingSystem lws on caws.dst = lws.id and lws.id not in (select WsId from @tblWs)
	end
	else -- Hard coded value
		insert into @tblWs (WsId) Values(@Ws)

	-- Now that we have the desired writing systems in @tblWs, we can return the desired information.
	select
		o.ObjId,
		(select top 1 isnull(ca.[txt], '***') + ' - ' + isnull(cn.[txt], '***')
			from LgWritingSystem lws
			left outer join CmPossibility_Name cn on cn.[ws] = lws.[Id] and cn.[Obj] = o.[objId]
			left outer join CmPossibility_Abbreviation ca on ca.[ws] = lws.[Id] and ca.[Obj] = o.[objId]
			join @tblWs wstbl on wstbl.WsId = lws.id
			order by (
				select [Ord] = CASE
					WHEN cn.[txt] IS NOT NULL THEN wstbl.[ord]
					WHEN ca.[txt] IS NOT NULL THEN wstbl.[ord] + 9000
					ELSE wstbl.[Ord] + 99000
					END)),
		isnull((select top 1 lws.id
			from LgWritingSystem lws
			left outer join CmPossibility_Name cn on cn.[ws] = lws.[Id] and cn.[Obj] = o.[objId]
			left outer join CmPossibility_Abbreviation ca on ca.[ws] = lws.[Id] and ca.[Obj] = o.[objId]
			join @tblWs wstbl on wstbl.WsId = lws.id
			order by (
				select [Ord] = CASE
					WHEN cn.[txt] IS NOT NULL THEN wstbl.[ord]
					WHEN ca.[txt] IS NOT NULL THEN wstbl.[ord] + 9000
					ELSE wstbl.[Ord] + 99000
					END)
			), (select top 1 WsId from @tblws)),
		o.OwnerDepth, cp.ForeColor, cp.BackColor, cp.UnderColor, cp.UnderStyle, o.RelObjId
	from @tblObjInfo o
		left outer join CmPossibility cp on cp.[id] = o.[objId]
	order by o.OwnerDepth, o.RelOrder

	return @retval
go

-------------------------------------------------------------------------------
-- FWC-10: Reload stored procedure GetPossKeyword
-------------------------------------------------------------------------------
-----------------------------------------------------------------------------------------
-- Description: retrieves the possibilities and their abbreviations of a specified possibility list
--    that have a specific keyword in their names
-- Parameters:
--    @ObjId=the object Id of the possibility list;
--    @Ws=the writing system;
--    @sKeyword=the keyword to search for
-- Returns: 0 if successful, otherwise an error code

-- ***** TODO: This is not correct. It doesn't handle kwsVernAnals & kwsAnalVerns, and might possibly
-- have other problems that we solved in GetPossibilities. However, this whole procedure will
-- need to be updated before release to do proper matching based on proper Unicode algorithms,
-- so we aren't fixing this at this tiem.

if object_id('GetPossKeyword') is not null begin
	print 'removing proc GetPossKeyword'
	drop proc [GetPossKeyword]
end
go
print 'creating proc GetPossKeyword'
go

create proc [GetPossKeyword]
	@ObjId int,
	@Ws int,
	@sKeyword nvarchar(250)
as
	declare @retval int

	-- get all of the possibilities owned by the specified possibility list object

	declare @tblObjInfo table (
		[ObjId]	int not null,
		[ObjClass] int null,
		[InheritDepth] int null default(0),
		[OwnerDepth] int null default(0),
		[RelObjId] int null,
		[RelObjClass] int null,
		[RelObjField] int null,
		[RelOrder] int null,
		[RelType] int null,
		[OrdKey] varbinary(250)	null default(0))


	insert into @tblObjInfo
		select * from fnGetOwnedObjects$(@ObjId, null, 176160768, 1, 0, 1, null, 1)

	-- Fudge this for now so it doesn't crash.
	if @Ws = 0xffffffff or @Ws = 0xfffffffd or @Ws = 0xfffffffb

		--( To avoid seeing stars, send a "magic" writing system of 0xffffffff.
		--( This will cause the query to return the first non-null string.
		--( Priority is givin to encodings with the highest order.

		select
			o.ObjId,
			isnull((select top 1 txt
				from CmPossibility_Name cn
				left outer join LgWritingSystem le on le.[Id] = cn.[ws]
				left outer join LangProject_AnalysisWss lpaws on lpaws.[dst] = le.[id]
				left outer join LangProject_CurAnalysisWss lpcaws on lpcaws.[dst] = lpaws.[dst]
				where cn.[Obj] = o.[objId] and cn.[Txt] like '%' + @sKeyword + '%'
				order by isnull(lpcaws.[ord], 99999)), '***'),
			isnull((select top 1 txt
				from CmPossibility_Abbreviation ca
				left outer join LgWritingSystem le on le.[Id] = ca.[ws]
				left outer join LangProject_AnalysisWss lpaws on lpaws.[dst] = le.[id]
				left outer join LangProject_CurAnalysisWss lpcaws on lpcaws.[dst] = lpaws.[dst]
				where ca.[Obj] = o.[objId]
				order by isnull(lpcaws.[ord], 99999)), '***'),
			o.OrdKey
		from @tblObjInfo o
		where o.[ObjClass] = 7  -- CmPossibility
		order by o.OrdKey

	else if @Ws = 0xfffffffe or @Ws = 0xfffffffc or @Ws = 0xfffffffa

		--( To avoid seeing stars, send a "magic" writing system of 0xfffffffe.
		--( This will cause the query to return the first non-null string.
		--( Priority is givin to encodings with the highest order.

		select
			o.ObjId,
			isnull((select top 1 txt
				from CmPossibility_Name cn
				left outer join LgWritingSystem le on le.[Id] = cn.[ws]
				left outer join LangProject_VernWss lpvws on lpvws.[dst] = le.[id]
				left outer join LangProject_CurVernWss lpcvws on lpcvws.[dst] = lpvws.[dst]
				where cn.[Obj] = o.[objId] and cn.[Txt] like '%' + @sKeyword + '%'
				order by isnull(lpcvws.[ord], 99999)), '***'),
			isnull((select top 1 txt
				from CmPossibility_Abbreviation ca
				left outer join LgWritingSystem le on le.[Id] = ca.[ws]
				left outer join LangProject_VernWss lpvws on lpvws.[dst] = le.[id]
				left outer join LangProject_CurVernWss lpcvws on lpcvws.[dst] = lpvws.[dst]
				where ca.[Obj] = o.[objId]
				order by isnull(lpcvws.[ord], 99999)), '***'),
			o.OrdKey
		from @tblObjInfo o
		where o.[ObjClass] = 7  -- CmPossibility
		order by o.OrdKey

	else
		select	o.ObjId, isnull(cn.txt, '***'), isnull(ca.txt, '***'), o.OrdKey
			from @tblObjInfo o
				left outer join [CmPossibility_Name] cn
					on cn.[Obj] = o.[ObjId] and cn.[Ws] = @Ws
				left outer join [CmPossibility_Abbreviation] ca
					on ca.[Obj] = o.[ObjId] and ca.[Ws] = @Ws
			where o.[ObjClass] = 7  -- CmPossibility
				and cn.[Txt] like '%' + @sKeyword + '%'
			order by o.OrdKey

	return @retval
go
-------------------------------------------------------------------------------
-- FWC-10: Reload stored procedure GetTagInfo$
-------------------------------------------------------------------------------
if object_id('GetTagInfo$') is not null begin
	print 'removing procedure GetTagInfo$'
	drop proc [GetTagInfo$]
end
go
print 'creating proc GetTagInfo$'
go

create proc GetTagInfo$
	@iOwnerId int,
	@iWritingSystem int
as

	declare @fIsNocountOn int
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- TODO (SteveM) This needs to be fixed to handle 0xfffffffb and 0xfffffffa properly.
	--( if "magic" writing system is for analysis encodings
	if @iWritingSystem = 0xffffffff or @iWritingSystem = 0xfffffffd or @iWritingSystem = 0xfffffffb
		select
			[co].[Guid$],
			[opi].[Dst],
			isnull((select top 1 [ca].[txt]
				from CmPossibility_Abbreviation ca
				left outer join LgWritingSystem le
					on le.[Id] = ca.[ws]
				left outer join LangProject_AnalysisWss lpaws
					on lpaws.[dst] = le.[id]
				left outer join LangProject_CurAnalysisWss lpcaws
					on lpcaws.[dst] = lpaws.[dst]
				where ca.[Obj] = [opi].[Dst]
				order by isnull(lpcaws.[ord], 99999)), '***'),
			isnull((select top 1 [cn].[txt]
				from CmPossibility_Name cn
				left outer join LgWritingSystem le
					on le.[Id] = cn.[ws]
				left outer join LangProject_AnalysisWss lpaws
					on lpaws.[dst] = le.[id]
				left outer join LangProject_CurAnalysisWss lpcaws
					on lpcaws.[dst] = lpaws.[dst]
				where cn.[Obj] = [opi].[Dst]
				order by isnull(lpcaws.[ord], 99999)), '***'),
			[cp].[ForeColor],
			[cp].[BackColor],
			[cp].[UnderColor],
			[cp].[UnderStyle],
			[cp].[Hidden]
		from CmOverlay_PossItems [opi]
			join CmPossibility [cp] on [cp].[id] = [opi].[Dst]
			join CmObject [co] on [co].[id] = [cp].[id]
		where [opi].[Src] = @iOwnerId
		order by [opi].[Dst]

	--( if "magic" writing system is for vernacular encodings
	else if @iWritingSystem = 0xfffffffe or @iWritingSystem = 0xfffffffc or @iWritingSystem = 0xfffffffa
		select
			[co].[Guid$],
			[opi].[Dst],
			isnull((select top 1 txt
				from CmPossibility_Abbreviation ca
				left outer join LgWritingSystem le
					on le.[Id] = ca.[ws]
				left outer join LangProject_VernWss lpvws
					on lpvws.[dst] = le.[id]
				left outer join LangProject_CurVernWss lpcvws
					on lpcvws.[dst] = lpvws.[dst]
				where ca.[Obj] = [opi].[Dst]
				order by isnull(lpcvws.[ord], 99999)), '***'),
			isnull((select top 1 txt
				from CmPossibility_Name cn
				left outer join LgWritingSystem le
					on le.[Id] = cn.[ws]
				left outer join LangProject_VernWss lpvws
					on lpvws.[dst] = le.[id]
				left outer join LangProject_CurVernWss lpcvws
					on lpcvws.[dst] = lpvws.[dst]
				where cn.[Obj] = [opi].[Dst]
				order by isnull(lpcvws.[ord], 99999)), '***'),
			[cp].[ForeColor],
			[cp].[BackColor],
			[cp].[UnderColor],
			[cp].[UnderStyle],
			[cp].[Hidden]
		from CmOverlay_PossItems [opi]
			join CmPossibility [cp] on [cp].[id] = [opi].[Dst]
			join CmObject [co] on [co].[id] = [cp].[id]
		where [opi].[Src] = @iOwnerId
		order by [opi].[Dst]

	--( if one particular writing system is wanted
	else
		select
			[co].[Guid$],
			[opi].[Dst],
			isnull([ca].[txt], '***'),
			isnull([cn].[txt], '***'),
			[cp].[ForeColor],
			[cp].[BackColor],
			[cp].[UnderColor],
			[cp].[UnderStyle],
			[cp].[Hidden]
		from CmOverlay_PossItems [opi]
			join CmPossibility [cp] on [cp].[id] = [opi].[Dst]
			join CmObject [co] on [co].[id] = [cp].[id]
			left outer join CmPossibility_Abbreviation [ca]
				on [ca].[Obj] = [opi].[Dst] and [ca].[ws] = @iWritingSystem
			left outer join CmPossibility_Name cn
				on [cn].[Obj] = [opi].[Dst] and [cn].[ws] = @iWritingSystem
		where [opi].[Src] = @iOwnerId
		order by [opi].[Dst]

	--( if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

LFail:
	--( if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

go

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200204
BEGIN
	UPDATE Version$ SET DbVer = 200205
	COMMIT TRANSACTION
	PRINT 'database updated to version 200205'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200204 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
