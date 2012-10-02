/***********************************************************************************************
 * GetTagInfo$
 *
 * Description:
 *   Gets info about overlay tags, using encodings by priority
 *
 * Parameters:
 *   @iOwnerId - The ID of the owner
 *   @iWritingSystem - See notes below
 *
 * Notes:
 *   The @iWritingSystem parameter supports a "magic" value. If kwsAnal, the query will
 *   return the first non-null string, giving priority to the first Analysis Writing system
 *   with the highest Order. Likewise, if @iWritingSystem is kwsVern, the query will
 *   return the first non-null string, giving priority to the first Vernacular Writing system
 *
 * Example:
 *	exec GetTagInfo$ 2573, kwsAnal
 *
***********************************************************************************************/
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

	-- TODO (SteveM) This needs to be fixed to handle kwsAnalVerns and kwsVernAnals properly.
	--( if "magic" writing system is for analysis encodings
	if @iWritingSystem = kwsAnal or @iWritingSystem = kwsAnals or @iWritingSystem = kwsAnalVerns
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
	else if @iWritingSystem = kwsVern or @iWritingSystem = kwsVerns or @iWritingSystem = kwsVernAnals
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
