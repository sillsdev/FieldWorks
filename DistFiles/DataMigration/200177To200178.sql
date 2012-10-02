-- update database FROM version 200177 to 200178
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
--FWM-127
--Add Publication.BaseFontSize : integer
--Add Publication.BaseLineSpacing : integer

--FWM-128
--Add CmResource : class (CmObject)
--Add CmResource.Name : unicode
--Add CmResource.Version : guid
--Add Scripture.Resources : owned collection of CmResource
--Migrate Scripture.StylesheetVersion to Scripture.Resources
--Remove Scripture.StylesheetVersion

--FWM-129
--Add LexicalDatabase.Resources : owned collection of CmResource
--Migrate LexicalDatabase.StylesheetVersion to LexicalDatabase.Resources
--Remove LexicalDatabase.StylesheetVersion

-------------------------------------------------------------------------------

-------------------------------------------------------------------------------
--Changes to Publication for FWM-127
-------------------------------------------------------------------------------
--To Publication (42)
--	BaseFontSize : integer (15)
--	BaseLineSpacing : integer (16)

	INSERT INTO [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(42015, 2, 42, null, 'BaseFontSize', 0, Null, null, null, null)
GO
	INSERT INTO [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(42016, 2, 42, null, 'BaseLineSpacing', 0, Null, null, null, null)
GO
	EXEC UpdateClassView$ 42, 1
GO


-------------------------------------------------------------------------------
--Add CmResource for FWM-128
-------------------------------------------------------------------------------

--Add class CmResource (70) and its members
	-- Add CmResource.Name : unicode (1)
	-- Add CmResource.Version : guid (2)

	INSERT INTO Class$ ([Id], [Mod], [Base], [Abstract], [Name])
		values(70, 0, 0, 0, 'CmResource')
GO
	INSERT INTO [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(70001, 15, 70, null, 'Name', 0, Null, null, null, null)
GO
	INSERT INTO [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(70002, 6, 70, null, 'Version', 0, Null, null, null, null)
GO

-------------------------------------------------------------------------------
--Changes to Scripture for FWM-128
-------------------------------------------------------------------------------
--Add Scripture.Resources (3001033) : owned collection (25) of CmResource (70)


	INSERT INTO [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(3001033, 25, 3001, 70, 'Resources', 0, Null, null, null, null)
GO

--Remove Scripture.StylesheetVersion : guid (3001021)
DELETE FROM [Field$] WHERE Id = 3001021
GO


EXEC UpdateClassView$ 3001
GO

-------------------------------------------------------------------------------
--Changes to LexicalDatabase for FWM-129
-------------------------------------------------------------------------------
--Add LexicalDatabase.Resources (5005021) : owned collection (25) of CmResource (70)


	INSERT INTO [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(5005021, 25, 5005, 70, 'Resources', 0, Null, null, null, null)
GO

--Migrate LexicalDatabase.StylesheetVersion to LexicalDatabase.Resources

declare @id int
declare @owner int
declare @version uniqueidentifier
declare @objid uniqueidentifier
select @owner = id, @version = StylesheetVersion from LexicalDatabase

set @id = null
set @objid = newid()
if not exists (select * from CmResource where Name = 'FlexStyles')
begin
exec CreateOwnedObject$ 70, @id out, @objid, @owner, 5005021, 25, null, 0, 1, null
update CmResource set Name = 'FlexStyles', Version = @version where id = @id
end

GO

--Remove LexicalDatabase.StylesheetVersion : guid (5005020)
DELETE FROM [Field$] WHERE Id = 5005020
GO


EXEC UpdateClassView$ 5005
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
DECLARE @dbVersion int
SELECT @dbVersion = [DbVer] FROM [Version$]
IF @dbVersion = 200177
BEGIN
	UPDATE [Version$] SET [DbVer] = 200178
	COMMIT TRANSACTION
	PRINT 'database updated to version 200178'
END

ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200177 (DbVer = ' +
		convert(varchar, @dbVersion) + ')'
END
GO
