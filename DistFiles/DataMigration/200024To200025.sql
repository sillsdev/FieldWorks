-- update database from version 200024 to 200025
BEGIN TRANSACTION  --( will be rolled back if wrong version#

-------------------------------------------------------------------------------
-- NoteInterlinProcessTime needs to return the set of created objects
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
	select top 1 @lpid = id from LanguageProject

	set @nvNew = ''

	declare MakeAnnCursor cursor local static forward_only read_only for
	select tp.id from StTxtPara_ tp (readuncommitted)
	left outer join CmBaseAnnotation_ cb (readuncommitted)
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
	from CmBaseAnnotation_ cba (readuncommitted)
	join StTxtPara_ tp (readuncommitted) on cba.BeginObject = tp.id and tp.owner$ = @stid
	where cba.AnnotationType = @atid

	return @@ERROR
END
GO

-------------------------------------------------------------------------------

declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200024
begin
	update Version$ set DbVer = 200025
	COMMIT TRANSACTION
	print 'database updated to version 200025'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200024 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
