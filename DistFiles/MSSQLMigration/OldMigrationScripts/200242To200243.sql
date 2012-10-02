-- Update database from version 200242 to 200243
BEGIN TRANSACTION  --( will be rolled back if wrong version #)
-------------------------------------------------------------------------------
-- March 13, 2009 Ann Bush,  FWM-164 Finish variant entry changes
-- Add LexEntryRef: RefType : Integer (max 127).
-- Clean up unused data.
--    CmPossibilityList owned in LexDb_EntryTypes
--    CmPossibilityList owned in LexDb_AllomorphConditions
--
--	  LexEntry_Condition
--	  LexEntry_EntryType
--	  LexDb_EntryTypes
--	  LexDb_AllomorphConditions
--	  LexEntryType_Type
--
-------------------------------------------------------------------------------
DECLARE @ntIds varchar(40),
	@Id INT

--Delete Possibility Lists
Select @ntIds = Id from cmObject where OwnFlid$ = '5005004'   -- AllomorphConditions
Select @ntIds = @ntIds + ', ' + CAST(Id AS VARCHAR) from cmObject where OwnFlid$ = '5005018' -- EntryTypes
EXEC DeleteObjects @ntIds

Delete from field$
 where id in (5002023,5002024, 5005018,5005004, 5118002)
go
--==( LexEntryRef )==--
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5127008, 2, 5127,
		null, 'RefType',0,Null, null, null, null)
go

-----------------------------------------------------------------------------------
------ Finish or roll back transaction as applicable
-----------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200242
BEGIN
	UPDATE Version$ SET DbVer = 200243
	COMMIT TRANSACTION
	PRINT 'database updated to version 200243'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200242 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
