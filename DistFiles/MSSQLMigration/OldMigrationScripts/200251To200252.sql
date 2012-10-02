-- Update database from version 200251 to 200252
BEGIN TRANSACTION  --( will be rolled back if wrong version #)
-------------------------------------------------------------------------------
-- LT-9414, LT-9413, LT-5272, LT-6286 various fixes to lexical relations list
-------------------------------------------------------------------------------

declare cur cursor local static forward_only read_only for
select pss.dst from LexDb_References refs
	join CmPossibilityList_Possibilities pss on pss.src = refs.dst -- 18248
declare @lrt int, @en int, @name nvarchar(100), @revname nvarchar(100), @para int, @disc nvarchar(4000)
select @en = id from LgWritingSystem where IcuLocale = 'en'
open cur
	fetch cur into @lrt
	while @@fetch_status = 0 begin
		select @name=pn.txt, @revname=rn.txt, @disc=para.contents, @para=para.id from CmPossibility_Name pn
			left outer join LexRefType_ReverseName rn on rn.obj = pn.obj and rn.ws = @en
			left outer join CmPossibility_Discussion dis on dis.src = @lrt
			left outer join StText_Paragraphs paras on paras.src = dis.dst
			left outer join StTxtPara para on para.id = paras.dst
			where pn.obj = @lrt and pn.ws = @en
		if @name = 'Parts' and @revname = 'Whole' begin
			update CmPossibility_Name set txt = 'Part' where obj = @lrt
			if @disc = 'A part/whole relation establishes a link between the sense for the whole (e.g., room), and senses for the parts (e.g., ceiling, wall, floor). To create a part/whole relation, start in the sense that represents the whole, right+click "Lexical Relations" and choose "Insert Parts Relation (to this Whole)", then select a sense that represents a part of this sense. Add more parts by clicking the button to the right and adding as many parts as desired. Every part selected in this way will automatically have a corresponding Whole field. Note that you can actually build a whole/part tree. For example, you could add a Parts relation to "house" that includes "roof", "room", and "foundation". The sense "room" would then show two fields, one for the parts, and one for the whole.' begin
				update StTxtPara set contents = null, contents_fmt = null where id = @para
				update CmPossibility_Description set txt = 'A part/whole relation establishes a link between the sense for the whole (e.g., room), and senses for the parts (e.g., ceiling, wall, floor).'
					where obj = @lrt and ws = @en
			end
		end
		if @name = 'Specifics' and @revname = 'Generic' begin
			update CmPossibility_Name set txt = 'Specific' where obj = @lrt
			if @disc = 'A generic/specific relation establishes a link between the sense for the generic (e.g., bird), and senses for the specifics (e.g., robin, cardinal, dove). To create a generic/specific relation, start in the sense that represents the generic, right+click "Lexical Relations" and choose "Insert Specifics Relation (to this Generic)", then select a sense that represents a specific of this sense. Add more specifics by clicking the button to the right and adding as many specifics as desired. Every specific selected in this way will automatically have a corresponding Generic field. Note that you can actually build a generic/specific tree. For example, you could add a Specifics relation to "animal" that includes "mammal", "bird", and "reptile". The sense "bird" would then show two fields, one for the specifics, and one for the generic.' begin
				update StTxtPara set contents = null, contents_fmt = null where id = @para
				update CmPossibility_Description set txt = 'A generic/specific relation establishes a link between the sense for the generic (e.g., bird), and senses for the specifics (e.g., robin, cardinal, dove).'
					where obj = @lrt and ws = @en
			end
		end
		if @disc = 'A synonym relationship links any number of senses together. To create a synonym relation from any sense, right+click "Lexical Relations" and choose "Insert Synonyms Relation", then select a synonym of the current sense. You can add more synonyms to the same set by clicking the button to the right to add as many senses as desired. The other senses will automatically show all of the senses that are part of this set, excluding the current sense. If you want to add another synonym to an existing set, click the button to the right of the Synonym field and add more senses. If you do not want all senses to be part of the same set, you can insert another synonym relationship by right+clicking "Lexical Relations" and choosing "Insert Synonym Relation". This will add a new synonym set with the current sense as a member of the set.' begin
			update StTxtPara set contents = null, contents_fmt = null where id = @para
		end
		if @disc = 'To create an antonym relation, from either sense right+click "Lexical Relations" and choose "Insert Antonym Relation", then select the antonym of the current sense. This will automatically provide an Antonym field on the selected sense to the current sense. As currently defined, an antonym relationship only allows 2 senses. If you want to allow multiple senses, change the Mapping Type to "Sense collection..."' begin
			update StTxtPara set contents = null, contents_fmt = null where id = @para
		end
		if @disc = 'A calendar relation is a type of scale or sequence set. Sequences include a group of senses that are related in an ordered fashion. The calendar relation is one type of sequence that can be used to store days of the week or months of the year. For example, to create a calendar relation for days of the week, go to the Sunday sense, right+click "Lexical Relations" and choose "Insert Calendar Relation", then select the sense for Monday. Continue to add senses for the remaining days of the week. If a sense is out of order, you can correct it by right+clicking the sense and choosing "Move Left" or "Move Right". Note that in all scale relations, the current sense shows up in the list since order is important. From any other sense that is part of the sequence, you''ll see the complete set of senses in exactly the same order. You can label this relation as days of the week by right+clicking "Calendar" and choosing "Edit Reference Name/Comment". In the Name box you could add "Days of the week". To add a relation for months of the year, you would use the same process but start with a sense representing a month, and then add the remaining months. You can then label this relation as "Months of the year".' begin
			update StTxtPara set contents = null, contents_fmt = null where id = @para
		end
		if @disc = 'A compare relation provides a generic way to reference other entries. Normally it is better to use more specific types of lexical relations. To add a compare relation, right+click "Cross References" and choose "Insert Compare Relation", then select an entry that you want to compare with the current entry. You can add additional entries as desired. If you want compare relations to be associated with senses rather than entries, you can change the Mapping Type to "Sense Collection..." It would then show up as an option when you right+click "Lexical Relations".' begin
			update StTxtPara set contents = null, contents_fmt = null where id = @para
			update CmPossibility_Description set txt = 'Use this type for relationships that cannot be classified by a specific label, or of which there are too few examples to warrant creating a custom lexical relation.'
				where obj = @lrt and ws = @en
		end
		if @name = 'Classifier' and @revname = 'Classified nouns' begin
			update CmPossibility_Name set txt = 'Classified Noun' where obj = @lrt
			update CmPossibility_Abbreviation set txt = 'clf. for' where obj = @lrt
			update LexRefType_ReverseName set txt = 'Classifier' where obj = @lrt
			update LexRefType_ReverseAbbreviation set txt = 'clf' where obj = @lrt
			if @disc = 'To create a link between a classifier and the words it is used for, go to the lexical entry for the classifier. Then right-click on "Lexical Relations" and choose "Insert Classified nouns (to this Classifier)". Then select a noun that this classifier is used with. Alternatively, if you are in the entry for a noun and want to indicate what its classifier is, right-click on "Lexical Relations" and then choose "Insert Classifier (to this Classified nouns)". Then choose the classifier that is used for this word.' begin
				update StTxtPara set contents = null, contents_fmt = null where id = @para
				update CmPossibility_Description set txt = 'Use this relation for linking classifiers to the words that they classify.'
					where obj = @lrt and ws = @en
			end
		end
		fetch cur into @lrt
	end
close cur
deallocate cur

go

-----------------------------------------------------------------------------------
------ Finish or roll back transaction as applicable
-----------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200251
BEGIN
	UPDATE Version$ SET DbVer = 200252
	COMMIT TRANSACTION
	PRINT 'database updated to version 200252'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200251 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
