#==========================================================================
# OutputParamTestFailure.py
#
# adodbiapi has callproc(), which fails under certain conditions. This
# script shows that the output parameters don't get updated when DML occurs
# in a SQL Server script.
#
# This is related to the bug report of 2003-05-28:
#
#   http://sourceforge.net/tracker/index.php?func=detail&aid=744898&group_id=63427&atid=503936
#
# I tried the workaround reported there, without success.
#
# As of March, 2007, the following APIs simply do not support callproc(),
# at least not with SQL Server:
#
#   mxODBC www.egenix.com/files/python/mxODBC.html
#   pyodbc  http://pyodbc.sourceforge.net/docs.html
#   PyMSSQL
#   MSSQL www.object-craft.com.au/projects/mssql/examples.html
#==========================================================================

import adodbapi # http://adodbapi.sourceforge.net/
import socket   # To get the computer name for a SQL Server instance.

# Get the connection info
instance = socket.gethostname() + r"\SILFW"
database = 'TestLangProj'
user = "sa"
password = "inscrutable"
conStr = "Provider=SQLOLEDB.1; Data Source=%s; Initial Catalog=%s; User ID=%s; password=%s" % (
	instance, database, user, password)
con = adodbapi.connect(conStr)

# Call the test procedure
cur = con.cursor()
params = cur.callproc("Test", [1, "two", 3, 4])
print "The fourth parameter should be incremented, and have a value of 5: ", params[3]
