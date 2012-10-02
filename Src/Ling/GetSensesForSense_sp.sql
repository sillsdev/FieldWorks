if object_id('dbo.GetSensesForSense') is not null begin
	print 'removing proc GetSensesForSense'
	drop proc dbo.GetSensesForSense
end
print 'creating proc GetSensesForSense'
go
/*****************************************************************************
 * GetSensesForSense
 *
 * Description:
 *	Returns the senses associated with the specified sense
 *
 * Parameters:
 *	@SenseId=the object Id of the sense for which the associated senses
 *		should be returned
 *
 * Returns:
 *	0 if successful, otherwise the associated error code
 *****************************************************************************/
create proc dbo.GetSensesForSense
	@SenseId as integer
as
	declare @nCurDepth int, @rowCnt int, @str varchar(100)
	declare @fIsNocountOn int

	set @nCurDepth = 0

	declare @lexSenses table (
		ownrId	int,
		sensId	int,
		ord	int,
		depth	int,
		sensNum	nvarchar(1000)
	)

	-- deterimine if no count is currently set to on
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	insert into @lexSenses
	select 	Src, Dst, ord, @nCurDepth, convert(nvarchar(10), ord+1)
	from	lexSense_Senses
	where	Src = @SenseId

	set @rowCnt = @@rowcount
	while @rowCnt > 0
	begin
		set @nCurDepth = @nCurDepth + 1

		insert into @lexSenses
		select 	lst.Src, lst.Dst, lst.ord, @nCurDepth, sensNum+'.'+replicate(' ', 5-len(convert(nvarchar(10), lst.ord+1)))+convert(nvarchar(10), lst.ord+1)
		from	@lexSenses ls
		join lexSense_Senses lst on ls.sensId = lst.Src
		where	depth = @nCurDepth - 1

		set @rowCnt = @@rowcount
	end

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	select 	*
	from 	@lexSenses
	order by sensNum
go
