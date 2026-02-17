// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Xml;
using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.CacheLight;
using SIL.LCModel.Utils;
using System.Text;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.CacheLightTests
{
	#region MetaDataCacheInitializationTests class

	/// <summary>
	/// Test cache initialization.
	/// </summary>
	[TestFixture]
	public class MetaDataCacheInitializationTests
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Don't use the real file system
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[OneTimeSetUp]
		public void FixtureSetup()
		{
			FileUtils.Manager.SetFileAdapter(new MockFileOS());
		}

		/// <summary/>
		[OneTimeTearDown]
		public void FixtureTeardown()
		{
			FileUtils.Manager.Reset();
		}

		/// <summary>
		/// Tests creating a MetaDataCache with no input pathname to the XML file.
		/// </summary>
		[Test]
		public void CreateMetaDataCacheNoFile()
		{
			Assert.That(() =>  MetaDataCache.CreateMetaDataCache(null), Throws.ArgumentNullException);
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		public void CreateMetaDataCacheEmptyPathname()
		{
			Assert.That(() =>  MetaDataCache.CreateMetaDataCache(""), Throws.ArgumentException);
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		public void CreateMetaDataCacheBadPathname()
		{
			Assert.That(() => MetaDataCache.CreateMetaDataCache("MyBadpathname"), Throws.TypeOf<FileNotFoundException>());
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
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
				Assert.That(() =>  MetaDataCache.CreateMetaDataCache(bogusDataPathname), Throws.TypeOf<XmlException>());
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
				Assert.That(() =>  MetaDataCache.CreateMetaDataCache(bogusDataPathname), Throws.ArgumentException);
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
				Assert.That(() => mdc.InitXml(bogusDataPathname, false), Throws.ArgumentException);
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
				Assert.That(() =>  MetaDataCache.CreateMetaDataCache(bogusDataPathname), Throws.ArgumentException);
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
				Assert.That(() => mdc.InitXml(bogusDataPathname, false), Throws.ArgumentException);
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
		[OneTimeSetUp]
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
		[OneTimeTearDown]
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
			Assert.That(m_metaDataCache.GetDstClsName(59005), Is.EqualTo("ClassL"), "Wrong class name");
		}

		/// <summary>
		/// Test of GetOwnClsName method
		/// </summary>
		[Test]
		public void GetOwnClsNameTest()
		{
			Assert.That(m_metaDataCache.GetOwnClsName(15068), Is.EqualTo("ClassG"), "Wrong class name");
		}

		/// <summary>
		/// Test of GetOwnClsId method
		/// </summary>
		[Test]
		public void GetOwnClsIdTest()
		{
			Assert.That(m_metaDataCache.GetOwnClsId(15068), Is.EqualTo(15), "Wrong class implementor.");
		}

		/// <summary>
		/// Test of GetDstClsId method
		/// </summary>
		[Test]
		public void GetDstClsIdTest()
		{
			Assert.That(m_metaDataCache.GetDstClsId(59003), Is.EqualTo(49), "Wrong class Signature.");
		}

		/// <summary>
		/// This should test for any case where the given flid is not valid.
		/// </summary>
		[Test]
		public void GetClsNameForBadFlidTest()
		{
			Assert.That(() => m_metaDataCache.GetOwnClsName(50), Throws.ArgumentException);
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
			using (var flids = MarshalEx.ArrayToNative<int>(testFlidSize))
			{
				m_metaDataCache.GetFieldIds(testFlidSize, flids);
				ids = MarshalEx.NativeToArray<int>(flids, testFlidSize);
				Assert.That(ids.Length, Is.EqualTo(testFlidSize), "Wrong size of fields returned.");
				foreach (var flid in ids)
					Assert.That(flid > 0, Is.True, "Wrong flid value: " + flid);
			}
			testFlidSize = flidSize;
			using (var flids = MarshalEx.ArrayToNative<int>(testFlidSize))
			{
				m_metaDataCache.GetFieldIds(testFlidSize, flids);
				ids = MarshalEx.NativeToArray<int>(flids, testFlidSize);
				Assert.That(ids.Length, Is.EqualTo(testFlidSize), "Wrong size of fields returned.");
				foreach (var flid in ids)
					Assert.That(flid > 0, Is.True, "Wrong flid value: " + flid);
			}
			testFlidSize = flidSize + 1;
			using (var flids = MarshalEx.ArrayToNative<int>(testFlidSize))
			{
				m_metaDataCache.GetFieldIds(testFlidSize, flids);
				ids = MarshalEx.NativeToArray<int>(flids, testFlidSize);
				Assert.That(ids.Length, Is.EqualTo(testFlidSize), "Wrong size of fields returned.");
				for (var iflid = 0; iflid < ids.Length; ++iflid)
				{
					var flid = ids[iflid];
					if (iflid < ids.Length - 1)
						Assert.That(flid > 0, Is.True, "Wrong flid value: " + flid);
					else
						Assert.That(flid, Is.EqualTo(0), "Wrong value for flid beyond actual length.");
				}
			}
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetFieldNameTest()
		{
			Assert.That(m_metaDataCache.GetFieldName(2003), Is.EqualTo("MultiUnicodeProp12"));
		}

		/// <summary>
		/// CacheLight doesn't support field labels, so they are always null
		/// </summary>
		[Test]
		public void GetFieldLabelIsNullTest()
		{
			Assert.That(m_metaDataCache.GetFieldLabel(59003), Is.Null, "Field label not null.");
		}

		/// <summary>
		/// CacheLight doesn't support field help, so it is always null
		/// </summary>
		[Test]
		public void GetFieldHelpIsNullTest()
		{
			Assert.That(m_metaDataCache.GetFieldHelp(59003), Is.Null, "Field help not null.");
		}

		/// <summary>
		/// CacheLight doesn't support field XML, so it is always null
		/// </summary>
		[Test]
		public void GetFieldXmlIsNullTest()
		{
			Assert.That(m_metaDataCache.GetFieldXml(59003), Is.Null, "Field XML not null.");
		}

		/// <summary>
		/// CacheLight doesn't support writing system selectors, so they are always 0
		/// </summary>
		[Test]
		public void GetFieldWsIsZeroTest()
		{
			Assert.That(m_metaDataCache.GetFieldWs(59003), Is.EqualTo(0), "Writing system not zero.");
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
			Assert.That((CellarPropertyType)m_metaDataCache.GetFieldType(2027), Is.EqualTo(CellarPropertyType.Boolean), "Wrong field data type for Boolean data.");

			Assert.That((CellarPropertyType)m_metaDataCache.GetFieldType(26002), Is.EqualTo(CellarPropertyType.Integer), "Wrong field data type for Integer data.");

			Assert.That((CellarPropertyType)m_metaDataCache.GetFieldType(2005), Is.EqualTo(CellarPropertyType.Time), "Wrong field data type for Time data.");

			Assert.That((CellarPropertyType)m_metaDataCache.GetFieldType(8002), Is.EqualTo(CellarPropertyType.Guid), "Wrong field data type for Guid data.");

			Assert.That((CellarPropertyType)m_metaDataCache.GetFieldType(13004), Is.EqualTo(CellarPropertyType.GenDate), "Wrong field data type for GenDate data.");

			Assert.That((CellarPropertyType)m_metaDataCache.GetFieldType(15002), Is.EqualTo(CellarPropertyType.Binary), "Wrong field data type for Binary data.");

			Assert.That((CellarPropertyType)m_metaDataCache.GetFieldType(97008), Is.EqualTo(CellarPropertyType.String), "Wrong field data type for String data.");

			Assert.That((CellarPropertyType)m_metaDataCache.GetFieldType(97021), Is.EqualTo(CellarPropertyType.MultiString), "Wrong field data type for MultiString data.");

			Assert.That((CellarPropertyType)m_metaDataCache.GetFieldType(1001), Is.EqualTo(CellarPropertyType.Unicode), "Wrong field data type for Unicode data.");

			Assert.That((CellarPropertyType)m_metaDataCache.GetFieldType(7001), Is.EqualTo(CellarPropertyType.MultiUnicode), "Wrong field data type for MultiUnicode data.");
		}

		/// <summary>
		/// Check for validity of adding the given clid to some field.
		/// </summary>
		[Test]
		public void get_IsValidClassTest()
		{
			// Exact match
			bool isValid = m_metaDataCache.get_IsValidClass(59004, 0);
			Assert.That(isValid, Is.True, "Object of type BaseClass should be able to be assigned to a field whose signature is BaseClass");

			// Prevent use of base class when specific subclass is expected
			isValid = m_metaDataCache.get_IsValidClass(59003, 0);
			Assert.That(isValid, Is.False, "Object of type BaseClass should NOT be able to be assigned to a field whose signature is ClassB");

			// Mismatch
			isValid = m_metaDataCache.get_IsValidClass(59003, 45);
			Assert.That(isValid, Is.False, "Object of type ClassL2 should NOT be able to be assigned to a field whose signature is ClassB");

			// Allow subclass when base class is expected
			isValid = m_metaDataCache.get_IsValidClass(59005, 45);
			Assert.That(isValid, Is.True, "Object of type ClassL2 should be able to be assigned to a field whose signature is ClassL");

			// Prevent assignment of object to field that is expecting a basic type
			isValid = m_metaDataCache.get_IsValidClass(28002, 97);
			Assert.That(isValid, Is.False, "Can put a ClassJ into a basic (Unicode) field?");
		}

		/// <summary>
		/// Check for validity of adding the given clid to an illegal field of 0.
		/// </summary>
		[Test]
		public void get_IsValidClassBadTest()
		{
			Assert.That(() => m_metaDataCache.get_IsValidClass(0, 0), Throws.ArgumentException);
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
			Assert.That(m_metaDataCache.GetClassName(49), Is.EqualTo("ClassB"), "Wrong class name for ClassB.");
		}

		/// <summary>
		/// Check for finding the class name based on the given clid.
		/// </summary>
		[Test]
		public void GetAbstractTest()
		{
			Assert.That(m_metaDataCache.GetAbstract(49), Is.False, "ClassB is a concrete class.");
			Assert.That(m_metaDataCache.GetAbstract(0), Is.True, "BaseClass is an abstract class.");
		}

		/// <summary>
		/// Check for finding the class name based on the given clid.
		/// </summary>
		[Test]
		public void GetBaseClsIdTest()
		{
			Assert.That(m_metaDataCache.GetBaseClsId(49), Is.EqualTo(7), "Wrong base class id for ClassB.");
		}

		/// <summary>
		/// Check for finding the class name based on the given clid.
		/// </summary>
		[Test]
		public void GetBaseClsIdBadTest()
		{
			Assert.That(() => m_metaDataCache.GetBaseClsId(0), Throws.ArgumentException);
		}

		/// <summary>
		/// Check for finding the class name based on the given clid.
		/// </summary>
		[Test]
		public void GetBaseClsNameTest()
		{
			Assert.That(m_metaDataCache.GetBaseClsName(49), Is.EqualTo("ClassK"), "Wrong base class id for ClassB.");
		}

		/// <summary>
		/// Check for finding the class name based on the given clid.
		/// </summary>
		[Test]
		public void GetBaseClsNameBadTest()
		{
			Assert.That(() => m_metaDataCache.GetBaseClsName(0), Throws.ArgumentException);
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
			using (var clids = MarshalEx.ArrayToNative<int>(countAllClasses))
			{
				m_metaDataCache.GetClassIds(countAllClasses, clids);
				ids = MarshalEx.NativeToArray<int>(clids, countAllClasses);
				Assert.That(ids.Length, Is.EqualTo(countAllClasses), "Wrong number of classes returned.");
			}
			countAllClasses = 2;
			using (var clids = MarshalEx.ArrayToNative<int>(countAllClasses))
			{
				// Check ClassL (all of its direct subclasses).
				m_metaDataCache.GetClassIds(countAllClasses, clids);
				ids = MarshalEx.NativeToArray<int>(clids, 2);
				Assert.That(ids.Length, Is.EqualTo(countAllClasses), "Wrong number of classes returned.");
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void GetFieldsTest()
		{
			int countAllFlidsOut;
			using (var flids = MarshalEx.ArrayToNative<int>(500))
			{
				countAllFlidsOut = m_metaDataCache.GetFields(0, true, (int)CellarPropertyTypeFilter.All, 0, flids);
				var countAllFlids = countAllFlidsOut;
				countAllFlidsOut = m_metaDataCache.GetFields(0, true, (int)CellarPropertyTypeFilter.All, countAllFlidsOut, flids);
				Assert.That(countAllFlidsOut, Is.EqualTo(countAllFlids), "Wrong number of fields returned for BaseClass.");
			}
			using (var flids = MarshalEx.ArrayToNative<int>(500))
			{
				countAllFlidsOut = m_metaDataCache.GetFields(49, true, (int)CellarPropertyTypeFilter.All, 0, flids);
				countAllFlidsOut = m_metaDataCache.GetFields(49, true, (int)CellarPropertyTypeFilter.All, countAllFlidsOut, flids);
				Assert.That(countAllFlidsOut, Is.EqualTo(8), "Wrong number of fields returned for 49.");
			}
			using (var flids = MarshalEx.ArrayToNative<int>(500))
			{
				countAllFlidsOut = m_metaDataCache.GetFields(49, true, (int)CellarPropertyTypeFilter.AllReference, 0, flids);
				countAllFlidsOut = m_metaDataCache.GetFields(49, true, (int)CellarPropertyTypeFilter.AllReference, countAllFlidsOut, flids);
				Assert.That(countAllFlidsOut, Is.EqualTo(1), "Wrong number of fields returned for 49.");
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void GetFieldsBadTest()
		{
			using (var flids = MarshalEx.ArrayToNative<int>(500))
			{
				int countAllFlidsOut = 1;
				Assert.That(() => m_metaDataCache.GetFields(49, true, (int)CellarPropertyTypeFilter.All,
					countAllFlidsOut, flids), Throws.ArgumentException);
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
			Assert.That(clid, Is.EqualTo(2), "Wrong class Id.");
		}

		/// <summary>
		/// Tests GetClassId with an invalid class name
		/// </summary>
		[Test]
		public void GetClassId_Invalid()
		{
			Assert.That(() => m_metaDataCache.GetClassId("NonExistantClassName"), Throws.ArgumentException);
		}

		/// <summary>
		/// Tests the GetFieldId method on a field that is directly on the named class
		/// </summary>
		[Test]
		public void GetFieldId_SansSuperClass()
		{
			var flid = m_metaDataCache.GetFieldId("ClassD", "MultiUnicodeProp12", false);
			Assert.That(flid, Is.EqualTo(2003), "Wrong field Id.");
		}

		/// <summary>
		/// Tests the GetFieldId method on a field that is on a superclass
		/// </summary>
		[Test]
		public void GetFieldId_WithSuperClass()
		{
			var flid = m_metaDataCache.GetFieldId("ClassL2", "Whatever", true);
			Assert.That(flid, Is.EqualTo(35001), "Wrong field Id.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly.
		/// </summary>
		[Test]
		public void GetFieldId_SansSuperClass_Nonexistent()
		{
			Assert.That(m_metaDataCache.GetFieldId("BaseClass", "Monkeyruski", false), Is.EqualTo(0));
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetFieldId_WithSuperClass_Nonexistent()
		{
			var flid = m_metaDataCache.GetFieldId("ClassL2", "Flurskuiwert", true);
			Assert.That(flid, Is.EqualTo(0), "Wrong field Id.");
		}

		/// <summary>
		/// Tests GetFieldId2 method on a class that has the requested field directly
		/// </summary>
		[Test]
		public void GetFieldId2_SansSuperClass()
		{
			var flid = m_metaDataCache.GetFieldId2(2, "MultiUnicodeProp12", false);
			Assert.That(flid, Is.EqualTo(2003), "Wrong field Id.");
		}

		/// <summary>
		/// Tests GetFieldId2 method on a class whose superclass has the requested field
		/// </summary>
		[Test]
		public void GetFieldId2_WithSuperClass()
		{
			var flid = m_metaDataCache.GetFieldId2(45, "Whatever", true);
			Assert.That(flid, Is.EqualTo(35001), "Wrong field Id.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly.
		/// </summary>
		[Test]
		public void GetFieldId2_SansSuperClass_Nonexistent()
		{
			Assert.That(m_metaDataCache.GetFieldId2(1, "MultiUnicodeProp12", false), Is.EqualTo(0));
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetFieldId2_WithSuperClass_Nonexistent()
		{
			Assert.That(m_metaDataCache.GetFieldId2(45, "MultiUnicodeProp12", true), Is.EqualTo(0));
		}

		/// <summary>
		/// Test the GetDirectSubclasses method for a class having no sublasses
		/// </summary>
		[Test]
		public void GetDirectSubclasses_None()
		{
			int countDirectSubclasses;
			using (var clids = MarshalEx.ArrayToNative<int>(10))
			{
				// Check ClassB.
				m_metaDataCache.GetDirectSubclasses(45, 10, out countDirectSubclasses, clids);
				Assert.That(countDirectSubclasses, Is.EqualTo(0), "Wrong number of subclasses returned.");
			}
		}

		/// <summary>
		/// Test the GetDirectSubclasses method for a class having two subclasses
		/// </summary>
		[Test]
		public void GetDirectSubclasses()
		{
			int countDirectSubclasses;
			using (var clids = MarshalEx.ArrayToNative<int>(10))
			{
				// Check ClassL (all of its direct subclasses).
				m_metaDataCache.GetDirectSubclasses(35, 10, out countDirectSubclasses, clids);
				Assert.That(countDirectSubclasses, Is.EqualTo(2), "Wrong number of subclasses returned.");
				var ids = MarshalEx.NativeToArray<int>(clids, 10);
				for (var i = 0; i < ids.Length; ++i)
				{
					var clid = ids[i];
					if (i < 2)
						Assert.That(((clid == 28) || (clid == 45)), Is.True, "Clid should be 28 or 49 for direct subclasses of ClassL.");
					else
						Assert.That(clid, Is.EqualTo(0), "Clid should be 0 from here on.");
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
			Assert.That(countAllClasses, Is.EqualTo(2), "Wrong number of subclasses returned.");
		}

		/// <summary>
		/// Tests getting the count of all subclasses of a class that has none. Count includes only the class itself.
		/// </summary>
		[Test]
		public void GetAllSubclasses_None()
		{
			using (var clids = MarshalEx.ArrayToNative<int>(10))
			{
				// Check ClassC.
				int countAllSubclasses;
				m_metaDataCache.GetAllSubclasses(26, 10, out countAllSubclasses, clids);
				Assert.That(countAllSubclasses, Is.EqualTo(1), "Wrong number of subclasses returned.");
			}
		}

		/// <summary>
		/// Tests getting the count of all subclasses of a class (includes the class itself)
		/// </summary>
		[Test]
		public void GetAllSubclasses_ClassL()
		{
			using (var clids = MarshalEx.ArrayToNative<int>(10))
			{
				// Check ClassL (all of its direct subclasses).
				int countAllSubclasses;
				m_metaDataCache.GetAllSubclasses(35, 10, out countAllSubclasses, clids);
				Assert.That(countAllSubclasses, Is.EqualTo(3), "Wrong number of subclasses returned.");
			}
		}

		/// <summary>
		/// Tests getting the count of all subclasses of a class (includes the class itself), limited
		/// by the maximum number requested
		/// </summary>
		[Test]
		public void GetAllSubclasses_ClassL_Limited()
		{
			using (var clids = MarshalEx.ArrayToNative<int>(2))
			{
				// Check ClassL (but get it and only 1 of its subclasses).
				int countAllSubclasses;
				m_metaDataCache.GetAllSubclasses(35, 2, out countAllSubclasses, clids);
				Assert.That(countAllSubclasses, Is.EqualTo(2), "Wrong number of subclasses returned.");
			}
		}

		/// <summary>
		/// Tests getting the count of all subclasses of the base class (includes the class itself)
		/// </summary>
		[Test]
		public void GetAllSubclasses_BaseClass()
		{
			var countAllClasses = m_metaDataCache.ClassCount;
			using (var clids = MarshalEx.ArrayToNative<int>(countAllClasses))
			{
				// Check BaseClass.
				int countAllSubclasses;
				m_metaDataCache.GetAllSubclasses(0, countAllClasses, out countAllSubclasses, clids);
				Assert.That(countAllSubclasses, Is.EqualTo(countAllClasses), "Wrong number of subclasses returned.");
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
			Assert.That(newFlid, Is.EqualTo(flid), "Wrong field Id.");
			// Check its data type.
			Assert.That((CellarPropertyType)m_metaDataCache.GetFieldType(flid), Is.EqualTo(type), "Wrong field type.");
			// Check to see it is virtual.
			var isVirtual = m_metaDataCache.get_IsVirtual(flid);
			Assert.That(isVirtual, Is.True, "Wrong field virtual setting.");
			// Check the clid it was supposed to be placed in.
			var clid = m_metaDataCache.GetClassId(className);
			Assert.That(m_metaDataCache.GetOwnClsId(flid), Is.EqualTo(clid), "Wrong clid for new virtual field.");
			Assert.That(m_metaDataCache.GetFieldName(flid), Is.EqualTo(fieldName), "Wrong field name for new virtual field.");
		}

		/// <summary>
		/// Check to see if some existing field is virtual.
		/// (It should not be.)
		/// </summary>
		[Test]
		public void get_IsVirtualTest()
		{
			Assert.That(m_metaDataCache.get_IsVirtual(1001), Is.False, "Wrong field virtual setting.");
		}

		/// <summary>
		/// Check for case where the specified class for the new virtual field doesn't exist.
		/// </summary>
		[Test]
		public void AddVirtualPropNoClassTest()
		{
			const int flid = 2000000002;
			const int type = (int)CellarPropertyType.Image;
			const string className = "BogusClass";
			const string fieldName = "NewImageVP";
			Assert.That(() => m_metaDataCache.AddVirtualProp(className, fieldName, flid, type), Throws.ArgumentException);
		}

		/// <summary>
		/// Check for case where the specified field name for the new virtual field exists.
		/// </summary>
		[Test]
		public void AddVirtualPropFieldExistsTest()
		{
			const int flid = 2000000003;
			const int type = (int)CellarPropertyType.Image;
			const string className = "ClassK";
			const string fieldName = "MultiStringProp11";
			Assert.That(() => m_metaDataCache.AddVirtualProp(className, fieldName, flid, type), Throws.ArgumentException);
		}

		/// <summary>
		/// Check for case where the specified flid for the new virtual field exists.
		/// </summary>
		[Test]
		public void AddVirtualPropFlidExistsTest()
		{
			var flid = m_metaDataCache.GetFieldId("ClassB", "WhoCares", true);
			const int type = (int)CellarPropertyType.Image;
			const string className = "ClassB";
			const string fieldName = "NewName";
			Assert.That(() => m_metaDataCache.AddVirtualProp(className, fieldName, flid, type), Throws.ArgumentException);
		}

		/// <summary>
		/// Check for case where the specified flid for the new virtual field exists.
		/// </summary>
		[Test]
		public void AddVirtualPropInvalidLowFieldTypeTest()
		{
			const int flid = 2000000004;
			const int type = 0;
			const string className = "ClassB";
			const string fieldName = "NewName";
			Assert.That(() => m_metaDataCache.AddVirtualProp(className, fieldName, flid, type), Throws.ArgumentException);
		}

		/// <summary>
		/// Check for case where the specified flid for the new virtual field exists.
		/// </summary>
		[Test]
		public void AddVirtualPropInvalidHighFieldTypeTest()
		{
			const int flid = 2000000005;
			const int type = 1000;
			const string className = "ClassB";
			const string fieldName = "NewName";
			Assert.That(() => m_metaDataCache.AddVirtualProp(className, fieldName, flid, type), Throws.ArgumentException);
		}
	}

	#endregion MetaDataCacheVirtualPropTests class
}
