if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[tsu_describe]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[tsu_describe]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[tsu__private_addError]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[tsu__private_addError]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[tsu__private_addFailure]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[tsu__private_addFailure]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[tsu__private_createTestResult]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[tsu__private_createTestResult]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[tsu__private_showTestResult]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[tsu__private_showTestResult]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[tsu_error]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[tsu_error]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[tsu_failure]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[tsu_failure]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[tsu_runTestSuite]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[tsu_runTestSuite]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[tsu_runTests]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[tsu_runTests]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[tsu_showTestResults]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[tsu_showTestResults]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[tsuActiveTest]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[tsuActiveTest]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[tsuErrors]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[tsuErrors]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[tsuFailures]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[tsuFailures]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[tsuLastTestResult]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[tsuLastTestResult]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[tsuTestResults]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[tsuTestResults]
GO
