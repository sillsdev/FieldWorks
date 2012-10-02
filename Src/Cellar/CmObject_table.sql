-- CmObject table.

if object_id('CmObject') is not null begin
	print 'removing table CmObject'
	drop table [CmObject]
end
go
print 'creating table CmObject'
create table [CmObject] (
	[Id] 		int 				primary key clustered identity(1,1),
	[Guid$] 	uniqueidentifier not null 	unique default newid(),
	[Class$] 	int 		not null	references [Class$] ([Id]),
	[Owner$] 	int 		null		references [CmObject] ([Id]),
	[OwnFlid$] 	int 		null		references [Field$] ([Id]),
	[OwnOrd$] 	int 		null,
	[UpdStmp] 	timestamp,
	[UpdDttm]	smalldatetime	not null	default getdate(),

	-- an object cannot be owned by itself and if an object has an owner an owning flid
	--	must be specified
	constraint [_CK_CmObject_Owner$] check
		(([Owner$] is not null or [OwnFlid$] is null) and
			([Owner$] <> [Id]))
)

create nonclustered index Ind_CmObject_Owner$_OwnFlid$_OwnOrd$ on [CmObject] ([Owner$], [OwnFlid$], [OwnOrd$])
go

--( The indexes on ID and Guid$ are from the index tuning wizard, on
--( TestlangProj, April 19, 2005. The one one ID was modified based on
--( the SQL Server 2005 Tuning Advsior, on the startup of a right-to-left
--( language, February 6, 2007. A couple more indexes were added due to
--( the February analysis:

CREATE INDEX Ind_CmObject_ID ON CmObject (Id ASC) INCLUDE (Class$, Owner$)
GO

CREATE INDEX Ind_CmObject_Guid$ ON dbo.CmObject (Guid$)
GO

CREATE INDEX Ind_CmObject_OwnFlid$ ON CmObject (OwnFlid$ ASC) INCLUDE ( Id, Owner$, OwnOrd$)
GO

CREATE INDEX Ind_CmObject_Class$ ON CmObject (Class$ ASC) INCLUDE (Id, Owner$, OwnFlid$)
GO

-- REVIEW (SteveMiller, Feb 2007):
-- I am uncertain why the advisor wanted us to create thess views.
-- They're currently not in use anywhere (obviously), and I'm not
-- sure we want schemabinding. I include it here for future reference.
--
--
-- CREATE VIEW [dbo].[_dta_mv_26] WITH SCHEMABINDING AS
-- SELECT
--		[dbo].[CmObject].[Id] as _col_1,
--		[dbo].[CmObject].[UpdStmp] as _col_2,
--		[dbo ].[LexEntry].[HomographNumber] as _col_3,
--		[dbo].[LexEntry].[id] as _col_4
-- FROM  [dbo].[LexEntry], [dbo].[CmObject]
-- WHERE  [dbo].[LexEntry].[id] = [dbo].[CmObject].[Id]
--	GO
--
-- CREATE UNIQUE CLUSTERED INDEX [_dta_index__dta_mv_26_c_13_39723244__K4]
--	ON [dbo].[_dta_mv_26] ([_col_4] ASC)WITH (
--		SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF)
-- ON [PRIMARY]
--	GO
--
-- CREATE VIEW [dbo].[_dta_mv_12] WITH SCHEMABINDING AS
-- SELECT
--		[dbo].[CmObject].[Id] as _col_1,
--		[dbo].[CmObject].[UpdStmp] as _col_2
--		FROM  [dbo].[CmObject],  [dbo].[MoStemAllomorph]
--	WHERE  [dbo].[CmObject].[Id] = [dbo].[MoStemAllomorph].[id]
--	GO
--
-- CREATE UNIQUE CLUSTERED INDEX [_dta_index__dta_mv_12_c_13_1803205524__K1]
--	ON [dbo].[_dta_mv_12] ([_col_1] ASC)
--		WITH (SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF)
--	ON [PRIMARY]
--	GO


-- REVIEW (SteveMiller, MSDE era): An index on Id, Owner, and OwnOrd$ actually
-- causes an execution plan to use the index, rather than use the clustered index
-- on ID. This, in spite of the fact that the index tuner recommended putting
-- the index on (in previous research). The nonclustered index that was here
-- was removed.
