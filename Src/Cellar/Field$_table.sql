-- Field$ table.
if object_id('Field$') is not null begin
	print 'removing proc Field$'
	drop table [Field$]
end
go
print 'creating table Field$'
create table [Field$] (
	[Id]		int				primary key clustered,
	[Type]		int		not null,
	[Class]		int		not null	references [Class$] ([Id]),
	[DstCls]	int		null		references [Class$] ([Id]),
	[Name] 		nvarchar(kcchMaxName) not null,
	[Custom] 	tinyint		not null 	default 1,
	[CustomId] 	uniqueidentifier null		default newid(),
	[Min]		bigint		null		default null,
	[Max]		bigint		null		default null,
	[Big]		bit		null		default 0,
	UserLabel	NVARCHAR(kcchMaxName) NULL DEFAULT NULL,
	HelpString	NVARCHAR(kcchMaxName) NULL DEFAULT NULL,
	ListRootId	INT NULL DEFAULT NULL,
	WsSelector	INT NULL,
	XmlUI		NTEXT NULL
	constraint [_UQ_Field$_Class_Fieldname]	unique ([class], [name]),
	constraint [_CK_Field$_DstCls]
		check (([Type] < kcptMinObj and [DstCls] is null) or
			([Type] >= kcptMinObj and [DstCls] is not null)),
	constraint [_CK_Field$_Custom]
		check (([Custom] = 0 and [CustomId] is null) or
			([Custom] = 1 and [CustomId] is not null)),
	constraint [_CK_Field$_Type_Integer]
		check (([Type] <> kcptInteger and [Min] is null and [Max] is null) or
			[Type] = kcptInteger),
	constraint [_CK_Field$_Type]
		check ([Type] In (	kcptBoolean,
					kcptInteger,
					kcptNumeric,
					kcptFloat,
					kcptTime,
					kcptGuid,
					kcptImage,
					kcptGenDate,
					kcptBinary,
					kcptString,
					kcptMultiString,
					kcptUnicode,
					kcptMultiUnicode,
					kcptBigString,
					kcptMultiBigString,
					kcptBigUnicode,
					kcptMultiBigUnicode,
					kcptOwningAtom,
					kcptReferenceAtom,
					kcptOwningCollection,
					kcptReferenceCollection,
					kcptOwningSequence,
					kcptReferenceSequence)),
	constraint [_CK_Field$_MinMax] check ((Max is null and min is null) or (Type = kcptInteger and Max >= Min))
)
create nonclustered index Ind_Field$_ClassType on Field$(Class, Type)
create nonclustered index ind_Field$_DstCls on Field$(DstCls)
go
