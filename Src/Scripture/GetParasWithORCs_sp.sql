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
	from	StTxtPara_ p
	join	StText_ t on p.Owner$ = t.[Id]
	join	ScrSection_ s on t.Owner$ = s.[Id]
	join	ScrBook b on s.Owner$ = b.[Id]
	and	b.[id] = @revId
	where	p.Contents COLLATE Latin1_General_BIN like N'%' + NCHAR(0xFFFC) + '%' COLLATE Latin1_General_BIN
	union all
	select	p.[Id], p.OwnOrd$, 0, 0, 0
	from	StTxtPara_ p
	join	StText_ t on p.Owner$ = t.[Id]
	join	ScrBook b on t.Owner$ = b.[Id]
	and	t.OwnFlid$ = 3002004
	and	b.[id] = @revId
	where	p.Contents COLLATE Latin1_General_BIN like N'%' + NCHAR(0xFFFC) + '%' COLLATE Latin1_General_BIN

	order by t_or_s, sord, tflid, pord--select PATINDEX('85BD0CE977CE49629850205F8B73C741', CAST(CAST(Contents_fmt AS varbinary(8000)) AS nvarchar(4000)))
end
GO