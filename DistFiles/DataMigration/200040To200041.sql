-- update database from version 200040 to 200041
BEGIN TRANSACTION  --( will be rolled back if wrong version#

-------------------------------------------------------------------------------
--( Set ownership of any ReversalIndex objects to the lexicon.
declare @ldbID int
SELECT  top 1 @ldbID=ID
FROM LexicalDatabase

UPDATE CmObject
SET Owner$=@ldbID, OwnFlid$=5005017
WHERE Class$=5052

GO

---------------------------------------------------------------------------------------------------------------------------------------------
--( Update any ReversalIndexEntry rows to new model, and move data from old Name property to new Form property.
DECLARE
		@nReversalIndexId INT,
		@nWritingSystem INT

--( Get the first row of ReversalIndex
SELECT TOP 1 @nReversalIndexId = Id,  @nWritingSystem = WritingSystem
FROM ReversalIndex
ORDER BY Id

--( Loop through the ReversalIndex records
WHILE @@ROWCOUNT > 0 BEGIN
		--( Update the ReversalIndexEntries owned by the current ReversalIndex
		UPDATE ReversalIndexEntry
		SET WritingSystem = @nWritingSystem, Form = Name, Name = null, Name_Fmt = null
		FROM ReversalIndexEntry rie,
				 fnGetOwnedObjects$(@nReversalIndexId, NULL, 528482304, 0, 0, 1, NULL, 0) goo
		WHERE rie.Id = goo.ObjId
				AND goo.ObjClass = 5053

		--( Get the next ReversalIndex record
		SELECT TOP 1 @nReversalIndexId = Id, @nWritingSystem = WritingSystem
		FROM ReversalIndex
		WHERE Id > @nReversalIndexId
		ORDER BY Id
END

GO
-------------------------------------------------------------------------------

declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200040
begin
	update Version$ set DbVer = 200041
	COMMIT TRANSACTION
	print 'database updated to version 200040'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200040 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
