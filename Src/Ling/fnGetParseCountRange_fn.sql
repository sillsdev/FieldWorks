/********************************************************************
 * Function: fnGetParseCountRange
 *
 * Description:
 *	Returns Wordforms in a range for an agent. The Wordforms may be
 *	accepted or not accepted
 *
 * Parameters:
 *	@nAgentId = Agent ID
 *	@nWritingSystem = Writing System ID
 *	@nAccepted = Positive (1) or Negative (2)
 *	@nRangeMin = Lower end of the range
 *	@nRangeMax = Higher end of the range
 *
 * Returns:
 *	Table containing WfiWordForm_Form information in the format:
 *		[Id]	INT				= Wordform Id
 *		[Txt]	NVARCHAR(4000)	= Wordform text
 *		[EvalCount] INT 		= number of evaluations
 *
 * Sample Call:
 *	SELECT * FROM fnGetParseCountRange (7452, 5784, 1, 2, 2)
 *******************************************************************/


if object_id('fnGetParseCountRange') is not null begin
	print 'removing function fnGetParseCountRange'
	drop function [fnGetParseCountRange]
end
go
print 'creating function fnGetParseCountRange'
go

CREATE FUNCTION [fnGetParseCountRange] (
	@nAgentId INT,
	@nWritingSystem INT,
	@nAccepted BIT,
	@nRangeMin INT,
	@nRangeMax INT)
RETURNS @tblWfiWordFormsCount TABLE (
	[Id] INT,
	--( See the notes under string tables in FwCore.sql about the
	--( COLLATE clause.
	Txt NVARCHAR(4000) COLLATE Latin1_General_BIN,
	EvalCount INT)
AS
BEGIN

	--( See Class Diagram CmAgent in the doc.
	--(-------------------------------------------
	--( CmAgentEvaluation.Target -->
	--(		CmObject --( subclassed as )-->
	--(		WfiWordForm or WfiAnalysis
	--(
	--(	WfiWordForm.Analyses -->
	--(		WfiAnalysis
	--(-------------------------------------------
	--( The Target of CmAgentEvaluation may either
	--( be a WfiWordForm, or a WfiAnalysis owned
	--( by a WfiWordForm. We want the latter.

	IF @nRangeMax != 0 BEGIN

		IF @nAccepted IS NULL

			INSERT INTO @tblWfiWordFormsCount
			SELECT wordformform.[Obj], wordformform.Txt, COUNT(wordformform.[Obj]) AS EvalCount
			FROM CmAgentEvaluation agenteval
			JOIN CmObject oagenteval ON oagenteval.[Id] = agenteval.[Id]
				AND oagenteval.[Owner$] = @nAgentId
			--( Don't need to join WfiAnalysis or WfiAnalysis_ here
			JOIN CmObject oanalysis ON oanalysis.[Id] = agenteval.[Target]
			JOIN WfiWordForm_Form wordformform ON wordformform.Obj = oanalysis.[Owner$]
				AND wordformform.ws = @nWritingSystem --( WfiWordForm_Form is actually MultiTxt$ with flid
			GROUP BY wordformform.[Obj], wordformform.Txt
			HAVING COUNT(wordformform.[Obj]) BETWEEN @nRangeMin AND @nRangeMax
			ORDER BY wordformform.Txt

		ELSE

			INSERT INTO @tblWfiWordFormsCount
			SELECT wordformform.[Obj], wordformform.Txt, COUNT(wordformform.[Obj]) AS EvalCount
			FROM CmAgentEvaluation agenteval
			JOIN CmObject oagenteval ON oagenteval.[Id] = agenteval.[Id]
				AND oagenteval.[Owner$] = @nAgentId
			--( Don't need to join WfiAnalysis or WfiAnalysis_ here
			JOIN CmObject oanalysis ON oanalysis.[Id] = agenteval.[Target]
			JOIN WfiWordForm_Form wordformform ON wordformform.Obj = oanalysis.[Owner$]
				AND wordformform.ws = @nWritingSystem --( WfiWordForm_Form is actually MultiTxt$ with flid
			WHERE agenteval.accepted = @nAccepted
			GROUP BY wordformform.[Obj], wordformform.Txt
			HAVING COUNT(wordformform.[Obj]) BETWEEN @nRangeMin AND @nRangeMax
			ORDER BY wordformform.Txt

	END
	ELSE --( IF @nRangeMax = 0

		--( 0 Parses:	wordform has an evaluation, but analyses--if
		--(				any--don't have evaluations

		--( Randy Regnier:
		--( I think it will have an evaluation, but for cases where the
		--( parser couldn't come up with any parses at all, I add a CmBaseAnnotation,
		--( and set its InstanceOfRAHvo and BeginObjectRAHvo to the HVO of the wordform.
		--( The CompDetails of the annotation will say "Analysis Failure".
		--( <snip>
		--( John Thomson: Which 'it' will have an evaluation?
		--( <snip>
		--( RR: We add evaluations to both the wordform and any parses retruned by the
		--( parser. In the case of no parses being returned, we jsut add an evaluation
		--( top the wordform, along with the annotation.

		INSERT INTO @tblWfiWordFormsCount
		SELECT wordformform.[Obj], wordformform.Txt, 0 AS EvalCount
		FROM WfiWordForm_Form wordformform
		JOIN CmAgentEvaluation agenteval ON agenteval.Target = wordformform.Obj
		JOIN CmObject oagenteval ON oagenteval.[Id] = agenteval.[Id]
			AND oagenteval.[Owner$] = @nAgentId
		LEFT OUTER JOIN CmObject oAnalysis ON oAnalysis.Owner$ = wordformform.Obj
		LEFT OUTER JOIN CmAgentEvaluation aneval ON aneval.Target = oanalysis.[Id]
		WHERE aneval.Target IS NULL

	RETURN
END
GO
