-- Update database from version 200213 to 200214
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- Finish off FWNX-55: Shorten PATRString_FsFeatureSpecification
-------------------------------------------------------------------------------

if object_id('PATRString_FsFeatureSpecification') is not null begin
	print 'Deleting PATRString_FsFeatureSpecification'
	drop proc PATRString_FsFeatureSpecification
end
go

print 'creating proc PATRString_FsFeatSpec'
go
create proc PATRString_FsFeatSpec
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

---------------------------------------------------------------------------------
-- TE-6605: Import_NewScrBookJRP isn't being used anymore.
---------------------------------------------------------------------------------

if object_id('Import_NewScrBookJRP') is not null begin
	print 'Deleting Import_NewScrBookJRP'
	drop proc Import_NewScrBookJRP
end
go

---------------------------------------------------------------------------------
---- Finish or roll back transaction as applicable
---------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200213
BEGIN
	UPDATE Version$ SET DbVer = 200214
	COMMIT TRANSACTION
	PRINT 'database updated to version 200214'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200213 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
