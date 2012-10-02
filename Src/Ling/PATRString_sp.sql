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
if object_id('PATRString_FsFeatSpec') is not null begin
	drop proc PATRString_FsFeatSpec
end
go

-- Create to 'empty' SPs.
print 'creating proc PATRString_FsFeatSpec'
go
create proc PATRString_FsFeatSpec
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
/***********************************************************************************************
 * PATRString_FsAbstractStructure
 * Description:
 *   Returns a PC-PATR usable string.
 *   The string is of the form [per:1], which is usable by PC-PATR in the XAmple parser.
 *
 * Parameters:
 *   @Def - Should be either an empty string (''), or '!', which is the tag used by PC-PATR
 *		for a default value.
 *   @Id - An id of an FsFeatStruc.
 *   @PATRString - The returned PC-PATR string
***********************************************************************************************/
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
			exec @retval = PATRString_FsFeatSpec @Def, @CurDstId, @Txt output
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


print 'altering proc PATRString_FsFeatSpec'
go
/***********************************************************************************************
 * PATRString_FsFeatSpec
 *
 * Description:
 *   Returns a PC-PATR usable string for an FsFeatureSpecification.
 *   The string is of the form [per:2], which is usable by PC-PATR in the XAmple parser.
 *
 * Parameters:
 *   @Def - Should be either an empty string (''), or '!', which is the tag used by PC-PATR
 *		for a default value.
 *   @Id - An id of an FsFeatureSpecification.
 *   @PATRString - The returned PC-PATR string
***********************************************************************************************/

alter proc PATRString_FsFeatSpec
	@Def nvarchar(1),
	@Id int,
	@PATRString nvarchar(4000) output
as
	declare @fIsNocountOn int, @retval int,
		@tCount int, @cCur int, @CurId int,
		@CurDstId int, @Class int,
		@ValueId int, @ValueClass int, @FDID int,
		@Label nvarchar(4000), @Value nvarchar(4000)

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- Get name of FsFeatDefn.
	-- If there is no name or FsFeatDefn, then quit.
	-- We don't care which writing system is used.
	select top 1 @Label = fdn.Txt, @FDID = fd.Id
	from FsFeatureSpecification fs
	join FsFeatDefn fd On fs.Feature = fd.Id
	join FsFeatDefn_Name fdn On fd.Id = fdn.Obj
	where fs.Id = @Id
	order by Ws
	-- Check for null value in @PATRString
	if @Label is null begin
		set @PATRString = ''
		set @retval = 1
		goto LFail
	end

	-- Handle various values in subclasses of FsFeatureSpecification
	select @Class = Class$
	from CmObject
	where Id = @Id
	if @Class = 2003 begin	-- FsClosedValue
		select top 1 @Value = Txt
		from FsClosedValue cv
		join FsSymFeatVal sfv On cv.Value = sfv.Id
		join FsSymFeatVal_Name sfvn On sfvn.Obj = sfv.Id
		where cv.Id = @Id
		if @Value is null begin
			-- Try default value.
			select @FDID=Dst
			from FsFeatDefn_Default
			where Src=@FDID
			exec @retval = PATRString_FsFeatSpec '!', @FDID, @Value output
			if @retval != 0 begin
				set @PATRString = ''
				set @retval = 1
				goto LFail
			end
			set @PATRString = @Value
		end
		else set @PATRString = @Label + ':' + @Def + @Value
	end
	else if @Class = 2006 begin	-- FsDisjunctiveValue
		set @PATRString = @Label + ':{'
		set @tCount = 0
		select top 1 @CurDstId = Dst
		from FsDisjunctiveValue_Value
		where Src = @Id
		order by Dst
		set @Value = ''
		while @@rowcount > 0 begin
			if @tCount > 0 set @PATRString = @PATRString + ' '
			if @Def = '!' set @PATRString = @PATRString + '!'
			set @tCount = 1
			select top 1 @Value = Txt
			from FsSymFeatVal sfv
			join FsSymFeatVal_Name sfvn On sfvn.Obj = sfv.Id
			where sfv.Id = @CurDstId
			if @Value is null begin
				-- Try default value.
				select @FDID=Dst
				from FsFeatDefn_Default
				where Src=@FDID
				exec @retval = PATRString_FsFeatSpec '!', @FDID, @Value output
				if @retval != 0 begin
					set @PATRString = ''
					set @retval = 1
					goto LFail
				end
				set @PATRString = @PATRString + @Value
			end
			else set @PATRString = @PATRString + @Value
			-- Try getting another one
			select top 1 @CurDstId = Dst
			from FsDisjunctiveValue_Value
			where Src = @Id and Dst > @CurDstId
			order by Dst
		end
		set @PATRString = @PATRString + '}'
	end
	else if @Class = 2013 begin	-- FsNegatedValue
		select top 1 @Value = Txt
		from FsNegatedValue nv
		join FsSymFeatVal sfv On nv.Value = sfv.Id
		join FsSymFeatVal_Name sfvn On sfvn.Obj = sfv.Id
		where nv.Id = @Id
		if @Value is null begin
			-- Try default value.
			select @FDID=Dst
			from FsFeatDefn_Default
			where Src=@FDID
			exec @retval = PATRString_FsFeatSpec '!', @FDID, @Value output
			if @retval != 0 begin
				set @PATRString = ''
				set @retval = 1
				goto LFail
			end
			set @PATRString = @Value
		end
		else set @PATRString = @Label + ':~' + @Def + @Value
	end
	else if @Class = 2005 begin	-- FsComplexValue
		-- Need to get class of Value, so we call the right SP.
		select @ValueClass = cmo.Class$, @ValueId = cmo.Id
		from FsComplexValue_Value cvv
		join CmObject cmo On cvv.Dst = cmo.Id
		where cvv.Src = @Id
		if @ValueClass is null or @ValueId is null begin
			declare @cmpxFS int, @cmpxValId int
			-- Try default value.
			select @cmpxFS=Dst
			from FsFeatDefn_Default
			where Src=@FDID
			if @cmpxFS is null begin
				set @PATRString = ''
				set @retval = 1
				goto LFail
			end
			select @cmpxValId=Dst
			from FsComplexValue_Value
			where Src=@cmpxFS
			if @cmpxValId is null begin
				set @PATRString = ''
				set @retval = 1
				goto LFail
			end
			exec @retval = PATRString_FsAbstractStructure '!', @cmpxValId, @Value output
			if @retval != 0 begin
				set @PATRString = ''
				set @retval = 1
				goto LFail
			end
			set @PATRString = @Value
		end
		else if @ValueClass = 2009 or @ValueClass = 2010 begin
			-- FsFeatStruc or FsFeatStrucDisj
			exec @retval = PATRString_FsAbstractStructure @Def, @ValueId, @Value output
			if @retval != 0 begin
				set @PATRString = ''
				set @retval = 1
				goto LFail
			end
			set @PATRString = @Label + ':' + @Def + @Value
		end
		else begin	-- Bad class.
			set @PATRString = ''
			set @retval = 1
			goto LFail
		end
		set @PATRString = @Label + ':' + @Value
	end
	else if @Class = 2015 begin	-- FsOpenValue
		-- We don't care which writing system is used.
		select top 1 @Value = Txt
		from FsOpenValue_Value
		where Obj=@Id
		order by Ws
		if @Value is null begin
			set @PATRString = ''
			set @retval = 1
			goto LFail
		end
		set @PATRString = @Label + ':' + @Value
	end
	else if @Class = 2016 begin	-- FsSharedValue
		-- We don't do FsSharedValue at the moment.
		set @PATRString = ''
		set @retval = 1
		goto LFail
	end
	else begin
		-- Unknown class
		set @PATRString = ''
		set @retval = 1
		goto LFail
	end

	set @retval = 0
LFail:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @retval
go

print 'creating proc PATRString_FsFeatStruc'
go
/***********************************************************************************************
 * PATRString_FsFeatStruc
 *
 * Description: Returns an xml string of ids and PC-PATR usable strings,
 *   or a table of IDs and PC-PATR strings, depending on the value of the @XMLOut parameter.
 *   The strings are of the form [per:1], which is usable by PC-PATR in the XAmple parser.
 *   example: <FS Id="119" PATRTxt="[]"/><FS Id="1413" PATRTxt="[Lex:anochecer]"/>
 *
 * Parameters
 *   @XMLOut - 0 to return a table of IDs and strings, 1 to return XML output.
 *   @XMLIds - null to process all FsFeatStrucs, otherwise a list of IDs to process.
 *
 * Return:
 *   0 if successful, otherwise an error status.
***********************************************************************************************/
create proc PATRString_FsFeatStruc
	@XMLOut bit = 0,
	@XMLIds ntext = null
as
	declare @retval int,
		@CurId int, @Txt nvarchar(4000),
		@fIsNocountOn int

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	--Table variable.
	declare @FS table (
		Id int,
		PATRTxt nvarchar(4000) )

	if @XMLIds is null begin
		-- Do all feature structures.
		insert into @FS (Id, PATRTxt)
			select	Id, '[]'
			from	FsFeatStruc_
			where OwnFlid$ != 2005001 -- Owned by FsComplexValue
				and OwnFlid$ != 2010001 -- Owned by FsFeatStrucDisj
	end
	else begin
		-- Do feature structures provided in xml string.
		declare @hdoc int
		exec sp_xml_preparedocument @hdoc output, @XMLIds
		if @@error <> 0 begin
			set @retval = 1
			goto LExit
		end
		insert into @FS (Id, PATRTxt)
			select	ol.[Id], '[]'
			from	openxml(@hdoc, '/FeatureStructures/FeatureStructure')
			with ([Id] int) as ol
			-- Remove all pretenders, since they won't 'join'.
			join CmObject cmo
				On ol.Id=cmo.Id
				and cmo.Class$=2009 -- Check for class being FsFeatStruc
				and cmo.OwnFlid$ != 2005001 -- Owned by FsComplexValue
				and cmo.OwnFlid$ != 2010001 -- Owned by FsFeatStrucDisj
		-- Turn loose of the handle
		exec sp_xml_removedocument @hdoc
		if @@error <> 0 begin
			set @retval = 1
			goto LExit
		end
	end

	-- Loop through all ids.
	select top 1 @CurId = Id
	from @FS
	order by Id
	while @@rowcount > 0 begin
		-- Call PATRString_FsAbstractStructure for each ID. It will return the PATR string.
		exec @retval = PATRString_FsAbstractStructure '', @CurId, @Txt output
		-- Note: If @retval is not 0, then we already are set to use '[]'
		-- for the string, so nothing mnore need be done.
		if @retval = 0 begin
			update @FS
			Set PATRTxt = @Txt
			where Id = @CurId
		end
		-- Try for another one.
		select top 1 @CurId = Id
		from @FS
		where Id > @CurId
		order by Id
	end

	if @XMLOut = 0
		select * from @FS
	else
		select * from @FS for xml auto
	set @retval = 0
LExit:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @retval
go
/***********************************************************************************************
 * End of PC-PATR suitable FsFeatStruc SPs.
***********************************************************************************************/
