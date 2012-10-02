/***********************************************************************************************
 * GetPossibilities
 *
 * Description:
 *	retrieves the possibilities and their abbreviations of a specified possibility list
 *
 * Parameters:
 *	@ObjId=the object Id of the possibility list;
 *	@Ws=the writing system
 *
 * Returns:
 *	0 if successful, otherwise an error code (not currently being used)
 **********************************************************************************************/

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
	declare
		@uid uniqueidentifier,
		@retval int,
		@StrId NVARCHAR(20);

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

	SET @StrId = CAST(@ObjId AS NVARCHAR(20));

	insert into @tblObjInfo
		select * from fnGetOwnedObjects$(@StrId, 176160768, 0, 0, 1, 7, 0)

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
