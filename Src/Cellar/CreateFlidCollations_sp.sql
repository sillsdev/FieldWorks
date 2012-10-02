/*******************************************************************************
 * Procedure: CreateFlidCollations
 *
 * Description:
 *	Creates flid/Collation combinations
 *
 * Parameters--ONLY 1 parameter is expected to be not null:
 *	@nEncID			= Writing system ID
 *	@nCollationID	= Collation ID
 *	@nFlid			= Field ID
 *****************************************************************************/

IF OBJECT_ID('CreateFlidCollations') IS NOT NULL BEGIN
	PRINT 'removing procedure CreateFlidCollations'
	DROP PROCEDURE [CreateFlidCollations]
END
GO
PRINT 'creating procedure CreateFlidCollations'
GO
CREATE PROCEDURE [CreateFlidCollations]
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

		--( We want to create flidcollations only for current writing systems.
		--( We also don't want to duplicate any Writing System/Collation/Flid
		--( combination.

		INSERT INTO FlidCollation$ ([Ws], [CollationId], [Flid])
		SELECT curwritingsys.[Ws], wsc.[Dst] AS [CollationID], @nFlid AS [Flid]
		FROM (
			SELECT [Dst] AS [Ws] FROM LangProject_CurAnalysisWss
			UNION
			SELECT [Dst] AS [Ws] FROM LangProject_CurVernWss) curwritingsys
		JOIN LgWritingSystem_Collations wsc ON wsc.[Src] = curwritingsys.[Ws]
		LEFT OUTER JOIN FlidCollation$ fc ON
			fc.[Ws] = curwritingsys.[Ws] AND
			fc.[CollationID] = wsc.[Dst] AND
			fc.[Flid] = @nFlid
		WHERE (
			fc.[Ws] IS NULL OR
			fc.[CollationID] IS NULL OR
			fc.[Flid] IS NULL)

	--( The insert into FlidCollation$ should trigger an insert trigger
	--( sort key creation in MultiTxtSortKey$

	-- TODO (SteveMi): above comment.

GO
