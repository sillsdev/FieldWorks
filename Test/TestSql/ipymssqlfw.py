"""
	ipymssqlfw

	Description:
		Extra utilities for building FW database unit tests on top of ipymssql.

"""

class FWDatabase:
	"""Utilities for FW databases"""

	# TODO: Is setting rdbms to a constant like this the best way to go? Should
	# it be a paramter?

	rdbms = 'mssql'

	def getrdbms(self):
		return self.rdbms

	def connect(self):
		if self.rdbms == 'mssql':
			import socket   #( To get the computer name for a SQL Server instance.
			import ipymssql #( Custom IronPython .NET Data Provider for SQL Server

			Instance = socket.gethostname() + r"\SILFW"
			Database = 'TestLangProj'
			conStr = 'Data Source=%s; Initial Catalog=%s; Integrated Security=True' % (Instance, Database)
			con = ipymssql.connect(conStr)

		elif self.rdbms == 'fb' or self.rdbms == 'firebird':
			import kinterbasdb #( http://kinterbasdb.sourceforge.net

			#( kinterbasdb must be initialized for life to be happy. See
			#( "Incompatibilities" at the top of:
			#(
			#(    http://kinterbasdb.sourceforge.net/dist_docs/usage.html
			#(
			#( The default for kinterbasdb.init is only for backward compatibility.
			#( The reasons for this are written up at:
			#(
			#(    http://kinterbasdb.sourceforge.net/dist_docs/usage.html#faq_fep_is_mxdatetime_required
			#(
			#( The ideal is to use type_conv=200

			kinterbasdb.init(type_conv=200)

			con = kinterbasdb.connect(
				dsn = 'C:\Program Files\Firebird_2_0\Data\TESTLANGPROJ.FDB',
				user = "sysdba",
				password = "inscrutable",
				charset = 'UTF8',
				dialect = 3)

		return con

	def fetchonevalue(self, connect, query):
		"""Get a single value from the database. """

		cur = connect.cursor()
		cur.execute(query)
		row = cur.fetchone()
		if row:
			returnvalue = row[0]
		else:
			returnvalue = None
		cur.close()
		return returnvalue
