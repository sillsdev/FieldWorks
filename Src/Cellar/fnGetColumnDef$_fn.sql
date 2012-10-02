/***********************************************************************************************
 * Function: fnGetColumnDef$
 *
 * Description:
 *	Returns a SQL Server column datatype definition based on the FieldWorks datatype
 *
 * Parameters:
 *	@sFieldType=the FieldWorks datatype value
 *
 * Returns:
 *	nvarchar(1000) string containing the SQL Server column definition
 **********************************************************************************************/
if object_id('[fnGetColumnDef$]') is not null begin
	print 'removing function fnGetColumnDef$'
	drop function [fnGetColumnDef$]
end
go
print 'creating function fnGetColumnDef$'
go
create function [fnGetColumnDef$] (@nFieldType int)
returns nvarchar(1000)
as
begin
	return case @nFieldType
			when 1 then N'bit = 0'					-- Boolean
			when 2 then N'int = 0'					-- Integer
			when 3 then N'decimal(28,4) = 0'		-- Numeric
			when 4 then N'float = 0.0'				-- Float
			when 5 then N'datetime = null'			-- Time
			when 6 then N'uniqueidentifier = null'	-- Guid
			when 7 then N'image = null'				-- Image
			when 8 then N'int = 0'					-- GenDate
			when 9 then N'varbinary(8000) = null'	-- Binary
			when 13 then N'nvarchar(4000) = null'	-- String
			when 15 then N'nvarchar(4000) = null'	-- Unicode
			when 17 then N'ntext = null'			-- BigString
			when 19 then N'ntext = null'			-- BigUnicode
		end
end
go
