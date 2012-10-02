/*******************************************************************************
 * Procedure: DeleteFlidCollations
 *
 * Description:
 *	Deletes flid/Collation combinations
 *
 * Parameters--ONLY 1 parameter is expected to be not null:
 *	@nEncID			= Writing system ID
 *	@nCollationID	= Collation ID
 *	@nFlid			= Field ID
 *****************************************************************************/

IF OBJECT_ID('DeleteFlidCollations') IS NOT NULL BEGIN
	PRINT 'removing procedure DeleteFlidCollations'
	DROP PROCEDURE [DeleteFlidCollations]
END
GO
PRINT 'creating procedure DeleteFlidCollations'
GO
CREATE PROCEDURE [DeleteFlidCollations]
	@nEncID INT,
	@nCollationID INT,
	@nFlid INT
AS

	-- TODO (SteveMi): Only certain strings should have FlidCollation
	-- and MultiTxtSortKey$

	IF @nEncId IS NOT NULL
		PRINT 'todo'
		-- TODO (SteveMi): this chunk

	ELSE IF @nCollationID IS NOT NULL
		PRINT 'todo'
		-- TODO (SteveMi): this chunk

	ELSE IF @nFlid IS NOT NULL


		INSERT INTO FlidCollation$ ([Ws], [CollationId], [Flid])
		SELECT e.[Ws], wsc.[Dst] AS [CollationID], @nFlid AS [Flid]
		FROM (
			SELECT [Dst] AS [Ws] FROM LangProject_CurAnalysisWss
			UNION
			SELECT [Dst] AS [Ws] FROM LangProject_CurVernWss) e
		JOIN LgWritingSystem_Collations wsc ON wsc.[Src] = e.[Ws]
		LEFT OUTER JOIN FlidCollation$ fc ON
			fc.[Ws] = e.[Ws] AND
			fc.[CollationID] = wsc.[Dst] AND
			fc.[Flid] = @nFlid
		WHERE (
			fc.[Ws] IS NULL OR
			fc.[CollationID] IS NULL OR
			fc.[Flid] IS NULL)

	--( The delete into FlidCollation$ should trigger an delete trigger
	--( sort key creation in MultiTxtSortKey$

	-- TODO (SteveMi): above comment.

GO
