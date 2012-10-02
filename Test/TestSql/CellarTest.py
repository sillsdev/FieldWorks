"""
	CellarTest.py

	Description:
		Unit tests for core database code.

	Notes:
		Many of these tests are ported from its predecessor, ut_FwCore.sql and
		ut_FwCore2.sql. By the time the port is finished, it will hopefully
		have unit tests for all the core stored procedures and functions.
"""

#TODO: Figure out how to make System.Data.SqlTypes.SqlInt32.Null global, as well as
# System.Data.SqlTypes.SqlGuid.Null

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
	global nullint

	fwdb = ipymssqlfw.FWDatabase()
	con = fwdb.connect()

	unitassert = unittestasserts.UnitAssert()

#==( Tests )==#

#( Tests are done in alpha order by class name. (Gag.) The cleanup class
#( is named testZFinal, because nothing after unittest.main executes.

class test00ClassInsertTrigger(unittest.TestCase):
	"""Class$ insert trigger test"""

	def testInsertClass(self):
		"""Try inserting a row into Class$."""
		id = 999999
		possibilityid = 7

		cur = con.cursor()
		cur.execute("INSERT INTO Class$ VALUES (%s, %s, %s, %s, %s);" % (
			id, 0, possibilityid, 0, "'classinsert'"))
		cur.close()

		#( Make sure the record made it into Class$

		count = fwdb.fetchonevalue(con,"SELECT COUNT(Id) FROM Class$ WHERE Id = %s;" % (id))
		unitassert.assertEqual(
			count, 1, "The class Test wasn't created in the Class$ table.", "testInsertClass")

		#( Make sure that the new class has a class hierarchy of CmObject (0),
		#( CmPossibility (7), and Test (999999)

		cur = con.cursor()
		cur.execute("SELECT Dst FROM ClassPar$ WHERE Src = %s ORDER BY Depth DESC;" % (id))
		classtree = cur.fetchall()
		cur.close()

		tree = [0, possibilityid, id]

		#added a change here, need to test it.
		itemfound = -1
		for classid in classtree:
			if itemfound == 0:
				break
			else:
				itemfound = 0
			for item in tree:
				if classid[0] == item:
					itemfound = 1
					break
			unitassert.assertEqual(
				itemfound, 1, "class %s not found in ClassPar$" % (classid[0]), "testInsertClass")

		#( Check to see the table got created. If it didn't, this whole test will fail.
		#( The trigger itself has a check to make sure the table was created.

		count = fwdb.fetchonevalue(con,"SELECT COUNT(*) FROM classinsert;")
		unitassert.assertEqual(
			count, 0, "How can the count on classinsert be greater than 0?", "testInsertClass")

		#( The new table will itself have an insert trigger. To test it, we need to:
		#(
		#(  1. add a new column to the test table, because the ID field has special issues
		#(  2. insert a new record into the records reperesenting the test table's parent classes
		#(  3. insert a new record into the test table
		#(  4. get the value of CmObject.UpdDttm
		#(  5. update the column of the test table, which fires the insert trigger, updating
		#(      CmObject.UpdDttm
		#(  6. get the new value of CmObject.UpdDttm, which should be different than the former one.
		#(
		#( This is difficult to accomplish here, and better handled with the test for the CreateObject
		#( stored procedure


#class testFnGetRefsToObj(unittest.TestCase):
#	"""fnGetRefsToObj test"""

#	def testFn(self):
#		ObjId = 1
#		ClassId = System.Data.SqlTypes.SqlInt32.Null

#		count = fwdb.fetchonevalue(con, "SELECT COUNT(*) FROM dbo.fnGetRefsToObj(%s, %s) fn;" %(ObjId, ClassId))
#		print count



class testFieldInsertTrigger(unittest.TestCase):
	"""Field$ insert trigger test"""

	def testInsertField(self):
		"""Try inserting a row into Field$."""

		id = 999991
		possibilityid = 7

		cur = con.cursor()
		cur.execute("INSERT INTO Class$ VALUES (%s, %s, %s, %s, %s);" % (
			id, 0, possibilityid, 0, "'fieldinsert'"))
		cur.close()

		#( Try inserting an int.
		cur = con.cursor()
		cur.execute("INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId) VALUES (999991001, 2, %s, %s, %s, 0, %s);" %
				(id, System.Data.SqlTypes.SqlInt32.Null, "'TestInt'", System.Data.SqlTypes.SqlInt32.Null))
		cur.close()

		count = fwdb.fetchonevalue(con, "SELECT COUNT(TestInt) FROM fieldinsert;")
		unitassert.assertEqual(
		count, 0, "This assert should never hit. If the int didn't get created, this will blow up.",
		'testInsertField')

		#( Try inserting a txt (MultiUnicode).
		cur = con.cursor()
		cur.execute("INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId) VALUES (999991002, 16, %s, %s, %s, 0, %s);" %
				(id, System.Data.SqlTypes.SqlInt32.Null, "'TestTxt'", System.Data.SqlTypes.SqlInt32.Null))
		cur.close()

		count = fwdb.fetchonevalue(con, "SELECT COUNT(Txt) FROM fieldinsert_TestTxt;")
		unitassert.assertEqual(
		count, 0, "This assert should never hit. If the txt didn't get created, this will blow up.",
		'testInsertField')

		#( Try inserting a txt (MultiStr).
		cur = con.cursor()
		cur.execute("INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId) VALUES (999991003, 14, %s, %s, %s, 0, %s);" %
				(id, System.Data.SqlTypes.SqlInt32.Null, "'TestTxtFmt'", System.Data.SqlTypes.SqlInt32.Null))
		cur.close()

		count = fwdb.fetchonevalue(con, "SELECT COUNT(Fmt) FROM fieldinsert_TestTxtFmt;")
		unitassert.assertEqual(
		count, 0, "This assert should never hit. If the txt didn't get created, this will blow up.",
		'testInsertField')

		#( Try inserting an Owning Sequence.
		cur = con.cursor()
		cur.execute("INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId) VALUES (999991004, 27, %s, 7, %s, 0, %s);" %
				(id, "'TestOwnSeq'", System.Data.SqlTypes.SqlInt32.Null))
		cur.close()

		count = fwdb.fetchonevalue(con, "SELECT COUNT(Ord) FROM fieldinsert_TestOwnSeq;")
		unitassert.assertEqual(
		count, 0, "This assert should never hit. If the seq didn't get created, this will blow up.",
		'testInsertField')

		#( Adding an owning atomic StText field requires all existing instances
		#( of the owning class to possess an empty StText and StTxtPara.

		# TODO (SteveMiller): See the SQL code. Not sure how to test this
		# without any instance data.

		#( Try inserting a Reference Collection.
		cur = con.cursor()
		cur.execute("INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId) VALUES (999991005, 26, %s, 7, %s, 0, %s);" %
				(id, "'TestRefCollec'", System.Data.SqlTypes.SqlInt32.Null))
		cur.close()

		count = fwdb.fetchonevalue(con, "SELECT COUNT(Src) FROM fieldinsert_TestRefCollec;")
		unitassert.assertEqual(
		count, 0, "This assert should never hit. If the reference collection didn't get created, this will blow up.",
		'testInsertField')

		#( Testing the insert triggers created by this trigger is easier done
		#( by the CreateObject$ test. See the code there.

		#( Testing the delete triggers created by this trigger is easier done
		#( by the delete architecture tests. See the code there.

		#( Try inserting a decimal.
		cur = con.cursor()
		cur.execute("INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId) VALUES (999991006, 3, %s, %s, %s, 0, %s);" %
				(id, System.Data.SqlTypes.SqlInt32.Null, "'TestDec'", System.Data.SqlTypes.SqlInt32.Null))
		cur.close()

		count = fwdb.fetchonevalue(con, "SELECT COUNT(TestDec) FROM fieldinsert;")
		unitassert.assertEqual(
		count, 0, "This assert should never hit. If the decimal didn't get created, this will blow up.",
		'testInsertField')


class testDefineCreateProc(unittest.TestCase):
	"""DefineCreateProc stored procedure test"""

	def testDefineCreateProc(self):
		"""Generate a MakeObj_ stored procedure for a given ClassId, then run the created procedure."""
		cur = con.cursor()

		null = System.Data.SqlTypes.SqlInt32.Null
		Owner = fwdb.fetchonevalue(con, "SELECT Id FROM CmObject WHERE Guid$='1F6AE209-141A-40DB-983C-BEE93AF0CA3C';")
		target = fwdb.fetchonevalue(con, "SELECT CAST(Id AS NVARCHAR(10)) FROM CmObject WHERE Owner$ = %s AND OwnFlid$ = 7012;" %(Owner))
		OwnFlid = 7012 #( CmPossibility.Discussion
		classid = 14 #( StText

		#( Drop the generated procedure we already have.
		cur.execute("DROP PROCEDURE MakeObj_StText;")

		#( Now run DefineCreateProc$ to regenerate the stored procedure.

		#cur.execute doesn't yet support parameters.
		#cur.execute("EXEC DefineCreateProc$ %s;" %(classid))
		params = cur.callproc("DefineCreateProc$", [classid], "OUTPUT")

		#( Drop the owned object already existing. If it doesn't get dropped, a constraint will complain
		#( when you try to add a new one.

		#cur.execute doesn't yet support parameters.
		#cur.execute("EXEC DeleteObjects %s;" %(classid))
		params = cur.callproc("DeleteObjects", [target, null], "OUTPUT")

		#cur = con.cursor()
		#cur.execute("EXEC MakeObj_StText 0, %s, %s, 0, %s, %s, 0, %s;"  %(Owner, OwnFlid, Null, Null, Null))
		#params = cur.fetchall()
		#cur.close()
		#params = cur.callproc("MakeObj_StText",
		#   [0, Owner, OwnFlid, System.Data.SqlTypes.SqlInt32.Null, System.Data.SqlTypes.SqlInt32.Null,
		#	System.Data.SqlTypes.SqlInt32.Null, 0, System.Data.SqlTypes.SqlInt32.Null])
		#NewObjId = params[1]
		#NewObjGuid = params[2]
		#NewObjTimeStamp = params[3]
		#cur.close();

		#count = fwdb.fetchonevalue(con, "SELECT COUNT(*) FROM CmObject WHERE Guid$ = %s;" %(NewObjGuid))
		#unitassert.assertEqual(count, 1, "There should be exactly one row in CmObject with corresponding Guid", "testDefineCreateProc")

		#id1 = fwdb.fetchonevalue(con, "SELECT Id FROM CmObject WHERE Guid$ = %s;" %(NewObjGuid))
		#id2 = fwdb.fetchonevalue(con, "SELECT Id FROM StText WHERE Id = %s;" %(id1))
		#unitassert.assertEqual(id1, id2, "The Object Id in CmObject should match the Id in its own table", "testDefineCreateProc")

		cur.close()

class test01FieldDeleteTrigger(unittest.TestCase):
	"""Field$ delete trigger test"""

	#( case 1 Field$.Type = 14, 18, 20 with corresponding table names (MultiBigStr$, MultiBigTxt$) respectively.   16 is a different case
	#( case 2 Field$.Type = 23, 25, 27
	#( cases 1 and 2 don't work because of foreign key constraints.  In fact there's a piece of the trigger that never gets executed
	#( see the note in TR_Field$_UpdateModel_Del.  No need to test these.  Case 6 doesn't work for some other odd reason i can't figure out
	def testDeleteField(self):
		"""Test: TR_Field$_UpdateModel_Del"""

		#( case 3
		Type = 26 # table name is CmAnthroItem, could also be 28
		Flid = fwdb.fetchonevalue(con, "SELECT TOP 1 Id FROM Field$ WHERE Type = %s;" % (Type))
		#print "case3 Flid= ", Flid

		Name = fwdb.fetchonevalue(con, "SELECT Name FROM Field$ WHERE Id = %s;" % (Flid))
		Class = fwdb.fetchonevalue(con, "SELECT Name FROM Class$ c WHERE c.Id IN (SELECT Class FROM Field$ WHERE Id = %s);" % (Flid))
		#print "name= ", Name
		#print "class= ", Class

		cur = con.cursor()
		cur.execute("DELETE FROM Field$ WHERE Id = %s;" % (Flid))
		cur.close()

		# selecting from a table that has been dropped throws and EnvironmentError "Invalid Object"
		# if this error is thrown it means the table was dropped properly
		# if caught the assert increments the number of passed tests, else the assert will increment the number of failed tests
		try:
			count = fwdb.fetchonevalue(con, "SELECT COUNT(*) FROM %s_%s;" % (Class,Name))
		except EnvironmentError:
			unitassert.assertEqual(0,0, "There should be no rows in Table %s_%s because it has been dropped" %(Class,Name), "testFieldDelete")
		else:
			unitassert.assertEqual(
				count, 0, "There should be no rows in Table %s_%s because it has been dropped" %(Class,Name), "testFieldDelete")


		#( case 4
		Type = 13 # could also be 17
		Flid = fwdb.fetchonevalue(con, "SELECT TOP 1 Id FROM Field$ WHERE Type = %s;" % (Type))
		#print "case4 Flid= ", Flid

		Name = fwdb.fetchonevalue(con, "SELECT Name FROM Field$ WHERE Id = %s;" % (Flid))
		Class = fwdb.fetchonevalue(con, "SELECT Name FROM Class$ c WHERE c.Id IN (SELECT Class FROM Field$ f WHERE f.Id = %s);" % (Flid))
		#print "name= ", Name
		#print "class= ", Class

		cur = con.cursor()
		cur.execute("DELETE FROM Field$ WHERE Id = %s;" % (Flid))
		cur.close()

		# selecting from a column that has been dropped throws and EnvironmentError "Invalid Column"
		# if this error is thrown it means the column was dropped properly
		# if caught the assert increments the number of passed tests, else the assert will increment the number of failed tests
		try:
			count = fwdb.fetchonevalue(con, "SELECT COUNT(%s_Fmt) FROM %s;" % (Name,Class))
		except EnvironmentError:
			unitassert.assertEqual(0,0,"There should be no column named %s_Fmt in table %s because it has been dropped" %(Name,Class), "testFieldDelete")
		else:
			unitassert.assertEqual(
				count, 0, "There should be no column named %s_Fmt in table %s because it has been dropped" %(Name,Class), "testFieldDelete")

		try:
			count = fwdb.fetchonevalue(con, "SELECT COUNT(%s) FROM %s;" % (Name,Class))
		except EnvironmentError:
			unitassert.assertEqual(0,0,"There should be no column named %s in table %s because it has been dropped" %(Name,Class), "testFieldDelete")
		else:
			unitassert.assertEqual(
				count, 0, "There should be no column named %s in table %s because it has been dropped" %(Name,Class), "testFieldDelete")

		#( case5
		Type = 24
		Flid = fwdb.fetchonevalue(con, "SELECT TOP 1 Id FROM Field$ WHERE Type = %s;" % (Type))
		#print "case5 Flid= ", Flid

		Name = fwdb.fetchonevalue(con, "SELECT Name FROM Field$ WHERE Id = %s;" % (Flid))
		Class = fwdb.fetchonevalue(con, "SELECT Name FROM Class$ c WHERE c.Id IN (SELECT Class FROM Field$ f WHERE f.Id = %s);" % (Flid))
		#print "name= ", Name
		#print "class= ", Class

		cur = con.cursor()
		cur.execute("DELETE FROM Field$ WHERE Id = %s;" % (Flid))
		cur.close()

		# selecting from a column that has been dropped throws and EnvironmentError "Invalid Column"
		# if this error is thrown it means the column was dropped properly
		# if caught the assert increments the number of passed tests, else the assert will increment the number of failed tests
		try:
			count = fwdb.fetchonevalue(con, "SELECT COUNT(%s) FROM %s;" % (Name,Class))
		except EnvironmentError:
			unitassert.assertEqual(0,0,"There should be no column named %s in table %s because it has been dropped" %(Name,Class), "testFieldDelete")
		else:
			unitassert.assertEqual(
				count, 0, "There should be no column named %s in table %s because it has been dropped" %(Name,Class), "testFieldDelete")


		#(case 6
		#Type = 1 # could also be 2,3,4,5,8
		#Flid = fwdb.fetchonevalue(con, "SELECT TOP 1 Id FROM Field$ WHERE Type = %s;" % (Type))
		#print "case6 Flid= ", Flid

		#Name = fwdb.fetchonevalue(con, "SELECT Name FROM Field$ WHERE Id = %s;" % (Flid))
		#Class = fwdb.fetchonevalue(con, "SELECT Name FROM Class$ c WHERE c.Id IN (SELECT Class FROM Field$ f WHERE f.Id = %s);" % (Flid))
		#print "name= ", Name
		#print "class= ", Class

		#cur = con.cursor()
		#try:
		#	cur.execute("DELETE FROM Field$ WHERE Id = %s;" % (Flid))
		#except EnvironmentError, (strerror):
		#	print "EnvironmentError: %s" % (strerror)
		#cur.close()

#( TODO (JohnS, SteveMiller): fix this error
#ERROR: Test: TR_Field$_UpdateModel_Del
#------------------------------------------------------------------
#Traceback (most recent call last):
#  File "CellarTest.py", line 307, in testDeleteField
#    cur.execute("DELETE FROM Field$ WHERE Id = %s;" % (Flid))
#  File "C:\fw\fw\Test\ipymssql\ipymssql.py", line 412, in execute
#    self.datareader = sqlCom.ExecuteNonQuery()
#EnvironmentError: Could not find contents of view classinsert_
#The transaction ended in the trigger. The batch has been aborted.

# this also causes an error with class$ insert.  use the following to reverse it
#delete from field$ where class = 999991
#delete from classpar$ where src = 999991
#delete from class$ where name = 'fieldinsert'
#drop table fieldinsert

		# selecting from a column that has been dropped throws and EnvironmentError "Invalid Column"
		# if this error is thrown it means the column was dropped properly
		# if caught the assert increments the number of passed tests, else the assert will increment the number of failed tests
		#try:
		#	count = fwdb.fetchonevalue(con, "SELECT COUNT(%s) FROM %s;" % (Name,Class))
		#except EnvironmentError:
		#	unitassert.assertEqual(0,0,"There should be no column named %s in table %s because it has been dropped" %(Name,Class), "testFieldDelete")
		#else:
		#	unitassert.assertEqual(
		#		count, 0, "There should be no column named %s in table %s because it has been dropped" %(Name,Class), "testFieldDelete")



class test02CreateObject(unittest.TestCase):
	"""CreateObject$ test"""

	#( CreateObject$ creates a new object for a given class ID.
	#(
	#( The insert trigger for a given object table, such as CmPerson, is created by the Class$
	#( insert trigger, but it is very difficult to test it at that point. It gets tested here.

	def testCreatePerson(self):
		"""Try creating a CmPerson object."""
		personclassid = fwdb.fetchonevalue(con, "SELECT Id FROM Class$ WHERE Name = 'CmPerson';")

		newObjId = -1
		cur = con.cursor()
		params = cur.callproc(
			"CreateObject$",
			[personclassid, System.Data.SqlTypes.SqlInt32.Null, System.Data.SqlTypes.SqlGuid.Null])
		objid = params[2]
		cur.close()

		#( We will be looping through the class tree found in ClassPar$, making sure
		#( the object is created in each of the tables that represent classes.

		cur = con.cursor()
		cur.execute(
			"SELECT c.Name FROM ClassPar$ cp JOIN Class$ c ON c.Id = cp.Dst WHERE cp.Src = %s ORDER BY cp.Depth DESC;" %
			(personclassid))
		classtree = cur.fetchall()
		cur.close()

		for classname in classtree:
		   count = fwdb.fetchonevalue(con, "SELECT COUNT(Id) FROM %s WHERE Id = %s;" % (classname[0], objid))
		   unitassert.assertEqual(count, 1, "The object wasn't created in %s" % (classname[0]), "testCreatePerson")

		#( Check that the update trigger updates CmObject.UpdDttm. A smalldatetime in SQL Sever
		#( has an accuracy of only one minute. Therefore, a comparison of UpdDttm will always fail.
		#( However, whenever anything is updated in CmObject, the UpdStmp field gets updated to
		#( a new value.

		updstmp1 = fwdb.fetchonevalue(con,"SELECT UpdStmp FROM CmObject WHERE Id = %s;" % (objid))

		cur = con.cursor()
		cur.execute("UPDATE CmPerson SET Gender = 1 WHERE Id = %s;" % (objid))
		cur.close()

		updstmp2 = fwdb.fetchonevalue(con,"SELECT UpdStmp FROM CmObject WHERE Id = %s;" % (objid))
		unitassert.assertNotEqual(
			updstmp1, updstmp2, "CmObject.UpdStmp should have changed", "testInsertClass")

		#( Check that the reference collection table insert trigger updates CmObject.UpdDttm.
		#( The trigger gets created by the Field$ insert trigger, but it is easier to test it
		#( here.

		updstmp1 = fwdb.fetchonevalue(con,"SELECT UpdStmp FROM CmObject WHERE Id = %s;" % (objid))

		cur = con.cursor()
		cur.execute("UPDATE CmPerson_Positions SET Src = Src WHERE Src = %s;" % (objid))
		cur.close()

		updstmp2 = fwdb.fetchonevalue(con,"SELECT UpdStmp FROM CmObject WHERE Id = %s;" % (objid))
		unitassert.assertNotEqual(
			updstmp1, updstmp2, "CmObject.UpdStmp should have changed for CmPerson_Positions", "testInsertClass")


class testCopyObj(unittest.TestCase):
	"""CopyObj$ test"""

	#( (This test was ported from tsqlunit.)

	#( CopyObj$ copies all the attributes from one object to a new one
	#( of the same type. On the database side, it must create records
	#( for each of the super classes, owned objects, and referencing
	#( object information.

	def testNoMultiBigTxt(self):
		"""MultiBigTxt is not implemented yet."""

		#( A "MultiBigTxt" is a FieldWorks type. No object has this type to
		#( date. If one turns up, we need to make sure we can handle it.

		id = fwdb.fetchonevalue(con, "select ID from FIELD$ where TYPE = 20")
		unitassert.assertEqual(
			id, None,
			'MultiBigTxt is not yet implemented, per YAGNI.', 'testNoMultiBigTxt')

	def testCopyPossList(self):
		"""Test: CopyObj$ copying Possibility Lists"""

		#( In database terms, a PossibilityList is a lookup table. In FieldWorks,
		#( a class has been set up to handle various lists. The lists are subclassed
		#( from a variety of classes, and copying a PossibilityList is harder than
		#( first appears.

		sourceObjId = fwdb.fetchonevalue(
			con, "select Obj from CMMAJOROBJECT_NAME where TXT = 'Confidence Levels';")
		unitassert.assertNotEqual(
			sourceObjId, None,
			"The database has changed. Can't find 'Confidence Levels'.", 'testCopyPossList')

		#--( Execute the proc )--#

		cur = con.cursor()
		params = cur.callproc("CopyObj$", [sourceObjId, 1, 6001025, 0])
		newObjId = params[4]
		cur.close()

		#--( Check )--#

		txt = fwdb.fetchonevalue(
			con, "select TXT from CMMAJOROBJECT_NAME where OBJ = %s and TXT = 'Confidence Levels';" % (newObjId))
		unitassert.assertNotEqual(
			txt, None, "Can't find copied object 'Confidence Levels'",'testCopyPossList')

		dateSource = fwdb.fetchonevalue(
			con, "select DATEMODIFIED from CMMAJOROBJECT where ID = %s;" % sourceObjId)
		dateTarget = fwdb.fetchonevalue(
			con, "select DATEMODIFIED from CMMAJOROBJECT where ID = %s;" % newObjId)
		unitassert.assertNotEqual(
			dateSource, dateTarget,
			"Date modified on the new object hasn't been modified.", 'testCopyPossList')

	def testCopyRefToObj(self):
		"""Test: Copy possibility list references"""

		#( When an object owns other objects, the owned objects get copied.
		#( Referenced objects don't get copied. However, a referenced
		#( object might be part of the ownership tree that gets copied.
		#( In that case, we want to reference the newly copied object,
		#( not the source object. For example, the People possibility
		#( list owns CmPerson items. CmPerson is subclassed from
		#( CmPossibility, which has a reference to Researchers.
		#( Researchers is the very same People possibility list we're
		#( copying. Therefore, CmPerson can point to itself as a
		#( reference. (Clear as mud?)

		livingstonId = fwdb.fetchonevalue(
			con, "select OBJ from CMPOSSIBILITY_NAME where TXT = 'D. Livingston';")
		unitassert.assertNotEqual(
			livingstonId, None,
			"The database has changed. Can't find 'D. Livingston'.",
			'testCopyRefToObj')

		#( The reference to itself does not exist in the current TestLangProj.
		#( We'll see about this one anyway. If it's not there, we'll create it
		#( for the test.

		id = fwdb.fetchonevalue(
			con, "select SRC from CMPOSSIBILITY_RESEARCHERS where SRC = %s and DST = %s;" % (
			livingstonId, livingstonId))
		if not id:
			cur = con.cursor()
			cur.execute("INSERT INTO CmPossibility_Researchers VALUES (%s, %s);" % (
				livingstonId, livingstonId))
			cur.close()

		newObjId = -1
		cur = con.cursor()
		params = cur.callproc(
			"CopyObj$",
			[livingstonId, System.Data.SqlTypes.SqlInt32.Null, System.Data.SqlTypes.SqlInt32.Null, -1])
		newObjId = params[4]
		cur.close()
		unitassert.assertNotEqual(
			newObjId, None, "The CmPossibility_Researchers reference didn't get copied right.",
			'testCopyRefToObj')

	def testCopyRnEvent(self):

		#( An RnEvent has some properties that the previous tests do not have.

		"""Test: copy an RnEvent"""

		eventId = fwdb.fetchonevalue(
			con, "SELECT eventbase.Id " +
			"FROM CmObject eventbase " +
			"JOIN CmObject txtbase ON txtbase.Owner$ = eventbase.Id " +
			"JOIN CmObject txtparabase ON txtparabase.Owner$ = txtbase.Id " +
			"JOIN StTxtPara tp ON tp.Id = txtparabase.Id " +
			"AND tp.Contents LIKE 'Contains information%';")
		unitassert.assertNotEqual(
			eventId, None, "RnEvent doesn't exist anymore.", 'testCopyRnEvent')

		newObjId = -1
		cur = con.cursor()
		params = cur.callproc(
			"CopyObj$",
			[eventId, System.Data.SqlTypes.SqlInt32.Null, System.Data.SqlTypes.SqlInt32.Null, -1])
		newObjId = params[4]
		cur.close()
		unitassert.assertNotEqual(
			newObjId, None, "CopyObj$ didn't create a new RnEvent.", 'testCopyRnEvent')

		#--( RnGenericRec, a base of RnEvent )--#

		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(rg1.Id) " +
			"FROM RnGenericRec rg1 " +
			"JOIN RnGenericRec rg2 ON " +
			"rg2.Title = rg1.Title AND " +
			"rg2.Title_Fmt = rg1.Title_Fmt AND " +
			"rg2.Confidence = rg1.Confidence " +
			"WHERE rg1.Id = %s AND rg2.Id = %s;" % (eventId, newObjId))
		unitassert.assertEqual(
			count, 1, "New RnGenericRec Title, Title_Fmt, or Confidence doesn't agree with source RnGenericRec.",
			'testCopyRnEvent')

		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(Id) " +
			"FROM RnGenericRec " +
			"WHERE Id = %s AND DATEDIFF(day, DateCreated, GETDATE()) != 0;" % (newObjId))
		unitassert.assertEqual(
			count, 0, "DateCreated doesn't have today's date", 'testCopyRnEvent')

		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(Id) " +
			"FROM RnGenericRec " +
			"WHERE Id = %s AND DATEDIFF(day, DateModified, GETDATE()) != 0;" % (newObjId))
		unitassert.assertEqual(
			count, 0, "DateModified doesn't have today's date", 'testCopyRnEvent')

		#--( The RnEvent record iteslf )--#

		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(Id) FROM RnEvent WHERE Id = %s;" % (newObjId))
		unitassert.assertEqual(
			count, 1, "The RnEvent record itself is wrong.", 'testCopyRnEvent')

		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(e1.Id) " +
			"FROM RnEvent e1 " +
			"JOIN RnEvent e2 ON e2.Type = e1.Type AND e2.DateOfEvent = e1.DateOfEvent " +
			"WHERE e1.Id = %s AND e2.Id = %s;" % (eventId, newObjId))
		unitassert.assertEqual(
			count, 1, "New RnEvent Type or DateOfEvent doesn't agree with Source RnEvent",
			'testCopyRnEvent')

		#--( RnGenericRec Relationships )--#

		#( RnGenericRec.VersionHistory: owning atomic. It should own an
		#( StText, which in turn owns a StTxtPara.
		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(Id) " +
			"FROM CmObject WHERE Owner$ = %s AND OwnFlid$ = 4004002;" % (newObjId))
		unitassert.assertEqual(count, 1, "Version history is wrong.", "testCopyRnEvent")

		stTextId = fwdb.fetchonevalue(
			con, "SELECT Id FROM CmObject WHERE Owner$ = %s AND OwnFlid$ = 4004002;" % (newObjId))

		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(Id) FROM StText WHERE Id = %s;" % (stTextId))
		unitassert.assertEqual(count, 1, "StText owned by VersionHistory is wrong.", "testCopyRnEvent")

		#( RnGenericRec.Reminders: reference collection. No Reminders here; nothing much to check
		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(Src) FROM RnGenericRec_Reminders WHERE Src = %s;" % (newObjId)) #( 4004003
		unitassert.assertEqual(count, 0, "Reminders is wrong.", "testCopyRnEvent")

		#( RnGenericRec.Researchers: reference collection. It references a CmPerson
		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(Src) FROM RnGenericRec_Researchers WHERE Src = %s;" % (newObjId)) #( 4004004
		unitassert.assertEqual(count, 1, "Researchers is wrong.", "testCopyRnEvent")

		#( RnGenericRec.Confidence: reference atomic. (flid 4004005) The
		#( Confidence field exists directly in the RnGenericRec record.
		id = fwdb.fetchonevalue(
			con, "SELECT Confidence FROM RnGenericRec WHERE Id = %s;" % (newObjId))

		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(Id) FROM CmPossibility WHERE Id = %s;" % (id))
		unitassert.assertEqual(
			count, 1,
			"CmPossibility for RnGenericRec.Confidence reference is missing", "testCopyRnEvent")

		#( RnGenericRec.Restrictions: reference collection.
		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(Src) FROM RnGenericRec_Restrictions WHERE Src = %s;" % (newObjId)) #( 4004006
		unitassert.assertEqual(count, 0, "Restrictions is wrong.", "testCopyRnEvent")

		#( RnGenericRec.AnthroCodes: reference collection.
		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(Src) FROM RnGenericRec_AnthroCodes WHERE Src = %s;" % (newObjId)) #( 4004007
		unitassert.assertEqual(count, 3, "AnthroCodes is wrong.", "testCopyRnEvent")

		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(Id) FROM CmAnthroItem WHERE Id IN (SELECT Dst FROM RnGenericRec_AnthroCodes WHERE Src = %s);" % (newObjId))
		unitassert.assertEqual(
			count, 3,
			"AnthroItems for RnGenericRec_AnthroCodes references are missing.", "testCopyRnEvent")

		#( RnGenericRec.PhraseTags: reference collection.
		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(Src) FROM RnGenericRec_PhraseTags WHERE Src = %s;" % (newObjId)) #( 4004008
		unitassert.assertEqual(count, 0, "PhraseTags is wrong.", "testCopyRnEvent")

		#( SubRecords: owning sequence. This will be one of the most intense workouts the stored
		#( procedure will get in copying owned objects.

		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(Id) FROM CmObject WHERE Owner$ = %s AND OwnFlid$ = 4004009;" % (newObjId))
		unitassert.assertEqual(count, 5, "SubRecords number is wrong.", "testCopyRnEvent")

		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(Id) FROM RnGenericRec WHERE Id IN (SELECT Id FROM CmObject WHERE Owner$ = %s AND OwnFlid$ = 4004009);" % (newObjId))
		unitassert.assertEqual(count, 5, "RnGenericRecs base for SubRecords number is wrong.", "testCopyRnEvent")

		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(Id) FROM RnEvent WHERE Id IN (SELECT Id FROM CmObject WHERE Owner$ = %s AND OwnFlid$ = 4004009);" % (newObjId))
		unitassert.assertEqual(count, 4, "RnEvents for SubRecords number is wrong.", "testCopyRnEvent")

		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(Id) FROM RnAnalysis WHERE Id IN (SELECT Id FROM CmObject WHERE Owner$ = %s AND OwnFlid$ = 4004009);" % (newObjId))
		unitassert.assertEqual(count, 1, "RnAnalysis base for SubRecords number is wrong.", "testCopyRnEvent")

		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(Id) FROM CmObject WHERE Owner$ IN (SELECT Id FROM CmObject WHERE Owner$ = %s AND OwnFlid$ = 4004009);" % (newObjId))
		#( Objects owned by 4 RnEvents and 1 RnAnalysis
		unitassert.assertEqual(count, 35, "Objects owned by subRecords number is wrong.", "testCopyRnEvent")

		#( RnGenericRec.CrossReferences: reference collection
		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(Src) FROM RnGenericRec_CrossReferences WHERE Src = %s;" % (newObjId)) #( 4004012
		unitassert.assertEqual(count, 0, "CrossReferences is wrong.", "testCopyRnEvent")

		#( External Materials: Owning Atomic
		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(Id) FROM CmObject WHERE Owner$ = %s AND OwnFlid$ = 4004013;" % (newObjId))
		unitassert.assertEqual(count, 1, "External Materials is wrong.", "testCopyRnEvent")

		#( FurtherQuestions: Owning Atomic
		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(Id) FROM CmObject WHERE Owner$ = %s AND OwnFlid$ = 4004014;" % (newObjId))
		unitassert.assertEqual(count, 1, "FurtherQuestions is wrong.", "testCopyRnEvent")

		#( RnGenericRec.SeeAlso: Reference Collection
		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(Src) FROM RnGenericRec_SeeAlso WHERE Src = %s;" % (newObjId)) #( 4004015
		unitassert.assertEqual(count, 0, "SeeAlso is wrong.", "testCopyRnEvent")

		#--( RnEvent Relationships )--#

		#( RnEvent.Description: owning atomic
		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(Id) FROM CmObject WHERE Owner$ = %s AND OwnFlid$ = 4006001;" % (newObjId))
		unitassert.assertEqual(count, 1, "Description is wrong.", "testCopyRnEvent")

		#( RnEvent.Participants: owning collection
		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(Id) FROM CmObject WHERE Owner$ = %s AND OwnFlid$ = 4006002;" % (newObjId))
		unitassert.assertEqual(count, 4, "Participants is wrong.", "testCopyRnEvent")

		#( RnEvent.Locations: reference collection
		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(Src) FROM RnEvent_Locations WHERE Src = %s;" % (newObjId)) #( 4006003
		unitassert.assertEqual(count, 1, "Locations is wrong.", "testCopyRnEvent")

		#( RnEvent.Type: reference atomic. (flid 4006004) The value exists directly in the RnEvent record.

		#( RnEvent.Weather: reference collection
		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(Src) FROM RnEvent_Weather WHERE Src = %s;" % (newObjId)) #( 4006006
		unitassert.assertEqual(count, 1, "Weather is wrong.", "testCopyRnEvent")

		#( RnEvent.Sources: reference collection
		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(Src) FROM RnEvent_Sources WHERE Src = %s;" % (newObjId)) #( 4006007
		unitassert.assertEqual(count, 1, "Sources is wrong.", "testCopyRnEvent")

		#( RnEvent.TimeOfEvent: reference collection
		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(Src) FROM RnEvent_TimeOfEvent WHERE Src = %s;" % (newObjId)) #( 4006009
		unitassert.assertEqual(count, 1, "TimeOfEvent is wrong.", "testCopyRnEvent")

		#( RnEvent.PersonalNotes
		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(Id) FROM CmObject WHERE Owner$ = %s AND OwnFlid$ = 4006010;" % (newObjId))
		unitassert.assertEqual(count, 1, "Personal Notes is wrong.", "testCopyRnEvent")

	def testCopyUserView(self):

		#( Copy a UserView for some more checks.

		"""Test: copy a UserView"""

		userviewId = fwdb.fetchonevalue(
			con, "SELECT Obj " +
			"FROM UserView_Name uvn " +
			"JOIN UserView uv ON uv.Id = uvn.Obj " +
			"WHERE uvn.Txt = 'Browse';")
		unitassert.assertNotEqual(
			userviewId, None, "Userview doesn't exist anymore.", 'testCopyRnEvent')

		newObjId = -1
		cur = con.cursor()
		params = cur.callproc(
			"CopyObj$",
			[userviewId, System.Data.SqlTypes.SqlInt32.Null, System.Data.SqlTypes.SqlInt32.Null, -1])
		newObjId = params[4]
		cur.close()
		unitassert.assertNotEqual(
			newObjId, None, "CopyObj$ didn't create a new Id.", 'testCopyRnEvent')

		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(uv1.Id) " +
			"FROM UserView uv1 " +
			"JOIN UserView uv2 ON " +
			"uv2.Type = uv1.Type AND " +
			"uv2.App = uv1.App AND " +
			"uv2.System = uv1.System AND " +
			"uv2.SubType = uv1.SubType " +
			"WHERE uv1.Id = %s AND uv2.Id = %s;" % (userviewId, newObjId))
		unitassert.assertEqual(
			count, 1, "New UserView record doesn't match with the new one.",
			'testCopyRnEvent')

		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(Obj) FROM UserView_Name WHERE Obj = %s;" % (newObjId))

		unitassert.assertEqual(count, 1, "New UserView Name is wrong.", "testCopyRnEvent")

	def testCopyScrBook(self):

		#( Copy a ScrBook for some final checks.

		"""Test: copy a ScrBook"""

		scrbookId = fwdb.fetchonevalue(
			con, "SELECT Obj " +
			"FROM ScrBook_Name sbn " +
			"WHERE sbn.Txt = 'Philemon';")
		unitassert.assertNotEqual(
			scrbookId, None, "Philemon doesn't exist anymore.", 'testCopyRnEvent')

		cur = con.cursor()
		cur.execute("SELECT Owner$, OwnFlid$ FROM CmObject WHERE Id = %s;" % (scrbookId))
		row = cur.fetchone()
		if row:
			ownerId, ownFlid = row
		cur.close()

		newObjId = -1
		cur = con.cursor()
		params = cur.callproc("CopyObj$", [scrbookId, ownerId, ownFlid, -1])
		newObjId = params[4]
		cur.close()

		count = fwdb.fetchonevalue(
			con, "SELECT COUNT(*) FROM ScrBook_Name WHERE Obj = %s AND Txt = 'Philemon';" % (newObjId))
		unitassert.assertEqual(count, 1, "Philemon didn't get copied.", "testCopyRnEvent")

class testZFinalCleanup(unittest.TestCase):
	"""Final unittest operations"""

	def testCleanup(self):
		"""Final unittest.main() operations"""

		#( unittest.main() does not return, but exits the program. This is a
		#( hack for cleanup. It's given the name with a Z in it, because
		#( unittest runs tests in alpha order for some reason. We want this
		#( to be the last test run.

		unitassert.showpassfail()

		con.close()

if __name__ == '__main__':
	main()

	#==( Run the tests )==#

	unittest.main()

	#==( Cleanup )==#

	#( Cleanup is moved to testZFinal, because nothing after unittest.main
	#( executes.
