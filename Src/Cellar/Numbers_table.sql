/******************************************************************************
 * Numbers table
 *
 * Description:
 *	This is a support table, created to help with parsing a comma delimited
 *	list.
 *
 * Notes:
 *	The table replaces an iterative loop in SQL code with a single SELECT
 *	statement. I learned the technique from an article by Jeff Moden, "The
 *	'Numbers' or 'Tally' Table: What it is and how it replaces a loop."
 *	http://www.sqlservercentral.com/articles/TSQL/62867/. The code below
 *	is based upon the code in that article You can see the numbers table in
 *	use in our code at fnGetIdsFromString(). It could potentially have other
 *	uses as well.
 *****************************************************************************/

-- REVIEW (SteveMiller): The original code specifies using TempDb, a "DB that
-- everyone has where we can cause no harm." Fair enough, but I don't want to
-- risk confusing our build process.

--USE TempDB
SET NOCOUNT ON;

IF OBJECT_ID('dbo.Numbers') IS NOT NULL
	DROP TABLE Numbers;

--( All we are interested in here is creating a whole lot of rows, so we
--( can get a sequential list of 11,000 numbers (numbers 1 to 11,000).

--( Much to my surprise, we don't need a CREATE TABLE statement first.
--( SQL Server is smart enough to create a table based on the SELECT command.

SELECT TOP 11000 IDENTITY(INT,1,1) AS N
INTO Numbers
FROM Master..SysColumns sc1, Master..SysColumns sc2;

ALTER TABLE Numbers
	ADD CONSTRAINT PK_Numbers_N PRIMARY KEY CLUSTERED (N) WITH FILLFACTOR = 100;
GO
