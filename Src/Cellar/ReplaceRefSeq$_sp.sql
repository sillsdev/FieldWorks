/***********************************************************************************************
 * ReplaceRefSeq$
 *
 * Description:
 *	General procedure that calls the appropriate ReplaceRefSeq_ procedure
 *
 * Parameters:
 *	@flid = the FLID of the field that contains the reference sequence relationship
 *	@SrcObjId = the id of the object that contains the reference sequence relationship
 *	@ListStmp = the timestamp value associated with the sequence list when it was first
 *		read from the database
 *	@hXMLdoc = prepared XML document handle
 *	@StartObj = where the inserted objects should be placed before; if null then the objects
 *		will be appended to the end of the list and no objects will be deleted/replaced
 *	@StartObjOccurrence = the occurrence of the StartObj where the inserted objects should
 *		be placed
 *	@EndObj = the last object in the list of objects that is to be removed; if null
 *		no objects will be deleted/replaced
 *	@EndObjOccurrence = the occurrence of the EndObj that indicates the last object that
 *		is to be removed
 *	@fRemoveXMLDoc = flag that determines if the prepared XML documents should be removed
 *		from memory/cache (default is true)
 *
 * Returns:
 *	0 if successful, otherwise the appropriate error code
 *
 * Example:
 *	declare @hdoc int
 *	-- prepare an XML document and get its handle
 *	exec sp_xml_preparedocument @hdoc output,
 *	'<root><Obj Id="4708" Ord="0"/><Obj Id="4708" Ord="1"/><Obj Id="4708" Ord="2"/>
 *	<Obj Id="4708" Ord="3"/></root>'
 *	-- call the generic ReplaceRefSeq$ procedure
 *	exec ReplaceRefSeq$ 6001019, 1, null, @hdoc, 4710, 1, null, null
 **********************************************************************************************/

if object_id('ReplaceRefSeq$') is not null begin
	print 'removing proc ReplaceRefSeq$'
	drop proc [ReplaceRefSeq$]
end
go
print 'creating proc ReplaceRefSeq$'
go
create proc [ReplaceRefSeq$]
	@flid int,
	@SrcObjId int,
	@ListStmp int,
	@hXMLdoc int,
	@StartObj int = null,
	@StartObjOccurrence int = 1,
	@EndObj int = null,
	@EndObjOccurrence int = 1,
	@fRemoveXMLDoc tinyint = 1
as
	declare @Err int
	declare	@sDynSql varchar(500)

	select	@sDynSql = 'exec ReplRS_' +
			SUBSTRING(c.[Name], 1, 11) + '_' + SUBSTRING(f.[Name], 1, 11) + ' ' +
			coalesce(convert(varchar(11), @SrcObjId), 'null') + ',' +
			coalesce(convert(varchar(11), @ListStmp), 'null') + ',' +
			coalesce(convert(varchar(11), @hXMLdoc), 'null') + ',' +
			coalesce(convert(varchar(11), @StartObj), 'null') + ',' +
			coalesce(convert(varchar(11), @StartObjOccurrence), 'null') + ',' +
			coalesce(convert(varchar(11), @EndObj), 'null') + ',' +
			coalesce(convert(varchar(11), @EndObjOccurrence), 'null') + ',' +
			coalesce(convert(varchar(3), @fRemoveXMLDoc), 'null')
	from	Field$ f join Class$ c on f.[Class] = c.[Id]
	where	f.[Id] = @flid and f.[Type] = 28

	if @@rowcount <> 1 begin
		raiserror('ReplaceRefSeq$: Invalid flid: %d', 16, 1, @flid)
		return 50000
	end

	exec (@sDynSql)
	set @Err = @@error
	if @Err <> 0 begin
		raiserror('ReplaceRefSeq$: SQL Error %d: Unable to perform replace: %d', 16, 1, @Err, @sDynSql)
		return @Err
	end

	return 0
go
