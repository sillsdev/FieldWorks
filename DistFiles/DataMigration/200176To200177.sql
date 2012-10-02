-- update database FROM version 200176 to 200177
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
--FWM-87 Additions to the CheckList model for KeyTerms, etc.
--Added CheckSense : class
--Add ChkSense.Explanation : multiUnicode
--Add ChkSense.Sense : atomic reference of LexSense
--Add ChkItem.Senses : unordered collection
--
--FWM-116
--Fill ScrBook data as appropriate from ScrBookRef data
--Remove ScrBookGroup : class
--Remove ScrBookAnnotations.BookId : atomic reference
--Remove ScrBookRef.BookId : unicode
--Modify CreateNewScrBook stored procedure to fill in the
--     canonical number when a book is created.
--
--FWM-126
--Add Publication.PaperHeight : integer
--Add Publication.PaperWidth : integer
--Add Publication.BindingEdge : integer
--Add Publication.SheetLayout : integer
--Add Publication.SheetsPerSig : integer
--Add PubDivision.NumColumns : integer
--Remove PubPageLayout.MaxPosFootnote : integer
-------------------------------------------------------------------------------

-------------------------------------------------------------------------------
--Changes to CheckList for FWM-87
-------------------------------------------------------------------------------

--Add class ChkSense and its members
	-- Add ChkSense.Explanation : multiUnicode (16)
	-- Add ChkSense.Sense : ReferenceAtomic (24) of LexSense (5016)

	INSERT INTO Class$ ([Id], [Mod], [Base], [Abstract], [Name])
		values(5121, 5, 0, 0, 'ChkSense')
GO
	INSERT INTO [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(5121001, 16, 5121, null, 'Explanation',0,Null, null, null, null)
GO
	INSERT INTO [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(5121002, 24, 5121, 5016, 'Sense',0,Null, null, null, null)
GO

--Add CheckItem.Senses : OwningCollection (25) of ChkSense (5121)

	INSERT INTO [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(5115002, 25, 5115, 5121, 'Senses',0,Null, null, null, null)
GO


-------------------------------------------------------------------------------
--Changes to CheckList for FWM-116
-------------------------------------------------------------------------------
--Fill ScrBook data as appropriate from ScrBookRef data
----------Fill ScrBook.CanonicalNum from ScrBook.BookId and ScrBookRef
UPDATE ScrBook
	SET CanonicalNum = CASE sbr.BookId
		WHEN 'GEN' THEN 1
		WHEN 'EXO' THEN 2
		WHEN 'LEV' THEN 3
		WHEN 'NUM' THEN 4
		WHEN 'DEU' THEN 5
		WHEN 'JOS' THEN 6
		WHEN 'JDG' THEN 7
		WHEN 'RUT' THEN 8
		WHEN '1SA' THEN 9
		WHEN '2SA' THEN 10
		WHEN '1KI' THEN 11
		WHEN '2KI' THEN 12
		WHEN '1CH' THEN 13
		WHEN '2CH' THEN 14
		WHEN 'EZR' THEN 15
		WHEN 'NEH' THEN 16
		WHEN 'EST' THEN 17
		WHEN 'JOB' THEN 18
		WHEN 'PSA' THEN 19
		WHEN 'PRO' THEN 20
		WHEN 'ECC' THEN 21
		WHEN 'SNG' THEN 22
		WHEN 'ISA' THEN 23
		WHEN 'JER' THEN 24
		WHEN 'LAM' THEN 25
		WHEN 'EZK' THEN 26
		WHEN 'DAN' THEN 27
		WHEN 'HOS' THEN 28
		WHEN 'JOL' THEN 29
		WHEN 'AMO' THEN 30
		WHEN 'OBA' THEN 31
		WHEN 'JON' THEN 32
		WHEN 'MIC' THEN 33
		WHEN 'NAM' THEN 34
		WHEN 'HAB' THEN 35
		WHEN 'ZEP' THEN 36
		WHEN 'HAG' THEN 37
		WHEN 'ZEC' THEN 38
		WHEN 'MAL' THEN 39
		WHEN 'MAT' THEN 40
		WHEN 'MRK' THEN 41
		WHEN 'LUK' THEN 42
		WHEN 'JHN' THEN 43
		WHEN 'ACT' THEN 44
		WHEN 'ROM' THEN 45
		WHEN '1CO' THEN 46
		WHEN '2CO' THEN 47
		WHEN 'GAL' THEN 48
		WHEN 'EPH' THEN 49
		WHEN 'PHP' THEN 50
		WHEN 'COL' THEN 51
		WHEN '1TH' THEN 52
		WHEN '2TH' THEN 53
		WHEN '1TI' THEN 54
		WHEN '2TI' THEN 55
		WHEN 'TIT' THEN 56
		WHEN 'PHM' THEN 57
		WHEN 'HEB' THEN 58
		WHEN 'JAS' THEN 59
		WHEN '1PE' THEN 60
		WHEN '2PE' THEN 61
		WHEN '1JN' THEN 62
		WHEN '2JN' THEN 63
		WHEN '3JN' THEN 64
		WHEN 'JUD' THEN 65
		WHEN 'REV' THEN 66
		ELSE 0
		END
	FROM ScrBook AS sb
	JOIN ScrBookRef AS sbr ON sbr.Id = sb.BookId
GO

----------Fill empty fields in ScrBook.Name from ScrBookRef.BookName
INSERT INTO ScrBook_Name (Obj, Ws, Txt)
SELECT sb.id,  sbrn.Ws, sbrn.Txt
FROM ScrBookRef sbr inner join ScrBook sb ON sbr.id = sb.BookId
inner join ScrBookRef_BookName sbrn ON sbr.id = sbrn.obj
left join ScrBook_Name sbn ON sb.id = sbn.Obj and sbrn.Ws = sbn.Ws
WHERE sbn.Obj is null
GO

----------Fill empty fields in ScrBook.Abbrev from ScrBookRef.BookAbbrev
INSERT INTO ScrBook_Abbrev (Obj, Ws, Txt)
SELECT sb.id,  sbra.Ws, sbra.Txt
FROM ScrBookRef sbr inner join ScrBook sb ON sbr.id = sb.BookId
inner join ScrBookRef_BookAbbrev sbra ON sbr.id = sbra.obj
left join ScrBook_Abbrev sba ON sb.id = sba.Obj and sbra.WS = sba.Ws
WHERE sba.Obj is null
GO


--Remove ScrBookGroup : class
DELETE FROM [Field$] WHERE [Class] = 3007
DELETE FROM [ClassPar$] WHERE [Src] = 3007
DELETE FROM [Class$] WHERE [Id] = 3007
GO
DROP VIEW ScrBookGroup_
DROP TABLE ScrBookGroup
GO

--Remove ScrBookAnnotations.BookId : atomic reference (3017001)
DELETE FROM [Field$] WHERE Id = 3017001
GO
EXEC UpdateClassView$ 3017
GO

--Remove ScrBookRef.BookId : unicode (3004002)
DELETE FROM [Field$] WHERE Id = 3004002
GO
EXEC UpdateClassView$ 3004
GO

--<<<<<<<<<
--Modify the CreateNewScrBook stored procedure to use the new version
--    (changed to fill in the canonical number when a book is created.)

if exists (select *
			 from sysobjects
			where name = 'CreateNewScrBook')
	drop proc CreateNewScrBook
GO
print 'creating proc CreateNewScrBook'
GO
/*****************************************************************************
 * CreateNewScrBook
 *
 * Description: Deletes an existing book (if any) and creates a new one having
 * the given "canonical" book number. It also creates a title object for the
 * book.
 * Parameters:
 *	hvoScripture	Id of owning Scripture
 * 	nBookNumber	"canonical" book number (e.g., 1=GEN, 2=EXO, ...)
 *	hvoBook		id of new ScrBook object - output
 *	hvoBookTitle	id of new Title (an StText object) - output
 * Returns: Error code if an error occurs
 *
 *****************************************************************************/
create proc CreateNewScrBook
	@hvoScripture	int,
	@nBookNumber	int,
	@hvoBook	int = null output,
	@hvoBookTitle	int = null output
as
	declare @clid int, @flid int, @guid uniqueidentifier,
		@err int, @nTrnCnt int, @sTranName varchar(50),
		@fIsNocountOn int, @ord int, @hvoScrBookRef int

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- Determine if a transaction already exists; if one does then create a savepoint, otherwise create a
	-- transaction
	set @nTrnCnt = @@trancount
	set @sTranName = 'NewScrBook_tr' + convert(varchar(8), @@nestlevel)
	if @nTrnCnt = 0 begin tran @sTranName
	else save tran @sTranName

	-- Determine the ScrBookRef corresponding to this book and the relative position
	-- of this book in the ScrBook table.
	set @hvoScrBookRef = 0
	select @hvoScrBookRef = [id]
	from ScrBookRef_
	where OwnOrd$ = @nBookNumber
	if @hvoScrBookRef = 0 begin
		set @err = 55678
		raiserror('No matching ScrBookRef: %d', 16, 1, @nBookNumber)
		goto LFail
	end

	set @clid = 3002 -- ScrBook
	set @flid = 3001001 -- Scripture_ScriptureBooks
	set @ord = 0

	-- In the case of redo, the book id could be passed in as a prameter.
	-- If not, then we check to see if there's an existing book in the
	-- ScriptureBooks sequence. If we do find an existing book, @ord will
		-- get set > 0, and we will delete it below.
	if @hvoBook is null begin
		select @ord = OwnOrd$, @hvoBook = [id]
		from ScrBook_
		where BookId = @hvoScrBookRef
		and OwnFlid$ = @flid
		and Owner$ = @hvoScripture
	end
	else if EXISTS(SELECT * FROM ScrBook_ WHERE BookId = @hvoScrBookRef) begin
		set @err = 55679
		raiserror('Redo attempting to insert existing book: ID=%d', 16, 4, @hvoBook)
		goto LFail
	end

	if @ord > 0 begin
		-- Delete the existing book
		exec DeleteObj$ @hvoBook
		if @@error <> 0 begin
			set @err = @@error
			goto LFail
		end
		SET @hvoBook = null
-- REVIEW TomB: Should we be calling DeletOwnSeq here instead in order to preserve other book info?
	end
	else begin
		-- Select the lowest ord for any existing book beyond the one we want
		-- to create.
		select	@ord = coalesce(max(bk.[OwnOrd$])+1, 0)
		from	[ScrBook_] bk with (serializable)
		join	ScrBookRef_ on ScrBookRef_.[id] = bk.BookId
		and	ScrBookRef_.OwnOrd$ < @nBookNumber
		where	bk.[Owner$] = @hvoScripture
			and bk.[OwnFlid$] = @flid

		if exists(select * from [ScrBook_] bk with (serializable)
			where	bk.[Owner$] = @hvoScripture
			and	bk.[OwnFlid$] = @flid
			and	bk.[OwnOrd$] = @ord) begin

			update	[CmObject] with (serializable)
			set 	[OwnOrd$]=[OwnOrd$]+1
			where 	[Owner$] = @hvoScripture
				and [OwnFlid$] = @flid
				and [OwnOrd$] >= @ord
			if @@error <> 0 begin
				set @err = @@error
				goto LFail
			end
		end
	end

	-- Create the new ScrBook (base) object
	set @guid = NewId()
	if @hvoBook is null begin
		insert [CmObject] ([Guid$], [Class$], [Owner$], [OwnFlid$], [OwnOrd$])
		values(@guid, @clid, @hvoScripture, @flid, @ord)
		if @@error <> 0 begin
			set @err = @@error
			goto LFail
		end
		set @hvoBook = @@identity
	end
	else begin
		insert [CmObject] ([id], [Guid$], [Class$], [Owner$], [OwnFlid$], [OwnOrd$])
		values(@hvoBook, @guid, @clid, @hvoScripture, @flid, @ord)
		if @@error <> 0 begin
			set @err = @@error
			goto LFail
		end
	end

	-- Create the new Title (base) object
	set @clid = 14 -- StText
	set @flid = 3002004 -- Scripture_ScriptureBooks
	set @guid = NewId()

	if @hvoBookTitle is null begin
		insert [CmObject] ([Guid$], [Class$], [Owner$], [OwnFlid$], [OwnOrd$])
		values(@guid, @clid, @hvoBook, @flid, NULL)
		if @@error <> 0 begin
			set @err = @@error
			goto LFail
		end
		set @hvoBookTitle = @@identity
	end
	else begin
		insert [CmObject] ([id], [Guid$], [Class$], [Owner$], [OwnFlid$], [OwnOrd$])
		values(@hvoBookTitle, @guid, @clid, @hvoBook, @flid, NULL)
		if @@error <> 0 begin
			set @err = @@error
			goto LFail
		end
	end

	-- Insert into ScrBook
	insert [ScrBook] ([Id], [BookId], [CanonicalNum])
	values(@hvoBook, @hvoScrBookRef, @nBookNumber)
	if @@error <> 0 begin
		set @err = @@error
		raiserror('Unable to add a row to ScrBook: ID=%d', 16, 2, @hvoBook)
		goto LFail
	end

	-- Insert into StText
	-- ENHANCE TomB: Implement Right-to-left
	insert [StText] ([Id], [RightToLeft])
	values(@hvoBookTitle, 0)
	if @@error <> 0 begin
		set @err = @@error
		raiserror('Unable to add a row to StText: ID=%d', 16, 3, @hvoBookTitle)
		goto LFail
	end

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	-- if a transaction was created within this procedure commit it
	if @nTrnCnt = 0 commit tran @sTranName
	return 0

LFail:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	rollback tran @sTranName
	return @err
GO
-->>>>>>>>>>>>>>>>>

-------------------------------------------------------------------------------
--Changes to CheckList for FWM-126
-------------------------------------------------------------------------------

--Add Publication (42) members
	--Add Publication.PaperHeight : integer (10)
	--Add Publication.PaperWidth : integer (11)
	--Add Publication.BindingEdge : integer (12)
	--Add Publication.SheetLayout : integer (13)
	--Add Publication.SheetsPerSig : integer (14)

	INSERT INTO [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(42010, 2, 42, null, 'PaperHeight',0,Null, null, null, null)
GO
	INSERT INTO [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(42011, 2, 42, null, 'PaperWidth',0,Null, null, null, null)
GO
	INSERT INTO [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(42012, 2, 42, null, 'BindingEdge',0,Null, null, null, null)
GO
	INSERT INTO [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(42013, 2, 42, null, 'SheetLayout',0,Null, null, null, null)
GO
	INSERT INTO [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(42014, 2, 42, null, 'SheetsPerSig',0,Null, null, null, null)
GO
	EXEC UpdateClassView$ 42, 1
GO


--Add PubDivision.NumColumns

	INSERT INTO [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(43006, 2, 43, null, 'NumColumns',0,Null, null, null, null)
GO
	EXEC UpdateClassView$ 43, 1
GO



--Remove PubPageLayout.MaxPosFootnote (44009)

DELETE FROM Field$ WHERE Id = 44009
GO
EXEC UpdateClassView$ 44
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
DECLARE @dbVersion int
SELECT @dbVersion = [DbVer] FROM [Version$]
IF @dbVersion = 200176
BEGIN
	UPDATE [Version$] SET [DbVer] = 200177
	COMMIT TRANSACTION
	PRINT 'database updated to version 200177'
END

ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200176 (DbVer = ' +
		convert(varchar, @dbVersion) + ')'
END
GO
