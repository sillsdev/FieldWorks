using System;
using System.Xml;
using System.IO;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.CacheLight;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using System.Text;

namespace SIL.FieldWorks.CacheLightTests
{
	#region MetaDataCacheInitializationTests class

	/// <summary>
	/// Test cache initialization.
	/// </summary>
	[TestFixture]
	public class MetaDataCacheInitializationTests: SIL.FieldWorks.Test.TestUtils.BaseTest
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Don't use the real file system
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			FileUtils.Manager.SetFileAdapter(new MockFileOS());
		}

		/// <summary/>
		public override void FixtureTeardown()
		{
			FileUtils.Manager.Reset();
			base.FixtureTeardown();
		}

		/// <summary>
		/// Tests creating a MetaDataCache with no input pathname to the XML file.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void CreateMetaDataCacheNoFile()
		{
			MetaDataCache.CreateMetaDataCache(null);
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void CreateMetaDataCacheEmptyPathname()
		{
			MetaDataCache.CreateMetaDataCache("");
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		[ExpectedException(typeof(FileNotFoundException))]
		public void CreateMetaDataCacheBadPathname()
		{
			MetaDataCache.CreateMetaDataCache("MyBadpathname");
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		[ExpectedException(typeof(XmlException))]
		public void CreateMetaDataCacheNotXMLData()
		{
			// <?xml version="1.0" encoding="utf-8"?>
			const string bogusDataPathname = "Bogus.txt";
			using (var w = FileUtils.OpenFileForWrite(bogusDataPathname, Encoding.UTF8))
			{
				w.WriteLine("Non-XML data");
				w.Flush();
				w.Close();
			}
			try
			{
				MetaDataCache.CreateMetaDataCache(bogusDataPathname);
			}
			finally
			{
				FileUtils.Delete(bogusDataPathname);
			}
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void CreateMetaDataCacheWrongXMLData()
		{
			const string bogusDataPathname = "Bogus.xml";
			using (var w = FileUtils.OpenFileForWrite(bogusDataPathname, Encoding.UTF8))
			{
				w.WriteLine("<?xml version='1.0' encoding='utf-8'?>");
				w.WriteLine("<BogusElement/>");
				w.Flush();
				w.Close();
			}
			try
			{
				MetaDataCache.CreateMetaDataCache(bogusDataPathname);
			}
			finally
			{
				FileUtils.Delete(bogusDataPathname);
			}
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		public void CreateMetaDataCacheEntireModelXMLData()
		{
			const string bogusDataPathname = "Bogus.xml";
			using (var w = FileUtils.OpenFileForWrite(bogusDataPathname, Encoding.UTF8))
			{
				w.WriteLine("<?xml version='1.0' encoding='utf-8'?>");
				w.WriteLine("<EntireModel>");
				w.WriteLine("<CellarModule id='ling' num='5'/>");
				w.WriteLine("</EntireModel>");
				w.Flush();
				w.Close();
			}
			try
			{
				MetaDataCache.CreateMetaDataCache(bogusDataPathname);
			}
			finally
			{
				FileUtils.Delete(bogusDataPathname);
			}
		}

		/// <summary>
		/// Tests creating a MetaDataCache with an XML file containing no classes.
		/// </summary>
		[Test]
		public void CreateMetaDataCacheCellarModuleXMLData()
		{
			const string bogusDataPathname = "Bogus.xml";
			using (var w = FileUtils.OpenFileForWrite(bogusDataPathname, Encoding.UTF8))
			{
				w.WriteLine("<?xml version='1.0' encoding='utf-8'?>");
				w.WriteLine("<EntireModel/>");
				w.Flush();
				w.Close();
			}
			try
			{
				MetaDataCache.CreateMetaDataCache(bogusDataPathname);
			}
			finally
			{
				FileUtils.Delete(bogusDataPathname);
			}
		}

		/// <summary>
		/// Tests attempting to initialize a MetaDataCache twice with the same XML file.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void CreateMetaDataCacheDuplicateFiles()
		{
			const string bogusDataPathname = "Good.xml";
			using (var w = FileUtils.OpenFileForWrite(bogusDataPathname, Encoding.UTF8))
			{
				w.WriteLine("<?xml version='1.0' encoding='utf-8'?>");
				w.WriteLine("<EntireModel/>");
				w.Flush();
				w.Close();
			}
			IFwMetaDataCache mdc;
			try
			{
				mdc = MetaDataCache.CreateMetaDataCache(bogusDataPathname);
			}
			finally
			{
				FileUtils.Delete(bogusDataPathname);
			}
			// Not good, since a file with that name has already been processed.
			using (var w = File.CreateText(bogusDataPathname))
			{
				w.WriteLine("<?xml version='1.0' encoding='utf-8'?>");
				w.WriteLine("<EntireModel/>");
				w.Flush();
				w.Close();
			}
			try
			{
				mdc.InitXml(bogusDataPathname, false);
			}
			finally
			{
				FileUtils.Delete(bogusDataPathname);
			}
		}

		/// <summary>
		/// Tests creating a MetaDataCache with duplicate classes defined in an XML file.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void CreateMetaDataCacheDuplicateClassesInXmlFile()
		{
			const string bogusDataPathname = "Bogus.xml";
			using (var w = FileUtils.OpenFileForWrite(bogusDataPathname, Encoding.UTF8))
			{
				w.WriteLine("<?xml version='1.0' encoding='utf-8'?>");
				w.WriteLine("<EntireModel>");
				w.WriteLine("<class num='0' id='BaseClass' abstract='true'/>");
				w.WriteLine("<class num='1' id='ClassA' abstract='false' base='BaseClass'/>");
				w.WriteLine("<class num='1' id='ClassA' abstract='false' base='BaseClass'/>");
				w.WriteLine("</EntireModel>");
				w.Flush();
				w.Close();
			}
			try
			{
				MetaDataCache.CreateMetaDataCache(bogusDataPathname);
			}
			finally
			{
				FileUtils.Delete(bogusDataPathname);
			}
		}

		/// <summary>
		/// Tests creating a MetaDataCache with duplicate classes defined in different XML files.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void CreateMetaDataCacheDuplicateClassesInTwoFiles()
		{
			var bogusDataPathname = "Good.xml";
			using (var w = FileUtils.OpenFileForWrite(bogusDataPathname, Encoding.UTF8))
			{
				w.WriteLine("<?xml version='1.0' encoding='utf-8'?>");
				w.WriteLine("<EntireModel>");
				w.WriteLine("<class num='0' id='BaseClass' abstract='true'/>");
				w.WriteLine("<class num='1' id='ClassA' abstract='false' base='BaseClass'/>");
				w.WriteLine("</EntireModel>");
				w.Flush();
				w.Close();
			}
			IFwMetaDataCache mdc;
			try
			{
				mdc = MetaDataCache.CreateMetaDataCache(bogusDataPathname);
			}
			finally
			{
				FileUtils.Delete(bogusDataPathname);
			}
			// Not good, since a file with that name has already been processed.
			bogusDataPathname = "ReallyGood.xml";
			using (var w = FileUtils.OpenFileForWrite(bogusDataPathname, Encoding.UTF8))
			{
				w.WriteLine("<?xml version='1.0' encoding='utf-8'?>");
				w.WriteLine("<EntireModel>");
				w.WriteLine("<class num='1' id='ClassA' abstract='false' base='BaseClass'/>");
				w.WriteLine("</EntireModel>");
				w.Flush();
				w.Close();
			}
			try
			{
				mdc.InitXml(bogusDataPathname, false);
			}
			finally
			{
				FileUtils.Delete(bogusDataPathname);
			}
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-duplicate classes.
		/// </summary>
		[Test]
		public void CreateMetaDataCacheNonDuplicateClasses()
		{
			const string bogusDataPathname = "Good.xml";
			using (var w = FileUtils.OpenFileForWrite(bogusDataPathname, Encoding.UTF8))
			{
				w.WriteLine("<?xml version='1.0' encoding='utf-8'?>");
				w.WriteLine("<EntireModel>");
				w.WriteLine("<class num='0' id='BaseClass' abstract='true'/>");
				w.WriteLine("<class num='2' id='ClassD' abstract='false' base='BaseClass'/>");
				w.WriteLine("<class num='14' id='LexPronunciation' abstract='false' base='BaseClass'/>");
				w.WriteLine("</EntireModel>");
				w.Flush();
				w.Close();
			}
			try
			{
				MetaDataCache.CreateMetaDataCache(bogusDataPathname);
			}
			finally
			{
				FileUtils.Delete(bogusDataPathname);
			}
		}
	}

	#endregion MetaDataCacheInitializationTests class

	#region MetaDataCacheBase class

	/// <summary>
	/// Base class for testing the field, class, and virtual methods.
	/// </summary>
	public class MetaDataCacheBase
	{
		/// <summary></summary>
		protected IFwMetaDataCache m_metaDataCache;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public virtual void FixtureSetup()
		{
			FileUtils.Manager.SetFileAdapter(new MockFileOS());
			using (TextWriter fw = FileUtils.OpenFileForWrite("TestModel.xsd", Encoding.UTF8))
				fw.Write(Properties.Resources.TestModel_xsd);
			using (TextWriter fw = FileUtils.OpenFileForWrite("TestModel.xml", Encoding.UTF8))
				fw.Write(Properties.Resources.TestModel_xml);
			m_metaDataCache = MetaDataCache.CreateMetaDataCache("TestModel.xml");
		}

		/// <summary/>
		[TestFixtureTearDown]
		public virtual void FixtureTearDown()
		{
			FileUtils.Manager.Reset();
		}
		}

	#endregion MetaDataCacheBase class

	#region MetaDataCacheFieldAccessTests class

	/// <summary>
	/// Test the field access methods.
	/// </summary>
	[TestFixture]
	public class MetaDataCacheFieldAccessTests : MetaDataCacheBase
	{
		/// <summary>
		/// Test of GetDstClsName method
		/// </summary>
		[Test]
		public void GetDstClsNameTest()
		{
			Assert.AreEqual("ClassL", m_metaDataCache.GetDstClsName(59005), "Wrong class name");
		}

		/// <summary>
		/// Test of GetOwnClsName method
		/// </summary>
		[Test]
		public void GetOwnClsNameTest()
		{
			Assert.AreEqual("ClassG", m_metaDataCache.GetOwnClsName(15068), "Wrong class name");
		}

		/// <summary>
		/// Test of GetOwnClsId method
		/// </summary>
		[Test]
		public void GetOwnClsIdTest()
		{
			Assert.AreEqual(15, m_metaDataCache.GetOwnClsId(15068), "Wrong class implementor.");
		}

		/// <summary>
		/// Test of GetDstClsId method
		/// </summary>
		[Test]
		public void GetDstClsIdTest()
		{
			Assert.AreEqual(49, m_metaDataCache.GetDstClsId(59003), "Wrong class Signature.");
		}

		/// <summary>
		/// This should test for any case where the given flid is not valid.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void GetClsNameForBadFlidTest()
		{
			m_metaDataCache.GetOwnClsName(50);
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void GetFieldIdsTest()
		{
			var flidSize = m_metaDataCache.FieldCount;

			int[] ids;
			var testFlidSize = flidSize - 1;
			using (var flids = MarshalEx.ArrayToNative(testFlidSize, typeof(int)))
			{
				m_metaDataCache.GetFieldIds(testFlidSize, flids);
				ids = (int[])MarshalEx.NativeToArray(flids, testFlidSize, typeof(int));
				Assert.AreEqual(testFlidSize, ids.Length, "Wrong size of fields returned.");
				foreach (var flid in ids)
					Assert.IsTrue(flid > 0, "Wrong flid value: " + flid);
			}
			testFlidSize = flidSize;
			using (var flids = MarshalEx.ArrayToNative(testFlidSize, typeof(int)))
			{
				m_metaDataCache.GetFieldIds(testFlidSize, flids);
				ids = (int[])MarshalEx.NativeToArray(flids, testFlidSize, typeof(int));
				Assert.AreEqual(testFlidSize, ids.Length, "Wrong size of fields returned.");
				foreach (var flid in ids)
					Assert.IsTrue(flid > 0, "Wrong flid value: " + flid);
			}
			testFlidSize = flidSize + 1;
			using (var flids = MarshalEx.ArrayToNative(testFlidSize, typeof(int)))
			{
				m_metaDataCache.GetFieldIds(testFlidSize, flids);
				ids = (int[])MarshalEx.NativeToArray(flids, testFlidSize, typeof(int));
				Assert.AreEqual(testFlidSize, ids.Length, "Wrong size of fields returned.");
				for (var iflid = 0; iflid < ids.Length; ++iflid)
				{
					var flid = ids[iflid];
					if (iflid < ids.Length - 1)
						Assert.IsTrue(flid > 0, "Wrong flid value: " + flid);
					else
						Assert.AreEqual(0, flid, "Wrong value for flid beyond actual length.");
				}
			}
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetFieldNameTest()
		{
			Assert.AreEqual("MultiUnicodeProp12", m_metaDataCache.GetFieldName(2003));
		}

		/// <summary>
		/// CacheLight doesn't support field labels, so they are always null
		/// </summary>
		[Test]
		public void GetFieldLabelIsNullTest()
		{
			Assert.IsNull(m_metaDataCache.GetFieldLabel(59003), "Field label not null.");
		}

		/// <summary>
		/// CacheLight doesn't support field help, so it is always null
		/// </summary>
		[Test]
		public void GetFieldHelpIsNullTest()
		{
			Assert.IsNull(m_metaDataCache.GetFieldHelp(59003), "Field help not null.");
		}

		/// <summary>
		/// CacheLight doesn't support field XML, so it is always null
		/// </summary>
		[Test]
		public void GetFieldXmlIsNullTest()
		{
			Assert.IsNull(m_metaDataCache.GetFieldXml(59003), "Field XML not null.");
		}

		/// <summary>
		/// CacheLight doesn't support writing system selectors, so they are always 0
		/// </summary>
		[Test]
		public void GetFieldWsIsZeroTest()
		{
			Assert.AreEqual(0, m_metaDataCache.GetFieldWs(59003), "Writing system not zero.");
		}

		/// <summary>
		/// Check for all the types used in the model.
		/// </summary>
		/// <remarks>
		/// See the Same named method in the NoRealDataForTests for data types not used in the model.
		/// </remarks>
		[Test]
		public void GetFieldTypeTest()
		{
			Assert.AreEqual(CellarPropertyType.Boolean, (CellarPropertyType)m_metaDataCache.GetFieldType(2027),
				"Wrong field data type for Boolean data.");

			Assert.AreEqual(CellarPropertyType.Integer, (CellarPropertyType)m_metaDataCache.GetFieldType(26002),
				"Wrong field data type for Integer data.");

			Assert.AreEqual(CellarPropertyType.Time, (CellarPropertyType)m_metaDataCache.GetFieldType(2005),
				"Wrong field data type for Time data.");

			Assert.AreEqual(CellarPropertyType.Guid, (CellarPropertyType)m_metaDataCache.GetFieldType(8002),
				"Wrong field data type for Guid data.");

			Assert.AreEqual(CellarPropertyType.GenDate, (CellarPropertyType)m_metaDataCache.GetFieldType(13004),
				"Wrong field data type for GenDate data.");

			Assert.AreEqual(CellarPropertyType.Binary, (CellarPropertyType)m_metaDataCache.GetFieldType(15002),
				"Wrong field data type for Binary data.");

			Assert.AreEqual(CellarPropertyType.BigString, (CellarPropertyType)m_metaDataCache.GetFieldType(15068),
				"Wrong field data type for BigString data.");

			Assert.AreEqual(CellarPropertyType.String, (CellarPropertyType)m_metaDataCache.GetFieldType(97008),
				"Wrong field data type for String data.");

			Assert.AreEqual(CellarPropertyType.MultiBigString, (CellarPropertyType)m_metaDataCache.GetFieldType(97020),
				"Wrong field data type for MultiBigString data.");

			Assert.AreEqual(CellarPropertyType.MultiString, (CellarPropertyType)m_metaDataCache.GetFieldType(97021),
				"Wrong field data type for MultiString data.");

			Assert.AreEqual(CellarPropertyType.BigUnicode, (CellarPropertyType)m_metaDataCache.GetFieldType(97031),
				"Wrong field data type for BigUnicode data.");

			Assert.AreEqual(CellarPropertyType.Unicode, (CellarPropertyType)m_metaDataCache.GetFieldType(1001),
				"Wrong field data type for Unicode data.");

			Assert.AreEqual(CellarPropertyType.MultiUnicode, (CellarPropertyType)m_metaDataCache.GetFieldType(7001),
				"Wrong field data type for MultiUnicode data.");
		}

		/// <summary>
		/// Check for validity of adding the given clid to some field.
		/// </summary>
		[Test]
		public void get_IsValidClassTest()
		{
			// Exact match
			bool isValid = m_metaDataCache.get_IsValidClass(59004, 0);
			Assert.IsTrue(isValid, "Object of type BaseClass should be able to be assigned to a field whose signature is BaseClass");

			// Prevent use of base class when specific subclass is expected
			isValid = m_metaDataCache.get_IsValidClass(59003, 0);
			Assert.IsFalse(isValid, "Object of type BaseClass should NOT be able to be assigned to a field whose signature is ClassB");

			// Mismatch
			isValid = m_metaDataCache.get_IsValidClass(59003, 45);
			Assert.IsFalse(isValid, "Object of type ClassL2 should NOT be able to be assigned to a field whose signature is ClassB");

			// Allow subclass when base class is expected
			isValid = m_metaDataCache.get_IsValidClass(59005, 45);
			Assert.IsTrue(isValid, "Object of type ClassL2 should be able to be assigned to a field whose signature is ClassL");

			// Prevent assignment of object to field that is expecting a basic type
			isValid = m_metaDataCache.get_IsValidClass(28002, 97);
			Assert.IsFalse(isValid, "Can put a ClassJ into a basic (Unicode) field?");
		}

		/// <summary>
		/// Check for validity of adding the given clid to an illegal field of 0.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void get_IsValidClassBadTest()
		{
			m_metaDataCache.get_IsValidClass(0, 0);
		}
	}

	#endregion MetaDataCacheFieldAccessTests class

	#region MetaDataCacheClassAccessTests class

	/// <summary>
	/// Test the class access methods.
	/// </summary>
	[TestFixture]
	public class MetaDataCacheClassAccessTests : MetaDataCacheBase
	{
		/// <summary>
		/// Check for finding the class name based on the given clid.
		/// </summary>
		[Test]
		public void GetClassNameTest()
		{
			Assert.AreEqual("ClassB", m_metaDataCache.GetClassName(49),
				"Wrong class name for ClassB.");
		}

		/// <summary>
		/// Check for finding the class name based on the given clid.
		/// </summary>
		[Test]
		public void GetAbstractTest()
		{
			Assert.IsFalse(m_metaDataCache.GetAbstract(49), "ClassB is a concrete class.");
			Assert.IsTrue(m_metaDataCache.GetAbstract(0), "BaseClass is an abstract class.");
		}

		/// <summary>
		/// Check for finding the class name based on the given clid.
		/// </summary>
		[Test]
		public void GetBaseClsIdTest()
		{
			Assert.AreEqual(7, m_metaDataCache.GetBaseClsId(49), "Wrong base class id for ClassB.");
		}

		/// <summary>
		/// Check for finding the class name based on the given clid.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void GetBaseClsIdBadTest()
		{
			m_metaDataCache.GetBaseClsId(0);
		}

		/// <summary>
		/// Check for finding the class name based on the given clid.
		/// </summary>
		[Test]
		public void GetBaseClsNameTest()
		{
			Assert.AreEqual("ClassK", m_metaDataCache.GetBaseClsName(49),
				"Wrong base class id for ClassB.");
		}

		/// <summary>
		/// Check for finding the class name based on the given clid.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void GetBaseClsNameBadTest()
		{
			m_metaDataCache.GetBaseClsName(0);
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetClassIdsTest()
		{
			// Just use the count for all classes,
			// even though we know it will never be that high a number that can be returned.
			int[] ids;
			var countAllClasses = m_metaDataCache.ClassCount;
			using (var clids = MarshalEx.ArrayToNative(countAllClasses, typeof(int)))
			{
				m_metaDataCache.GetClassIds(countAllClasses, clids);
				ids = (int[])MarshalEx.NativeToArray(clids, countAllClasses, typeof(int));
				Assert.AreEqual(countAllClasses, ids.Length, "Wrong number of classes returned.");
			}
			countAllClasses = 2;
			using (var clids = MarshalEx.ArrayToNative(countAllClasses, typeof(int)))
			{
				// Check ClassL (all of its direct subclasses).
				m_metaDataCache.GetClassIds(countAllClasses, clids);
				ids = (int[])MarshalEx.NativeToArray(clids, 2, typeof(int));
				Assert.AreEqual(countAllClasses, ids.Length, "Wrong number of classes returned.");
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void GetFieldsTest()
		{
			int countAllFlidsOut;
			using (var flids = MarshalEx.ArrayToNative(500, typeof(int)))
			{
				countAllFlidsOut = m_metaDataCache.GetFields(0, true, (int)CellarPropertyTypeFilter.All, 0, flids);
				var countAllFlids = countAllFlidsOut;
				countAllFlidsOut = m_metaDataCache.GetFields(0, true, (int)CellarPropertyTypeFilter.All, countAllFlidsOut, flids);
				Assert.AreEqual(countAllFlids, countAllFlidsOut, "Wrong number of fields returned for BaseClass.");
			}
			using (var flids = MarshalEx.ArrayToNative(500, typeof(int)))
			{
				countAllFlidsOut = m_metaDataCache.GetFields(49, true, (int)CellarPropertyTypeFilter.All, 0, flids);
				countAllFlidsOut = m_metaDataCache.GetFields(49, true, (int)CellarPropertyTypeFilter.All, countAllFlidsOut, flids);
				Assert.AreEqual(8, countAllFlidsOut, "Wrong number of fields returned for 49.");
			}
			using (var flids = MarshalEx.ArrayToNative(500, typeof(int)))
			{
				countAllFlidsOut = m_metaDataCache.GetFields(49, true, (int)CellarPropertyTypeFilter.AllReference, 0, flids);
				countAllFlidsOut = m_metaDataCache.GetFields(49, true, (int)CellarPropertyTypeFilter.AllReference, countAllFlidsOut, flids);
				Assert.AreEqual(1, countAllFlidsOut, "Wrong number of fields returned for 49.");
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void GetFieldsBadTest()
		{
			using (var flids = MarshalEx.ArrayToNative(500, typeof(int)))
			{
				int countAllFlidsOut = 1;
				m_metaDataCache.GetFields(49, true, (int)CellarPropertyTypeFilter.All, countAllFlidsOut, flids);
			}
		}
	}

	#endregion MetaDataCacheClassAccessTests class

	#region MetaDataCacheReverseAccessTests class

	/// <summary>
	/// Test the reverse access methods.
	/// </summary>
	[TestFixture]
	public class MetaDataCacheReverseAccessTests : MetaDataCacheBase
	{
		/// <summary>
		/// Tests GetClassId with a valid class name
		/// </summary>
		[Test]
		public void GetClassId_Valid()
		{
			var clid = m_metaDataCache.GetClassId("ClassD");
			Assert.AreEqual(2, clid, "Wrong class Id.");
		}

		/// <summary>
		/// Tests GetClassId with an invalid class name
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void GetClassId_Invalid()
		{
			m_metaDataCache.GetClassId("NonExistantClassName");
		}

		/// <summary>
		/// Tests the GetFieldId method on a field that is directly on the named class
		/// </summary>
		[Test]
		public void GetFieldId_SansSuperClass()
		{
			var flid = m_metaDataCache.GetFieldId("ClassD", "MultiUnicodeProp12", false);
			Assert.AreEqual(2003, flid, "Wrong field Id.");
		}

		/// <summary>
		/// Tests the GetFieldId method on a field that is on a superclass
		/// </summary>
		[Test]
		public void GetFieldId_WithSuperClass()
		{
			var flid = m_metaDataCache.GetFieldId("ClassL2", "Whatever", true);
			Assert.AreEqual(35001, flid, "Wrong field Id.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly.
		/// </summary>
		[Test]
		public void GetFieldId_SansSuperClass_Nonexistent()
		{
			Assert.AreEqual(0, m_metaDataCache.GetFieldId("BaseClass", "Monkeyruski", false));
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetFieldId_WithSuperClass_Nonexistent()
		{
			var flid = m_metaDataCache.GetFieldId("ClassL2", "Flurskuiwert", true);
			Assert.AreEqual(0, flid, "Wrong field Id.");
		}

		/// <summary>
		/// Tests GetFieldId2 method on a class that has the requested field directly
		/// </summary>
		[Test]
		public void GetFieldId2_SansSuperClass()
		{
			var flid = m_metaDataCache.GetFieldId2(2, "MultiUnicodeProp12", false);
			Assert.AreEqual(2003, flid, "Wrong field Id.");
		}

		/// <summary>
		/// Tests GetFieldId2 method on a class whose superclass has the requested field
		/// </summary>
		[Test]
		public void GetFieldId2_WithSuperClass()
		{
			var flid = m_metaDataCache.GetFieldId2(45, "Whatever", true);
			Assert.AreEqual(35001, flid, "Wrong field Id.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly.
		/// </summary>
		[Test]
		public void GetFieldId2_SansSuperClass_Nonexistent()
		{
			Assert.AreEqual(0, m_metaDataCache.GetFieldId2(1, "MultiUnicodeProp12", false));
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetFieldId2_WithSuperClass_Nonexistent()
		{
			Assert.AreEqual(0, m_metaDataCache.GetFieldId2(45, "MultiUnicodeProp12", true));
		}

		/// <summary>
		/// Test the GetDirectSubclasses method for a class having no sublasses
		/// </summary>
		[Test]
		public void GetDirectSubclasses_None()
		{
			int countDirectSubclasses;
			using (var clids = MarshalEx.ArrayToNative(10, typeof(int)))
			{
				// Check ClassB.
				m_metaDataCache.GetDirectSubclasses(45, 10, out countDirectSubclasses, clids);
				Assert.AreEqual(0, countDirectSubclasses, "Wrong number of subclasses returned.");
			}
		}

		/// <summary>
		/// Test the GetDirectSubclasses method for a class having two subclasses
		/// </summary>
		[Test]
		public void GetDirectSubclasses()
		{
			int countDirectSubclasses;
			using (var clids = MarshalEx.ArrayToNative(10, typeof(int)))
			{
				// Check ClassL (all of its direct subclasses).
				m_metaDataCache.GetDirectSubclasses(35, 10, out countDirectSubclasses, clids);
				Assert.AreEqual(2, countDirectSubclasses, "Wrong number of subclasses returned.");
				var ids = (int[])MarshalEx.NativeToArray(clids, 10, typeof(int));
				for (var i = 0; i < ids.Length; ++i)
				{
					var clid = ids[i];
					if (i < 2)
						Assert.IsTrue(((clid == 28) || (clid == 45)), "Clid should be 28 or 49 for direct subclasses of ClassL.");
					else
						Assert.AreEqual(0, clid, "Clid should be 0 from here on.");
				}
			}
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetDirectSubclasses_CountUnknown()
		{
			int countAllClasses;
			m_metaDataCache.GetDirectSubclasses(35, 0, out countAllClasses, null);
			Assert.AreEqual(2, countAllClasses, "Wrong number of subclasses returned.");
		}

		/// <summary>
		/// Tests getting the count of all subclasses of a class that has none. Count includes only the class itself.
		/// </summary>
		[Test]
		public void GetAllSubclasses_None()
		{
			using (var clids = MarshalEx.ArrayToNative(10, typeof(int)))
			{
				// Check ClassC.
				int countAllSubclasses;
				m_metaDataCache.GetAllSubclasses(26, 10, out countAllSubclasses, clids);
				Assert.AreEqual(1, countAllSubclasses, "Wrong number of subclasses returned.");
			}
		}

		/// <summary>
		/// Tests getting the count of all subclasses of a class (includes the class itself)
		/// </summary>
		[Test]
		public void GetAllSubclasses_ClassL()
		{
			using (var clids = MarshalEx.ArrayToNative(10, typeof(int)))
			{
				// Check ClassL (all of its direct subclasses).
				int countAllSubclasses;
				m_metaDataCache.GetAllSubclasses(35, 10, out countAllSubclasses, clids);
				Assert.AreEqual(3, countAllSubclasses, "Wrong number of subclasses returned.");
			}
		}

		/// <summary>
		/// Tests getting the count of all subclasses of a class (includes the class itself), limited
		/// by the maximum number requested
		/// </summary>
		[Test]
		public void GetAllSubclasses_ClassL_Limited()
		{
			using (var clids = MarshalEx.ArrayToNative(2, typeof(int)))
			{
				// Check ClassL (but get it and only 1 of its subclasses).
				int countAllSubclasses;
				m_metaDataCache.GetAllSubclasses(35, 2, out countAllSubclasses, clids);
				Assert.AreEqual(2, countAllSubclasses, "Wrong number of subclasses returned.");
			}
		}

		/// <summary>
		/// Tests getting the count of all subclasses of the base class (includes the class itself)
		/// </summary>
		[Test]
		public void GetAllSubclasses_BaseClass()
		{
			var countAllClasses = m_metaDataCache.ClassCount;
			using (var clids = MarshalEx.ArrayToNative(countAllClasses, typeof(int)))
			{
				// Check BaseClass.
				int countAllSubclasses;
				m_metaDataCache.GetAllSubclasses(0, countAllClasses, out countAllSubclasses, clids);
				Assert.AreEqual(countAllClasses, countAllSubclasses, "Wrong number of subclasses returned.");
			}
		}
	}

	#endregion MetaDataCacheReverseAccessTests class

	#region MetaDataCacheVirtualPropTests class

	/// <summary>
	/// Test the reverse access methods.
	/// </summary>
	[TestFixture]
	public class MetaDataCacheVirtualPropTests : MetaDataCacheBase
	{
		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void AddVirtualPropTest()
		{
			const int flid = 2000000001;
			const CellarPropertyType type = CellarPropertyType.Image;
			const string className = "ClassB";
			const string fieldName = "NewImageVP";
			m_metaDataCache.AddVirtualProp(className, fieldName, flid, (int)type);
			// Check its flid.
			var newFlid = m_metaDataCache.GetFieldId(className, fieldName, false);
			Assert.AreEqual(flid, newFlid, "Wrong field Id.");
			// Check its data type.
			Assert.AreEqual(type, (CellarPropertyType)m_metaDataCache.GetFieldType(flid), "Wrong field type.");
			// Check to see it is virtual.
			var isVirtual = m_metaDataCache.get_IsVirtual(flid);
			Assert.IsTrue(isVirtual, "Wrong field virtual setting.");
			// Check the clid it was supposed to be placed in.
			var clid = m_metaDataCache.GetClassId(className);
			Assert.AreEqual(clid, m_metaDataCache.GetOwnClsId(flid),
				"Wrong clid for new virtual field.");
			Assert.AreEqual(fieldName, m_metaDataCache.GetFieldName(flid),
				"Wrong field name for new virtual field.");
		}

		/// <summary>
		/// Check to see if some existing field is virtual.
		/// (It should not be.)
		/// </summary>
		[Test]
		public void get_IsVirtualTest()
		{
			Assert.IsFalse(m_metaDataCache.get_IsVirtual(1001), "Wrong field virtual setting.");
		}

		/// <summary>
		/// Check for case where the specified class for the new virtual field doesn't exist.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void AddVirtualPropNoClassTest()
		{
			const int flid = 2000000002;
			const int type = (int)CellarPropertyType.Image;
			const string className = "BogusClass";
			const string fieldName = "NewImageVP";
			m_metaDataCache.AddVirtualProp(className, fieldName, flid, type);
		}

		/// <summary>
		/// Check for case where the specified field name for the new virtual field exists.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void AddVirtualPropFieldExistsTest()
		{
			const int flid = 2000000003;
			const int type = (int)CellarPropertyType.Image;
			const string className = "ClassK";
			const string fieldName = "MultiStringProp11";
			m_metaDataCache.AddVirtualProp(className, fieldName, flid, type);
		}

		/// <summary>
		/// Check for case where the specified flid for the new virtual field exists.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void AddVirtualPropFlidExistsTest()
		{
			var flid = m_metaDataCache.GetFieldId("ClassB", "WhoCares", true);
			const int type = (int)CellarPropertyType.Image;
			const string className = "ClassB";
			const string fieldName = "NewName";
			m_metaDataCache.AddVirtualProp(className, fieldName, flid, type);
		}

		/// <summary>
		/// Check for case where the specified flid for the new virtual field exists.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void AddVirtualPropInvalidLowFieldTypeTest()
		{
			const int flid = 2000000004;
			const int type = 0;
			const string className = "ClassB";
			const string fieldName = "NewName";
			m_metaDataCache.AddVirtualProp(className, fieldName, flid, type);
		}

		/// <summary>
		/// Check for case where the specified flid for the new virtual field exists.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void AddVirtualPropInvalidHighFieldTypeTest()
		{
			const int flid = 2000000005;
			const int type = 1000;
			const string className = "ClassB";
			const string fieldName = "NewName";
			m_metaDataCache.AddVirtualProp(className, fieldName, flid, type);
		}
	}

	#endregion MetaDataCacheVirtualPropTests class
}
