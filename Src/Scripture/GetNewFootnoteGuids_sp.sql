if exists (select *
			 from sysobjects
			where name = 'GetNewFootnoteGuids')
	drop proc GetNewFootnoteGuids
go
print 'creating proc GetNewFootnoteGuids'
go
/*****************************************************************************
 * GetNewFootnoteGuids
 *
 * Description: Retrieves a result set that contains an ordered mapping
 * between the GUIDs of footnotes for a ScrBook and the GUIDs of footnotes for
 * a revision of that book.
 *
 * Parameters:
 *	bookId	Id of ScrBook
 *	revId	Id of an archived revision of the ScrBook
 * Returns: 0
 *
 *****************************************************************************/
create proc GetNewFootnoteGuids
	@bookId	int,
	@revId	int
as
	select	bookfn.Guid$ "ScrBookFootnoteGuid",
		revfn.Guid$ "RevisionFootnoteGuid",
		revfn.ownord$
	from	StFootnote_ bookfn
	join	StFootnote_ revfn on bookfn.ownord$ = revfn.ownord$
	where	bookfn.owner$ = @bookId
	and	revfn.owner$ = @revId
	order by revfn.ownord$
GO
