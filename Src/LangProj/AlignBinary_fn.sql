/***********************************************************************************************
 * Function: AlignBinary
 *
 * Description:
 *	Realigns bits - converts from little endian to big endian or vice versa
 *
 * Parameters:
 *	@b=four byte binary string
 *
 * Returns:
 *	realigned four byte binary string
 **********************************************************************************************/
if object_id('AlignBinary') is not null
begin
	drop function AlignBinary
end
go
print 'creating function AlignBinary'
go
create function AlignBinary (@b binary(4))
returns binary(4)
as
begin
	declare @New binary(4)

	set @New = substring(@b, 4, 1) + substring(@b, 3, 1) + substring(@b, 2, 1) + substring(@b, 1, 1)
	return @New
end
go
