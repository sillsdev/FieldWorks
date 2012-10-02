/***********************************************************************************************
	Final model creation processes for the Scripture domain.

	Note: These declarations need to be ordered, such that if stored procedure X
	calls stored procedure Y, then X should be created first, then Y.
	Doing this avoids an error message about dendencies.
***********************************************************************************************/

print '****************************** Loading ScriptureSP.sql ******************************'
go

SET NOCOUNT ON
