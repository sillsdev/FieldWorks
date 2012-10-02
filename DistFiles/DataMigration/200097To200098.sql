-- Update database from version 200097 to 200098
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

/* Migration Script Contents */
-- FWM-97 : Change LexSense_Pictures to use CmPicture
	-- MIGRATION: Assume we don't have existing LexPictures to migrate.
	-- 1) Change LexSense_Pictures[num=10->29] from owning sequence of LexPicture to CmPicture
	-- 2) Delete class LexPicture
-- FWM-91 : Change CmPicture_Caption [num=1->3] to MultiString
	-- MIGRATION: CmPicture_Caption string to MultiString
	-- Change type from string to MultiString
-- FWM-93 : Add MoEndocentricCompound_OverridingMsa [num=2] which owns an atomic MoStemMsa
	-- MIGRATION: Assume no data to migrate.
-- FWM-95 : Add ProductivityRestrictions to model
	-- MIGRATION: Assume no data to migrate.
	-- 1) MoMorphologicalData_ProductivityRestrictions [num=9] which owns a CmPossibilityList
	-- 2) MoStemMsa changes:
	-- 		a.	Change the ExceptionFeatures attribute to refer to a collection of CmPossibility.
	-- 		b.	Rename it to ProductivityRestrictions.[num=4->6]
	-- 3) MoDerivationalAffixMsa changes:
	--		a.	Change the FromExceptionFeatures attribute to refer to a collection of CmPossibility.
	--		b.	Rename it to FromProductivityRestrictions [num=7->13]
	-- 		d.	Change the ToExceptionFeatures attribute to refer to a collection of CmPossibility.
	--		e.	Rename it to ToProductivityRestrictions. [num=11->14]
	-- 4) MoInflectionalAffixMsa changes:
	--		a.	Change the FromExceptionFeatures attribute to refer to a collection of CmPossibility.
	--		b.	Rename it to FromProductivityRestrictions.[num=3->8]
	-- 5) MoCompoundRule changes:
	--		a.	Change the ToExceptionFeatures attribute to refer to a collection of CmPossibility.
	--		b.	Rename it to ToProductivityRestrictions.[num=4->8]
	-- 6) MoDerivationalStepMsa changes:
	--		a.	Change the ExceptionFeatures attribute to refer to a collection of CmPossibility.
	--		b.	Rename it to ProductivityRestrictions. [num=5->6]

/* IMPLEMENTATIONS */
-- FWM-97 : Change LexSense_Pictures to use CmPicture
	-- MIGRATION: Assume we don't have existing LexPictures to migrate.
	-- 1) Change LexSense_Pictures[num=10->29] from owning sequence of LexPicture to CmPicture
	-- 2) Delete class LexPicture

	/* Delete LexPicture Class */
	EXEC DeleteModelClass 5013
GO
	/* Create new LexSense_Pictures field */
	insert into [Field$]
			  ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(5016029, 27, 5016, 48, 'Pictures',0,Null, null, null, null)
GO
-- FWM-91 : Change CmPicture_Caption [num=1->3] to MultiString
	-- MIGRATION: CmPicture_Caption string to MultiString
	-- Change type from string to MultiString

	/* Create new CmPicture_Caption field */
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(48003, 14, 48,
			null, 'Caption2',0,Null, null, null, null)
GO
	/* Copy old CmPicture_Caption to new CmPicture_Caption */
	-- New CmPicture String Data
	DECLARE @Caption_MultiStr_ws int
	-- Set CmPicture_Caption WS to CurrentVernacularWritingSystem
	select top 1 @Caption_MultiStr_ws=dst from LanguageProject_CurrentVernacularWritingSystems
		Order By Ord
	-- Copy old caption string to new multiString caption
	insert into [MultiStr$] with (rowlock)([Flid],[Obj],[Ws],[Txt],[Fmt]) 		select 48003, Id, @Caption_MultiStr_ws, Caption, Caption_Fmt from CmPicture
			where Caption is not null
GO
	/* Delete Old CmPicture_Caption field */
	DELETE FROM Field$ WHERE id=48001
GO
	/* Rename New CmPicture_Caption field */
	ALTER TABLE Field$ DISABLE TRIGGER TR_Field$_No_Upd
	UPDATE Field$ SET Name='Caption' WHERE Id=48003
	ALTER TABLE Field$ ENABLE TRIGGER TR_Field$_No_Upd
GO
	-- Reconstruct our view
	CREATE VIEW CmPicture_Caption AS
		select Obj, Flid, Ws, Txt, Fmt
		FROM MultiStr$
		WHERE [Flid] = 48003
GO
	-- Remove old view
	if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[CmPicture_Caption2]') and OBJECTPROPERTY(id, N'IsView') = 1)
		drop view [dbo].[CmPicture_Caption2]
GO
	-- Cleanup CmPicture class stuff
	EXEC UpdateClassView$ 48, 1 -- CmPicture
	EXEC DefineCreateProc$ 48	-- CreateObject_CmPicture
GO
-- FWM-93 : Add MoEndocentricCompound_OverridingMsa [num=2] which owns an atomic MoStemMsa
	-- MIGRATION: Assume no data to migrate.
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(5033002, 23, 5033, 5001, 'OverridingMsa',0,Null, null, null, null)
GO

-- FWM-95 : Add ProductivityRestrictions to model
	-- MIGRATION: Assume no data to migrate.
	-- 1) MoMorphologicalData_ProductivityRestrictions [num=9] which owns a CmPossibilityList
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(5040009, 23, 5040, 8, 'ProductivityRestrictions',0,Null, null, null, null)
GO
	-- 2) MoStemMsa changes:
	-- 		a.	Change the ExceptionFeatures attribute to refer to a collection of CmPossibility.
	-- 		b.	Rename it to ProductivityRestrictions.[num=4->6]
	-- 3) MoDerivationalAffixMsa changes:
	--		a.	Change the FromExceptionFeatures attribute to refer to a collection of CmPossibility.
	--		b.	Rename it to FromProductivityRestrictions [num=7->13]
	-- 		d.	Change the ToExceptionFeatures attribute to refer to a collection of CmPossibility.
	--		e.	Rename it to ToProductivityRestrictions. [num=11->14]
	-- 4) MoInflectionalAffixMsa changes:
	--		a.	Change the FromExceptionFeatures attribute to refer to a collection of CmPossibility.
	--		b.	Rename it to FromProductivityRestrictions.[num=3->8]
	-- 5) MoCompoundRule changes:
	--		a.	Change the ToExceptionFeatures attribute to refer to a collection of CmPossibility.
	--		b.	Rename it to ToProductivityRestrictions.[num=4->8]
	-- 6) MoDerivationalStepMsa changes:
	--		a.	Change the ExceptionFeatures attribute to refer to a collection of CmPossibility.
	--		b.	Rename it to ProductivityRestrictions. [num=5->6]

	/* Create new ProductivityRestriction Fields for each of the following classes */
	-- MoStemMsa
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(5001006, 26, 5001, 7, 'ProductivityRestrictions',0,Null, null, null, null)
	-- MoCompoundRule
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(5030008, 26, 5030, 7, 'ToProductivityRestrictions',0,Null, null, null, null)
	-- MoDerivationalAffixMsa
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(5031013, 26, 5031, 7, 'FromProductivityRestrictions',0,Null, null, null, null)
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(5031014, 26, 5031, 7, 'ToProductivityRestrictions',0,Null, null, null, null)
	-- MoDerivationalStepMsa
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(5032006, 26, 5032, 7, 'ProductivityRestrictions',0,Null, null, null, null)
	-- MoInflectionalAffixMsa
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(5038008, 26, 5038, 7, 'FromProductivityRestrictions',0,Null, null, null, null)
GO
	/* Delete old ExceptionFeature fields */
	DELETE FROM Field$ WHERE id=5030004	-- MoCompoundRule_ToExceptionFeatures
	DELETE FROM Field$ WHERE id=5031007 -- MoDerivationalAffixMsa_FromExceptionFeatures
	DELETE FROM Field$ WHERE id=5031011 -- MoDerivationalAffixMsa_ToExceptionFeatures
	DELETE FROM Field$ WHERE id=5032005 -- MoDerivationalStepMsa_ExceptionFeatures
	DELETE FROM Field$ WHERE id=5038003 -- MoInflectionalAffixMsa_FromExceptionFeatures
	DELETE FROM Field$ WHERE id=5001004 -- MoStemMsa_ExceptionFeatures
GO
---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200097
begin
	update Version$ set DbVer = 200098
	COMMIT TRANSACTION
	print 'database updated to version 200098'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200097 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
