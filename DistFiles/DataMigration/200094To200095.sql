-- update database FROM version 200094 to 200095
BEGIN TRANSACTION  --( will be rolled back if wrong version#

declare @flid int
declare @clid int
declare @sql nvarchar(4000)
declare @sClass nvarchar(4000)
declare @sName nvarchar(4000)
declare @ws int
declare @fmt varbinary(1000)
Select @flid= min([id]) from Field$ where Custom = 1 and Type = 15
while @flid is not null begin
	set @clid = (select class from Field$ where id = @flid)
	set @sName = (select [Name] from Field$ where id = @flid)
	set @sClass = (select cls.Name from Class$ cls join Field$ f on f.id = @flid and f.class = cls.id)
	--- add the _Fmt field
	set @sql = 'ALTER TABLE [' + @sClass + '] ADD [' + @sName + '_Fmt] VARBINARY(8000) NULL'
	exec (@sql)
	-- update the Field$ entry (Todo: overcome constraint)
	alter table Field$ disable trigger TR_Field$_No_Upd
	update Field$ set type = 13 where id = @flid
	alter table Field$ enable trigger TR_Field$_No_Upd
	-- figure out the writing system we want to use
	set @ws = (select WsSelector from Field$ where id = @flid)
	if (@ws = -1) begin
		set @ws = (select top 1 Dst from LanguageProject_CurrentAnalysisWritingSystems order by ord)
	end
	else begin
		set @ws = (select top 1 Dst from LanguageProject_CurrentVernacularWritingSystems order by ord)
	end
	-- add a dummy Fmt value to every row, something like
	-- 0x0100000000000000000000000100069E970000
	-- 0x0100000000000000000000000100069e970000
	-- which breaks down as: 01000000 00000000 00000000 01 00 06 9E970000
	-- 01000000 - 4 bytes byte, says one run in string.
	-- 00000000 - 4 bytes, says first run starts at offset 0 (as always)
	-- 00000000 - 4 bytes, says fmt info for first run starts at offet 0 in props area (as always)
	-- 01 indicates one integer-valued property specified for the first run
	-- 00 indicate no string-valued proeperties specifeid for the first run
	-- 06 - in a complex way, this indicates that the following 4 bytes are the ID of the writing system.
	-- 9E970000 - ID of writing system, least significant byte first.
	set @sql = 'UPDATE [' + @sClass + '] set [' + @sName + '_Fmt] = 0x010000000000000000000000010006'
		+ substring(master.dbo.fn_varbintohexstr(dbo.AlignBinary(convert(varbinary(4),@ws))), 3, 8)
	exec (@sql)

	exec UpdateClassView$ @clid, 1

	-- move on to any remaining relevant fields.
	Select @flid= min([id]) from Field$ where Custom = 1 and Type = 15
end
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200094
begin
	UPDATE Version$ SET DbVer = 200095
	COMMIT TRANSACTION
	print 'database updated to version 200095'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200094 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
