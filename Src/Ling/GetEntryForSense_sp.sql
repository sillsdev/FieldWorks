if object_id('GetEntryForSense') is not null begin
	drop proc GetEntryForSense
end
print 'creating proc GetEntryForSense'
go
/*****************************************************************************
 * GetEntryForSense
 *
 * Description:
 *	Returns the Id of the entry that owns the sense, directly or indirectly.
 * Parameters:
 *	@SenseId=the ID of the sense for which the entry should be returned.
 * Returns:
 *	0 if successful, otherwise 1
 *****************************************************************************/
create proc [GetEntryForSense]
	@SenseId as integer
as
	declare @OwnerId int, @OwnFlid int, @ObjId int
	declare @fIsNocountOn int

	set @OwnerId = 0
	if @SenseId < 1 return 1	-- Bad Id

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	set @OwnFlid = 0
	set @ObjId = @SenseId

	-- Loop until we find an owning flid of 5002011 (or null for some ownership error).
	while @OwnFlid != 5002011
	begin
		select 	@OwnerId=isnull(Owner$, 0), @OwnFlid=OwnFlid$
		from	CmObject
		where	Id=@ObjId

		set @ObjId=@OwnerId
		if @OwnerId = 0
			return 1
	end

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	-- select the sense back to the caller
	select 	@OwnerId LeId
	return 0
go
