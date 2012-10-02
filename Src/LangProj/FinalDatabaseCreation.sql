/***********************************************************************************************
	Final model creation processes for language projects.

	Note: These declarations need to be ordered, such that if stored procedure X
	calls stored procedure Y, then X should be created first, then Y.
	Doing this avoids an error message about dendencies.
***********************************************************************************************/

print '****************************** Loading LangProjSP.sql ******************************'
go

--
-- post model creation performance enhancements
--

-- once the model is built very few modifications are made to the following
--	tables, so use a 95% fillfactor
dbcc dbreindex ('Field$', '', 95)

-- once the model is built no modifications are made to the following tables,
--	so use a 100% fillfactor
dbcc dbreindex ('Class$', '', 100)
dbcc dbreindex ('ClassPar$', '', 100)
go

--
-- create MakeObj_ procedures for all non-abstract classes
--
begin
	print 'creating MakeObj_ procedures...'

	declare @sClassName sysname, @clid int

	declare class_cur cursor local fast_forward for
	select	[Name], [Id]
	from	[Class$]
	where	[Abstract] = 0

	-- loop through each non-abstract class and build an ObjectCreate_ procedure
	open class_cur
	fetch class_cur into @sClassName, @clid
	while @@fetch_status = 0 begin
		exec GenMakeObjProc @clid
		fetch class_cur into @sClassName, @clid
	end

	close class_cur
	deallocate class_cur
end
go

--( Creating delete triggers

BEGIN
	PRINT 'Creating delete triggers...'
	DECLARE	@nClassId INT
	--( First class
	SELECT TOP 1 @nClassId = Id FROM Class$ ORDER BY Id
	WHILE @@ROWCOUNT != 0 BEGIN
		EXEC CreateDeleteObj @nClassId
		--( Next class
		SELECT TOP 1 @nClassId = Id FROM Class$ WHERE Id > @nClassId ORDER BY Id
	END
END
GO

--
-- create class views that contain all base classes including CmObject
--
begin
	print 'creating class views...'

	declare @depth int, @clid int, @sClass sysname

	create table #tblInheritStack (depth int, clid int)
	create clustered index ind_#tblInheritStack on #tblInheritStack (depth)

	-- start with CmObject
	insert into #tblInheritStack (Depth, clid)
	select	0, [Id]
	from	[Class$]
	where	name = 'CmObject'

	-- loop through the inheritance hierarchy and build the inheritance stack
	set @depth = 0
	while 1 = 1 begin
		insert into #tblInheritStack (Depth, clid)
		select	@depth+1, cp.[Src]
		from	[ClassPar$] cp join #tblInheritStack inhstack on cp.[Dst] = inhstack.[clid]
		where	cp.[Depth] = 1
			and inhstack.[Depth] = @depth
		if @@rowcount = 0 break

		set @depth = @depth + 1
	end

	declare classview_cur cursor local fast_forward for
	select	[clid]
	from	#tblInheritStack
	where	depth > 0
	order by [Depth]

	open classview_cur
	fetch classview_cur into @clid
	while @@fetch_status = 0 begin
		select	@sClass = [Name]
		from	Class$
		where	[Id] = @clid
		exec UpdateClassView$ @clid, 0
		fetch classview_cur into @clid
	end
	close classview_cur
	deallocate classview_cur

	drop table #tblInheritStack
end
go

--==( Create fnGetRefsToObj )==--

EXEC CreateGetRefsToObj
GO