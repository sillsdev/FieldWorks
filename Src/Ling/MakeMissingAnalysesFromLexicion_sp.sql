/***********************************************************************************************
 * MakeMissingAnalysesFromLexicion
 * Description: This procedure creates a WfiAnalyis and WfiGloss for words in a paragraph
 * that have no known analysis but which exactly match the form of an MoForm somewhere.
 * We begin by identifying unique WfiWordforms linked to the paragraph by an annotation
 * which own no WfiAnalyses, and which have a form that matches the form of some MoForm.
 * For each of these, we find the first sense of the lex entry that owns the MoForm, its gloss,
 * and (if possible) a PartOfSpeech that is the POS of the MoStemMsa that is the MSA of the
 * sense.
 * For each such WfiWordform, we create a WfiAnalysis and set its POS to the POS we found;
 * create a WfiMorphBundle owned by the WfiAnalysis, and set its sense, msa, and form;
 * and create a WfiGloss owned by the WfiAnalysis and set its form to the gloss of the sense.
 * In other words, we create an analysis that has one morpheme, the MoForm we found,
 * and let its sense and MSA be determined by the first sense of that form, both at the
 * morpheme and gloss level.
 * Parameters:
 *    @paraid - the paragraph we want to apply the heuristic to
 *    @ws - the ws we're using for glosses, typically DefaultAnalWs.
 * Return: 0 if successful, otherwise 1.
***********************************************************************************************/

if object_id('MakeMissingAnalysesFromLexicion') is not null begin
	drop proc MakeMissingAnalysesFromLexicion
end
go
print 'creating proc MakeMissingAnalysesFromLexicion'
go

CREATE  proc MakeMissingAnalysesFromLexicion
	@paraid int,
	@ws int
as

declare wf_cur cursor local static forward_only read_only for

select distinct wf.id wfid, mff.obj fid, ls.id lsid, msta.id msaid, lsg.Txt gloss, msta.PartOfSpeech pos
	from CmBaseAnnotation_ cba
	join WfiWordform wf on  cba.BeginObject = @paraid and cba.InstanceOf = wf.id -- annotations of this paragraph that are wordforms
	left outer join WfiAnalysis_ wa on wa.owner$ = wf.id
	-- if the above produced anything, with the restriction on wa.owner being null below, they are wordforms we want
	join WfiWordform_Form wff on wff.obj = wf.id
	join MoForm_Form mff on wff.Txt = mff.txt and mff.ws = wff.ws
	-- now we have ones whose form matches an MoForm in the same ws
	join CmObject mfo on mfo.id = mff.obj
	join CmObject leo on leo.id = mfo.owner$
	join LexSense_ ls on ls.owner$ = leo.id
	left outer join LexSense_Gloss lsg on lsg.obj = ls.id and lsg.ws = @ws
	left outer join MoStemMsa msta on msta.id = ls.MorphoSyntaxAnalysis
	-- combines with left outer join above for effect of
		-- "not exists (select * from WfiAnalysis_ wa where wa.owner$ = wf.id)"
	-- (that is, we want wordforms that have no analyses)
	-- but is faster
	where wa.owner$ is null

open wf_cur

declare @wfid int, @formid int, @senseid int,  @msaid int, @pos int
declare @gloss nvarchar(1000)
declare @NewObjGuid uniqueidentifier,
	@NewObjTimestamp int

-- 5062002 5062002
-- 5059011 5059011
-- 5059010 5059010
-- 5060001 50600001
fetch wf_cur into @wfid, @formid, @senseid, @msaid, @gloss, @pos
while @@fetch_status = 0 begin
	declare @analysisid int
	declare @mbid int
	declare @wgid int
	exec MakeObj_WfiAnalysis @wfid, 5062002, null, @analysisid out, @NewObjGuid out, 0, @NewObjTimestamp
	exec MakeObj_WfiMorphBundle null, null, null, @analysisid, 5059011, null, @mbid out, @NewObjGuid out, 0, @NewObjTimestamp
	exec MakeObj_WfiGloss @ws, @gloss, @analysisid, 5059010, null, @wgid out, @NewObjGuid out, 0, @NewObjTimestamp
	update WfiMorphBundle set Morph = @formid, Msa = @msaid, Sense = @senseid where id = @mbid
	update WfiAnalysis set Category = @pos where id = @analysisid
	fetch wf_cur into @wfid, @formid, @senseid, @msaid, @gloss, @pos
end
close wf_cur
deallocate wf_cur
go
