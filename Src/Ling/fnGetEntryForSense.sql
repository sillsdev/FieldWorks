/*****************************************************************************
 * fnGetEntryForSense
 *
 * Description:
 *	Returns the Id of the entry that owns the sense, directly or indirectly.
 * Parameters:
 *	@SenseId=the ID of the sense for which the entry should be returned.
 * Returns:
 *	0 if unsuccessful, otherwise the ID of the owning Entry
 *****************************************************************************/
IF OBJECT_ID('fnGetEntryForSense') IS NOT NULL BEGIN
	PRINT 'removing function fnGetEntryForSense'
	DROP FUNCTION fnGetEntryForSense
END
GO
PRINT 'creating function fnGetEntryForSense'
GO

CREATE FUNCTION dbo.fnGetEntryForSense(@hvoSense INT)
RETURNS INT
AS
BEGIN
	declare @hvoEntry int, @OwnFlid int, @ObjId int

	set @hvoEntry = 0
	if @hvoSense < 1 return(@hvoEntry)	-- Bad Id

	set @OwnFlid = 0
	set @ObjId = @hvoSense

	-- Loop until we find an owning flid of 5002011 (or null for some ownership error).
	while @OwnFlid != 5002011
	begin
		select 	@hvoEntry=isnull(Owner$, 0), @OwnFlid=OwnFlid$
		from	CmObject
		where	Id=@ObjId

		set @ObjId=@hvoEntry
		if @hvoEntry = 0
			return(@hvoEntry)
	end

	return(@hvoEntry)
END
GO
