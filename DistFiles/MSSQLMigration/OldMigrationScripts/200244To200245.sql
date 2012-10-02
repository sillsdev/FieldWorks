-- Update database from version 200244 to 200245
BEGIN TRANSACTION  --( will be rolled back if wrong version #)
-------------------------------------------------------------------------------
-- Add morpheme boundary symbol LT-8896
-- Fix to classifier name LT-9269

-- Add fixed guid to existing phonological rule word boundary
update CmObject set guid$ = '7DB635E0-9EF3-4167-A594-12551ED89AAA'
	where id in (select id from PhBdryMarker bm
				join PhTerminalUnit_Name tun on tun.obj = bm.id
				where Txt = '#')
-- Add phonological rule morph boundary
declare @bmarks int, @en int, @bmid int, @pcid int, @guid uniqueidentifier, @fmt varbinary(8000),
	@listid int, @clid int, @now datetime, @txtid int, @parid int
select @en = id from LgWritingSystem where IcuLocale = 'en'
select top 1 @fmt=Fmt from MultiStr$ where Ws=@en order by len(Fmt)
select @bmarks = id from PhPhonemeSet
exec MakeObj_PhBdryMarker @en, '+', @en, 'Morpheme boundary', @fmt, @bmarks, 5089003,
	null, @bmid output, @guid output
update CmObject set guid$ = '3BDE17CE-E39A-4bae-8A5C-A8D96FD4CB56' where id = @bmid
exec MakeObj_PhCode @en, '+', @bmid, 5090003, null, @pcid output, @guid output

-- If missing, add a Classifier LexRefType
select @listid = Dst from LexDb_References
select @now = getdate()
select @clid = id from LexRefType lrt
	join CmPossibility_Name pn on pn.obj = lrt.id
	where txt = 'Classifier'
if @clid is null begin
	exec MakeObj_LexRefType @en, 'Classifier', @en, 'clf', @en,
		'Use this relation for linking classifiers to the words that they classify.', @fmt,
		0, @now, @now, null, -1073741824, -1073741824, -1073741824, 0, 0, 0,
		@en, 'clf. for', 3, @en, 'Classified nouns', @listid, 8008, null, @clid output, @guid output
	exec MakeObj_StText 0, @clid, 7012, null, @txtid output, @guid output
	exec MakeObj_StTxtPara null, null, null, null, 'To create a link between a classifier and the words it is used for, go to the lexical entry for the classifier. Then right-click on "Lexical Relations" and choose "Insert Classified nouns (to this Classifier)". Then select a noun that this classifier is used with. Alternatively, if you are in the entry for a noun and want to indicate what its classifier is, right-click on "Lexical Relations" and then choose "Insert Classifier (to this Classified nouns)". Then choose the classifier that is used for this word.',
		@fmt, @txtid, 14001, null, @parid output, @guid output
end

go

-----------------------------------------------------------------------------------
------ Finish or roll back transaction as applicable
-----------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200244
BEGIN
	UPDATE Version$ SET DbVer = 200245
	COMMIT TRANSACTION
	PRINT 'database updated to version 200245'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200244 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
