/***********************************************************************************************
 * SetObjList$
 *
 * Description:
 *	places an array of objects into the ObjListTbl$ table
 *
 * Paramters:
 *	@ObjList=list of objects;
 *	@uid=unique identifier associated with the list of objects;
 *	@Tbl=the table to place the objects onto (1=ObjListTbl$, otherwise ObjInfoTbl$);
 *	@fLittleEndian=a flag that determines if the varbinary contains integers stored in
 *		little or big endian (1=little endian, otherwise big endian)
 *
 * Returns:
 *	Returns: 0 if successful, otherwise an error code
 *
 * Notes:
 *	The @ObjList parameter allows for a maximum of 1975 object Id's (each one is 4 bytes)
 **********************************************************************************************/
if object_id('SetObjList$') is not null begin
	print 'removing proc SetObjList$'
	drop proc [SetObjList$]
end
go
print 'creating proc SetObjList$'
go
create proc [SetObjList$]
	@ObjList varbinary(7900),
	@uid uniqueidentifier = null output, -- if null then a guid is generated, otherwise use the supplied value
	@Tbl tinyint = 1,
	@fLittleEndian tinyint = 1
as
	declare @nNumObjs int, @iCurrObj int
	declare	@fIsNocountOn int, @iTemp int
	declare @Err int

	-- check for the arbitrary case where the array of objects is null
	if @ObjList is null return 0

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	if @uid is null set @uid = newid()

	-- get the number of objects in the destination object array
	set @nNumObjs = datalength(@ObjList) / 4

	set @iCurrObj = 0
	if @Tbl = 1 begin
		-- loop through the array of objects and insert each one into the ObjListTbl$ table
		if @fLittleEndian = 1 begin
			while @iCurrObj < @nNumObjs begin
				set @iTemp = @iCurrObj * 4
				insert into [ObjListTbl$] with (rowlock) ([uid], [ObjId], [Ord])
				values(@uid, substring(@ObjList, @iTemp + 4, 1) +
					substring(@ObjList, @iTemp + 3, 1) +
					substring(@ObjList, @iTemp + 2, 1) +
					substring(@ObjList, @iTemp + 1, 1), @iCurrObj)
				set @Err = @@error
				if @Err <> 0 goto LFail
				set @iCurrObj = @iCurrObj + 1
			end
		end
		else begin
			while @iCurrObj < @nNumObjs begin
				insert into [ObjListTbl$] with (rowlock) ([uid], [ObjId], [Ord])
					values(@uid, substring(@ObjList, @iCurrObj*4+1, 4), @iCurrObj)
				set @Err = @@error
				if @Err <> 0 goto LFail
				set @iCurrObj = @iCurrObj + 1
			end
		end
	end
	else begin
		-- loop through the array of objects and insert each one into the ObjInfoTbl$ table
		if @fLittleEndian = 1 begin
			while @iCurrObj < @nNumObjs begin
				set @iTemp = @iCurrObj * 4
				insert into [ObjInfoTbl$] with (rowlock) ([uid], [ObjId], [OrdKey]) values(@uid,
					substring(@ObjList, @iTemp + 4, 1) +
					substring(@ObjList, @iTemp + 3, 1) +
					substring(@ObjList, @iTemp + 2, 1) +
					substring(@ObjList, @iTemp + 1, 1), @iCurrObj)
				set @Err = @@error
				if @Err <> 0 goto LFail
				set @iCurrObj = @iCurrObj + 1
			end
		end
		else begin
			while @iCurrObj < @nNumObjs begin
				insert into [ObjInfoTbl$] with (rowlock) ([uid], [ObjId], [OrdKey])
					values(@uid, substring(@ObjList, @iCurrObj*4+1, 4), @iCurrObj)
				set @Err = @@error
				if @Err <> 0 goto LFail
				set @iCurrObj = @iCurrObj + 1
			end
		end
	end

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	return 0

LFail:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	raiserror ('SetObjList$: SQL Error %d; Unable to insert rows into the ObjListTbl$.', 16, 1, @Err)
	return @Err
go