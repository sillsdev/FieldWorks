/***********************************************************************************************
 * Function: fnGetLastCommaDelimID$
 *
 * Description:
 *  Certain strings have a comma delimmited list of IDs. In the case of CmSortSpec, some fields
 *  have a series of IDs, the last of which is of interest to us.
 *
 * Notes:
 *	An enhancement may be made to get the nth ID desired, instead of the last. If made, this
 *  function would more closely resemble the INSTR() function of Oracle and FoxPro. Code that
 *  mimics Oracle's can be found at:
 *    http://www.brasileiro.net:8080/postgres/cookbook/view-one-recipe.adp?recipe_id=33
 **********************************************************************************************/

IF OBJECT_ID('fnGetLastCommaDelimID$') IS NOT NULL BEGIN
	PRINT 'removing function fnGetLastCommaDelimID$'
	DROP FUNCTION [fnGetLastCommaDelimID$]
END
GO
PRINT 'creating function fnGetLastCommaDelimID$'
GO

CREATE FUNCTION [fnGetLastCommaDelimID$] (
	@nvcSourceString NVARCHAR(4000))
RETURNS INT
AS
BEGIN
	DECLARE
		@nScratch SMALLINT,
		@nCommaPosition SMALLINT

	IF @nvcSourceString IS NULL
		SET @nCommaPosition = NULL
	ELSE BEGIN

		--( In case there are no commas
		SET @nScratch = CHARINDEX(',', @nvcSourceString)
		SET @nCommaPosition = @nScratch

		--( Find the last comma
		WHILE @nScratch > 0 BEGIN
			SET @nCommaPosition = @nScratch
			SET @nScratch = CHARINDEX(',', @nvcSourceString, @nCommaPosition + 1)
		END
	END

	--( Return the ID after the last comma.
	RETURN CONVERT(INT, SUBSTRING(
		@nvcSourceString,
		@nCommaPosition + 1,
		LEN(@nvcSourceString) - @nCommaPosition))
END

GO
