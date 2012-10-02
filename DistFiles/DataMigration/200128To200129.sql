-- update database FROM version 200128 to 200129

BEGIN TRANSACTION  --( will be rolled back if wrong version#

-------------------------------------------------------------------------------
-- TE-4101: Correct GUID representing Translator Note annotation type in filter.
-------------------------------------------------------------------------------
BEGIN
	DECLARE	@format varbinary(8000),
		@hvoEnglish int,
		@hvoHexEnglish binary(4),
		@byte1 int,
		@byte2 int,
		@byte3 int,
		@byte4 int

	SELECT	@hvoEnglish = [id]
	FROM	LgWritingSystem
	WHERE	[ICULocale] = 'en'

	SET @byte1 = @hvoEnglish / CONVERT(int, 0x1000000)
	SET @byte2 = (@hvoEnglish - @byte1 * 0x1000000) / CONVERT(int, 0x10000)
	SET @byte3 = (@hvoEnglish - @byte1 * 0x1000000 - @byte2 * 0x10000) / CONVERT(int, 0x100)
	SET @byte4 = @hvoEnglish - @byte1 * 0x1000000 - @byte2 * 0x10000 - @byte3 * 0x100
	-- Format fields require byte order to be reversed
	SET @hvoHexEnglish =	CONVERT(binary(1), @byte4) +
					CONVERT(binary(1), @byte3) +
				CONVERT(binary(1), @byte2) +
				CONVERT(binary(1), @byte1)

	SET	@format = 0x0200000000000000000000000800000007000000010006 + @hvoHexEnglish + 0x010106 + @hvoHexEnglish + 0x06090300
	UPDATE CmCell
	SET Contents_Fmt=@format + 0x2957AE80D89C4D428E7196C1A8FD5821
	where id = (select c.id from CmCell_ c
		join cmrow_ r on c.Owner$ = r.id
		join cmfilter f on r.Owner$ = f.id
		WHERE f.Name='Translator'
		and App='A7D421E1-1DD3-11D5-B720-0010A4B54856')
END


-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200128
begin
	UPDATE Version$ SET DbVer = 200129
	COMMIT TRANSACTION
	print 'database updated to version 200129'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200128 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
