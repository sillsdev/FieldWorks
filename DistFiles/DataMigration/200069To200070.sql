-- Update database from version 200069 to 200070
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

/*****************************************************************************
 * Modified the CreateNewScrBook stored procedure
 *****************************************************************************/

if exists (select *
			 from sysobjects
			where name = 'CreateNewScrBook')
	drop proc CreateNewScrBook
go
print 'creating proc CreateNewScrBook'
go
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
	insert [ScrBook] ([Id], [BookId])
	values(@hvoBook, @hvoScrBookRef)
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

/*****************************************************************************
 * Close the MoMorphType possibility list
 *****************************************************************************/
update CmPossibilityList set IsClosed = 1 where ItemClsid = 5042
GO



---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200069
begin
	update Version$ set DbVer = 200070
	COMMIT TRANSACTION
	print 'database updated to version 200070'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200069 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
