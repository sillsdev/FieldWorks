-- update database from version 200018 to 200019
BEGIN TRANSACTION

-- FindOrCreateWfiAnalysis has been incorporated into UpdWfiAnalysisAndEval$
-- because of problems getting a collection of ids from FindOrCreateWfiAnalysis.
-- Therefore, it is being deleted, not updated.
if object_id('FindOrCreateWfiAnalysis') is not null begin
	print 'removing procedure FindOrCreateWfiAnalysis'
	drop proc FindOrCreateWfiAnalysis
end

if object_id('MakeMissingAnalysesFromLexicion') is not null begin
	print 'removing procedure MakeMissingAnalysesFromLexicion'
	drop proc MakeMissingAnalysesFromLexicion
end
go
CREATE  proc MakeMissingAnalysesFromLexicion
	@paraid int,
	@ws int
as

declare wf_cur cursor local static forward_only read_only for

select distinct wf.id wfid, mff.obj fid, ls.id lsid, msta.id msaid, lsg.Txt gloss, msta.PartOfSpeech pos
	from CmBaseAnnotation_ cba (readuncommitted)
	join WfiWordform wf (readuncommitted) on  cba.BeginObject = @paraid and cba.InstanceOf = wf.id -- annotations of this paragraph that are wordforms
	left outer join WfiAnalysis_ wa on wa.owner$ = wf.id
	-- if the above produced anything, with the restriction on wa.owner being null below, they are wordforms we want
	join WfiWordform_Form wff (readuncommitted) on wff.obj = wf.id
	join MoForm_Form mff (readuncommitted) on wff.Txt = mff.txt and mff.ws = wff.ws
	-- now we have ones whose form matches an MoForm in the same ws
	join CmObject mfo (readuncommitted) on mfo.id = mff.obj
	join CmObject leo (readuncommitted) on leo.id = mfo.owner$
	join LexSense_ ls (readuncommitted) on ls.owner$ = leo.id
	left outer join LexSense_Gloss lsg (readuncommitted) on lsg.obj = ls.id and lsg.ws = @ws
	left outer join MoStemMsa msta (readuncommitted) on msta.id = ls.MorphoSyntaxAnalysis
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
	exec CreateObject_WfiAnalysis @wfid, 5062002, null, @analysisid out, @NewObjGuid out, 0, @NewObjTimestamp
	exec CreateObject_WfiMorphBundle null, null, null, @analysisid, 5059011, null, @mbid out, @NewObjGuid out, 0, @NewObjTimestamp
	exec CreateObject_WfiGloss @ws, @gloss, @analysisid, 5059010, null, @wgid out, @NewObjGuid out, 0, @NewObjTimestamp
	update WfiMorphBundle set Morph = @formid, Msa = @msaid, Sense = @senseid where id = @mbid
	update WfiAnalysis set Category = @pos where id = @analysisid
	fetch wf_cur into @wfid, @formid, @senseid, @msaid, @gloss, @pos
end
close wf_cur
deallocate wf_cur

go

declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200018
begin
	update Version$ set DbVer = 200019
	COMMIT TRANSACTION
	print 'database updated to version 200019'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200018 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
