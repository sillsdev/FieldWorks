/***********************************************************************************************
 * Procedure: CountUpToDateParas
 *
 * Description:
 *	Count the number of paragraphs in the specified StText which have an up-to-date
 *	Process Time annotation. If this is not equal to the number of paragraphs,
 *	the text needs to be reparsed.
 *
 * Parameters:
 *	@atid int=id of the attribute defin for process type (app typically has it cached)
 * 	@stid int=id of the StText whose paragraphs are to be marked.
 * Returns:
 *	A result set containing one integer, the count.
 **********************************************************************************************/
if object_id('CountUpToDateParas') is not null begin
	print 'removing proc CountUpToDateParas'
	drop proc [CountUpToDateParas]
end
go
print 'creating proc CountUpToDateParas'
go

create proc CountUpToDateParas
	@atid int, @stid int
as
select count(tp.id) from StTxtPara_ tp
	join CmBaseAnnotation_ cb on cb.BeginObject = tp.id and cb.AnnotationType = @atid
		and cast(cast(tp.UpdStmp as bigint) as NVARCHAR(20)) = cast(cb.CompDetails as NVARCHAR(20))
	where tp.owner$ = @stid
	group by tp.owner$
go
