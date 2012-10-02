#========================================
#: fw_sqlunit.py
#:
#: Sets up the tsqunit test framework in a test database, and runs the
#: SQL unit tests. The test framework is not removed because the database
#: will be destroyed.
#:
#: Notes:
#:  tsqlunit can be found at: http://tsqlunit.sourceforge.net/index.html
#=======================================

import os
#import string
import adodbapi
import unittest
import socket

class DbConnectInfo(object):

	ComputerName = socket.gethostname() + r"\SILFW"
	Database = 'TestLangProj'
	UserId = "sa"
	Password = "inscrutable"

	ConnectStr = "Provider=SQLOLEDB.1; User ID=%s;Password=%s; Initial Catalog=%s; Data Source=%s" % (
		UserId, Password, Database, ComputerName)

	OsqlStarterStr = "osql -U %s -P %s -S %s -d %s " % (
		UserId, Password, ComputerName, Database)

	OsqlQuietStarterStr = OsqlStarterStr + "-o osqloutput.txt "

class InstallTSQLUnit(unittest.TestCase):

	def setUp(self):

		#( Get an object with connection info
		self.DbConnect = DbConnectInfo()

		#( Set up the tsqlunit framework
		self.RunSqlScript("tsqlunit.sql")

	def testRunall(self):

		self.RunSqlScript("dropallunittests.sql")

		print
		print "--------------------"
		print "Running normal tests"
		print "--------------------"
		self.RunNormalTests()
		self.RunSqlScript("dropallunittests.sql")

		print
		print "------------------------"
		print "Running multi-user tests"
		print "------------------------"
		self.RunMultiUserTests()
		self.RunSqlScript("dropallunittests.sql")

		# Print list of loaded modules so we know which files need to be checked into Perforce.

		# Cannot use this code after the call to unittest.main() at the end of the file
		# because unittest.main() does not seem to return. Currently commented out since
		# it is not needed for running the tests, uncomment if you are installing
		# a new version of the Python runtime and need to see what files are needed.
		#import sys
		#for module in sys.modules:
		#    print sys.modules[module]

	def RunNormalTests(self):

		self.RunSqlScript("..\..\Src\Cellar\Test\ut_FwCore.sql")
		self.RunSqlScript("..\..\Src\Ling\Test\ut_LingSP.sql")
		self.RunSqlScript("..\..\Src\Scripture\Test\ut_Scripture.sql")
		self.RunSqlScript("..\..\Src\LangProj\Test\ut_LangProjSP.sql")

		self.RunTests()

	def RunMultiUserTests(self):

		self.RunSqlScript("..\..\Src\Cellar\Test\ut_FwCore2.sql")

		#( Get a dummy connection to the database
		connection = adodbapi.connect(self.DbConnect.ConnectStr)
		crsr = connection.cursor()

		self.RunTests()

		#( Tear down the dummy connection
		connection.rollback()
		connection.close()
		connection = None
		crsr = None

	def RunTests(self):

		try:
			#( For some reason the callproc below did not run stored procedure tsu_runtests
			#( On Steve Miller's computer, so the call got changed to using osql to run the stored proc.
			#self.crsr.callproc("tsu_runTests")

			#tempCommand = self.DbConnect.OsqlStarterStr + ' -Q"EXEC tsu_runTests" '
			tempCommand = self.DbConnect.OsqlQuietStarterStr + ' -Q"EXEC tsu_runTests" '
			os.system(tempCommand)

		except:
			pass  #( ADO aborts when the error occurs, ignore

		#( Get a utility connection to the database
		connection = adodbapi.connect(self.DbConnect.ConnectStr)
		crsr = connection.cursor()

		crsr.execute("SELECT success, testCount, failureCount, errorCount FROM tsuLastTestResult")
		rs = crsr.fetchone()

		print type(rs)
		if type(rs) != tuple:
			print 'Failure on fetching a row from tsuLastTestResult, probably because the tsqlunit'
			print 'unit test framework failed. Likely cause for tsqlunit failing is one of the SQL'
			print 'unit tests crashed. Consider running fw_sqlunit.py using the OsqlStarterStr unremarked.'
			f=open('error.txt', 'w')
			f.write('tsqlunit framework likely failed.')
			f.write('\n')
			f.close()
		else:
			print
			print "Tests:", rs[1]
			print "Failed Tests:", rs[2]
			print "Errors:", rs[3]
			print
			if rs[0] == 1:
				print "SUCCESS!"
			else :
				print "Failure Information:"
				print
				crsr.execute("SELECT test, message FROM tsuFailures")
				rsFailedTests = crsr.fetchone()
				passed = 1
				while rsFailedTests != None:
					passed = 0
					print "    Test:", rsFailedTests[0]
					print "   ", rsFailedTests[1]
					print
					rsFailedTests = crsr.fetchone()
				print "FAILURE!"
				f=open('error.txt', 'w')
				f.write('SQL  unit test error(s).')
				f.write('\n')
				f.close()
			print


		#( Tear down the utility connection
		connection.rollback()
		connection.close()
		connection = None
		crsr = None

	def RunSqlScript(self, SqlFile):

		#( The output file is used to suppress output from going to screen
		OsqlCommand = self.DbConnect.OsqlQuietStarterStr + " -i %s" % SqlFile
		os.system(OsqlCommand)

	def tearDown(self):

		#( We really don't need to tear down the tsqlunit framework here
		#( because the test database will be destroyed immediately
		#( after this runs, but it does'nt take many CPU cycles, and it's
		#( nice for debugging.
		self.RunSqlScript("removetsqlunit.sql")

		os.remove("osqloutput.txt")

if __name__ == '__main__':

	# this method call does not seem to return
	unittest.main()
