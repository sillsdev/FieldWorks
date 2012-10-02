-- update database from version 200022 to 200023
BEGIN TRANSACTION  --( will be rolled back if wrong version#

-------------------------------------------------------------
-- Fix Process Time annotations that were missing their owner
-------------------------------------------------------------

if object_id('NoteInterlinProcessTime') is not null begin
	print 'removing proc NoteInterlinProcessTime'
	drop proc [NoteInterlinProcessTime]
end
go
print 'creating proc NoteInterlinProcessTime'
go

create proc NoteInterlinProcessTime
	@atid int, @stid int
as

declare @lpid int
select top 1 @lpid = id from LanguageProject

declare MakeAnnCursor cursor local static forward_only read_only for
select tp.id from StTxtPara_ tp (readuncommitted)
left outer join CmBaseAnnotation_ cb (readuncommitted) on cb.BeginObject = tp.id and cb.AnnotationType = @atid
where tp.owner$ = @stid and cb.id is null

declare @tpid int,
	@NewObjGuid uniqueidentifier,
	@cbaId int
open MakeAnnCursor
	fetch MakeAnnCursor into @tpid
	while @@fetch_status = 0 begin
		exec CreateOwnedObject$ 37, @cbaId out, @NewObjGuid out, @lpid, 6001044, 25
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
go

declare @lpid int, @ptim int
select top 1 @lpid = id from LanguageProject
select @ptim = id from CmObject where guid$ = '20CF6C1C-9389-4380-91F5-DFA057003D51' -- Process Time
update CmObject set owner$ = @lpid, ownflid$ = 6001044
from CmObject o (readuncommitted)
join CmAnnotation ca (readuncommitted) on ca.id = o.id
where ca.AnnotationType = @ptim

-------------------------------------------------------------------------------

declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200022
begin
	update Version$ set DbVer = 200023
	COMMIT TRANSACTION
	print 'database updated to version 200023'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200022 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO