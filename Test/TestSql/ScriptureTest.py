"""
	ScriptureTest.py

	Description:
		Unit tests for database code in Scripture.

	Notes:
		Many of these tests are ported from its predecessor, ut_Scripture.sql.
		By the time the port is finished, it will hopefully have unit tests
		for all the Scripture stored procedures and functions.
"""

import sys
import System
import unittest #( "PyUnit". See http://docs.python.org/lib/module-unittest.html

import unittestasserts #( asserts for IronPython and unittest
import ipymssqlfw

#( Globals
fwdb = None
con = None
unitassert = None #( unit assert object

def main():
	global fwdb, con, unitassert

	fwdb = ipymssqlfw.FWDatabase()
	con = fwdb.connect()

	unitassert = unittestasserts.UnitAssert()

#==( Tests )==#

#( Tests are done in alpha order. (Gag.)

class testGetNewFootnoteGuids(unittest.TestCase):
	"""GetNewFootnoteGuids test"""

	#( Only known call is from FdoScripture.cs,
	#( Scripture.AdjustObjectsInArchivedBook(). I (SteveMiller) don't know what
	#( this procedure is trying to accomplish. The test simply tries two
	#( records with the same OwnOrd$ from different books.

	def testFootnoteBooks(self):
		"""Test: getting footnotes from two books"""
		cur = con.cursor()
		cur.execute("SELECT Owner$, Guid$, OwnOrd$ FROM StFootnote_ WHERE FootnoteMarker = 'c'")
		rows = cur.fetchall()
		cur.close()

		#( REVIEW SteveMiller: I know there's an easier to unmarshall a list,
		#( but can't remember it right now.
		bookA = rows[0][0]
		guidA = rows[0][1]
		ownord = rows[0][2]
		bookB = rows[1][0]
		guidB = rows[1][1]
		#( ownord from row B should be the same as ownord from row A

		unitassert.assertNotEqual(
			bookA, bookB,
			"Should have two different book IDs", 'testFootnoteBooks')

		cur = con.cursor()
		cur.callproc("GetNewFootnoteGuids", [bookA, bookB])
		row = cur.fetchone()
		#( Get the row we're interested in.
		while row and row[2] != ownord:
			row = cur.fetchone()
		cur.close()

		scrbookid = row[0]
		revid = row[1]
		unitassert.assertEqual(
			guidA, scrbookid,
			"GUID doesn't match for ScrBook", 'testFootnoteBooks')
		unitassert.assertEqual(
			guidB, revid,
			"GUID doesn't match for RevBook", 'testFootnoteBooks')

	def testZFinal(self):
		"""Final unittest.main() operations"""

		#( unittest.main() does not return, but exits the program. This is a
		#( hack for cleanup. It's given the name with a Z in it, because
		#( unittest runs tests in alpha order for some reason. We want this
		#( to be the last test run.

		unitassert.showpassfail()

		con.close()

#TODO: figure out how to get a new class in here
#TODO: get testZFinal() down under this class somehow.

if __name__ == '__main__':
	main()

	#==( Run the tests )==#

	unittest.main()

	#==( Cleanup )==#

	#( Cleanup is moved to testZFinal, because nothing after unittest.main
	#( executes.