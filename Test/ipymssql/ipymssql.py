"""
	ipymssql.py

	Description:
		An IronPython API using the .NET Framework Data Provider for Microsoft
		SQL Server, adhering to the Python Database API Specification v.2.0,
		and supporting callproc().

	Requirements:
		IronPython

	Links:
		The Python Database API Specification (PEP 249):
			http://www.python.org/dev/peps/pep-0249/
		System.Data.SqlClient Namespace:
			http://msdn2.microsoft.com/en-us/library/system.data.sqlclient.aspx
		IronPython:
			http://www.codeplex.com/Wiki/View.aspx?ProjectName=IronPython
		An overview of the data provider:
			http://msdn2.microsoft.com/en-us/library/kb9s9ks0(VS.80).aspx

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

import string
import clr
clr.AddReference("System.Data") #( MS ADO
import System
import exceptions

#==( Globals )==#

# TODO:
# Make sure these are set these to appropriate values.

apilevel = '2.0'
threadsafety = 1 # legal values are 0, 1, 2, 3, or 4
paramstyle = 'qmark' #( For example, '...WHERE x = ?'

#==( Exceptions )==#

# TODO:
# Get exception handling down better.

#( PEP 249, defines the following, but also provides "Optional Error Handling
#( Extensions".

class Warning(exceptions.StandardError):
	pass

class Error(exceptions.StandardError):
	pass

class InterfaceError(Error):
	pass

class DatabaseError(Error):
	pass

class DataError(DatabaseError):
	pass

class OperationalError(DatabaseError):
	pass

class IntegrityError(DatabaseError):
	pass

class InternalError(DatabaseError):
	pass

class ProgrammingError(DatabaseError):
	pass

class NotSupportedError(DatabaseError):
	pass

#==( Connection Constructor )==#

def connect(connectstr):

	try:
		con = System.Data.SqlClient.SqlConnection(connectstr)
	except:
		raise InterfaceError
	con.Open()
	return Connection(con)

#==( Classes )==#

class Connection:

	def __init__(self, con):
		self.con = con
		try:
			self.begintransaction(self.con)
		except:
			raise InterfaceError

	def begintransaction(self, con):
		"""Begin a transaction for the connection"""

		#( PEP 249 specifies a commit and rollback methods on the connection,
		#( rather than on the execute methods of a cursor. The transaction has
		#( to be started somewhere to match the commit or rollback.

		self.tran = con.BeginTransaction()

	def close(self):
		"""Close the connection now, rather than whenever __del__ is called."""
		self.rollback()
		self.con.Close()

	def commit(self):
		"""Commit the transaction"""
		self.tran.Commit()

		#( If one transaction is committed, another should start.
		self.begintransaction(self.con)

	def cursor(self):
		"""Return a new cursor object."""
		return Cursor(self.con, self.tran)

	def rollback(self):
		"""Rollback the transaction"""
		self.tran.Rollback()

	def __del__(self):
		self.rollback()
		self.con.Close()
		self.con = None

class Cursor:

	datareader = None

	#( Defined by Python DB-API.
	decsription = None
	rowcount = -1
	arraysize = 1   #( used for fetchmany() or executemany()

	def __init__(self, connection, transaction):
		self.con = connection
		self.tran = transaction

	def _disposedatareader(self):
		""" Dispose the datareader if it exists"""

		# KLUDGE:
		# The following should be "if type(self.datareader) == SqlDataReader",
		# but I couldn't get that to work. Probably my Python ignorance.

		if self.datareader and type(self.datareader) != int:
			self.datareader.Dispose()
			self.rowcount = -1

	def _executereader(self, com):
		""" Executes a command.ExecuteReader() and sets the cursor's
		datareader. """

		self.datareader = com.ExecuteReader()

		# TODO: self.datareader.RecordsAffected doesn't appear to be set.
		# It's possible that it's only set by fetch, but this needs to be
		# checeked.
		self.rowcount = self.datareader.RecordsAffected
		#print "com.CommandText: ", com.CommandText
		#print "_executereader self.rowcount: ", self.rowcount

		# TODO:
		# To mimic .NET's execute reader, we may want to return
		# self.datareader here. Check to see if this is allowable under
		# the Python spec.

	def _getrowvalues(self):
		""" Get rows out of a data reader and put them into a tuple. """

		recvalues = System.Array.CreateInstance(System.Object,
												self.datareader.FieldCount)
		self.datareader.GetValues(recvalues)
		return tuple(recvalues)

	def callproc(self, storedprocname, parameters = None, returns = ''):
		""" Execute a stored procedure"""

		#( See PEP 249 for the first two parameters storedprocname and
		#( parameters.
		#(
		#( SqlCommand.ExecuteNonQuery() will return output parameters, but not
		#( a rowset (datareader). SqlCommand.ExecuteReader() will return a
		#( rowset, but not updated output parameters, or an updated return
		#( value. Therefore the returns parameter was added to callproc():
		#(
		#(  ""  = (default) return a rowset. A datareader will be produced.
		#(  "OUTPUT" = return output parameters. No datareader will be
		#(             produced.
		#(  "BOTH" = return both output parameters and a rowset. Note that
		#(              this will call the stored procedure twice. This is
		#(              not recommended, but could be helpful.

		sqlCom = self.con.CreateCommand()
		sqlCom.CommandType = System.Data.CommandType.StoredProcedure
		sqlCom.CommandText = storedprocname
		sqlCom.Transaction = self.tran

		#-- Set up Parameters )--#

		# TODO:
		# Pull parameter management out into a helper procedure for execute().

		#( If we have a list of lists, the parameters of the stored procedure
		#( have been explicitly defined. If we don't have a list of lists, we
		#( need to query the database to find out what the parameters are.

		if type(parameters[0]) != list:

			#( Get stored procedure parameter information
			#(
			#( "Deriving parameter information does require an added trip to
			#( the data source for the information. If parameter information
			#( is known at design time, you can improve the performance of
			#( your application by setting the parameters explicitly."
			#( http://msdn2.microsoft.com/en-us/library/yy6y35y8.aspx

			System.Data.SqlClient.SqlCommandBuilder.DeriveParameters(sqlCom)

			for i in range(parameters.Count):

				#( The first parameter of sqlCom.Parameters will be the return
				#( parameter value. The parameters passed in do not include the
				# return parameter, and we don't need to set it.

				sqlCom.Parameters[i + 1].Value = parameters[i]

		else:

			#( In this version, parameters is expected to be a list. Each
			#( element of the list will represent one parameter, and will
			#( have the structure:
			#(
			#(  [<value>, <name>, <data_type>, <parameter_direction>.
			#(
			#( For example:
			#(
			#(  param0 = [0, "@p0", "int", "return"]
			#(  param1 = ["test", "@p1", "nvarchar(4000)", "input"]
			#(  param2 = [20, "@p2", "int", "output"]
			#(  parameters = param0, param1, param2

			i = -1
			for param in parameters:
				i += 1

				#( The SqlDbType Enumeration doc is at:
				#( http://msdn2.microsoft.com/en-us/library/system.data.sqldbtype.aspx

				paramtype = string.upper(param[2])
				if paramtype == "BIGINT":
					dbtype = System.Data.SqlDbType.BigInt
				elif paramtype == "BINARY":
					dbtype = System.Data.SqlDbType.Binary
				elif paramtype == "BIT":
					dbtype = System.Data.SqlDbType.Bit
				elif paramtype == "CHAR":
					dbtype = System.Data.SqlDbType.Char
				elif paramtype == "DATETIME":
					dbtype = System.Data.SqlDbType.DateTime
				elif paramtype == "DECIMAL":
					dbtype = System.Data.SqlDbType.Decimal
				elif paramtype == "FLOAT":
					dbtype = System.Data.SqlDbType.Float
				elif paramtype == "IMAGE":
					dbtype = System.Data.SqlDbType.Image
				elif paramtype == "INT":
					dbtype = System.Data.SqlDbType.Int
				elif paramtype == "MONEY":
					dbtype = System.Data.SqlDbType.Money
				elif paramtype == "NCHAR":
					dbtype = System.Data.SqlDbType.NChar
				elif paramtype == "NTEXT":
					dbtype = System.Data.SqlDbType.NText
				elif paramtype == "NVARCHAR":
					dbtype = System.Data.SqlDbType.NVarChar
				elif paramtype == "REAL":
					dbtype = System.Data.SqlDbType.Real
				elif paramtype == "SMALLDATETIME":
					dbtype = System.Data.SqlDbType.SmallDateTime
				elif paramtype == "SMALLINT":
					dbtype = System.Data.SqlDbType.SmallInt
				elif paramtype == "SMALLMONEY":
					dbtype = System.Data.SqlDbType.SmallMoney
				elif paramtype == "TEXT":
					dbtype = System.Data.SqlDbType.Text
				elif paramtype == "TIMESTAMP":
					dbtype = System.Data.SqlDbType.TimeStamp
				elif paramtype == "TINYINT":
					dbtype = System.Data.SqlDbType.TinyInt
				elif paramtype == "UDT":
					dbtype = System.Data.SqlDbType.Udt
				elif paramtype == "UNIQUEIDENTIFIER":
					dbtype = System.Data.SqlDbType.UniqueIdentifier
				elif paramtype == "VARBINARY":
					dbtype = System.Data.SqlDbType.VarBinary
				elif paramtype == "VARCHAR":
					dbtype = System.Data.SqlDbType.VarChar
				elif paramtype == "VARIANT":
					dbtype = System.Data.SqlDbType.Variant
				elif paramtype == "XML":
					dbtype = System.Data.SqlDbType.Xml

				sqlCom.Parameters.Add(param[1], dbtype).Value = param[0]

				paramdir = string.upper(param[3])
				if paramdir == 'INPUT':
					sqlCom.Parameters[i].Direction = System.Data.ParameterDirection.Input
				elif paramdir == 'OUTPUT':
					sqlCom.Parameters[i].Direction = System.Data.ParameterDirection.Output
				elif paramdir == 'INPUTOUTPUT':
					sqlCom.Parameters[i].Direction = System.Data.ParameterDirection.InputOutput
				elif paramdir == 'RETURNVALUE':
					sqlCom.Parameters[i].Direction = System.Data.ParameterDirection.ReturnValue

		#--( Execute the Stored Proc )--#

		self._disposedatareader()

		#( See note above about the returns parameter.
		if string.upper(returns) == "OUTPUT":
			sqlCom.ExecuteNonQuery()

		#( I don't expect to use this, but here it is.
		elif string.upper(returns) == "BOTH":

			#( This executes the stored procedure twice: once to get the
			#( rowset, the other to get the output parameters.

			sqlCom.ExecuteNonQuery()
			self._executereader(sqlCom)

		else: #( Send out a rowset
			self._executereader(sqlCom)

			#( This BeginExecuteReader() worked the same as a simple
			#( sqlCom.ExecuteReader():
			#(
			#( #( This results in a DbAsyncResult, not IAsyncResult
			#( self.sqlResult = sqlCom.BeginExecuteReader()
			#( self.datareader = sqlCom.EndExecuteReader(self.sqlResult)

		returnvalues = []
		for i in range(sqlCom.Parameters.Count):
			returnvalues.append(sqlCom.Parameters[i].Value)

		sqlCom.Dispose()
		return returnvalues

	def close(self):
		"""Close the cursor"""
		self._disposedatareader()
		self.datareader = None
		self.tran = None
		self.con = None

	def execute(self, operation, parameters = None):
		"""Execute a query or command"""

		# TODO:
		# Support parameters. See the todo under callproc

		sqlCom = self.con.CreateCommand()
		sqlCom.Transaction = self.tran
		sqlCom.CommandText = operation

		self._disposedatareader() #( Needed even for ExecuteNonQuery().

		if string.upper(string.lstrip(operation))[:6] == 'SELECT':
			sqlCom.CommandType = System.Data.CommandType.Text #( Just in case
			self._executereader(sqlCom)

		elif string.upper(string.lstrip(operation))[:4] == 'EXEC':
			# TODO: Include this command:
			# sqlCom.CommandType = System.Data.CommandType.StoredProcedure

			# TODO:
			# This isn't working for some reason with this command from the
			# calling program:
			# cur2.execute('exec Test 1, "test2", 3, 4')
			# self._executereader(sqlCom)

			pass

		else:
			sqlCom.CommandType = System.Data.CommandType.Text
			self.datareader = sqlCom.ExecuteNonQuery()

		sqlCom.Dispose()

	# TODO:
	# write executemany()

	def fetchall(self):
		"""Fetch all the rows from the dataset"""
		rowset = []
		while self.datareader.Read():
			rowset.Append(self._getrowvalues())
		return rowset

	# TODO:
	# write fetchmany()
	def fetchmany(self):
		pass

	def fetchone(self):
		"""Fetch a single row from the dataset"""
		if self.datareader.Read():
			return self._getrowvalues()
		else:
			return None