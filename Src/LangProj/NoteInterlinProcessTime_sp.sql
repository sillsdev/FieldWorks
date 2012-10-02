/***********************************************************************************************
 * Procedure: NoteInterlinProcessTime
 * Note: this needs to be in LangProjSP.sql since it depends on language project
 *
 * Description:
 *	For each paragraph in a specified StText, ensures that there is a CmBaseAnnotation
 *	of type Process Time (actually the type is passed as an argument) whose BeginObject
 *	is the paragraph, and sets its CompDetails to a representation of the UpdStmp of the
 * 	paragraph.
 *
 * Parameters:
 *	@atid int=id of the attribute defin for process type (app typically has it cached)
 * 	@stid int=id of the StText whose paragraphs are to be marked.
 *
 * Returns:
 *  the object ids of any created CmBaseAnnotation objects
 **********************************************************************************************/
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
			exec CreateOwnedObject$ 37, @cbaId out, @NewObjGuid out, @lpid, kflidLangProject_Annotations, kcptOwningCollection
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
