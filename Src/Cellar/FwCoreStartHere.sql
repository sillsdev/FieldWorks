set nocount on

/***********************************************************************************************
	Note: These declarations need to be ordered, such that if stored procedure X
	calls stored procedure Y, then X should be created first, then Y.
	Doing this avoids an error message like the following:

creating proc FindOrCreateWfiAnalysis
Cannot add rows to sysdepends for the current stored procedure because it depends on the
missing object 'ChkDupWFAnalysis'. The stored procedure will still be created.
***********************************************************************************************/
