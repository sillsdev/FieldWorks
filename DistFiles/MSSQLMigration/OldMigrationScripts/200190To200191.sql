-- Update database from version 200190 to 200191
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- FWM-137: Add Discourse Model
-------------------------------------------------------------------------------

--==( New Classes )==--

--( Add new classes
insert into Class$ ([Id], [Mod], [Base], [Abstract], [Name])
	values(5124, 5, 0, 0, 'DsDiscourseData')
go
exec UpdateClassView$ 5124, 1
go

insert into Class$ ([Id], [Mod], [Base], [Abstract], [Name])
	values(5122, 5, 5, 0, 'DsChart')
go
exec UpdateClassView$ 5122, 1
go

insert into Class$ ([Id], [Mod], [Base], [Abstract], [Name])
	values(5123, 5, 5122, 0, 'DsConstChart')
go
exec UpdateClassView$ 5123, 1
go

--( Add DsDiscourseData fields
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5124001, 23, 5124,
		8, 'ConstChartTemplates',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5124002, 25, 5124,
		5122, 'Charts',0,Null, null, null, null)
go
exec UpdateClassView$ 5124, 1
go

--( Add DsChart field
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5122001, 24, 5122,
		7, 'Template',0,Null, null, null, null)
go
exec UpdateClassView$ 5122, 1
go

--( Add DsConstChart
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5123001, 24, 5123,
		14, 'BasedOn',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5123002, 28, 5123,
		36, 'Rows',0,Null, null, null, null)
go
exec UpdateClassView$ 5123, 1
go

--==( Change CmIndirectAnnotation_AppliesTo )==--

--( Store off data existing in CmIndirectAnnotation.AppliesTo
create table #tblAppliesTo (
	[Src] [int],
	[Dst] [int])
go

insert into #tblAppliesTo
select src, dst
from CmIndirectAnnotation_AppliesTo
go

--( Out with the old
delete from Field$ where Id = 36001
go

--( In with the new
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(36001, 28, 36,
		34, 'AppliesTo',0,Null, null, null, null)
go

--select * from CmIndirectAnnotation_AppliesTo

declare @Ord int, @Src1 int, @Src2 int, @Dst int
set @Ord = 1

declare curAppliesTo cursor for
select Src, Dst
from #tblAppliesTo
order by Src

open curAppliesTo
fetch from curAppliesTo into @Src1, @Dst
set @Src2 = @Src1
while @@FETCH_STATUS = 0 begin
	insert into CmIndirectAnnotation_AppliesTo (Src, Dst, Ord)
		values (@Src1, @Dst, @Ord)

	fetch from curAppliesTo into @Src1, @Dst
	if @Src1 != @Src2
		set @Ord = 1
	else
		set @Ord = @Ord + 1

	set @Src2 = @Src1
end
close curAppliesTo
deallocate curAppliesTo
go

drop table #tblAppliesTo
go

--==(  Create Discourse Annotation Types (CCR and CCA under 'Discourse') )==--
declare @lp int, @list int, @en int, @root int, @item1 int, @item2 int, @now datetime, @guid uniqueidentifier, @fmt varbinary(4000)
select @now = getdate()
select top 1 @lp=id from LanguageProject
select @en=id from LgWritingSystem where ICULocale = 'en'
select top 1 @fmt=Fmt from MultiStr$ where Ws=@en order by Fmt
select @list=Dst from LanguageProject_AnnotationDefinitions

/*
[additional annotation defn flags]
DECLARE @CmAnnotationDefn_AllowsComment bit
DECLARE @CmAnnotationDefn_AllowsFeatureStructure bit
DECLARE @CmAnnotationDefn_AllowsInstanceOf bit
DECLARE @CmAnnotationDefn_InstanceOfSignature int
DECLARE @CmAnnotationDefn_UserCanCreate bit
DECLARE @CmAnnotationDefn_CanCreateOrphan bit
DECLARE @CmAnnotationDefn_PromptUser bit
DECLARE @CmAnnotationDefn_CopyCutPastable bit
DECLARE @CmAnnotationDefn_ZeroWidth bit
DECLARE @CmAnnotationDefn_Multi bit
DECLARE @CmAnnotationDefn_Severity int
*/

exec CreateObject_CmAnnotationDefn
	@en,		--( English Writing System for Name
	N'Discourse', --( Name
	@en,		--( English Writing System for Abbreviation
	'DCC',		--( Abbreviation
	@en,		--( English Writing System for Description
	'A chart component for discourse analysis',
	@fmt,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 1,	--( UnderStyle, Hidden, Protected
	0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, --(see above for additional AnnotationDefn flags)
	@list,		--( Owner
	8008,		--( OwnFlid
	NULL,		--( StartObj
	@root output, @guid output
update CmObject set Guid$ = 'A39A1272-38A0-4354-BDAC-8636D64C1EEC' where id = @root

exec CreateObject_CmAnnotationDefn
	@en,		--( English Writing System for Name
	N'Constituent Chart Row', --( Name
	@en,		--( English Writing System for Abbreviation
	'CCR',		--( Abbreviation
	@en,		--( English Writing System for Description
	'A row for a discourse constituent chart',
	@fmt,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 1,	--( UnderStyle, Hidden, Protected
	0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, --(see above for additional AnnotationDefn flags)
	@root,		--( Owner
	7004,		--( OwnFlid
	NULL,		--( StartObj
	@item1 output, @guid output
update CmObject set Guid$ = '50C1A53D-925D-4F55-8ED7-64A297905346' where id = @item1

exec CreateObject_CmAnnotationDefn
	@en,		--( English Writing System for Name
	N'Constituent Chart Annotation', --( Name
	@en,		--( English Writing System for Abbreviation
	'CCA',		--( Abbreviation
	@en,		--( English Writing System for Description
	'A cell in a row for a discourse constituent chart',
	@fmt,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 1,	--( UnderStyle, Hidden, Protected
	0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, --(see above for additional AnnotationDefn flags)
	@root,		--( Owner
	7004,		--( OwnFlid
	NULL,		--( StartObj
	@item2 output, @guid output
update CmObject set Guid$ = 'EC0A4DAD-7E90-4E73-901A-21D25F0692E3' where id = @item2

go

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200190
BEGIN
	UPDATE Version$ SET DbVer = 200191
	COMMIT TRANSACTION
	PRINT 'database updated to version 200191'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200190 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
