-- update database FROM version 200095 to 200096
BEGIN TRANSACTION  --( will be rolled back if wrong version#


/*****************************************************************************
 * MatchingEntries
 *
 * Description:
 *	Returns a table with zero or more rows of LexEntry objects which match in
 *	one or more of the parameters.  Set the string parameter to N'!' if you
 *  don't care about matching that particular item.  The @cf, @lf, and @af
 *  parameters are typically set to the same value.  Matching is always from
 *  the beginning of the string value, with any number of trailing characters
 *  allowed.
 * Parameters:
 *	@cf = the citation form to find.
 *	@lf = the lexeme form to find.
 *	@af = the allomorphs to find.
 *	@gl = the gloss to find.
 *	@wsv = vernacular ws for matching cf, lf, af
 *	@wsa = analysis ws for matching gl
 * Returns:
 *	Result set containing table of cf, lf, af, gl for each match, ordered by
 *  lf,cf,af,gl if @lf <> N'!', otherwise ordered by gl,lf,cf,af.
 *****************************************************************************/
if object_id('MatchingEntries') is not null begin
	print 'removing proc MatchingEntries'
	drop proc MatchingEntries
end
print 'creating proc MatchingEntries'
go

CREATE    proc [MatchingEntries]
	@cf nvarchar(4000),
	@lf nvarchar(4000),
	@af nvarchar(4000),
	@gl nvarchar(4000),
	@wsv int,
	@wsa int
AS
	declare @CFTxt nvarchar(4000), @cftext nvarchar(4000),
		@LFTxt nvarchar(4000), @lftext nvarchar(4000),
		@AFTxt nvarchar(4000), @aftext nvarchar(4000),
		@GLTxt nvarchar(4000), @gltext nvarchar(4000),
		@entryID int, @senseID int, @prevID int, @ObjId int,
		@class int, @UIWs int
	declare @fIsNocountOn int
	-- deterimine if no count is currently set to on
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- table variable to hold return information.
	declare @AllMatchingEntries table (
		EntryID int,			-- 1
		Class int,				-- 2
		CFTxt nvarchar(4000) default '***',	-- 3
		CFWs int,				-- 4
		LFTxt nvarchar(4000) default '***',	-- 5
		LFWs int,				-- 6
		AFTxt nvarchar(4000) default '***',	-- 7
		AFWs int,				-- 8
		GLTxt nvarchar(4000) default '***',	-- 9
		GLWs int				-- 10
		)

	-- table variable to hold return information.
	declare @MatchingEntries table (
		EntryID int,			-- 1
		Class int,				-- 2
		CFTxt nvarchar(4000) default '***',	-- 3
		CFWs int,				-- 4
		LFTxt nvarchar(4000) default '***',	-- 5
		LFWs int,				-- 6
		AFTxt nvarchar(4000) default '***',	-- 7
		AFWs int,				-- 8
		GLTxt nvarchar(4000) default '***',	-- 9
		GLWs int				-- 10
		)


	select top 1 @UIWs=id
	from LgWritingSystem (readuncommitted)
	where ICULocale='en'
	if @UIWs is null set @UIWs=@wsa


	INSERT INTO @AllMatchingEntries (EntryID, Class, CFTxt,CFWs, LFTxt,LFWs, AFTxt,AFWs, GLTxt,GLWs)
	SELECT le.Id 'EntryId', co.Class$ 'Class',
		isnull(cf.Txt, N'***') 'CFTxt', isnull(cf.Ws, @wsv) 'CFWs',
		isnull(lf.Txt, N'***') 'LFTxt', isnull(lf.Ws, @wsv) 'LFWs',
		isnull(af.Txt, N'***') 'AFTxt', isnull(af.Ws, @wsv) 'AFWs',
		isnull(gl.Txt, N'***') 'GLTxt', isnull(gl.Ws, @wsa) 'GLWs'
	FROM LexEntry le (readuncommitted)
	JOIN CmObject co (readuncommitted) ON co.Id = le.Id
	LEFT OUTER JOIN LexEntry_CitationForm cf (readuncommitted) ON cf.Obj=le.Id AND cf.Ws=@wsv
	LEFT OUTER JOIN LexEntry_LexemeForm lelf (readuncommitted) ON lelf.Src = le.Id
	LEFT OUTER JOIN MoForm_Form lf (readuncommitted) on lf.Obj = lelf.Dst AND lf.Ws=@wsv
	LEFT OUTER JOIN LexEntry_AlternateForms leaf (readuncommitted) ON leaf.Src = le.Id
	LEFT OUTER JOIN MoForm_Form af (readuncommitted) on af.Obj = leaf.Dst AND af.Ws=@wsv
	LEFT OUTER JOIN LexEntry_Senses les (readuncommitted) ON les.Src = le.Id
	LEFT OUTER JOIN LexSense_Gloss gl (readuncommitted) ON gl.Obj = les.Dst AND gl.Ws=@wsa
	WHERE LOWER(RTRIM(LTRIM(cf.Txt))) LIKE LOWER(RTRIM(LTRIM(@cf))) + '%'
	   OR LOWER(RTRIM(LTRIM(lf.Txt))) LIKE LOWER(RTRIM(LTRIM(@lf))) + '%'
	   OR LOWER(RTRIM(LTRIM(af.Txt))) LIKE LOWER(RTRIM(LTRIM(@af))) + '%'
	   OR LOWER(RTRIM(LTRIM(gl.Txt))) like LOWER(RTRIM(LTRIM(@gl))) + '%'
	ORDER BY le.Id, gl.Txt

	-- Eliminate duplicate rows
	INSERT INTO @MatchingEntries
		SELECT DISTINCT * FROM @AllMatchingEntries

	-- Set some wses to the uiws
	update @MatchingEntries
	set CFWs=@UIWs
	where CFTxt='***'
	update @MatchingEntries
	set LFWs=@UIWs
	where LFTxt = '***'

	--==( Near Final Pass )==--
	-- REVIEW (SteveMiller): This "final pass" can probably be enhanced by
	-- moving the logic into the query
	-- 	select * from @MatchingEntries
	-- Try to find some kind of string for any items that have not matched,
	declare @curFinalPass CURSOR, @rowcount int, @wsFoundling int
	set @curFinalPass = CURSOR FAST_FORWARD for
		select EntryID, AFTxt, GLTxt -- , CFTxt, LFTxt
		from @MatchingEntries
		where
			AFTxt = '***'
			or GLTxt = '***'
			-- or LFTxt = '***'
			-- or CFTxt = '***'

	OPEN @curFinalPass
	FETCH NEXT FROM @curFinalPass INTO @entryID, @aftext, @gltext -- , @cftext, @lftext
	WHILE @@FETCH_STATUS = 0
	BEGIN
		set @rowcount = 0
		--( Just leave the *** for missing CF, as per LT-2545.
		-- if @cftext = '***'
		--	update @MatchingEntries
		--	set CFWs=@UIWs
		--	where EntryID=@entryID
		--if @lftext = '***'
		--	update @MatchingEntries
		--	set LFWs=@UIWs
		--	where EntryID=@entryID
		if @aftext = '***'
		begin
			select top 1 @AFTxt=amf.Txt, @wsFoundling=@wsv
			from LexEntry_ le (readuncommitted)
			join LexEntry_AlternateForms lea (readuncommitted) ON lea.Src = le.id
			join MoForm_Form amf (readuncommitted) ON amf.Obj = lea.Dst and amf.ws = @wsv
			where le.id = @entryID
			set @rowcount = @@rowcount

			if @rowcount = 0 -- Nothing for the @wsv ws, so try all of them.
			begin
				SELECT top 1 @AFTxt=amf.Txt, @wsFoundling=amf.Ws
				from LexEntry_ le (readuncommitted)
				join LexEntry_AlternateForms lea (readuncommitted) ON lea.Src = le.id
				join MoForm_Form amf (readuncommitted) ON amf.Obj = lea.Dst
				join LanguageProject_CurrentVernacularWritingSystems ws (readuncommitted) ON ws.Dst = amf.Ws
				where le.id = @entryID
				ORDER BY ws.Ord
				set @rowcount = @@rowcount
			end

			if @rowcount > 0 -- Found one somewhere.
				update @MatchingEntries
				set AFTxt=@AFTxt, AFWs=@wsFoundling
				where EntryID=@entryID
			else
				update @MatchingEntries
				set AFWs=@UIWs
				where EntryID=@entryID
		end
		if @gltext = '***'
		begin
			SELECT top 1 @GLTxt=lsg.Txt, @wsFoundling=@wsa
			FROM dbo.fnGetSensesInEntry$(@entryID)
			join LexSense_Gloss lsg (readuncommitted) On lsg.Obj=SenseId and lsg.ws = @wsa
			set @rowcount = @@rowcount

			if @rowcount = 0 -- Nothing for the @wsa ws, so try all of them.
			begin
				SELECT top 1 @GLTxt=lsg.Txt, @wsFoundling=lsg.Ws
				FROM dbo.fnGetSensesInEntry$(@entryID)
				join LexSense_Gloss lsg (readuncommitted) On lsg.Obj=SenseId
				join LanguageProject_CurrentAnalysisWritingSystems ws (readuncommitted) ON ws.Dst = lsg.Ws
				ORDER BY ws.Ord
				set @rowcount = @@rowcount
			end

			if @rowcount > 0 -- Found one somewhere.
				update @MatchingEntries
				set GLTxt=@GLTxt, GLWs=@wsFoundling
				where EntryID=@entryID
			else
				update @MatchingEntries
				set GLWs=@UIWs
				where EntryID=@entryID
		end
		FETCH NEXT FROM @curFinalPass INTO @entryID, @aftext, @gltext -- , @cftext, @lftext
	END
	CLOSE @curFinalPass
	DEALLOCATE @curFinalPass

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	IF @lf <> N'!'
		SELECT * FROM @MatchingEntries ORDER BY LFTxt, CFTxt, AFTxt, GLTxt
	ELSE
		SELECT * FROM @MatchingEntries ORDER BY GLTxt, LFTxt, CFTxt, AFTxt
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200095
begin
	UPDATE Version$ SET DbVer = 200096
	COMMIT TRANSACTION
	print 'database updated to version 200096'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200095 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO