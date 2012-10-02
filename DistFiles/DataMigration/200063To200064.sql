-- Update database from version 200063 to 200064
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-- Obtain the best possible default magic binary format for any description strings.
DECLARE @rgbFmt varbinary(8000), @wsEn int, @lrt int, @st int, @stp int, @guid uniqueidentifier, @txt nvarchar(4000)
select top 1 @wsEn=id from LgWritingSystem where iculocale = 'en'
select top 1 @rgbFmt=Fmt from MultiStr$ where Ws=@wsEn order by Fmt

-- Add discussions to lex reference types

set @txt = 'To create an antonym relation, from either sense right+click "Lexical Relations" and choose "Insert Antonym Relation", then select the antonym of the current sense. This will automatically provide an Antonym field on the selected sense to the current sense. As currently defined, an antonym relationship only allows 2 senses. If you want to allow multiple senses, change the Mapping Type to "Sense collection..."'
set @st = null set @stp = null set @lrt = null
select top 1 @lrt = id from LexReferenceType t
join CmPossibility_Name n on n.obj = t.id
where n.txt = 'Antonym'
if @lrt is not null begin
	select top 1 @st = dst from CmPossibility_Discussion where src = @lrt
	if @st is null begin
		exec CreateObject_StText 0, @lrt, 7012, 0, @st output, @guid output
	end
	select top 1 @stp = dst from StText_Paragraphs where src = @st order by ord
	if @stp is null begin
		exec CreateObject_StTxtPara null, null, null, null, @txt, @rgbFmt, @st, 14001, null, @stp output, @guid output
	end
	else begin
		update StTxtPara set contents = @txt, contents_fmt = @rgbFmt where id = @stp
	end
end

set @txt = 'A synonym relationship links any number of senses together. To create a synonym relation from any sense, right+click "Lexical Relations" and choose "Insert Synonyms Relation", then select a synonym of the current sense. You can add more synonyms to the same set by clicking the button to the right to add as many senses as desired. The other senses will automatically show all of the senses that are part of this set, excluding the current sense. If you want to add another synonym to an existing set, click the button to the right of the Synonym field and add more senses. If you do not want all senses to be part of the same set, you can insert another synonym relationship by right+clicking "Lexical Relations" and choosing "Insert Synonym Relation". This will add a new synonym set with the current sense as a member of the set.'
set @st = null set @stp = null set @lrt = null
select top 1 @lrt = id from LexReferenceType t
join CmPossibility_Name n on n.obj = t.id
where n.txt = 'Synonyms'
if @lrt is not null begin
	select top 1 @st = dst from CmPossibility_Discussion where src = @lrt
	if @st is null begin
		exec CreateObject_StText 0, @lrt, 7012, 0, @st output, @guid output
	end
	select top 1 @stp = dst from StText_Paragraphs where src = @st order by ord
	if @stp is null begin
		exec CreateObject_StTxtPara null, null, null, null, @txt, @rgbFmt, @st, 14001, null, @stp output, @guid output
	end
	else begin
		update StTxtPara set contents = @txt, contents_fmt = @rgbFmt where id = @stp
	end
end

set @txt = 'A part/whole relation establishes a link between the sense for the whole (e.g., room), and senses for the parts (e.g., ceiling, wall, floor). To create a part/whole relation, start in the sense that represents the whole, right+click "Lexical Relations" and choose "Insert Parts Relation (to this Whole)", then select a sense that represents a part of this sense. Add more parts by clicking the button to the right and adding as many parts as desired. Every part selected in this way will automatically have a corresponding Whole field. Note that you can actually build a whole/part tree. For example, you could add a Parts relation to "house" that includes "roof", "room", and "foundation". The sense "room" would then show two fields, one for the parts, and one for the whole.'
set @st = null set @stp = null set @lrt = null
select top 1 @lrt = id from LexReferenceType t
join CmPossibility_Name n on n.obj = t.id
where n.txt = 'Parts'
if @lrt is not null begin
	select top 1 @st = dst from CmPossibility_Discussion where src = @lrt
	if @st is null begin
		exec CreateObject_StText 0, @lrt, 7012, 0, @st output, @guid output
	end
	select top 1 @stp = dst from StText_Paragraphs where src = @st order by ord
	if @stp is null begin
		exec CreateObject_StTxtPara null, null, null, null, @txt, @rgbFmt, @st, 14001, null, @stp output, @guid output
	end
	else begin
		update StTxtPara set contents = @txt, contents_fmt = @rgbFmt where id = @stp
	end
end

set @txt = 'A generic/specific relation establishes a link between the sense for the generic (e.g., bird), and senses for the specifics (e.g., robin, cardinal, dove). To create a generic/specific relation, start in the sense that represents the generic, right+click "Lexical Relations" and choose "Insert Specifics Relation (to this Generic)", then select a sense that represents a specific of this sense. Add more specifics by clicking the button to the right and adding as many specifics as desired. Every specific selected in this way will automatically have a corresponding Generic field. Note that you can actually build a generic/specific tree. For example, you could add a Specifics relation to "animal" that includes "mammal", "bird", and "reptile". The sense "bird" would then show two fields, one for the specifics, and one for the generic.'
set @st = null set @stp = null set @lrt = null
select top 1 @lrt = id from LexReferenceType t
join CmPossibility_Name n on n.obj = t.id
where n.txt = 'Specifics'
if @lrt is not null begin
	select top 1 @st = dst from CmPossibility_Discussion where src = @lrt
	if @st is null begin
		exec CreateObject_StText 0, @lrt, 7012, 0, @st output, @guid output
	end
	select top 1 @stp = dst from StText_Paragraphs where src = @st order by ord
	if @stp is null begin
		exec CreateObject_StTxtPara null, null, null, null, @txt, @rgbFmt, @st, 14001, null, @stp output, @guid output
	end
	else begin
		update StTxtPara set contents = @txt, contents_fmt = @rgbFmt where id = @stp
	end
end

set @txt = 'A calendar relation is a type of scale or sequence set. Sequences include a group of senses that are related in an ordered fashion. The calendar relation is one type of sequence that can be used to store days of the week or months of the year. For example, to create a calendar relation for days of the week, go to the Sunday sense, right+click "Lexical Relations" and choose "Insert Calendar Relation", then select the sense for Monday. Continue to add senses for the remaining days of the week. If a sense is out of order, you can correct it by right+clicking the sense and choosing "Move Left" or "Move Right". Note that in all scale relations, the current sense shows up in the list since order is important. From any other sense that is part of the sequence, you''ll see the complete set of senses in exactly the same order. You can label this relation as days of the week by right+clicking "Calendar" and choosing "Edit Reference Name/Comment". In the Name box you could add "Days of the week". To add a relation for months of the year, you would use the same process but start with a sense representing a month, and then add the remaining months. You can then label this relation as "Months of the year".'
set @st = null set @stp = null set @lrt = null
select top 1 @lrt = id from LexReferenceType t
join CmPossibility_Name n on n.obj = t.id
where n.txt = 'Calendar'
if @lrt is not null begin
	select top 1 @st = dst from CmPossibility_Discussion where src = @lrt
	if @st is null begin
		exec CreateObject_StText 0, @lrt, 7012, 0, @st output, @guid output
	end
	select top 1 @stp = dst from StText_Paragraphs where src = @st order by ord
	if @stp is null begin
		exec CreateObject_StTxtPara null, null, null, null, @txt, @rgbFmt, @st, 14001, null, @stp output, @guid output
	end
	else begin
		update StTxtPara set contents = @txt, contents_fmt = @rgbFmt where id = @stp
	end
end

set @txt = 'A compare relation provides a generic way to reference other entries. Normally it is better to use more specific types of lexical relations. To add a compare relation, right+click "Cross References" and choose "Insert Compare Relation", then select an entry that you want to compare with the current entry. You can add additional entries as desired. If you want compare relations to be associated with senses rather than entries, you can change the Mapping Type to "Sense Collection..." It would then show up as an option when you right+click "Lexical Relations".'
set @st = null set @stp = null set @lrt = null
select top 1 @lrt = id from LexReferenceType t
join CmPossibility_Name n on n.obj = t.id
where n.txt = 'Compare'
if @lrt is not null begin
	select top 1 @st = dst from CmPossibility_Discussion where src = @lrt
	if @st is null begin
		exec CreateObject_StText 0, @lrt, 7012, 0, @st output, @guid output
	end
	select top 1 @stp = dst from StText_Paragraphs where src = @st order by ord
	if @stp is null begin
		exec CreateObject_StTxtPara null, null, null, null, @txt, @rgbFmt, @st, 14001, null, @stp output, @guid output
	end
	else begin
		update StTxtPara set contents = @txt, contents_fmt = @rgbFmt where id = @stp
	end
end

--Fix depth problems on possibility lists

update CmPossibilityList set depth=1
from CmPossibilityList pl, LexicalDatabase_SenseTypes ld
where ld.dst = pl.id
update CmPossibilityList set depth=127
from CmPossibilityList pl, LexicalDatabase_UsageTypes ld
where ld.dst = pl.id
update CmPossibilityList set depth=127
from CmPossibilityList pl, LexicalDatabase_DomainTypes ld
where ld.dst = pl.id
update CmPossibilityList set depth=1
from CmPossibilityList pl, LexicalDatabase_MorphTypes ld
where ld.dst = pl.id
update CmPossibilityList set depth=127
from CmPossibilityList pl, LanguageProject_PartsOfSpeech ld
where ld.dst = pl.id
update CmPossibilityList set depth=127
from CmPossibilityList pl, ReversalIndex_PartsOfSpeech ld
where ld.dst = pl.id
update CmPossibilityList set depth=1
from CmPossibilityList pl, LanguageProject_TranslationTags ld
where ld.dst = pl.id
update CmPossibilityList set depth=127
from CmPossibilityList pl, LanguageProject_Locations ld
where ld.dst = pl.id
update CmPossibilityList set depth=127
from CmPossibilityList pl, LanguageProject_AnnotationDefinitions ld
where ld.dst = pl.id

go

---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200063
begin
	update Version$ set DbVer = 200064
	COMMIT TRANSACTION
	print 'database updated to version 200064'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200063 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
