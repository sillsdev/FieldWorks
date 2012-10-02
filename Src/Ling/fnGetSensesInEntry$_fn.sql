if object_id('dbo.fnGetSensesInEntry$') is not null begin
	print 'removing function fnGetSensesInEntry$'
	drop function dbo.fnGetSensesInEntry$
end
print 'creating function fnGetSensesInEntry$'
go

/*****************************************************************************
 * fnGetSensesInEntry$
 *
 * Description:
 *	Returns the lexical senses associated with the specified entry
 *
 * Parameters:
 *	@nEntryId = the object ID of the entry for which the senses should be
 *		returned. If null, get all senses for all entries.
 *
 * Returns:
 *	0 if successful, otherwise the associated error code
 *
 * Notes:
 *	Created from GetSensensInEntry$
 *****************************************************************************/

CREATE FUNCTION dbo.fnGetSensesInEntry$ (
	@nEntryId INT )
RETURNS @tblLexSenses TABLE (
	EntryId INT,
	OwnerId	INT,
	SenseId	INT,
	Ord	INT,
	depth INT,
	SenseNum NVARCHAR(1000),
	SenseNumDummy NVARCHAR(1000) )
AS
BEGIN
	DECLARE
		@nCurDepth INT,
		@nRowCount INT,
		@vcStr VARCHAR(100),
		@SenseId INT

	SET @nCurDepth = 0

	--== Get senses for all entries ==--

	IF @nEntryId IS NULL BEGIN
		-- insert lexical sense at the highest depth - sense related directly to the specified entry
		insert into @tblLexSenses
		select 	le.[Id], les.Src, les.Dst, les.ord, @nCurDepth,
			replicate('', 5-len(convert(nvarchar(10), les.ord)))+convert(nvarchar(10), les.ord),
			replicate('  ', 5-len(convert(nvarchar(10), les.ord)))+convert(nvarchar(10), les.ord)
		from LexEntry_Senses les
		JOIN LexEntry le ON le.[Id] = les.[Src]

		-- loop through the reference sequence hierarchy getting each of the senses at every depth
		set @nRowCount = @@rowcount
		while @nRowCount > 0
		begin
			set @nCurDepth = @nCurDepth + 1

			insert into @tblLexSenses
			select 	ls.EntryId, ls.SenseId, lst.Dst, lst.ord, @nCurDepth,
				SenseNum+'.'+replicate('', 5-len(convert(nvarchar(10), lst.ord)))+convert(nvarchar(10), lst.ord),
				SenseNumDummy+'.'+replicate('  ', 5-len(convert(nvarchar(10), lst.ord)))+convert(nvarchar(10), lst.ord)
			from	@tblLexSenses ls
			join lexSense_Senses lst on ls.SenseId = lst.Src
			where	depth = @nCurDepth - 1
			--( The original procedure had an order by SenseNumDummy here.

			set @nRowCount = @@rowcount
		end
	END

	--== Get senses for specified entry ==--

	ELSE BEGIN
		-- insert lexical sense at the highest depth - sense related directly to the specified entry
		insert into @tblLexSenses
		select 	@nEntryId, les.Src, les.Dst, les.ord, @nCurDepth,
			replicate('', 5-len(convert(nvarchar(10), les.ord)))+convert(nvarchar(10), les.ord),
			replicate('  ', 5-len(convert(nvarchar(10), les.ord)))+convert(nvarchar(10), les.ord)
		from	LexEntry_Senses les
		where	les.Src = @nEntryId

		-- loop through the reference sequence hierarchy getting each of the senses at every depth
		set @nRowCount = @@rowcount
		while @nRowCount > 0
		begin
			set @nCurDepth = @nCurDepth + 1

			insert into @tblLexSenses
			select 	@nEntryId, ls.SenseId, lst.Dst, lst.ord, @nCurDepth,
				SenseNum+'.'+replicate('', 5-len(convert(nvarchar(10), lst.ord)))+convert(nvarchar(10), lst.ord),
				SenseNumDummy+'.'+replicate('  ', 5-len(convert(nvarchar(10), lst.ord)))+convert(nvarchar(10), lst.ord)
			from	@tblLexSenses ls
			join lexSense_Senses lst on ls.SenseId = lst.Src
			where	depth = @nCurDepth - 1
			--( The original procedure had an order by SenseNumDummy here.

			set @nRowCount = @@rowcount
		end
	END

	RETURN
END
go
