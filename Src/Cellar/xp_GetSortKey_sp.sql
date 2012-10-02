/***********************************************************************************************
 * xp_GetSortKey
 *
 * Description:
 *	This is a temporary dummy until the extended stored procedure by the same name gets built.
 *	It is built for the purpose of building and testing other units. When completed, the
 *	extended stored procedure will generate a sort key using ICU.
 *
 * Parameters:
 *	@nvcText = text upon which the sort key is generated
 *	@nvcLocale = the locale for which the sort key is generated
 *	@nKey = the sort key produced
 *
 * Returns:
 *	output parameters, no return value
 **********************************************************************************************/

if object_id('xp_GetSortKey') is not null begin
	print 'removing proc xp_GetSortKey'
	drop proc [xp_GetSortKey]
end
go
print 'creating proc xp_GetSortKey'
go
CREATE PROCEDURE [xp_GetSortKey]
	@nvcText NVARCHAR(4000) OUTPUT,
	@nvcLocale NVARCHAR(100) OUTPUT,
	@nKey VARBINARY(900) OUTPUT
AS
	SET @nKey = ASCII(@nvcText) -- will take first character of @nvcText
GO
