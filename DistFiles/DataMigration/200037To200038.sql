-- update database from version 200037 to 200038
BEGIN TRANSACTION


IF OBJECT_ID('GetSegmentIndex') IS NOT NULL BEGIN
	if (select DbVer from Version$) = 200005
		PRINT 'removing proc GetSegmentIndex'
	DROP PROC GetSegmentIndex
END
GO
if (select DbVer from Version$) = 200005
	PRINT 'creating proc GetSegmentIndex'
GO
create  proc GetSegmentIndex
	@hvo int, @segDefn int

as
begin
	select count(cbaSeg.id) from CmBaseAnnotation_ cbaSeg
	join CmBaseAnnotation cbaWf
		on cbaWf.id = @hvo and cbaWf.BeginObject = cbaSeg.BeginObject
			and cbaSeg.AnnotationType = @segDefn
			and cbaSeg.BeginOffset <= cbaWf.BeginOffset
	end
GO

declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200037
begin
	update Version$ set DbVer = 200038
	COMMIT TRANSACTION
	print 'database updated to version 200038'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200037 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO