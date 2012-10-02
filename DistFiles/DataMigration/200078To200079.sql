-- Update database from version 200077 to 200078
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

if object_id('DisplayName_Msa') is not null begin
	drop proc DisplayName_MSA
end
go
print 'creating proc DisplayName_Msa'
go
/***********************************************************************************************
 * Procedure: DisplayName_Msa
 * Description: This procedure returns display information for all
 *	subclasses of MoMorphoSyntaxAnalysis
 * Assumptions:
 *	The input XML is of the form: <root><Obj Id="7164"/><Obj Id="7157"/>...</root>
 * Parameters:
 *    @XMLIds - the object IDs of the MSA(s), or all of them if null
 * Return: 0 if successful, otherwise 1.
***********************************************************************************************/
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
	--( 5031	MoDerivationalAffixMsa
	--( 5032	MoDerivationalStepMsa
	--( 5038	MoInflectionalAffixMsa
	--( 5117	MoUnclassifiedAffixMsa

	if @XMLIds is null begin
		insert #DNLE exec DisplayName_LexEntry null
		-- Do all MSAes.
		set @myCursor = CURSOR FAST_FORWARD for
			select Id, Class$
			from CmObject (readuncommitted)
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
			join CmObject cmo (readuncommitted)
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
		from CmObject (readuncommitted)
		where Id=@MsaId

		set @XMLLEId = '<root><Obj Id="' + cast(@LeId as nvarchar(100)) + '"/></root>'

		if @XMLIds is not null
			insert #DNLE exec DisplayName_LexEntry @XMLLEId
		select @MsaForm=FullTxt,
			@FormId=FormId, @FormFlid=Flid, @FormTxt=FormTxt, @FormFmt=FormFmt, @FormEnc=FormEnc,
			@SenseId=SenseId, @SenseTxt=SenseGloss, @SenseFmt=SenseFmt, @SenseEnc=SenseEnc
		from #DNLE (readuncommitted)
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
			from MoStemMsa msa (readuncommitted)
			left outer join PartOfSpeech pos (readuncommitted) On pos.Id=msa.PartOfSpeech
			left outer join CmPossibility_Name nm (readuncommitted) On nm.Obj=pos.Id and nm.Ws=@SenseEnc
			where msa.Id=@MsaId
			set @MsaForm = @MsaForm + 'stem/root: ' + @POSaTxt
		end
		else if @MsaClass=5038 begin --MoInflectionalAffixMsa
			select top 1 @POSaID=pos.Id, @POSaTxt=isnull(nm.Txt, '***'),
					@POSaFmt=cast(null as varbinary), @POSaEnc=nm.Ws
			from MoInflectionalAffixMsa msa
			left outer join PartOfSpeech pos On pos.Id=msa.PartOfSpeech
			left outer join CmPossibility_Name nm On nm.Obj=pos.Id and nm.Ws=@SenseEnc
			where msa.Id=@MsaId

			SET @SlotsTxt=''

			select top 1 @SlotTxt=slot_nm.Txt
			from MoInflectionalAffixMsa_Slots msa_as
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
				from MoInflectionalAffixMsa_Slots msa_as
				join MoInflAffixSlot slot On slot.Id=msa_as.Dst
				join MoInflAffixSlot_Name slot_nm On slot_nm.Obj=slot.Id and slot_nm.Ws=@SenseEnc
				where @MsaId=msa_as.Src AND slot_nm.Txt > @SlotTxt
				ORDER BY slot_nm.Txt
				SET @cnt = @@rowcount
			END

			if @SlotsTxt='' SET @SlotsTxt=null
			set @MsaForm = @MsaForm + 'inflectional: ' + @POSaTxt + ':(' + isnull(@SlotsTxt, '***') + ')'
		end
		else if @MsaClass=5031 begin	--MoDerivationalAffixMsa
			-- FromPartOfSpeech
			select top 1 @POSaID=pos.Id, @POSaTxt=isnull(nm.Txt, '***'),
					@POSaFmt=cast(null as varbinary), @POSaEnc=nm.Ws
			from MoDerivationalAffixMsa msa (readuncommitted)
			left outer join PartOfSpeech pos (readuncommitted) On pos.Id=msa.FromPartOfSpeech
			left outer join CmPossibility_Name nm (readuncommitted) On nm.Obj=pos.Id and nm.Ws=@SenseEnc
			where msa.Id=@MsaId
			-- ToPartOfSpeech
			select top 1 @POSbID=pos.Id, @POSbTxt=isnull(nm.Txt, '***'),
					@POSbFmt=cast(null as varbinary), @POSbEnc=nm.Ws
			from MoDerivationalAffixMsa msa (readuncommitted)
			left outer join PartOfSpeech pos (readuncommitted) On pos.Id=msa.ToPartOfSpeech
			left outer join CmPossibility_Name nm (readuncommitted) On nm.Obj=pos.Id and nm.Ws=@SenseEnc
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
		else if @MsaClass=5032 begin	--MoDerivationalStepMsa
			select top 1 @POSaID=pos.Id, @POSaTxt=isnull(nm.Txt, '***'),
					@POSaFmt=cast(null as varbinary), @POSaEnc=nm.Ws
			from MoDerivationalStepMsa msa (readuncommitted)
			left outer join PartOfSpeech pos (readuncommitted) On pos.Id=msa.PartOfSpeech
			left outer join CmPossibility_Name nm (readuncommitted) On nm.Obj=pos.Id and nm.Ws=@SenseEnc
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

---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200078
begin
	update Version$ set DbVer = 200079
	COMMIT TRANSACTION
	print 'database updated to version 200079'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200078 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end

GO
