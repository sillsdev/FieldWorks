/***********************************************************************************************
 * GetMultiTableName
 *
 * Description:
 *	Given a field ID (flid), which is stored in Field$.ID, returns the name of a multi
 *	table. For instance, flid 5062001 is for WfiWordform_Form.Form, and the the multilanguage
 *	data is stored in table WfiWordform_Form.
 *
 * Parameters:
 *	@nFlid = Field ID
 *	@nvcTableName = the name of the table returned.
 *
 * Returns:
 *	name of the table returned in parameter @nvcTableName
 **********************************************************************************************/

if object_id('GetMultiTableName') is not null begin
	print 'removing proc GetMultiTableName'
	drop proc [GetMultiTableName]
end
go
print 'creating proc GetMultiTableName'
go

CREATE PROCEDURE GetMultiTableName
	@nFlid INT = NULL,
	@nvcTableName NVARCHAR(60) OUTPUT
AS
	SELECT @nvcTableName = c.[Name] + '_' + f.[Name]
	FROM Class$ c
	JOIN Field$ f ON f.Class = c.[Id] AND f.[Id] = @nFlid
GO
