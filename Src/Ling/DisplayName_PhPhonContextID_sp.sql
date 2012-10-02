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
/***********************************************************************************************
 * DisplayName_PhPhonContextID
 * Returns a list of ids and strings.
 * The ids are PhPhonContext objects given in the XML input.
 * The strings are a 'pretty print' representation of a PhPhonContext
 * in the form of a left or right context of a string environment constraint,
 * as used by XAmple (e.g., # [C] _ a).
 * Parameters
 *   @ContextId - Id of PhPhonContext object to work on
 *   @ContextString nvarchar(4000) - The returned string
***********************************************************************************************/
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
