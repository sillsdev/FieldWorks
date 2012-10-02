-- VERSION:         tsqlunit-0.9
-- COPYRIGHT:
--    Copyright (C) 2002  Henrik Ekelund
--    Email: <http://sourceforge.net/sendmessage.php?touser=618411>
--
--    This library is free software; you can redistribute it and/or
--    modify it under the terms of the GNU Lesser General Public
--    License as published by the Free Software Foundation; either
--    version 2.1 of the License, or (at your option) any later version.
--
--    This library is distributed in the hope that it will be useful,
--    but WITHOUT ANY WARRANTY; without even the implied warranty of
--    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
--    Lesser General Public License for more details.
--
--    You should have received a copy of the GNU Lesser General Public
--    License along with this library; if not, write to the Free Software
--    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

CREATE TABLE [dbo].[tsuActiveTest] (
	[isError] [bit] NOT NULL ,
	[isFailure] [bit] NOT NULL ,
	[procedureName] [nvarchar] (255) NULL ,
	[message] [nvarchar] (200) NOT NULL
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[tsuErrors] (
	[testResultID] [int] NOT NULL ,
	[test] [nvarchar] (255) NOT NULL ,
	[message] [nvarchar] (255) NOT NULL
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[tsuFailures] (
	[testResultID] [int] NOT NULL ,
	[test] [nvarchar] (255) NOT NULL ,
	[message] [nvarchar] (255) NOT NULL
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[tsuLastTestResult] (
	[suite] [nvarchar] (255) NULL ,
	[success] [bit] NULL ,
	[testCount] [int] NULL ,
	[failureCount] [int] NULL ,
	[errorCount] [int] NULL ,
	[startTime] [datetime] NULL ,
	[stopTime] [datetime] NULL
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[tsuTestResults] (
	[testResultID] [int] IDENTITY (1, 1) NOT NULL ,
	[startTime] [datetime] NOT NULL ,
	[stopTime] [datetime] NULL ,
	[runs] [int] NOT NULL ,
	[testName] [nvarchar] (255) NOT NULL
) ON [PRIMARY]
GO

SET QUOTED_IDENTIFIER OFF
GO
SET ANSI_NULLS ON
GO




CREATE PROCEDURE tsu_describe
-- GENERAL INFO:    This stored procedure is a part of the tsqlunit
--                  unit testing framework. It is open source software
--                  available at http://tsqlunit.sourceforge.net
--
-- DESCRIPTION:     This procedure returns information about all testcases
--                  in the database.
-- PARAMETERS:      None
-- RETURNS:         Recordset with fields:
--                      TESTNAME:       the name of the test stored procedure
--                      SUITE:          the name of the testsuite, or blank if
--                                      the test does not belong to a suite.
--                      HASSETUP:       1 if the suite has a setup procedure.
--                      HASTEARDOWN:    1 if the suite has a teardown procedure.
--
-- VERSION:         tsqlunit-0.9
-- COPYRIGHT:
--    Copyright (C) 2002  Henrik Ekelund
--    Email: <http://sourceforge.net/sendmessage.php?touser=618411>
--
--    This library is free software; you can redistribute it and/or
--    modify it under the terms of the GNU Lesser General Public
--    License as published by the Free Software Foundation; either
--    version 2.1 of the License, or (at your option) any later version.
--
--    This library is distributed in the hope that it will be useful,
--    but WITHOUT ANY WARRANTY; without even the implied warranty of
--    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
--    Lesser General Public License for more details.
--
--    You should have received a copy of the GNU Lesser General Public
--    License along with this library; if not, write to the Free Software
--    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

AS

SET NOCOUNT ON

DECLARE @testcase sysname
DECLARE @testcase_prefix_removed sysname
DECLARE @ErrorSave INT
DECLARE @ErrorExec INT
DECLARE @hasSetup BIT
DECLARE @hasTearDown BIT
DECLARE @suitePrefixIndex INT

DECLARE @suite sysname
DECLARE @testPrefix varchar(10)
DECLARE @lengthOfTestPrefix INTEGER
DECLARE @LikeUnderscore char(3)
DECLARE @setupLikeExpression VARCHAR(255)
DECLARE @teardownLikeExpression VARCHAR(255)

SET @LikeUnderscore ='[_]'
SET @testPrefix='ut' + @LikeUnderscore
SET @lengthOfTestPrefix=3

CREATE TABLE #result (
 TESTNAME sysname,
 SUITE sysname,
 HASSETUP bit,
 HASTEARDOWN bit
)

DECLARE testcases_cursor CURSOR FOR
	select name from sysobjects where xtype='P' and name LIKE  @testPrefix + '%'

OPEN testcases_cursor

FETCH NEXT FROM testcases_cursor INTO @testcase

WHILE @@FETCH_STATUS = 0
BEGIN
	SET @hasSetup=0
	SET @hasTearDown=0
	SET @suite=''

	SET @testcase_prefix_removed=RIGHT(@testcase,LEN( @testcase)-@lengthOfTestPrefix)

	IF @testcase_prefix_removed LIKE '%' +@LikeUnderscore+ '%'
	BEGIN
	SET @suitePrefixIndex=CHARINDEX ( '_', @testcase_prefix_removed  )
	SET @suite= LEFT(@testcase_prefix_removed, @suitePrefixIndex - 1)
			SET @setupLikeExpression=@testPrefix +  @suite + @LikeUnderscore  + 'SetUp'
			SET @hasSetup= (select count(*) from sysobjects where xtype='P' and name LIKE @setupLikeExpression )

			SET @teardownLikeExpression=@testPrefix +  @suite + @LikeUnderscore  + 'TearDown'
	SET @hasTearDown= (select count(*) from sysobjects where xtype='P' and name LIKE @teardownLikeExpression )
	END

	IF NOT((@hasSetup=1 AND (@testcase LIKE @setupLikeExpression)) OR ( @hasTearDown=1 AND (@testcase LIKE @teardownLikeExpression)))
	BEGIN
	 INSERT INTO  #result ( TESTNAME ,
				 SUITE,
				 HASSETUP,
				 HASTEARDOWN )
	 VALUES (@testcase, @suite,@hasSetup,@hasTearDown)
	END
	FETCH NEXT FROM testcases_cursor  INTO @testcase
END

CLOSE testcases_cursor
DEALLOCATE testcases_cursor

SELECT TESTNAME, SUITE, HASSETUP, HASTEARDOWN FROM #result




GO
SET QUOTED_IDENTIFIER OFF
GO
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO
SET ANSI_NULLS ON
GO



CREATE   PROCEDURE tsu__private_addError @test NVARCHAR(255), @errorMessage NVARCHAR(255)
-- GENERAL INFO:    This stored procedure is a part of the tsqlunit
--                  unit testing framework. It is open source software
--                  available at http://tsqlunit.sourceforge.net
--
-- DESCRIPTION:     This procedure makes an internal notice when an error has occurred.
-- PARAMETERS:      @test               The name of the test
--                  @errorMessage       A description of the error
--
-- RETURNS:         Nothing
--
-- VERSION:         tsqlunit-0.9
-- COPYRIGHT:
--    Copyright (C) 2002  Henrik Ekelund
--    Email: <http://sourceforge.net/sendmessage.php?touser=618411>
--
--    This library is free software; you can redistribute it and/or
--    modify it under the terms of the GNU Lesser General Public
--    License as published by the Free Software Foundation; either
--    version 2.1 of the License, or (at your option) any later version.
--
--    This library is distributed in the hope that it will be useful,
--    but WITHOUT ANY WARRANTY; without even the implied warranty of
--    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
--    Lesser General Public License for more details.
--
--    You should have received a copy of the GNU Lesser General Public
--    License along with this library; if not, write to the Free Software
--    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

AS
SET NOCOUNT ON
DECLARE @id INTEGER
SET @id=(SELECT MAX(testResultID) FROM tsuTestResults)

INSERT INTO tsuErrors( test, message, testResultID) VALUES(@test,@errorMessage, @id)

GO
SET QUOTED_IDENTIFIER OFF
GO
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
SET ANSI_NULLS ON
GO




CREATE   PROCEDURE tsu__private_addFailure @test NVARCHAR(255), @errorMessage NVARCHAR(255)
-- GENERAL INFO:    This stored procedure is a part of the tsqlunit
--                  unit testing framework. It is open source software
--                  available at http://tsqlunit.sourceforge.net
--
-- DESCRIPTION:     This procedure makes an internal notice when a failure has occurred.
-- PARAMETERS:      @test               The name of the test
--                  @errorMessage       A description of the failure
--
-- RETURNS:         Nothing
--
-- VERSION:         tsqlunit-0.9
-- COPYRIGHT:
--    Copyright (C) 2002  Henrik Ekelund
--    Email: <http://sourceforge.net/sendmessage.php?touser=618411>
--
--    This library is free software; you can redistribute it and/or
--    modify it under the terms of the GNU Lesser General Public
--    License as published by the Free Software Foundation; either
--    version 2.1 of the License, or (at your option) any later version.
--
--    This library is distributed in the hope that it will be useful,
--    but WITHOUT ANY WARRANTY; without even the implied warranty of
--    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
--    Lesser General Public License for more details.
--
--    You should have received a copy of the GNU Lesser General Public
--    License along with this library; if not, write to the Free Software
--    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
AS
SET NOCOUNT ON
DECLARE @id INTEGER
SET @id=(SELECT MAX(testResultID) FROM tsuTestResults)

INSERT INTO tsuFailures( test, message, testResultID) VALUES(@test,@errorMessage, @id)



GO
SET QUOTED_IDENTIFIER OFF
GO
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
SET ANSI_NULLS ON
GO



CREATE PROCEDURE tsu__private_createTestResult @suiteName NVARCHAR(255)=''
-- GENERAL INFO:    This stored procedure is a part of the tsqlunit
--                  unit testing framework. It is open source software
--                  available at http://tsqlunit.sourceforge.net
--
-- DESCRIPTION:     This procedure intializes the testrsult before the
--                  tests in a suite are called.
-- PARAMETERS:      @suiteName          The name of the testsuite
--
-- RETURNS:         Nothing
--
-- VERSION:         tsqlunit-0.9
-- COPYRIGHT:
--    Copyright (C) 2002  Henrik Ekelund
--    Email: <http://sourceforge.net/sendmessage.php?touser=618411>
--
--    This library is free software; you can redistribute it and/or
--    modify it under the terms of the GNU Lesser General Public
--    License as published by the Free Software Foundation; either
--    version 2.1 of the License, or (at your option) any later version.
--
--    This library is distributed in the hope that it will be useful,
--    but WITHOUT ANY WARRANTY; without even the implied warranty of
--    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
--    Lesser General Public License for more details.
--
--    You should have received a copy of the GNU Lesser General Public
--    License along with this library; if not, write to the Free Software
--    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
AS
INSERT INTO tsuTestResults
	( runs, testName, startTime)
VALUES (0,@suiteName, GetDate())
IF @@ERROR <>0
	RETURN 100



GO
SET QUOTED_IDENTIFIER OFF
GO
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
SET ANSI_NULLS ON
GO



CREATE  PROCEDURE tsu__private_showTestResult
	@testResultID INTEGER
-- GENERAL INFO:    This stored procedure is a part of the tsqlunit
--                  unit testing framework. It is open source software
--                  available at http://tsqlunit.sourceforge.net
--
-- DESCRIPTION:     This procedure prints the results of testing a suite.
-- PARAMETERS:      @testResultID        The testresult is shown for this ID
--
-- RETURNS:         Nothing
--
-- VERSION:         tsqlunit-0.9
-- COPYRIGHT:
--    Copyright (C) 2002  Henrik Ekelund
--    Email: <http://sourceforge.net/sendmessage.php?touser=618411>
--
--    This library is free software; you can redistribute it and/or
--    modify it under the terms of the GNU Lesser General Public
--    License as published by the Free Software Foundation; either
--    version 2.1 of the License, or (at your option) any later version.
--
--    This library is distributed in the hope that it will be useful,
--    but WITHOUT ANY WARRANTY; without even the implied warranty of
--    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
--    Lesser General Public License for more details.
--
--    You should have received a copy of the GNU Lesser General Public
--    License along with this library; if not, write to the Free Software
--    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
AS
BEGIN
	SET NOCOUNT ON
	DECLARE @outstr NVARCHAR(120)
	SET @outstr=(SELECT 'Testsuite: ' + testName + ' (' + LTRIM(STR(runs)) + ' tests )'
				+ ' execution time: '  + LTRIM(STR(datediff(ms,startTime,stopTime))) + ' ms.'
			FROM tsuTestResults WHERE TestResultID=@testResultID)

	PRINT @outstr

	DECLARE msgcursor CURSOR FOR
		SELECT '>>> Test: ' + test + '     '  + message FROM tsuErrors
		WHERE testResultID=@testResultID UNION ALL
		SELECT '>>> Test: ' + test + '     '  + message FROM tsuFailures
		WHERE testResultID=@testResultID
	FOR READ ONLY
	OPEN msgcursor
	FETCH NEXT FROM msgcursor INTO @outstr
	WHILE @@FETCH_STATUS =0
	BEGIN
		PRINT @outstr
		FETCH NEXT FROM msgcursor INTO @outstr
	END
	CLOSE msgcursor
	DEALLOCATE msgcursor
END




GO
SET QUOTED_IDENTIFIER OFF
GO
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
SET ANSI_NULLS ON
GO




CREATE PROCEDURE tsu_error
	@errorNr INT
-- GENERAL INFO:    This stored procedure is a part of the tsqlunit
--                  unit testing framework. It is open source software
--                  available at http://tsqlunit.sourceforge.net
--
-- DESCRIPTION:     This procedure can be called by a unit test when an
--                  error occurs. Normally this should not be necessary, the
--                  runTestSuite procedure does this automatically.
-- PARAMETERS:      @errorNr        An error number
--
-- RETURNS:         Nothing
--
-- VERSION:         tsqlunit-0.9
-- COPYRIGHT:
--    Copyright (C) 2002  Henrik Ekelund
--    Email: <http://sourceforge.net/sendmessage.php?touser=618411>
--
--    This library is free software; you can redistribute it and/or
--    modify it under the terms of the GNU Lesser General Public
--    License as published by the Free Software Foundation; either
--    version 2.1 of the License, or (at your option) any later version.
--
--    This library is distributed in the hope that it will be useful,
--    but WITHOUT ANY WARRANTY; without even the implied warranty of
--    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
--    Lesser General Public License for more details.
--
--    You should have received a copy of the GNU Lesser General Public
--    License along with this library; if not, write to the Free Software
--    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
AS
BEGIN
	SET NOCOUNT ON
	DECLARE @msg NVARCHAR(255)

	IF @errorNr=14000 -- User defined error message generated by RAISERRROR
	BEGIN
		SET @msg='User defined error'
	END
	ELSE
	BEGIN
		SET @msg=(SELECT 'Severity:' + CAST([severity] AS VARCHAR(10)) + ' Description:' + [description] FROM master.dbo.[sysmessages]
					WHERE [error]=@errorNr)
	END
	UPDATE tsuActiveTest
		SET
			IsError=1,
			IsFailure=0,
			message=@msg
END






GO
SET QUOTED_IDENTIFIER OFF
GO
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
SET ANSI_NULLS ON
GO



CREATE  PROCEDURE tsu_failure
	@message NVARCHAR(255)
-- GENERAL INFO:    This stored procedure is a part of the tsqlunit
--                  unit testing framework. It is open source software
--                  available at http://tsqlunit.sourceforge.net
--
-- DESCRIPTION:     This procedure should be called by a unit test when a
--                  test fails.
-- PARAMETERS:      @message        A description of the failure
--
-- RETURNS:         Nothing
--
-- VERSION:         tsqlunit-0.9
-- COPYRIGHT:
--    Copyright (C) 2002  Henrik Ekelund
--    Email: <http://sourceforge.net/sendmessage.php?touser=618411>
--
--    This library is free software; you can redistribute it and/or
--    modify it under the terms of the GNU Lesser General Public
--    License as published by the Free Software Foundation; either
--    version 2.1 of the License, or (at your option) any later version.
--
--    This library is distributed in the hope that it will be useful,
--    but WITHOUT ANY WARRANTY; without even the implied warranty of
--    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
--    Lesser General Public License for more details.
--
--    You should have received a copy of the GNU Lesser General Public
--    License along with this library; if not, write to the Free Software
--    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
AS
BEGIN
	SET NOCOUNT ON
	UPDATE tsuActiveTest
		SET
			IsFailure=1,
			isError=0,
			message=ISNULL(@message,'(no description)')
END



GO
SET QUOTED_IDENTIFIER OFF
GO
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO
SET ANSI_NULLS ON
GO





CREATE PROCEDURE tsu_runTestSuite @suite NVARCHAR(255)
-- GENERAL INFO:    This stored procedure is a part of the tsqlunit
--                  unit testing framework. It is open source software
--                  available at http://tsqlunit.sourceforge.net
--
-- DESCRIPTION:     This procedure runs all the tests in a testsuite.
--                  It creates an entry in tsuTestResults with the results.
--                  As this procedure does not produce any graphical output, you
--                  should generally not call this procedure directly, instead
--                  look at tsu_runTests.
-- PARAMETERS:      @suite        The name of the suite
--
-- RETURNS:         Nothing
--
-- VERSION:         tsqlunit-0.9
-- COPYRIGHT:
--    Copyright (C) 2002  Henrik Ekelund
--    Email: <http://sourceforge.net/sendmessage.php?touser=618411>
--
--    This library is free software; you can redistribute it and/or
--    modify it under the terms of the GNU Lesser General Public
--    License as published by the Free Software Foundation; either
--    version 2.1 of the License, or (at your option) any later version.
--
--    This library is distributed in the hope that it will be useful,
--    but WITHOUT ANY WARRANTY; without even the implied warranty of
--    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
--    Lesser General Public License for more details.
--
--    You should have received a copy of the GNU Lesser General Public
--    License along with this library; if not, write to the Free Software
--    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
AS
BEGIN

SET NOCOUNT ON

BEGIN TRANSACTION

DECLARE @testcase sysname
DECLARE @hasSetup BIT
DECLARE @hasTearDown BIT
DECLARE @failure BIT
DECLARE @testPrefix CHAR(3)
DECLARE @setupError INT
DECLARE @tearDownError INT
DECLARE @procedureError INT
DECLARE @countTests INT
DECLARE @isError BIT
DECLARE @isFailure BIT
DECLARE @procedureName NVARCHAR(255)
DECLARE @message NVARCHAR(200)
DECLARE @retVal INT

SET @countTests=0


DECLARE @spName NVARCHAR(255)
SET @testPrefix='ut_'

CREATE TABLE #result (
	 TESTNAME sysname,
	 SUITE sysname,
	 HASSETUP bit,
	 HASTEARDOWN bit
)

INSERT INTO #result EXECUTE tsu_describe
IF @@ERROR<>0
BEGIN
	ROLLBACK TRANSACTION
	RETURN 100
END


EXEC @retVal=tsu__private_CreateTestResult
IF @@ERROR<>0 OR @retVal<>0
BEGIN
	ROLLBACK TRANSACTION
	RETURN 100
END

DECLARE testcases_cursor CURSOR FOR
	SELECT TESTNAME, HASSETUP, HASTEARDOWN FROM #result
		WHERE SUITE=@suite
	ORDER BY TESTNAME

OPEN testcases_cursor

FETCH NEXT FROM testcases_cursor INTO @testcase, @hasSetup, @hasTearDown
WHILE @@FETCH_STATUS = 0
BEGIN
	DELETE FROM  tsuActiveTest;
	INSERT INTO tsuActiveTest (isError,isFailure,message) VALUES (0,0,'')
	SET @countTests=@countTests+1

	SET ARITHABORT OFF

	SET @setupError=0
	SET @tearDownError=0
	SET @procedureError=0

	SET XACT_ABORT OFF

	SAVE TRANSACTION testTran

	IF @hasSetup =1
	BEGIN
		UPDATE tsuActiveTest
			SET procedureName=@testcase+ '(in SetUp)'
		SET @spName=@testPrefix +  @suite + '_SetUp'
		EXEC @spName
		SET @setupError=@@ERROR
		IF (@setupError <> 0)
			EXEC tsu_error @setupError
	END
	IF @setupError= 0
	BEGIN
		UPDATE tsuActiveTest
			SET procedureName=@testcase
		EXEC @testcase
		SET @procedureError=@@ERROR
		SET @failure=(SELECT isFailure FROM tsuActiveTest)
		IF (@procedureError <> 0 AND @setupError=0 AND @failure=0)  -- Only show the first error
			EXEC tsu_error @procedureError
	END

	IF @hasTearDown=1
	BEGIN
		UPDATE tsuActiveTest
			SET procedureName=@testcase+ '(in TearDown)'
		SET @spName=@testPrefix +  @suite + '_TearDown'
		EXEC @spName
		SET @tearDownError=@@ERROR
		IF (@tearDownError <> 0 AND @setupError = 0 AND @failure=0 AND @procedureError = 0 )
			EXEC tsu_error @tearDownError  -- Only show the first error
	END

	-- Copy the test result to local variables, then Do a Rollback and restore the state of the database



	SET @isError = (SELECT isError FROM tsuActiveTest )
	SET @isFailure=(SELECT isFailure FROM tsuActiveTest)
	SET @procedureName=(SELECT procedureName FROM tsuActiveTest)
	SET @message=(SELECT message FROM tsuActiveTest)

	ROLLBACK TRANSACTION testTran


	IF @isError=1
		EXEC tsu__private_addError @procedureName, @message
	ELSE IF @isFailure=1
		EXEC tsu__private_addFailure @procedureName, @message

	FETCH NEXT FROM testcases_cursor  INTO @testcase, @hasSetup, @hasTearDown
END

CLOSE testcases_cursor
DEALLOCATE testcases_cursor

UPDATE tsuTestResults
	SET stopTime=getdate(),
		   runs=@countTests
	WHERE testResultID= (SELECT MAX(testResultId) FROM tsuTestResults)

COMMIT TRANSACTION
END














GO
SET QUOTED_IDENTIFIER OFF
GO
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
SET ANSI_NULLS ON
GO





CREATE    PROCEDURE tsu_showTestResults
	@startTime datetime=NULL,
	@endTime datetime=NULL
-- GENERAL INFO:    This stored procedure is a part of the tsqlunit
--                  unit testing framework. It is open source software
--                  available at http://tsqlunit.sourceforge.net
--
-- DESCRIPTION:     This procedure shows the result of all tests done, or
--                  all tests within a certain period.
-- PARAMETERS:      @suite        The name of the suite
--
-- RETURNS:         Nothing
--
-- VERSION:         tsqlunit-0.9
-- COPYRIGHT:
--    Copyright (C) 2002  Henrik Ekelund
--    Email: <http://sourceforge.net/sendmessage.php?touser=618411>
--
--    This library is free software; you can redistribute it and/or
--    modify it under the terms of the GNU Lesser General Public
--    License as published by the Free Software Foundation; either
--    version 2.1 of the License, or (at your option) any later version.
--
--    This library is distributed in the hope that it will be useful,
--    but WITHOUT ANY WARRANTY; without even the implied warranty of
--    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
--    Lesser General Public License for more details.
--
--    You should have received a copy of the GNU Lesser General Public
--    License along with this library; if not, write to the Free Software
--    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
AS
BEGIN
	SET NOCOUNT ON
	DECLARE @testResultID INTEGER
	IF @startTime IS NULL
		SET @startTime=CONVERT(DATETIME,'1900-01-01',121)
	IF @endTime IS NULL
		SET @endTime=CONVERT(DATETIME,'2100-01-01',121)
	DECLARE cursorTestResultID CURSOR FOR
		   SELECT testResultID FROM tsuTestResults WHERE
		startTime>= CAST(@startTime AS timestamp) AND
		stopTime <= @endTime
		ORDER BY startTime
	OPEN cursorTestResultID
	FETCH NEXT FROM cursorTestResultID INTO @testResultID
	WHILE @@FETCH_STATUS =0
	BEGIN
		EXEC tsu__private_showTestResult @testResultID
		FETCH NEXT FROM cursorTestResultID INTO @testResultID
	END
	CLOSE cursorTestResultID
	DEALLOCATE cursorTestResultID
END





GO
SET QUOTED_IDENTIFIER OFF
GO
SET ANSI_NULLS ON
GO



CREATE PROCEDURE tsu_runTests
	@suite NVARCHAR(255)='' OUTPUT,
	@success BIT = 0 OUTPUT,
	@testCount INTEGER = 0 OUTPUT,
	@failureCount INTEGER = 0 OUTPUT,
	@errorCount INTEGER = 0 OUTPUT
-- GENERAL INFO:    This stored procedure is a part of the tsqlunit
--                  unit testing framework. It is open source software
--                  available at http://tsqlunit.sourceforge.net
--
-- DESCRIPTION:     This is the procedure you call when you want to run
--                  the tests and look at the result. It may also be called
--                  from code. Statistics are returned in the output parameters,
--                  and in the table tsuLastTestResult
--
-- PARAMETERS:      @suite          Optional name of a suite, if this is not
--                                  provided all tests in the database will be
--                                  executed.
--                  @success        1 if all tests were successful.
--                  @testCount      The number of tests executed.
--                  @failureCount   The number of failing tests.
--                  @errorCount     The number of tests with errors.
--
-- RETURNS:         Nothing
--
-- VERSION:         tsqlunit-0.9
-- COPYRIGHT:
--    Copyright (C) 2002  Henrik Ekelund
--    Email: <http://sourceforge.net/sendmessage.php?touser=618411>
--
--    This library is free software; you can redistribute it and/or
--    modify it under the terms of the GNU Lesser General Public
--    License as published by the Free Software Foundation; either
--    version 2.1 of the License, or (at your option) any later version.
--
--    This library is distributed in the hope that it will be useful,
--    but WITHOUT ANY WARRANTY; without even the implied warranty of
--    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
--    Lesser General Public License for more details.
--
--    You should have received a copy of the GNU Lesser General Public
--    License along with this library; if not, write to the Free Software
--    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
AS
BEGIN
SET NOCOUNT ON
DECLARE @testsuite NVARCHAR(255)
DECLARE @lastTestResultID INTEGER
DECLARE @startTime DATETIME
DECLARE @stopTime DATETIME
SET @success=0
SET @testCount=0
SET @failureCount=0
SET @errorCount=0

IF @suite='' OR @suite IS NULL SET @suite='%'

CREATE TABLE #tests (
	 TESTNAME sysname,
	 SUITE sysname,
	 HASSETUP bit,
	 HASTEARDOWN bit
)

INSERT INTO #tests EXECUTE tsu_describe

DECLARE testsuites_cursor CURSOR FOR
	SELECT DISTINCT SUITE FROM #tests
	WHERE SUITE LIKE @suite
	ORDER BY SUITE
OPEN testsuites_cursor

SET @startTime=GetDate()
PRINT REPLICATE ( '=' , 80 )
PRINT ' Run tests starts:' + CAST(@startTime AS VARCHAR(30))

FETCH NEXT FROM testsuites_cursor INTO @testsuite
WHILE @@FETCH_STATUS = 0
BEGIN
	EXEC tsu_runTestSuite @testsuite
	SET @lastTestResultID=(SELECT MAX(testResultID) FROM tsuTestResults)
	SET @testCount= @testCount+ (SELECT runs FROM tsuTestResults
					 WHERE testResultID=@lastTestResultID)
	SET @failureCount= @failureCount+ (SELECT COUNT(*) FROM tsuFailures
						WHERE testResultID=@lastTestResultID)
	SET @errorCount= @errorCount+ (SELECT COUNT(*) FROM tsuErrors
					 WHERE testResultID=@lastTestResultID)


	FETCH NEXT FROM testsuites_cursor  INTO @testsuite
END
SET @stopTime=GetDate()
IF @failureCount=0 and @errorCount=0
	 SET @success=1

CLOSE testsuites_cursor
DEALLOCATE testsuites_cursor

PRINT REPLICATE ( '=' , 80 )

EXEC tsu_showTestResults @startTime, @stopTime

PRINT REPLICATE ( '-' , 80 )
PRINT ' Run tests ends:' + CAST(@stopTime AS VARCHAR(30))
PRINT ' Summary:'
PRINT '     ' + LTRIM(STR(@testCount)) + ' tests, of which ' +
		  LTRIM(STR(@failureCount)) + ' failed and ' +
		  LTRIM(STR(@errorCount)) + ' had an error.'
PRINT ''
IF @success=1
	PRINT '     SUCCESS!'
ELSE
	PRINT '     FAILURE!'

PRINT REPLICATE ( '-' , 80 )
PRINT REPLICATE ( '=' , 80 )
--
--# According to Knownledge base article Q313861 a recordset will not return to ADO if the
--# stored procedure fails with a severe error. As a work around, the result of the
--# last test is saved to the table tsuLastTestResult
--
DELETE FROM tsuLastTestResult
INSERT INTO tsuLastTestResult ( suite, success, testCount, failureCount, errorCount, startTime, stopTime)
	VALUES (@suite, @success, @testCount, @failureCount, @errorCount, @startTime, @stopTime)

END


GO
SET QUOTED_IDENTIFIER OFF
GO
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
SET ANSI_NULLS ON
GO
