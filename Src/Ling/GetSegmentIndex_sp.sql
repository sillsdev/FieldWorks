/***********************************************************************************************
 * Procedure: GetSegmentIndex
 *
 * Description:
 *	Get the 1-based index of the text segment (typically sentence--a CmBaseAnnotation whose type
 *	is 'text segment') that contains the input twfic, relative to its paragraph.
 *	For example, if the input hvo is a twfic in the second sentence of its paragraph, answer 2.
 *	This is the last number in the reference generated in the concordance view.
 *
 * Parameters:
 *	@hvo int=id of the twfic (CmBaseAnnotation) we want the segment index for.
 * 	@segDefn int=id of the AnnotationDefn for text segment.
 * Returns:
 *	A result set containing one integer, the segment index.
 **********************************************************************************************/
if object_id('GetSegmentIndex') is not null begin
	print 'removing proc GetSegmentIndex'
	drop proc [GetSegmentIndex]
end
go
print 'creating proc GetSegmentIndex'
go

create proc GetSegmentIndex
	@hvo int, @segDefn int

as
begin
	select count(cbaSeg.id) from CmBaseAnnotation_ cbaSeg
	join CmBaseAnnotation cbaWf
		on cbaWf.id = @hvo and cbaWf.BeginObject = cbaSeg.BeginObject
			and cbaSeg.AnnotationType = @segDefn
			and cbaSeg.BeginOffset <= cbaWf.BeginOffset
	end
go
