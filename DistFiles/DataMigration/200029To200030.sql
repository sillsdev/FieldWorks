-- update database from version 200029 to 200030
BEGIN TRANSACTION  --( will be rolled back if wrong version#

-------------------------------------------------------------------------------
-- Remove ScrBook.RunningHeader
-------------------------------------------------------------------------------

DELETE FROM Field$ WHERE [Id] = 3002009 --( RunningHeader
GO

-------------------------------------------------------------------------------
-- Remove ScrSection.Footnotes
-------------------------------------------------------------------------------

DELETE FROM Field$ WHERE [Id] = 3005005 --( ScrSection.Footnotes
GO

-------------------------------------------------------------------------------
-- Add new attributes to Scripture
-------------------------------------------------------------------------------

INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
VALUES (3001019, 2, 3001, NULL, 'Versification', 0, NULL, NULL)
GO

INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
VALUES (3001020, 15, 3001, NULL, 'VersePunct', 0, NULL, NULL)
GO

INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
VALUES (3001021, 6, 3001, NULL, 'StylesheetVersion', 0, NULL, NULL)
GO

exec UpdateClassView$ 3001, 1
GO

-------------------------------------------------------------------------------
-- Add date fields to CmAnnotation
-------------------------------------------------------------------------------

INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
VALUES (34009, 5, 34, NULL, 'DateCreated', 0, NULL, NULL)
GO

INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
VALUES (34010, 5, 34, NULL, 'LastModified', 0, NULL, NULL)
GO

exec UpdateClassView$ 34, 1
GO
-------------------------------------------------------------------------------
-- Add date fields to ScrDraft
-------------------------------------------------------------------------------

INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
VALUES (3010003, 5, 3010, NULL, 'DateCreated', 0, NULL, NULL)
GO

exec UpdateClassView$ 3010, 1
GO

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
	from	StFootnote_ bookfn (readuncommitted)
	join	StFootnote_ revfn (readuncommitted) on bookfn.ownord$ = revfn.ownord$
	where	bookfn.owner$ = @bookId
	and	revfn.owner$ = @revId
	order by revfn.ownord$
GO

if exists (select *
			 from sysobjects
			where name = 'GetParasWithORCs')
drop proc GetParasWithORCs
go
print 'creating proc GetParasWithORCs'
go
/*****************************************************************************
 * GetParasWithORCs
 *
 * Description: Retrieves a list of HVO's of StTxtParas which contain ORC
 * characters (most of which are probably footnotes).
 *
 * Parameters:
 *	revId	Id of an archived revision of a ScrBook
 * Returns: 0
 *
 *****************************************************************************/
create proc GetParasWithORCs @revId int
as
begin
	select	p.[Id] "id", p.OwnOrd$ "pord", t.OwnFlid$ "tflid", s.OwnOrd$ "sord", 1 "t_or_s"
	from	StTxtPara_ p (readuncommitted)
	join	StText_ t (readuncommitted) on p.Owner$ = t.[Id]
	join	ScrSection_ s (readuncommitted) on t.Owner$ = s.[Id]
	join	ScrBook b (readuncommitted) on s.Owner$ = b.[Id]
	and	b.[id] = @revId
	where	p.Contents COLLATE Latin1_General_BIN like N'%' + NCHAR(0xFFFC) + '%' COLLATE Latin1_General_BIN
	union all
	select	p.[Id], p.OwnOrd$, 0, 0, 0
	from	StTxtPara_ p (readuncommitted)
	join	StText_ t (readuncommitted) on p.Owner$ = t.[Id]
	join	ScrBook b (readuncommitted) on t.Owner$ = b.[Id]
	and	t.OwnFlid$ = 3002004
	and	b.[id] = @revId
	where	p.Contents COLLATE Latin1_General_BIN like N'%' + NCHAR(0xFFFC) + '%' COLLATE Latin1_General_BIN

	order by t_or_s, sord, tflid, pord--select PATINDEX('85BD0CE977CE49629850205F8B73C741', CAST(CAST(Contents_fmt AS varbinary(8000)) AS nvarchar(4000)))
end
GO
-------------------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200029
begin
	update Version$ set DbVer = 200030
	COMMIT TRANSACTION
	print 'database updated to version 200030'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200029 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
