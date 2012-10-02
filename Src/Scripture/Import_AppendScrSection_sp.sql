if exists (select *
			 from sysobjects
			where name = 'Import_AppendScrSection')
	drop proc Import_AppendScrSection
go
print 'creating proc Import_AppendScrSection'
go
/*****************************************************************************
 * Import_AppendScrSection
 *
 * Description: Adds a Scripture ection to the end of the given ScrBook
 *
 * Parameters:
 *	hvoScrBook	Id of owning ScrBook
 *	ord		position for new section - must be max(OwningOrd$ + 1)
 *	hvoSection	Id of new ScrSection - output (optional)
 *	hvoSectHeading	Id of new StText to hold the section heading (optional)
 *	hvoSectContent	Id of new StText to hold the section contents (optional)
 * Returns: Error code if an error occurs
 *
 *****************************************************************************/
create proc Import_AppendScrSection
	@hvoScrBook int,			-- Id of owning ScrBook
	@ord int,				-- position for new section
	@hvoSection int = null output,		-- Id of new ScrSection
	@hvoSectHeading int = null output,	-- Id of new StText for section heading
	@hvoSectContent int = null output	-- Id of new StText for section contents
as
	declare @clid int, @flid int, @guid uniqueidentifier,
		@err int, @nTrnCnt int, @sTranName varchar(50),
		@fIsNocountOn int

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- Determine if a transaction already exists; if one does then create a savepoint, otherwise create a
	-- transaction
	set @nTrnCnt = @@trancount
	set @sTranName = 'AppendScrSection_tr' + convert(varchar(8), @@nestlevel)
	if @nTrnCnt = 0 begin tran @sTranName
	else save tran @sTranName

	set @clid = 3005 -- ScrSection
	set @flid = 3002001 -- ScrBook_Sections

	-- Create the new ScrSection object
	set @guid = NewId()
	insert [CmObject] ([Guid$], [Class$], [Owner$], [OwnFlid$], [OwnOrd$])
	values(@guid, @clid, @hvoScrBook, @flid, @ord)
	if @@error <> 0 begin
		set @err = @@error
		goto LFail
	end
	set @hvoSection = @@identity

	insert [ScrSection] ([Id])
	values(@hvoSection)
	if @@error <> 0 begin
		set @err = @@error
		raiserror('Unable to add a row to ScrSection: ID=%d', 16, 1, @hvoSection)
		goto LFail
	end

	-- Create the StTexts
	set @clid = 14 -- StText

	-- Create the new Section Heading object
	set @flid = 3005001 -- ScrSection_Heading
	set @guid = NewId()
	insert [CmObject] ([Guid$], [Class$], [Owner$], [OwnFlid$], [OwnOrd$])
	values(@guid, @clid, @hvoSection, @flid, NULL)
	if @@error <> 0 begin
		set @err = @@error
		goto LFail
	end
	set @hvoSectHeading = @@identity

	-- ENHANCE TomB: Implement Right-to-left
	insert [StText] ([Id], [RightToLeft])
	values(@hvoSectHeading, 0)
	if @@error <> 0 begin
		set @err = @@error
		raiserror('Unable to add a row to StText: ID=%d', 16, 1, @hvoSectHeading)
		goto LFail
	end

	-- Create the new Section Content object
	set @flid = 3005002 -- ScrSection_Content
	set @guid = NewId()
	insert [CmObject] ([Guid$], [Class$], [Owner$], [OwnFlid$], [OwnOrd$])
	values(@guid, @clid, @hvoSection, @flid, NULL)
	if @@error <> 0 begin
		set @err = @@error
		goto LFail
	end
	set @hvoSectContent = @@identity

	-- ENHANCE TomB: Implement Right-to-left
	insert [StText] ([Id], [RightToLeft])
	values(@hvoSectContent, 0)
	if @@error <> 0 begin
		set @err = @@error
		raiserror('Unable to add a row to StText: ID=%d', 16, 1, @hvoSectContent)
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
