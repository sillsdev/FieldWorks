-- Update database from version 200191 to 200192
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- Add Discourse Chart Markers and Constituent Chart Templates CmPossibilityLists
-------------------------------------------------------------------------------

--( Change DsChart to Abstract
update Class$ set Abstract=1 where id=5122
exec UpdateClassView$ 5122, 1
go

-- SteveMiller says we need to also remove the stored proc CreateObject_DsChart
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CreateObject_DsChart]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[CreateObject_DsChart]
go

--( Add LanguageProject_DiscourseData
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(6001053, 23, 6001,
		5124, 'DiscourseData',0,Null, null, null, null)
go
exec UpdateClassView$ 1, 1
go

--( Add DsDiscourseData_ChartMarkers
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5124003, 23, 5124,
		8, 'ChartMarkers',0,Null, null, null, null)
go
exec UpdateClassView$ 5124, 1
go

--==(  Create Initial ChartMarker possibility list )==--
declare @markerList int, @en int, @now datetime, @guid uniqueidentifier,
		@DiscData int, @lp int
select @now = getdate()
select @en=id from LgWritingSystem where ICULocale = 'en'
select @markerList=Dst from DsDiscourseData_ChartMarkers
select @lp=id from LanguageProject


--( Add DsDiscourseData
EXEC CreateObject_DsDiscourseData
	@lp,	-- @owner
	6001053,	-- @OwnFlid
	null,	-- @StartObj
	@DiscData output,	-- @NewObjId
	null,	-- @NewObjGuid
	0,	-- @fReturnTimestamp
	null	-- @NewObjTimstamp


--( Add ChartMarkers PossibilityList
EXEC CreateObject_CmPossibilityList
	@en,				-- @CmMajorObject_Name_ws
	N'Chart Markers',	-- @CmMajorObject_Name_txt nvarchar(4000)
	@now,				-- @CmMajorObject_DateCreated datetime
	@now,				-- @CmMajorObject_DateModified datetime
	null,				-- @CmMajorObject_Description_ws int
	null,				-- @CmMajorObject_Description_txt ntext
	null,				-- @CmMajorObject_Description_fmt image
	127,				-- @CmPossibilityList_Depth int (> 1 == Hierarchical)
	0,					-- @CmPossibilityList_PreventChoiceAboveLevel int
	0,					-- @CmPossibilityList_IsSorted bit
	0,					-- @CmPossibilityList_IsClosed bit
	0,					-- @CmPossibilityList_PreventDuplicates bit
	0,					-- @CmPossibilityList_PreventNodeChoices bit
	@en,				-- @CmPossibilityList_Abbreviation_ws int
	N'ChMrkrs',			-- @CmPossibilityList_Abbreviation_txt nvarchar(4000)
	null,				-- @CmPossibilityList_HelpFile nvarchar(4000)
	0,					-- @CmPossibilityList_UseExtendedFields bit
	0,					-- @CmPossibilityList_DisplayOption int
	7,					-- @CmPossibilityList_ItemClsid int (7 == CmPossibility)
	0,					-- @CmPossibilityList_IsVernacular bit
	-3,					-- @CmPossibilityList_WsSelector int
	null,				-- @CmPossibilityList_ListVersion uniqueidentifier
	@DiscData,			-- @Owner int (DsDiscourseData)
	5124003,			-- @OwnFlid int (DsDiscourseData_ChartMarkers)
	null,				-- @StartObj int
	@markerList output,	-- @NewObjId int
	null,				-- @NewObjGuid uniqueidentifier
	0,					-- @fReturnTimestamp tinyint
	null				-- @NewObjTimestamp int

--( Add TenseAspectMood tree
declare @TenseAspectMood int

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'TenseAspectMood', --( Name
	@en,		--( English Writing System for Abbreviation
	'TenseAspectMood',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@markerList,--( Owner
	8008,		--( OwnFlid (PossibilityList_Possibilities)
	NULL,		--( StartObj
	@TenseAspectMood output, @guid output

-- @TenseAspectMood
declare		@directImperative int,
				@imps int, @impp int
exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Direct imperative', --( Name
	@en,		--( English Writing System for Abbreviation
	'Direct imperative',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@TenseAspectMood,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@directImperative output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Direct imperative', --( Name
	@en,		--( English Writing System for Abbreviation
	'IMP.S',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@directImperative,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@imps output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Direct imperative', --( Name
	@en,		--( English Writing System for Abbreviation
	'IMP.P',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@directImperative,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@impp output, @guid output

-- @TenseAspectMood
declare		@subjunctive int,
				@sbv1 int, @sbv2 int
exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Subjunctive', --( Name
	@en,		--( English Writing System for Abbreviation
	'Subjunctive',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@TenseAspectMood,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@subjunctive output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Polite command', --( Name
	@en,		--( English Writing System for Abbreviation
	'SBV',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@subjunctive,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@sbv1 output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Purpose', --( Name
	@en,		--( English Writing System for Abbreviation
	'SBV',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@subjunctive,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@sbv2 output, @guid output

-- @TenseAspectMood
declare		@potentualCond int,
				@pot int, @cnd int

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Potential/conditional', --( Name
	@en,		--( English Writing System for Abbreviation
	'Potential/conditional',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@TenseAspectMood,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@potentualCond output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Potential', --( Name
	@en,		--( English Writing System for Abbreviation
	'POT',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@potentualCond,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@pot output, @guid output

declare @name nvarchar(100)
set @name = N'(w/' + nchar(236) + N'r' + nchar(237) + N') conditional'
exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	@name, --( Name
	@en,		--( English Writing System for Abbreviation
	'CND',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@potentualCond,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@cnd output, @guid output

-- @TenseAspectMood
declare		@potentualCondCf int,
				@potcf int, @cndcf int, @potcfp2 int, @cndcfp2 int, @cndcfp3 int,
				@cndrs int, @cndtl int, @yetcnd int
exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Potential/conditional, contrary-to-fact', --( Name
	@en,		--( English Writing System for Abbreviation
	'Potential/conditional, contrary-to-fact',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@TenseAspectMood,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@potentualCondCf output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Potential, C.F.', --( Name
	@en,		--( English Writing System for Abbreviation
	'POT.C.F',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@potentualCondCf,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@potcf output, @guid output

set @name = N'(w/ng' + nchar(225) + N') conditional, C.F.'
exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	@name, --( Name
	@en,		--( English Writing System for Abbreviation
	'CND.C.F',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@potentualCondCf,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@cndcf output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Potential, C.F. Past', --( Name
	@en,		--( English Writing System for Abbreviation
	'POT.C.F P2',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@potentualCondCf,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@potcfp2 output, @guid output

set @name = N'(w/ng' + nchar(225) + N') conditional, C.F. Past'
exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	@name, --( Name
	@en,		--( English Writing System for Abbreviation
	'CND C.F P2',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@potentualCondCf,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@cndcfp2 output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Conditional, C.F. Past', --( Name
	@en,		--( English Writing System for Abbreviation
	'CND C.F P2',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@potentualCondCf,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@cndcfp3 output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'If/when', --( Name
	@en,		--( English Writing System for Abbreviation
	'CND.RS',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@potentualCondCf,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@cndrs output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'If/when', --( Name
	@en,		--( English Writing System for Abbreviation
	'CND.TL',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@potentualCondCf,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@cndtl output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'If/when', --( Name
	@en,		--( English Writing System for Abbreviation
	'YET.CND',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@potentualCondCf,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@yetcnd output, @guid output

-- @TenseAspectMood
declare		@timeless int,
				@tl int
exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Timeless', --( Name
	@en,		--( English Writing System for Abbreviation
	'TL',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@TenseAspectMood,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@timeless output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Timeless', --( Name
	@en,		--( English Writing System for Abbreviation
	'Timeless',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@timeless,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@tl output, @guid output

-- @TenseAspectMood
declare		@temporalSequence int,
				@sq int, @sbsc int, @sbscdst int

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Temporal sequence', --( Name
	@en,		--( English Writing System for Abbreviation
	'Temporal sequence',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@TenseAspectMood,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@temporalSequence output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Sequential', --( Name
	@en,		--( English Writing System for Abbreviation
	'SQ',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@temporalSequence,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@sq output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Subsecutive', --( Name
	@en,		--( English Writing System for Abbreviation
	'SBSC',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@temporalSequence,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@sbsc output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Subsecutive distal', --( Name
	@en,		--( English Writing System for Abbreviation
	'SBSC.DST',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@temporalSequence,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@sbscdst output, @guid output

-- @TenseAspectMood
declare		@past int,
				@p1 int, @rsfrus int, @p1frus int, @prevprog int, @prevints int,
				@p2 int, @p2st int, @p2styet int, @p3 int

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Past', --( Name
	@en,		--( English Writing System for Abbreviation
	'Past',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@TenseAspectMood,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@past output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Past, recent', --( Name
	@en,		--( English Writing System for Abbreviation
	'P1',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@past,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@p1 output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Frustrated (RS)', --( Name
	@en,		--( English Writing System for Abbreviation
	'RS.FRUS',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@past,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@rsfrus output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Frustrated (P1)', --( Name
	@en,		--( English Writing System for Abbreviation
	'P1.FRUS',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@past,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@p1frus output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Previous, progressive', --( Name
	@en,		--( English Writing System for Abbreviation
	'PREV PROG',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@past,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@prevprog output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Previous, intentional', --( Name
	@en,		--( English Writing System for Abbreviation
	'PREV INTS',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@past,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@prevints output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Past, before yesterday', --( Name
	@en,		--( English Writing System for Abbreviation
	'P2',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@past,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@p2 output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Past, before yest., state', --( Name
	@en,		--( English Writing System for Abbreviation
	'P2.ST',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@past,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@p2st output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Past, before yest., unrealized exp.', --( Name
	@en,		--( English Writing System for Abbreviation
	'P2.ST-YET',	--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@past,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@p2st output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Remote past', --( Name
	@en,		--( English Writing System for Abbreviation
	'P3',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@past,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@p3 output, @guid output

-- @TenseAspectMood
declare		@present int,
				@prog int, @persprog int, @newlyprog int, @progints int, @rs int, @negyet int

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Present', --( Name
	@en,		--( English Writing System for Abbreviation
	'Present',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@TenseAspectMood,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@present output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Progressive state', --( Name
	@en,		--( English Writing System for Abbreviation
	'PROG',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@present,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@prog output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Persistive progressive state', --( Name
	@en,		--( English Writing System for Abbreviation
	'PERS PROG',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@present,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@persprog output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'New progressive state', --( Name
	@en,		--( English Writing System for Abbreviation
	'NEWLY PROG',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@present,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@newlyprog output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Intentional progressive state', --( Name
	@en,		--( English Writing System for Abbreviation
	'PROG INTS',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@present,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@progints output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Resultative state', --( Name
	@en,		--( English Writing System for Abbreviation
	'RS',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@present,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@rs output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Unrealized expectation', --( Name
	@en,		--( English Writing System for Abbreviation
	'NEG-YET',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@present,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@negyet output, @guid output

-- @TenseAspectMood
declare		@future int,
				@f1 int, @newlyf1 int, @f2 int, @f2int int, @newlyf2 int, @f3 int

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Future', --( Name
	@en,		--( English Writing System for Abbreviation
	'Future',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@TenseAspectMood,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@future output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Immediate', --( Name
	@en,		--( English Writing System for Abbreviation
	'F1',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@future,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@f1 output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Future, immediate, newly', --( Name
	@en,		--( English Writing System for Abbreviation
	'NEWLY F1',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@future,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@newlyf1 output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Future, unmarked', --( Name
	@en,		--( English Writing System for Abbreviation
	'F2',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@future,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@f2 output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Intentional', --( Name
	@en,		--( English Writing System for Abbreviation
	'F2',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@future,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@f2int output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Future, newly', --( Name
	@en,		--( English Writing System for Abbreviation
	'NEWLY F2',	--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@future,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@newlyf2 output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Remote future', --( Name
	@en,		--( English Writing System for Abbreviation
	'F3',	--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@future,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@f3 output, @guid output

--( Add Pronouns tree
declare @Pronouns int,
			@pctr int, @palt int, @pexcl int, @padd int
exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Pronouns', --( Name
	@en,		--( English Writing System for Abbreviation
	'Pronouns',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@markerList,--( Owner
	8008,		--( OwnFlid (PossibilityList_Possibilities)
	NULL,		--( StartObj
	@Pronouns output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Contrastive', --( Name
	@en,		--( English Writing System for Abbreviation
	'P:Ctr',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@Pronouns,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@pctr output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Alternative', --( Name
	@en,		--( English Writing System for Abbreviation
	'P:Alt',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@Pronouns,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@palt output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Exclusive', --( Name
	@en,		--( English Writing System for Abbreviation
	'P:Excl',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@Pronouns,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@pexcl output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Additive', --( Name
	@en,		--( English Writing System for Abbreviation
	'P:Add',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@Pronouns,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@padd output, @guid output

--( Add Demonstatives tree
declare @Demonstratives int,
			@dpc int, @dpr int, @dne int, @ddi int, @dre int, @dempc int, @dempr int,
			@dsaset int, @demdi int, @ddiset int, @demne int, @demre int
exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Demonstratives', --( Name
	@en,		--( English Writing System for Abbreviation
	'Demonstratives',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@markerList,--( Owner
	8008,		--( OwnFlid (PossibilityList_Possibilities)
	NULL,		--( StartObj
	@Demonstratives output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Proximal Contrastive', --( Name
	@en,		--( English Writing System for Abbreviation
	'D:P.C',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@Demonstratives,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@dpc output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Proximal', --( Name
	@en,		--( English Writing System for Abbreviation
	'D:Pr',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@Demonstratives,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@dpr output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Nearby', --( Name
	@en,		--( English Writing System for Abbreviation
	'D:Ne',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@Demonstratives,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@dne output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Distal', --( Name
	@en,		--( English Writing System for Abbreviation
	'D:Di',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@Demonstratives,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@ddi output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Remote', --( Name
	@en,		--( English Writing System for Abbreviation
	'D:Re',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@Demonstratives,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@dre output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Emphatic Proximal Contrastive', --( Name
	@en,		--( English Writing System for Abbreviation
	'D:Em.P.C',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@Demonstratives,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@dempc output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Emphatic Proximal', --( Name
	@en,		--( English Writing System for Abbreviation
	'D:Em.Pr',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@Demonstratives,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@dempr output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Emphatic Nearby', --( Name
	@en,		--( English Writing System for Abbreviation
	'D:Em.Ne',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@Demonstratives,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@demne output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Emphatic Distal', --( Name
	@en,		--( English Writing System for Abbreviation
	'D:Em.Di',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@Demonstratives,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@demdi output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Emphatic Remote', --( Name
	@en,		--( English Writing System for Abbreviation
	'D:Em.Re',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@Demonstratives,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@demre output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Same.set', --( Name
	@en,		--( English Writing System for Abbreviation
	'D:SaSet',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@Demonstratives,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@dsaset output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Different set', --( Name
	@en,		--( English Writing System for Abbreviation
	'D:DiSet',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@Demonstratives,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@ddiset output, @guid output

declare @TemplatesList int, @default int, @prenuclear int, @nucleus int, @postNuclear int
--( Add Consonant Chart Templates PossibilityList
EXEC CreateObject_CmPossibilityList
	@en,				-- @CmMajorObject_Name_ws
	N'Constituent Chart Templates',	-- @CmMajorObject_Name_txt nvarchar(4000)
	@now,				-- @CmMajorObject_DateCreated datetime
	@now,				-- @CmMajorObject_DateModified datetime
	null,				-- @CmMajorObject_Description_ws int
	null,				-- @CmMajorObject_Description_txt ntext
	null,				-- @CmMajorObject_Description_fmt image
	127,				-- @CmPossibilityList_Depth int (> 1 == Hierarchical)
	0,					-- @CmPossibilityList_PreventChoiceAboveLevel int
	0,					-- @CmPossibilityList_IsSorted bit
	0,					-- @CmPossibilityList_IsClosed bit
	0,					-- @CmPossibilityList_PreventDuplicates bit
	0,					-- @CmPossibilityList_PreventNodeChoices bit
	@en,				-- @CmPossibilityList_Abbreviation_ws int
	N'ConChrtTempl',			-- @CmPossibilityList_Abbreviation_txt nvarchar(4000)
	null,				-- @CmPossibilityList_HelpFile nvarchar(4000)
	0,					-- @CmPossibilityList_UseExtendedFields bit
	0,					-- @CmPossibilityList_DisplayOption int
	7,					-- @CmPossibilityList_ItemClsid int (7 == CmPossibility)
	0,					-- @CmPossibilityList_IsVernacular bit
	-3,					-- @CmPossibilityList_WsSelector int
	null,				-- @CmPossibilityList_ListVersion uniqueidentifier
	@DiscData,			-- @Owner int (DsDiscourseData)
	5124001,			-- @OwnFlid int (DsDiscourseData_ChartMarkers)
	null,				-- @StartObj int
	@TemplatesList output,	-- @NewObjId int
	null,				-- @NewObjGuid uniqueidentifier
	0,					-- @fReturnTimestamp tinyint
	null				-- @NewObjTimestamp int

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Default', --( Name
	@en,		--( English Writing System for Abbreviation
	'def',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@TemplatesList,--( Owner
	8008,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@default output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Pre-nuclear', --( Name
	@en,		--( English Writing System for Abbreviation
	'prenuc',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@default,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@prenuclear output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'EMP', --( Name
	@en,		--( English Writing System for Abbreviation
	'emp',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@prenuclear,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	null, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Outer', --( Name
	@en,		--( English Writing System for Abbreviation
	'out',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@prenuclear,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	null, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Inner', --( Name
	@en,		--( English Writing System for Abbreviation
	'in',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@prenuclear,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	null, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'nucleus', --( Name
	@en,		--( English Writing System for Abbreviation
	'nuc',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@default,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@nucleus output, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Subject', --( Name
	@en,		--( English Writing System for Abbreviation
	'subj',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@nucleus,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	null, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'Verb', --( Name
	@en,		--( English Writing System for Abbreviation
	'v',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@nucleus,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	null, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'O/[C]', --( Name
	@en,		--( English Writing System for Abbreviation
	'o/c',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@nucleus,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	null, @guid output

exec CreateObject_CmPossibility
	@en,		--( English Writing System for Name
	N'post-nuclear', --( Name
	@en,		--( English Writing System for Abbreviation
	'postnuc',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@default,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@postNuclear output, @guid output

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200191
BEGIN
	UPDATE Version$ SET DbVer = 200192
	COMMIT TRANSACTION
	PRINT 'database updated to version 200192'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200191 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
