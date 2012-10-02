/*********************************************************************************
 * GetOrderedMultiTxt
 *
 * Returns an ordered list of strings with their encodings. The order follows the
 * encodings in CurAnalysisWss or CurVernWss, then any other encodings, then ***.
 *
 * Parameters:
 *   @id - Comma delimited list of object IDs
 *   @flid - The property in which the string is stored
 *   @anal - 1 to use CurrentAnalaysisEncs (default), 0 to use CurVernWss
 *
 * Notes:
 *     1) A number of different combinations exist. One of two methods can be used
 *   to deal with them. i) Create dynamic SQL each time the proc is called. ii) Hard
 *   code each combination. The latter was picked. Although more code has to be
 *   compiled, the stored proc is called many times, particularly on start up. This
 *   means the compilation will be used many times. No noticeable difference was
 *   seen when this modification was implemented.
 *
 *     2) The fallback *** was typically created by doing a union. (See the query for
 *   MulitTxt$, for example.) This proved problematic with ntext columns.
 *     A union causes a "work table" in tempdb. See dbtabcount in Books On Line
 *   for a discussion of work tables. ntext variable types are not permitted in work
 *   tables. A workaround was attempted by using @@ROWCOUNT to see if any
 *   rows were returned by the query. If not, SELECT '***', 0, 99999 was fired. This
 *   works in Query Analyzer, but does not return a row in Debug, nor apparently to
 *   the interface. The use of the table variable was used as a workaround.
*************************************************************************************/

if object_id('GetOrderedMultiTxt') is not null begin
	print 'removing procedure GetOrderedMultiTxt'
	drop proc GetOrderedMultiTxt
end
go
print 'creating proc GetOrderedMultiTxt'
go

create proc GetOrderedMultiTxt
	@ObjIds NVARCHAR(MAX),
	@flid int,
	@anal tinyint = 1
as

	declare @fIsNocountOn int
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	declare
		@iFieldType int,
		@nvcTable NVARCHAR(60),
		@nvcSql NVARCHAR(4000)

	select @iFieldType = [Type] from Field$ where [Id] = @flid
	EXEC GetMultiTableName @flid, @nvcTable OUTPUT

	--== Analysis WritingSystems ==--

	if @anal = 1
	begin

		-- MultiStr$ --
		if @iFieldType = 14 --( kcptMultiString
			select
				isnull(ms.[txt], '***') txt,
				ms.[ws],
				isnull(lpcae.[ord], 99998) [ord]
			FROM fnGetIdsFromString(@ObjIds) i
			JOIN MultiStr$ ms ON ms.Flid = @Flid AND ms.Obj = i.Id
			left outer join LgWritingSystem le on le.[Id] = ms.[ws]
			left outer join LangProject_AnalysisWss lpae on lpae.[dst] = le.[id]
			left outer join LangProject_CurAnalysisWss lpcae on lpcae.[dst] = lpae.[dst]
			union all
			select '***', 0, 99999
			order by isnull([ord], 99998)

		-- MultiBigStr$ --
		else if @iFieldType = 18 --( kcptMultiBigString
		begin
			--( See note 2 in the header
			declare @tblMultiBigStrAnalysis table (
				--( See the notes under string tables in FwCore.sql about the
				--( COLLATE clause.
				Txt NTEXT COLLATE Latin1_General_BIN,
				[ws] int,
				[ord] int primary key)

			insert into @tblMultiBigStrAnalysis
			select
				isnull(mbs.[txt], '***') txt,
				mbs.[ws],
				isnull(lpcae.[ord], 99998) [ord]
			FROM fnGetIdsFromString(@ObjIds) i
			JOIN MultiBigStr$ mbs ON mbs.Flid = @Flid AND mbs.Obj = i.Id
			left outer join LgWritingSystem le on le.[Id] = mbs.[ws]
			left outer join LangProject_AnalysisWss lpae on lpae.[dst] = le.[id]
			left outer join LangProject_CurAnalysisWss lpcae on lpcae.[dst] = lpae.[dst]
			order by isnull([ord], 99998)

			insert into @tblMultiBigStrAnalysis
			select convert(ntext, '***') [txt], 0 [ws], 99999 [ord]

			select * from @tblMultiBigStrAnalysis order by [ord]
		end

		-- MultiBigTxt$ --
		else if @iFieldType = 20 --( kcptMultiBigUnicode
		begin
			--( See note 2 in the header
			declare @tblMultiBigTxtAnalysis table (
				--( See the notes under string tables in FwCore.sql about the
				--( COLLATE clause.
				Txt NTEXT COLLATE Latin1_General_BIN,
				[ws] int,
				[ord] int primary key)

			insert into @tblMultiBigTxtAnalysis
			select
				isnull(mbt.[txt], '***') txt,
				mbt.[ws],
				isnull(lpcae.[ord], 99998) [ord]
			FROM fnGetIdsFromString(@ObjIds) i
			JOIN MultiBigTxt$ mbt ON mbt.Flid = @Flid AND mbt.Obj = i.Id
			left outer join LgWritingSystem le on le.[Id] = mbt.[ws]
			left outer join LangProject_AnalysisWss lpae on lpae.[dst] = le.[id]
			left outer join LangProject_CurAnalysisWss lpcae on lpcae.[dst] = lpae.[dst]
			order by isnull([ord], 99998)

			insert into @tblMultiBigTxtAnalysis
			select convert(ntext, '***') [txt], 0 [ws], 99999 [ord]

			select * from @tblMultiBigTxtAnalysis order by [ord]
		end

		-- MultiTxt$ --
		else if @iFieldType = 16 BEGIN  --( kcptMultiUnicode
			SET @nvcSql =
				N'select ' + CHAR(13) +
					N'isnull(mt.[txt], ''***'') txt, ' + CHAR(13) +
					N'mt.[ws], ' + CHAR(13) +
					N'isnull(lpcae.[ord], 99998) [ord] ' + CHAR(13) +
				N'FROM fnGetIdsFromString(''' + @ObjIds + N''') i ' + CHAR(13) +
				N'JOIN ' + @nvcTable + ' mt ON mt.Obj = i.Id ' + + CHAR(13) +
				N'left outer join LgWritingSystem le on le.[Id] = mt.[ws] ' + CHAR(13) +
				N'left outer join LangProject_AnalysisWss lpae ' +
					N'on lpae.[dst] = le.[id] ' + CHAR(13) +
				N'left outer join LangProject_CurAnalysisWss lpcae ' +
					N'on lpcae.[dst] = lpae.[dst] ' + CHAR(13) +
				N'union all ' + CHAR(13) +
				N'select ''***'', 0, 99999 ' + CHAR(13) +
				N'order by isnull([ord], 99998) '

			EXECUTE (@nvcSql);
		END

	end

	--== Vernacular WritingSystems ==--

	else if @anal = 0
	begin

		-- MultiStr$ --
		if @iFieldType = 14 --( kcptMultiString
			select
				isnull(ms.[txt], '***') txt,
				ms.[ws],
				isnull(lpcve.[ord], 99998) [ord]
			FROM fnGetIdsFromString(@ObjIds) i
			JOIN MultiStr$ ms ON ms.Flid = @Flid AND ms.Obj = i.Id
			left outer join LgWritingSystem le on le.[Id] = ms.[ws]
			left outer join LangProject_VernWss lpve on lpve.[dst] = le.[id]
			left outer join LangProject_CurVernWss lpcve on lpcve.[dst] = lpve.[dst]
			union all
			select '***', 0, 99999
			order by isnull([ord], 99998)

		-- MultiBigStr$ --
		else if @iFieldType = 18 --( kcptMultiBigString
		begin
			--( See note 2 in the header
			declare @tblMultiBigStrVernacular table (
				--( See the notes under string tables in FwCore.sql about the
				--( COLLATE clause.
				Txt NTEXT COLLATE Latin1_General_BIN,
				[ws] int,
				[ord] int primary key)

			insert into @tblMultiBigStrVernacular
			select
				isnull(mbs.[txt], '***') txt,
				mbs.[ws],
				isnull(lpcve.[ord], 99998) [ord]
			FROM fnGetIdsFromString(@ObjIds) i
			JOIN MultiBigStr$ mbs ON mbs.Flid = @Flid AND mbs.Obj = i.Id
			left outer join LgWritingSystem le on le.[Id] = mbs.[ws]
			left outer join LangProject_VernWss lpve on lpve.[dst] = le.[id]
			left outer join LangProject_CurVernWss lpcve on lpcve.[dst] = lpve.[dst]
			order by isnull([ord], 99998)

			insert into @tblMultiBigStrVernacular
			select convert(ntext, '***') [txt], 0 [ws], 99999 [ord]

			select * from @tblMultiBigStrVernacular order by [ord]
		end

		-- MultiBigTxt$ --
		else if @iFieldType = 20 --( kcptMultiBigUnicode
		begin
			--( See note 2 in the header
			declare @tblMultiBigTxtVernacular table (
				--( See the notes under string tables in FwCore.sql about the
				--( COLLATE clause.
				Txt NTEXT COLLATE Latin1_General_BIN,
				[ws] int,
				[ord] int primary key)

			insert into @tblMultiBigTxtVernacular
			select
				isnull(mbt.[txt], '***') txt,
				mbt.[ws],
				isnull(lpcve.[ord], 99998) [ord]
			FROM fnGetIdsFromString(@ObjIds) i
			JOIN MultiBigTxt$ mbt ON mbt.Flid = @Flid AND mbt.Obj = i.Id
			left outer join LgWritingSystem le on le.[Id] = mbt.[ws]
			left outer join LangProject_VernWss lpve on lpve.[dst] = le.[id]
			left outer join LangProject_CurVernWss lpcve on lpcve.[dst] = lpve.[dst]
			order by isnull([ord], 99998)

			insert into @tblMultiBigTxtVernacular
			select convert(ntext, '***') [txt], 0 [ws], 99999 [ord]

			select * from @tblMultiBigTxtVernacular order by [ord]
		end

		-- MultiTxt$ --
		else if @iFieldType = 16 BEGIN --( kcptMultiUnicode
			SET @nvcSql =
				N' select ' + CHAR(13) +
					N'isnull(mt.[txt], ''***'') txt, ' + CHAR(13) +
					N'mt.[ws], ' + CHAR(13) +
					N'isnull(lpcve.[ord], 99998) ord ' + CHAR(13) +
				N'FROM fnGetIdsFromString(''' + @ObjIds + N''') i ' + CHAR(13) +
				N'JOIN ' + @nvcTable + ' mt ON mt.Obj = i.Id ' + + CHAR(13) +
				N'left outer join LgWritingSystem le on le.[Id] = mt.[ws] ' + CHAR(13) +
				N'left outer join LangProject_VernWss lpve ' +
					N'on lpve.[dst] = le.[id] ' + CHAR(13) +
				N'left outer join LangProject_CurVernWss lpcve ' +
					N'on lpcve.[dst] = lpve.[dst] ' + CHAR(13) +
				N'union all ' + CHAR(13) +
				N'select ''***'', 0, 99999 ' + CHAR(13) +
				N'order by isnull([ord], 99998) '

			EXECUTE (@nvcSql);
		END
	end
	else
		raiserror('@anal flag not set correctly', 16, 1)
		goto LFail

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

LFail:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	go
