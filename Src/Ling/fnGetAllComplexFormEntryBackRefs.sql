/*****************************************************************************
 * fnGetAllComplexFormEntryBackRefs
 *
 * Description:
 *	Getting proper backreferences for complex forms turns out to be rather
 *	difficult.  The original implementation ran out of either time or memory
 *	(or both).  This function may not be absolutely optimal, but can return
 *	almost 10000 rows in about 4 seconds.  Since this is actually used largely
 *	for export, that's probably satisfactory.
 * Parameters:
 *	none
 * Returns:
 *	table mapping LexEntry ids to LexEntryRef ids of LexEntryRef objects that
 *	point to the LexEntry, directly or indirectly through a LexSense owned by
 *	the LexEntry
 *****************************************************************************/

IF OBJECT_ID('fnGetAllComplexFormEntryBackRefs') is not null begin
	PRINT 'removing function fnGetAllComplexFormEntryBackRefs'
	DROP FUNCTION [fnGetAllComplexFormEntryBackRefs]
END
GO
PRINT 'creating function fnGetAllComplexFormEntryBackRefs'
GO

CREATE FUNCTION fnGetAllComplexFormEntryBackRefs()
RETURNS @res TABLE (Entry INT, EntryRef INT)
AS
BEGIN
	INSERT INTO @res
	SELECT DISTINCT le.Id, pl.Src
	FROM LexEntryRef_PrimaryLexemes pl
	JOIN LexEntryRef ler ON ler.Id=pl.Src AND ler.RefType=1
	JOIN LexEntry le ON le.Id=pl.Dst
	JOIN LexEntry_EntryRefs er ON er.Dst=ler.Id
	LEFT OUTER JOIN LexEntry_EntryRefs er2 ON er2.Src=er.Src AND er2.Dst<>er.Dst
	LEFT OUTER JOIN LexEntryRef ler2 ON ler2.Id=er2.Dst AND ler2.RefType=0
	WHERE ler2.Id IS NULL;

	DECLARE @hvoSense INT, @hvoRef INT, @hvoEntry INT
	DECLARE curSenses CURSOR LOCAL STATIC FORWARD_ONLY READ_ONLY FOR
		SELECT DISTINCT ls.Id, pl.Src
		FROM LexEntryRef_PrimaryLexemes pl
		JOIN LexEntryRef ler ON ler.Id=pl.Src AND ler.RefType=1
		JOIN LexSense ls ON ls.Id=pl.Dst
		JOIN LexEntry_EntryRefs er ON er.Dst=ler.Id
		LEFT OUTER JOIN LexEntry_EntryRefs er2 ON er2.Src=er.Src AND er2.Dst<>er.Dst
		LEFT OUTER JOIN LexEntryRef ler2 ON ler2.Id=er2.Dst AND ler2.RefType=0
		WHERE ler2.Id IS NULL
	OPEN curSenses
	FETCH curSenses INTO @hvoSense, @hvoRef
	WHILE @@FETCH_STATUS = 0 BEGIN
		SELECT @hvoEntry=dbo.fnGetEntryForSense(@hvoSense)
		IF (@hvoEntry IS NOT NULL)
			INSERT INTO @res SELECT @hvoEntry, @hvoRef
		FETCH curSenses INTO @hvoSense, @hvoRef
	END
	CLOSE curSenses
	DEALLOCATE curSenses
	RETURN
END
GO
