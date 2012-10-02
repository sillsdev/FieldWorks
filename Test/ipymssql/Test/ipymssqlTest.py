"""
	ipymssqlTest.py

	Description:
		A set of unit tests for ipymssql.py

	Requirements:
		IronPython installed
		ipymssql.py (the API we're testing)
		ipymssqlTest.sql, a script to create a test stored procedure in SQL
			Server

	Notes:
		(22 March, 2007). A bug in IronPython does not allow assertEqual to
		work properly in unittest, when the assert fails. A bug report has
		been filed.

	License:

		Copyright (C) 2007  SIL International

		This library is free software; you can redistribute it and/or
		modify it under the terms of the GNU Lesser General Public
		License as published by the Free Software Foundation; either
		version 2.1 of the License, or (at your option) any later version.

		This library is distributed in the hope that it will be useful,
		but WITHOUT ANY WARRANTY; without even the implied warranty of
		MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
		Lesser General Public License for more details.

		You should have received a copy of the GNU Lesser General Public
		License along with this library; if not, write to the Free Software
		Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA

"""

import ipymssql
import socket
import unittest

#class testConnection(unittest.TestCase):
	# TODO

class testCursor(unittest.TestCase):

	#==( fetch*() Tests )==#

		#( For testing fetchone() and fetchall(), see testCallprocResultSet.

		# TODO: fetchmany()

	#==( callproc() Tests )==#

	def testCallprocOutputParams(self):

		#--( Feed parameters, Return Output parameters )--#

		#( This version passes in parameters. It also sends a third parameter
		#( "output", telling callproc to return output parameters

		param0 = [0, "@return", "int", "ReturnValue"]
		param1 = [10, "@n1", "int", "Input"]
		param2 = ["twenty", "@n2", "nvarchar", "Input"]
		param3 = [30, "@n3", "int", "Input"]
		param4 = [40, "@n4", "int", "InputOutput"]
		values = cur.callproc("test", [param0, param1, param2, param3, param4], "OUTPUT")

		print "callproc() return values: ", values

		self.assertEqual(999, values[0], "The return parameter should be 999")
		self.assertEqual(41, values[4], "The output parameter should be 41")

	def testCallprocResultSet(self):

		#--( Have callproc find the parameters, Return rowset )--#

		#( This version has callproc query the stored procedure to get parameters.
		#( It also omits sending the third parameter, which uses the default of
		#( returning a rowset.

		values = cur.callproc("test", [10, "twenty", 30, 40])
		print "callproc rowcount: ", cur.rowcount

		row1 = cur.fetchone()
		print "callproc(), fetchone() results: ", row1
		print "fetchone() rowcount: ", cur.rowcount
		self.assertEqual("Test1", row1[0], "The first row should have a column with a value 'Test1'")

		rows = cur.fetchall()
		print "callproc(), fetchall() results: ", rows
		self.assertEqual("Test2", rows[0][0], "The first row should have a column with a value 'Test2'")

	def testExecCreateInsertSelectDrop(self):

		cur.execute("create table tablex (col1 int)")
		cur.execute("insert into tablex values (1)")
		cur.execute("select * from tablex")
		rows = cur.fetchall()
		print "testExecCreateInsertSelectDrop(): rows from tablex: ", rows
		self.assertEqual(1, rows[0][0], "The first row should have a column with a value 1")
		cur.execute("drop table tablex")

if __name__ == '__main__':

	#TODO: make conStr and database parameters.

	instance = socket.gethostname() + r"<named instance>"
	database = '<database name>'
	conStr = 'Data Source=%s; Initial Catalog=%s; Integrated Security=True' % (instance, database)

	con = ipymssql.connect(conStr)
	cur = con.cursor()

	#==( Run the unit tests )==#

	unittest.main()

	cur.Close()
	con.Close()
