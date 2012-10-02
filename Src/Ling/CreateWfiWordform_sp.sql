if object_id('CreateWfiWordform') is not null begin
	print 'removing proc CreateWfiWordform'
	drop proc [CreateWfiWordform]
end
go
----------------------------------------------------------------
-- This stored procedure provides an efficient way to create WfiWordforms.
-- It does no checking!
----------------------------------------------------------------

CREATE  proc [CreateWfiWordform]	@Owner int,
	@Ws int,
	@Form NVARCHAR(4000)
as
	declare @fIsNocountOn int, @Err int
	declare @ObjId int, @guid uniqueidentifier

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	set @guid = newid()
	-- leave ownord$ null for owning collection.
	-- 5063001 is WordformInventory_Wordforms
	-- 5062 is the class of WfiWordform.
	insert into [CmObject] with (rowlock) ([Guid$], [Class$], [Owner$], [OwnFlid$])
		values (@guid, 5062, @Owner, 5063001)
	set @Err = @@error
	set @ObjId = @@identity
	if @Err <> 0 begin
		raiserror('SQL Error %d: Unable to create the new object', 16, 1, @Err)
		goto LCleanUp
	end
	insert into [WfiWordform] ([Id]) 		values (@ObjId)	set @Err = @@error	if @Err <> 0 goto LCleanUp	insert into [WfiWordform_Form] (Obj,Ws,Txt) 		values (@ObjId, @Ws, @Form)	set @Err = @@error
LCleanUp:

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	select @ObjId

	return @Err

GO
SET QUOTED_IDENTIFIER OFF
GO
SET ANSI_NULLS ON
GO
