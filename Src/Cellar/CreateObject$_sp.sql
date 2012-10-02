/***********************************************************************************************
 * CreateObject$
 *
 * Description:
 *	Creates an object given its class id.
 *
 * Paramters:
 *	If @id is null, the object id is generated and returned.
 *  If @guid is null, the guid is generated and returned.
 *
 * Returns:
 * 	0 if successful, otherwise an error
 **********************************************************************************************/

if object_id('CreateObject$') is not null begin
	print 'removing proc CreateObject$'
	drop proc [CreateObject$]
end
go
print 'creating proc CreateObject$'
go
create proc [CreateObject$]
	@clid int,
	@id int output,
	@guid uniqueidentifier output
as
	SpBegin()

	declare @ObjId int
	declare @depth int
	declare @sSql nvarchar(kcchMaxSql), @sTbl nvarchar(kcchMaxName), @sId nvarchar(kcchMaxInt)
	declare @fAbs bit

	if @guid is null set @guid = NewId()

	select @fAbs = [Abstract], @sTbl = [Name] from [Class$] where [Id] = @clid
	if @@rowcount <> 1 begin
		RaisError('Bad Class ID: %d', 16, 1, @clid)
		set @err = @@error
		goto LFail
	end

	if @fAbs <> 0 begin
		RaisError('Cannot instantiate abstract class: @s', 16, 1, @sTbl)
		set @err = @@error
		goto LFail
	end

	select @depth = [Depth] from [ClassPar$] where [Src] = @clid and [Dst] = kclidCmObject
	if @@rowcount <> 1 begin
		RaisError('Bad Class Id or corrupt ClassPar$ table: %d', 16, 1, @clid)
		set @err = @@error
		goto LFail
	end

	-- if an Id was supplied assume that the IDENTITY_INSERT setting is turned on and the incoming Id is legal
	if @id is null begin
		insert into [CmObject] ([Guid$], [Class$], [OwnOrd$])
			values(@guid, @clid, null)
		SpCheck(@@error)

		set @id = @@identity
	end
	else begin
		insert into [CmObject] ([Guid$], [Id], [Class$], [OwnOrd$])
			values(@guid, @id, @clid, null)
		SpCheck(@@error)
	end
	set @sId = convert(nvarchar(kcchMaxInt), @id)

	while @depth > 0 begin
		set @depth = @depth - 1

		select @sTbl = c.[Name]
		from [ClassPar$] cp join [Class$] c on c.[Id] = cp.[Dst]
		where cp.[Src] = @clid and cp.[Depth] = @depth

		if @@rowcount <> 1 begin
			RaisError('Corrupt ClassPar$ table: %d', 16, 1, @clid)
			set @err = @@error
			goto LFail
		end

		set @sSql = 'insert into [' + @sTbl + '] with (rowlock) ([Id]) values(' + @sId + ')'
		exec (@sSql)
		SpCheck(@@error)
	end

	SpEnd()
go
