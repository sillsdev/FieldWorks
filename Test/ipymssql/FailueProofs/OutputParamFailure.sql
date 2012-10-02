/**************************************************************************
** Test stored procedure for OutputParamTestFailure.py
**
** See notes there in the Python script.
***************************************************************************/

create procedure Test
	@n1 int,
	@n2 nvarchar(4000),
	@n3 int,
	@n4 int output
as
begin
	-- Parameter 4 is the only output paramet
	set @n4 = @n4 + 1

	-- If these lines are remarked out, the output parameter will work fine.
	create table TestLangProj.dbo.x (y int)
	insert into x (y) values (1)
	select @n3 = y from TestLangProj.dbo.x
	drop table TestLangProj.dbo.x
end
