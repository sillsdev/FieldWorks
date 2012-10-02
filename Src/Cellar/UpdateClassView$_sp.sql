/***********************************************************************************************
 * UpdateClassView$
 *
 * Description:
 *	Recreates the view associated with the given class.
 *
 * Parameters:
 *	@clid=the class id number of the class to which a custom field has been added (or from
 *		which a custom field has been removed);
 *	@fRebuildSubClassViews=a flag that determines whether or not sub class views are recreated
 *
 * Returns:
 *	0 if successful, 1 if an error occurs.
 *
 * Notes:
 *	By taking the field information from the Field$ table directly, this procedure can be
 *	used either when a custom field is added or when one is removed. Fields that have the
 *	same name are not allowed in views, and are avoided here.
 **********************************************************************************************/

if object_id('UpdateClassView$') is not null begin
	print 'removing proc UpdateClassView$'
	drop proc [UpdateClassView$]
end
go
print 'creating proc UpdateClassView$'
go
create proc [UpdateClassView$]
	@clid int,
	@fRebuildSubClassViews tinyint=0
as
	declare @sTable sysname, @sBaseTable sysname, @sSubClass sysname
	declare @sField sysname, @nType int
	declare @sDynSql nvarchar(4000), @sViewtext nvarchar(4000)
	declare @Err int, @fIsNocountOn int, @nTrnCnt int
	DECLARE @SubClassId INT

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- validate the parameter
	select	@sTable = c.[Name], @sBaseTable = base.[Name]
	from	[Class$] c join [Class$] base on c.[Base] = base.[Id]
	where	c.[Id] = @clid
	if @sTable is null begin
		raiserror('Invalid class id %d', 16, 1, @clid)
		return 50001
	end
	if @sBaseTable is null begin
		raiserror('The Class$ table has been corrupted', 16, 1)
		return 50002
	end

	-- If this procedure was called within a transaction, then create a savepoint; otherwise
	--	create a transaction.
	set @nTrnCnt = @@trancount
	if @nTrnCnt = 0 begin transaction UpdateClassView$_Tran
	else save transaction UpdateClassView$_Tran
	set @Err = @@error
	if @Err <> 0 goto LFail

	-- drop the existing view
	set @sDynSql = N'if object_id(''' + @sTable + N'_'') is not null drop view [' + @sTable + N'_]'
	exec (@sDynSql)
	set @Err = @@error
	if @Err <> 0 goto LFail

	set @sDynsql = N'create view [' + @sTable + '_]' + char(13) +
		N' as ' + char(13) + N'select [' + @sBaseTable + N'_].* '

	declare fld_cur cursor local static forward_only read_only for
	select	[Name], [Type]
	from	[Field$]
	where	[Class] = @clid
		and ([Type] <= 9 or [Type] in (13,15,17,19,24) )
	order by [Id]

	open fld_cur
	fetch fld_cur into @sField, @nType
	while @@fetch_status = 0 begin
		set @sDynSql = @sDynSql + N',[' + @sTable + N'].[' + @sField + N']'

		-- check for strings, which have an additional format column
		if @nType = 13 or @nType = 17
			set @sDynSql = @sDynSql + N',[' + @sTable + N'].[' + @sField + N'_Fmt]'

		fetch fld_cur into @sField, @nType
	end
	close fld_cur
	deallocate fld_cur

	set @sDynSql = @sDynSql + char(13) + N'from [' + @sBaseTable + N'_] join [' +
		@sTable + N'] on [' + @sBaseTable + N'_].[Id] = [' + @sTable + N'].[Id]'
	exec (@sDynSql)
	set @Err = @@error
	if @Err <> 0 goto LFail

	if @fRebuildSubClassViews = 1 begin

		-- Refresh all the views that depend on this class. This is necessary because views
		--	based on the * operator are not automatically updated when tables are altered
		declare SubClass_cur cursor local static forward_only read_only for
		select	cp.Src, c.[Name]
		from	[ClassPar$] cp join [Class$] c on cp.[Src] = c.[Id]
		where	[Dst] = @clid
			and [Depth] > 0
		order by [Depth] desc

		open SubClass_cur
		fetch SubClass_cur into @SubClassId, @sSubClass
		while @@fetch_status = 0 begin
			SET @sViewText = N'CREATE VIEW [' + @sSubclass + '_]' + CHAR(13) +
				N' AS ' + CHAR(13) + N'SELECT [' + @sTable + N'_].*'

			DECLARE SubClassFields CURSOR LOCAL STATIC FORWARD_ONLY READ_ONLY FOR
				SELECT Name, Type
				FROM Field$
				WHERE Class = @SubClassId AND (Type <= 9 or Type IN (13,15,17,19,24))
				ORDER BY Id;

			OPEN SubClassFields;
			FETCH SubClassFields INTO @sField, @nType;
			WHILE @@FETCH_STATUS = 0 BEGIN
				SET @sViewText = @sViewText + N', [' + @sSubclass + N'].[' + @sField + N']';
				IF @nType = 13 or @nType = 17
					SET @sViewText = @sViewText + N', [' + @sSubclass + N'].[' + @sField + N'_Fmt]';
				FETCH SubClassFields INTO @sField, @nType;
			END
			CLOSE SubClassFields;
			DEALLOCATE SubClassFields;

			SET @sViewText = @sViewText + CHAR(13) + N' FROM [' + @sTable + N'_] JOIN [' +
					@sSubClass + N'] ON [' + @sSubClass + N'].[Id] = [' + @sTable + N'_].[Id]'

			-- remove the current view
			set @sDynSql = N'drop view [' + @sSubClass + '_]'
			exec ( @sDynSql )
			set @Err = @@error
			if @Err <> 0 goto LFail

			-- rebuild the view
			exec ( @sViewtext )
			set @Err = @@error
			if @Err <> 0 goto LFail

			fetch SubClass_cur into @SubClassId, @sSubClass
		end
		close SubClass_cur
		deallocate SubClass_cur
	end

	if @nTrnCnt = 0 commit tran UpdateClassView$_Tran

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	return 0

LFail:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	rollback tran UpdateClassView$_Tran
	return @Err
go
