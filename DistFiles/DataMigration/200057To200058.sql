-- Update database from version 200056 to 200057
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

declare @owner int
set @owner = (select id from LexicalDatabase)
declare @idNormal int
declare @id int

exec CreateOwnedObject$ 17, @idNormal out, '6DA6D9AE-D627-4E67-9851-EBFBD6B9AF28', @owner, 5005016, 25, null, 0, 1, null
update StStyle set Name = 'Normal', Next=@idNormal, IsBuiltIn= 1, IsModified=1,
Rules= 0x1802440026FFFFFF000C00AE0800000000BA0800000000B20800000000AA0800000000B60800000000BC08008209010000004A010000001A01710200220000000008004E0100000062027102001E010000008008005A017701005601000000100052010000002A000000001400010F3C00640065006600610075006C0074002000730065007200690066003E0093022B0900FFFFFF000300000000000600000000000800000000000200000000000700000000000400000000000A00000000000500000000000100540069006D006500730020004E0065007700200052006F006D0061006E00
where id = @idNormal

exec CreateOwnedObject$ 17, @id out, 'DC69AFDC-2052-4245-A840-5052B668D540', @owner, 5005016, 25, null, 0, 1, null
update StStyle set Name = 'Heading 1', BasedOn = @idNormal, Next=@idNormal, IsBuiltIn= 1,
Rules = 0x04010C021A016B03005A81BB00005601EE020001143C00640065006600610075006C0074002000730061006E0073002000730065007200690066003E00
where id = @id

set @id = null
exec CreateOwnedObject$ 17, @id out, '0B1A9792-0FB6-4BF8-B899-7635F36CF8C1', @owner, 5005016, 25, null, 0, 1, null
update StStyle set Name = 'Heading 2', BasedOn = @idNormal, Next=@idNormal, IsBuiltIn= 1,
Rules = 0x05010C021A01EE020008025A81BB00005601EE020001143C00640065006600610075006C0074002000730061006E0073002000730065007200690066003E00
where id = @id

set @id = null
exec CreateOwnedObject$ 17, @id out, '71B2233D-8B14-42D5-A625-AAC8EDE7503B', @owner, 5005016, 25, null, 0, 1, null
update StStyle set Name = 'Heading 3', BasedOn = @idNormal, Next=@idNormal, IsBuiltIn= 1,
Rules = 0x04010C001A01EE02005A81BB00005601EE020001143C00640065006600610075006C0074002000730061006E0073002000730065007200690066003E00
where id = @id

set @id = null
exec CreateOwnedObject$ 17, @id out, '97509992-852B-45DA-9740-F51161CA40BD', @owner, 5005016, 25, null, 0, 1, null
update StStyle set Name = 'Emphasized Text', Type = 1, IsBuiltIn= 1,
Rules = 0x03001A0171020008025A01000000
where id = @id

set @id = null
exec CreateOwnedObject$ 17, @id out, '64FF6BBA-704F-4591-AD86-6104BC8722E9', @owner, 5005016, 25, null, 0, 1, null
update StStyle set Name = 'Internal Link', Type = 1, IsBuiltIn= 1,
Rules = 0x0300220000FF002A0000FF001403
where id = @id

set @id = null
exec CreateOwnedObject$ 17, @id out, 'B26F65A7-B0AB-4641-9A9A-F45AA7746389', @owner, 5005016, 25, null, 0, 1, null
update StStyle set Name = 'Language Code', Type = 1, IsBuiltIn= 1,
Rules = 0x02001A01F40100222F60FF00
where id = @id

set @id = null
exec CreateOwnedObject$ 17, @id out, '012C34B2-84C1-44F5-A0E3-2999A3860B73', @owner, 5005016, 25, null, 0, 1, null
update StStyle set Name = 'External Link', Type = 1, IsBuiltIn= 1,
Rules = 0x0300227F007F002A7F007F001403
where id = @id
---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200057
begin
	update Version$ set DbVer = 200058
	COMMIT TRANSACTION
	print 'database updated to version 200058'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200057 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO