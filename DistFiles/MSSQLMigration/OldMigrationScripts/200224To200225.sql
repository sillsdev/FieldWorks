-- Update database from version 200224 to 200225
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- Fix picture and media folders in database
-- Due to bad code since FW5.0 we can have multiple
-- picture and sound folders with names in English, Chinese, or French
-- and some may be owned in the wrong property on LangProject
-------------------------------------------------------------------------------
declare @PictureFolder int, @MediaFolder int, @id int,
  @nvId nvarchar(20), @en int, @lp int
select @en = id from LgWritingSystem where IcuLocale = 'en'
select @lp = id from LangProject
-- If we have any pictures, create a New Picture Folder
-- and move all picture CmFiles into that folder
if (select count(id) from CmPicture) <> 0
begin
  exec MakeObj_CmFolder @en, 'Local Pictures', null, null, null, @lp, 6001048, null, @PictureFolder output, null
  update CmFolder_Files set src = @PictureFolder
	where dst in (select PictureFile from CmPicture)
end
-- If we have any media, create a New Media Folder
-- and move all media CmFiles into that folder
if (select count(id) from CmMedia) <> 0
begin
  exec MakeObj_CmFolder @en, 'Local Media', null, null, null, @lp, 6001051, null, @MediaFolder output, null
  update CmFolder_Files set src = @MediaFolder
	where dst in (select MediaFile from CmMedia)
end
-- Now get rid of all old CmFolders
declare @hvo int, @nvchvo nvarchar(20)
declare mycursor cursor local static forward_only read_only for
	select id from CmFolder_ where id not in (@PictureFolder, @MediaFolder)
open mycursor
fetch next from mycursor into @hvo
while @@fetch_status = 0
begin
	set @nvchvo = @hvo
	exec deleteobjects @nvchvo
	fetch next from mycursor into @hvo
end
close mycursor
deallocate mycursor

go

---------------------------------------------------------------------------------
---- Finish or roll back transaction as applicable
---------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200224
BEGIN
	UPDATE Version$ SET DbVer = 200225
	COMMIT TRANSACTION
	PRINT 'database updated to version 200225'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200224 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
