using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections;
using System.Xml;
using System.IO;

using NUnit.Framework;
using SIL.FieldWorks.CacheLight;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.CacheLightTests
{
	#region NoRealDataForTests class

	/// <summary>
	/// Test cache on methods that have no official data for in the model, or that have easily known values.
	/// </summary>
	[TestFixture]
	public class NoRealDataForTests : IFWDisposable
	{
		private IFwMetaDataCache m_mdc;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public virtual void FixtureSetup()
		{
			string bogusDataPathname = "Good.xml";
			StreamWriter w = File.CreateText(bogusDataPathname);
			w.WriteLine("<?xml version='1.0' encoding='utf-8'?>");
			w.WriteLine("<EntireModel>");
				w.WriteLine("<CellarModule id='cellar' num='0'>");
					w.WriteLine("<class num='1' id='MyClass' abstract='false' base='CmObject' >");
						w.WriteLine("<props>");
							//  flid='1001'
						w.WriteLine("<basic num='1' id='myField' sig='int' userlabel='My Field' fieldhelp='Abandon hope' fieldxml='Some stuff' listroot='25' wsselector='10'/>");
						//  flid='1002' kcptNumeric = 3, Not used in official model
						w.WriteLine("<basic num='2' id='myField' sig='Numeric'/>");
						//  flid='1003' kcptFloat = 4, Not used in official model
						w.WriteLine("<basic num='3' id='myField' sig='Float'/>");
						//  flid='1004' kcptImage = 7, Not used in official model
						w.WriteLine("<basic num='4' id='myField' sig='Image'/>");
						//  flid='1005' kcptMultiBigUnicode = 20, Not used in official model
						w.WriteLine("<basic num='5' id='myField' sig='MultiUnicode' big='true'/>");
						w.WriteLine("</props>");
					w.WriteLine("</class>");
				w.WriteLine("</CellarModule>");
			w.WriteLine("</EntireModel>");
			w.Flush();
			w.Close();
			try
			{
				m_mdc = MetaDataCache.CreateMetaDataCache(bogusDataPathname);
			}
			finally
			{
				File.Delete(bogusDataPathname);
			}
		}

		#region IDisposable & Co. implementation

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		~NoRealDataForTests()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Subclasses should override the Dispose method with the parameter
		/// and call the base method to tear down a test fisture class.
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_mdc = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetFieldXmlTest()
		{
			CheckDisposed();
			Assert.AreEqual("Some stuff", m_mdc.GetFieldXml(1001), "Wrong field XML.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetFieldListRootTest()
		{
			CheckDisposed();
			Assert.AreEqual(25, m_mdc.GetFieldListRoot(1001), "Wrong field list root.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetFieldHelpTest()
		{
			CheckDisposed();
			Assert.AreEqual("Abandon hope", m_mdc.GetFieldHelp(1001), "Wrong field help.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetFieldLabelTest()
		{
			CheckDisposed();
			Assert.AreEqual("My Field", m_mdc.GetFieldLabel(1001), "Wrong field label.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetFieldWsTest()
		{
			CheckDisposed();
			Assert.AreEqual(10, m_mdc.GetFieldWs(1001), "Wrong writing system.");
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
			CheckDisposed();

			Assert.AreEqual((int)CellarModuleDefns.kcptNumeric, m_mdc.GetFieldType(1002),
				"Wrong field data type for Numeric data.");

			Assert.AreEqual((int)CellarModuleDefns.kcptFloat, m_mdc.GetFieldType(1003),
				"Wrong field data type for Float data.");

			Assert.AreEqual((int)CellarModuleDefns.kcptImage, m_mdc.GetFieldType(1004),
				"Wrong field data type for Image data.");

			Assert.AreEqual((int)CellarModuleDefns.kcptMultiBigUnicode, m_mdc.GetFieldType(1005),
				"Wrong field data type for MultiBigUnicode data.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void get_ClassCountTest()
		{
			CheckDisposed();

			// CmObject gets added to whatever is in the XML file,
			// so add 1 to that count.
			Assert.AreEqual(2, m_mdc.ClassCount, "Wrong class count.");
		}
	}

	#endregion NoRealDataForTests class

	#region MetaDataCacheInitializationTests class

	/// <summary>
	/// Test cache initialization.
	/// </summary>
	[TestFixture]
	public class MetaDataCacheInitializationTests
	{
		/// <summary>
		/// Tests creating a MetaDataCache with no input pathname to the XML file.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void CreateMetaDataCacheNoFile()
		{
			IFwMetaDataCache mdc = MetaDataCache.CreateMetaDataCache(null);
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void CreateMetaDataCacheEmptyPathname()
		{
			IFwMetaDataCache mdc = MetaDataCache.CreateMetaDataCache("");
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		[ExpectedException(typeof(FileNotFoundException))]
		public void CreateMetaDataCacheBadPathname()
		{
			IFwMetaDataCache mdc = MetaDataCache.CreateMetaDataCache("MyBadpathname");
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		[ExpectedException(typeof(XmlException))]
		public void CreateMetaDataCacheNotXMLData()
		{
			// <?xml version="1.0" encoding="utf-8"?>
			string bogusDataPathname = "Bogus.txt";
			StreamWriter w = File.CreateText(bogusDataPathname);
			w.WriteLine("Non-XML data");
			w.Flush();
			w.Close();
			try
			{
				IFwMetaDataCache mdc = MetaDataCache.CreateMetaDataCache(bogusDataPathname);
			}
			finally
			{
				File.Delete(bogusDataPathname);
			}
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void CreateMetaDataCacheWrongXMLData()
		{
			string bogusDataPathname = "Bogus.xml";
			StreamWriter w = File.CreateText(bogusDataPathname);
			w.WriteLine("<?xml version='1.0' encoding='utf-8'?>");
			w.WriteLine("<BogusElement/>");
			w.Flush();
			w.Close();
			try
			{
				IFwMetaDataCache mdc = MetaDataCache.CreateMetaDataCache(bogusDataPathname);
			}
			finally
			{
				File.Delete(bogusDataPathname);
			}
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		public void CreateMetaDataCacheEntireModelXMLData()
		{
			string bogusDataPathname = "Bogus.xml";
			StreamWriter w = File.CreateText(bogusDataPathname);
			w.WriteLine("<?xml version='1.0' encoding='utf-8'?>");
			w.WriteLine("<EntireModel>");
			w.WriteLine("<CellarModule id='ling' num='5'/>");
			w.WriteLine("</EntireModel>");
			w.Flush();
			w.Close();
			try
			{
				IFwMetaDataCache mdc = MetaDataCache.CreateMetaDataCache(bogusDataPathname);
			}
			finally
			{
				File.Delete(bogusDataPathname);
			}
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		public void CreateMetaDataCacheCellarModuleXMLData()
		{
			string bogusDataPathname = "Bogus.xml";
			StreamWriter w = File.CreateText(bogusDataPathname);
			w.WriteLine("<?xml version='1.0' encoding='utf-8'?>");
			w.WriteLine("<CellarModule id='ling' num='5'/>");
			w.Flush();
			w.Close();
			try
			{
				IFwMetaDataCache mdc = MetaDataCache.CreateMetaDataCache(bogusDataPathname);
			}
			finally
			{
				File.Delete(bogusDataPathname);
			}
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void CreateMetaDataCacheDuplicateFiles()
		{
			string bogusDataPathname = "Good.xml";
			StreamWriter w = File.CreateText(bogusDataPathname);
			w.WriteLine("<?xml version='1.0' encoding='utf-8'?>");
			w.WriteLine("<EntireModel>");
			w.WriteLine("<CellarModule id='ling' num='5'/>");
			w.WriteLine("</EntireModel>");
			w.Flush();
			w.Close();
			IFwMetaDataCache mdc;
			try
			{
				mdc = MetaDataCache.CreateMetaDataCache(bogusDataPathname);
			}
			finally
			{
				File.Delete(bogusDataPathname);
			}
			// Not good, since a file with that name has already been processed.
			bogusDataPathname = "Good.xml";
			w = File.CreateText(bogusDataPathname);
			w.WriteLine("<?xml version='1.0' encoding='utf-8'?>");
			w.WriteLine("<EntireModel>");
			w.WriteLine("<CellarModule id='cellar' num='0'/>");
			w.WriteLine("</EntireModel>");
			w.Flush();
			w.Close();
			try
			{
				mdc.InitXml(bogusDataPathname, false);
			}
			finally
			{
				File.Delete(bogusDataPathname);
			}
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void CreateMetaDataCacheDuplicateClasses()
		{
			string bogusDataPathname = "Bogus.xml";
			StreamWriter w = File.CreateText(bogusDataPathname);
			w.WriteLine("<?xml version='1.0' encoding='utf-8'?>");
			w.WriteLine("<EntireModel>");
			w.WriteLine("<CellarModule id='ling' num='5'>");
			w.WriteLine("<class num='2' id='LexEntry' abstract='false' base='CmObject'/>");
			w.WriteLine("<class num='2' id='LexEntry' abstract='false' base='CmObject'/>");
			w.WriteLine("</CellarModule>");
			w.WriteLine("</EntireModel>");
			w.Flush();
			w.Close();
			try
			{
				IFwMetaDataCache mdc = MetaDataCache.CreateMetaDataCache(bogusDataPathname);
			}
			finally
			{
				File.Delete(bogusDataPathname);
			}
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void CreateMetaDataCacheDuplicateClasses2()
		{
			string bogusDataPathname = "Good.xml";
			StreamWriter w = File.CreateText(bogusDataPathname);
			w.WriteLine("<?xml version='1.0' encoding='utf-8'?>");
			w.WriteLine("<EntireModel>");
			w.WriteLine("<CellarModule id='ling' num='5'>");
			w.WriteLine("<class num='2' id='LexEntry' abstract='false' base='CmObject'/>");
			w.WriteLine("</CellarModule>");
			w.WriteLine("</EntireModel>");
			w.Flush();
			w.Close();
			IFwMetaDataCache mdc;
			try
			{
				mdc = MetaDataCache.CreateMetaDataCache(bogusDataPathname);
			}
			finally
			{
				File.Delete(bogusDataPathname);
			}
			// Not good, since a file with that name has already been processed.
			bogusDataPathname = "ReallyGood.xml";
			w = File.CreateText(bogusDataPathname);
			w.WriteLine("<?xml version='1.0' encoding='utf-8'?>");
			w.WriteLine("<EntireModel>");
			w.WriteLine("<CellarModule id='ling' num='5'>");
			w.WriteLine("<class num='2' id='LexEntry' abstract='false' base='CmObject'/>");
			w.WriteLine("</CellarModule>");
			w.WriteLine("</EntireModel>");
			w.Flush();
			w.Close();
			try
			{
				mdc.InitXml(bogusDataPathname, false);
			}
			finally
			{
				File.Delete(bogusDataPathname);
			}
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		public void CreateMetaDataCacheNonDuplicateClasses()
		{
			string bogusDataPathname = "Good.xml";
			StreamWriter w = File.CreateText(bogusDataPathname);
			w.WriteLine("<?xml version='1.0' encoding='utf-8'?>");
			w.WriteLine("<EntireModel>");
			w.WriteLine("<CellarModule id='ling' num='5'>");
			w.WriteLine("<class num='2' id='LexEntry' abstract='false' base='CmObject'/>");
			w.WriteLine("<class num='14' id='LexPronunciation' abstract='false' base='CmObject'/>");
			w.WriteLine("</CellarModule>");
			w.WriteLine("</EntireModel>");
			w.Flush();
			w.Close();
			try
			{
				IFwMetaDataCache mdc = MetaDataCache.CreateMetaDataCache(bogusDataPathname);
			}
			finally
			{
				File.Delete(bogusDataPathname);
			}
		}
	}

	#endregion MetaDataCacheInitializationTests class

	#region MetaDataCacheBase class

	/// <summary>
	/// Base class for testing the field, class, and virtual methods.
	/// </summary>
	public class MetaDataCacheBase : IFWDisposable
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
			string modelDir = DirectoryFinder.FwSourceDirectory;
			modelDir = modelDir.Substring(0, modelDir.LastIndexOf('\\'));
			modelDir = Path.Combine(modelDir, @"Output\XMI");
			m_metaDataCache = MetaDataCache.CreateMetaDataCache(Path.Combine(modelDir, "xmi2cellar3.xml"));
		}

		#region IDisposable & Co. implementation

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		~MetaDataCacheBase()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Subclasses should override the Dispose method with the parameter
		/// and call the base method to tear down a test fisture class.
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_metaDataCache = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation
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
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		public void GetDstClsNameTest()
		{
			CheckDisposed();
			Assert.AreEqual("LexSense", m_metaDataCache.GetDstClsName(5002011), "Wrong class name");
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		public void GetOwnClsNameTest()
		{
			CheckDisposed();
			Assert.AreEqual("LexSense", m_metaDataCache.GetOwnClsName(5016012), "Wrong class name");
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		public void GetOwnClsIdTest()
		{
			CheckDisposed();
			Assert.AreEqual(5016, m_metaDataCache.GetOwnClsId(5016012), "Wrong class implementor.");
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		public void GetDstClsIdTest()
		{
			CheckDisposed();
			Assert.AreEqual(7, m_metaDataCache.GetDstClsId(5016012), "Wrong class Signature.");
		}

		/// <summary>
		/// This should test for any case where the given flid is not valid.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void GetClsNameForBadFlidTest()
		{
			CheckDisposed();

			string className = m_metaDataCache.GetOwnClsName(50);
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		public void GetFieldIdsTest()
		{
			CheckDisposed();

			int flidSize = m_metaDataCache.FieldCount;
			// Don't fail every time a field is added or deleted!!!
			//Assert.AreEqual(821, flidSize, "Wrong field count");

			uint[] uIds;
			int testFlidSize = flidSize - 1;
			using (ArrayPtr flids = MarshalEx.ArrayToNative(testFlidSize, typeof(uint)))
			{
				m_metaDataCache.GetFieldIds(testFlidSize, flids);
				uIds = (uint[])MarshalEx.NativeToArray(flids, testFlidSize, typeof(uint));
				Assert.AreEqual(testFlidSize, uIds.Length, "Wrong size of fields returned.");
				foreach (uint flid in uIds)
					Assert.IsTrue(flid > 0, "Wrong flid value: " + flid.ToString());
			}
			testFlidSize = flidSize;
			using (ArrayPtr flids = MarshalEx.ArrayToNative(testFlidSize, typeof(uint)))
			{
				m_metaDataCache.GetFieldIds(testFlidSize, flids);
				uIds = (uint[])MarshalEx.NativeToArray(flids, testFlidSize, typeof(uint));
				Assert.AreEqual(testFlidSize, uIds.Length, "Wrong size of fields returned.");
				foreach (uint flid in uIds)
					Assert.IsTrue(flid > 0, "Wrong flid value: " + flid.ToString());
			}
			testFlidSize = flidSize + 1;
			using (ArrayPtr flids = MarshalEx.ArrayToNative(testFlidSize, typeof(uint)))
			{
				m_metaDataCache.GetFieldIds(testFlidSize, flids);
				uIds = (uint[])MarshalEx.NativeToArray(flids, testFlidSize, typeof(uint));
				Assert.AreEqual(testFlidSize, uIds.Length, "Wrong size of fields returned.");
				for (int iflid = 0; iflid < uIds.Length; ++iflid)
				{
					uint flid = uIds[iflid];
					if (iflid < uIds.Length - 1)
						Assert.IsTrue(flid > 0, "Wrong flid value: " + flid.ToString());
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
			CheckDisposed();
			Assert.AreEqual("CitationForm", m_metaDataCache.GetFieldName(5002003),
				"Wrong field name.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetFieldLabelIsNullTest()
		{
			CheckDisposed();
			Assert.IsNull(m_metaDataCache.GetFieldLabel(5002003), "Field label not null.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetFieldHelpIsNullTest()
		{
			CheckDisposed();
			Assert.IsNull(m_metaDataCache.GetFieldHelp(5002003), "Field help not null.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetFieldXmlIsNullTest()
		{
			CheckDisposed();
			Assert.IsNull(m_metaDataCache.GetFieldXml(5002003), "Field XML not null.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetFieldListRootIsZeroTest()
		{
			CheckDisposed();
			Assert.AreEqual(0, m_metaDataCache.GetFieldListRoot(5002003), "Field XML not zero.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetFieldWsIsZeroTest()
		{
			CheckDisposed();
			Assert.AreEqual(0, m_metaDataCache.GetFieldWs(5002003), "Writing system not zero.");
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
			CheckDisposed();

			Assert.AreEqual((int)CellarModuleDefns.kcptBoolean, m_metaDataCache.GetFieldType(7019),
				"Wrong field data type for Boolean data.");

			Assert.AreEqual((int)CellarModuleDefns.kcptInteger, m_metaDataCache.GetFieldType(7015),
				"Wrong field data type for Integer data.");

			Assert.AreEqual((int)CellarModuleDefns.kcptTime, m_metaDataCache.GetFieldType(7011),
				"Wrong field data type for Time data.");

			Assert.AreEqual((int)CellarModuleDefns.kcptGuid, m_metaDataCache.GetFieldType(8021),
				"Wrong field data type for Guid data.");

			Assert.AreEqual((int)CellarModuleDefns.kcptGenDate, m_metaDataCache.GetFieldType(13004),
				"Wrong field data type for GenDate data.");

			Assert.AreEqual((int)CellarModuleDefns.kcptBinary, m_metaDataCache.GetFieldType(15002),
				"Wrong field data type for Binary data.");

			Assert.AreEqual((int)CellarModuleDefns.kcptBigString, m_metaDataCache.GetFieldType(5016030),
				"Wrong field data type for BigString data.");

			Assert.AreEqual((int)CellarModuleDefns.kcptString, m_metaDataCache.GetFieldType(5097008),
				"Wrong field data type for String data.");

			Assert.AreEqual((int)CellarModuleDefns.kcptMultiBigString,
				m_metaDataCache.GetFieldType(5016020),
				"Wrong field data type for MultiBigString data.");

			Assert.AreEqual((int)CellarModuleDefns.kcptMultiString,
				m_metaDataCache.GetFieldType(5016016), "Wrong field data type for MultiString data.");

			Assert.AreEqual((int)CellarModuleDefns.kcptBigUnicode, m_metaDataCache.GetFieldType(34001),
				"Wrong field data type for BigUnicode data.");

			Assert.AreEqual((int)CellarModuleDefns.kcptUnicode, m_metaDataCache.GetFieldType(55009),
				"Wrong field data type for Unicode data.");

			Assert.AreEqual((int)CellarModuleDefns.kcptMultiUnicode,
				m_metaDataCache.GetFieldType(55005), "Wrong field data type for MultiUnicode data.");
		}

		/// <summary>
		/// Check for validity of adding the given clid to some field.
		/// </summary>
		[Test]
		public void get_IsValidClassTest()
		{
			CheckDisposed();

			bool isValid = m_metaDataCache.get_IsValidClass(5002019, 0);
			Assert.IsTrue(isValid, "Can't put a CmObject into a signature of CmObject?");

			isValid = m_metaDataCache.get_IsValidClass(5002011, 0);
			Assert.IsFalse(isValid, "Can put a CmObject into a signature of LexSense?");

			isValid = m_metaDataCache.get_IsValidClass(5016012, 5064);
			Assert.IsFalse(isValid, "Can put a WordFormLookup into a signature of CmPossibility?");

			isValid = m_metaDataCache.get_IsValidClass(5016012, 5049);
			Assert.IsTrue(isValid, "Can't put a PartOfSpeech into a signature of CmPossibility, even if you probably shouldn't?");

			isValid = m_metaDataCache.get_IsValidClass(5002001, 5049);
			Assert.IsFalse(isValid, "Can put a PartOfSpeech into a basic field?");
		}

		/// <summary>
		/// Check for validity of adding the given clid to an illegal field of 0.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void get_IsValidClassBadTest()
		{
			CheckDisposed();

			bool isValid = m_metaDataCache.get_IsValidClass(0, 0);
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
			CheckDisposed();
			Assert.AreEqual("PartOfSpeech", m_metaDataCache.GetClassName(5049),
				"Wrong class name for PartOfSpeech.");
		}

		/// <summary>
		/// Check for finding the class name based on the given clid.
		/// </summary>
		[Test]
		public void GetAbstractTest()
		{
			CheckDisposed();
			Assert.IsFalse(m_metaDataCache.GetAbstract(5049), "PartOfSpeech is a concrete class.");
			Assert.IsTrue(m_metaDataCache.GetAbstract(0), "CmObject is an abstract class.");
		}

		/// <summary>
		/// Check for finding the class name based on the given clid.
		/// </summary>
		[Test]
		public void GetBaseClsIdTest()
		{
			CheckDisposed();
			Assert.AreEqual(7, m_metaDataCache.GetBaseClsId(5049),
				"Wrong base class id for PartOfSpeech.");
		}

		/// <summary>
		/// Check for finding the class name based on the given clid.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void GetBaseClsIdBadTest()
		{
			CheckDisposed();
			uint baseClassClid = m_metaDataCache.GetBaseClsId(0);
		}

		/// <summary>
		/// Check for finding the class name based on the given clid.
		/// </summary>
		[Test]
		public void GetBaseClsNameTest()
		{
			CheckDisposed();
			Assert.AreEqual("CmPossibility", m_metaDataCache.GetBaseClsName(5049),
				"Wrong base class id for PartOfSpeech.");
		}

		/// <summary>
		/// Check for finding the class name based on the given clid.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void GetBaseClsNameBadTest()
		{
			CheckDisposed();

			string baseClassName = m_metaDataCache.GetBaseClsName(0);
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetClassIdsTest()
		{
			CheckDisposed();

			// Just use the count for all classes,
			// even though we know it will never be that high a number that can be returned.
			uint[] uIds;
			int countAllClasses = m_metaDataCache.ClassCount;
			using (ArrayPtr clids = MarshalEx.ArrayToNative(countAllClasses, typeof(uint)))
			{
				m_metaDataCache.GetClassIds(countAllClasses, clids);
				uIds = (uint[])MarshalEx.NativeToArray(clids, countAllClasses, typeof(uint));
				Assert.AreEqual(countAllClasses, uIds.Length, "Wrong number of classes returned.");
			}
			countAllClasses = 2;
			using (ArrayPtr clids = MarshalEx.ArrayToNative(countAllClasses, typeof(uint)))
			{
				// Check MoForm (all of its direct subclasses).
				m_metaDataCache.GetClassIds(countAllClasses, clids);
				uIds = (uint[])MarshalEx.NativeToArray(clids, 2, typeof(uint));
				Assert.AreEqual(countAllClasses, uIds.Length, "Wrong number of classes returned.");
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void GetFieldsTest()
		{
			CheckDisposed();

			uint[] uIds;
			int countAllFlids;
			int countAllFlidsOut;
			using (ArrayPtr flids = MarshalEx.ArrayToNative(500, typeof(uint)))
			{
				countAllFlidsOut = m_metaDataCache.GetFields(0, true, (int)CellarModuleDefns.kgrfcptAll, 0, flids);
				countAllFlids = countAllFlidsOut;
				countAllFlidsOut = m_metaDataCache.GetFields(0, true, (int)CellarModuleDefns.kgrfcptAll, countAllFlidsOut, flids);
				uIds = (uint[])MarshalEx.NativeToArray(flids, countAllFlidsOut, typeof(uint));
				Assert.AreEqual(countAllFlids, countAllFlidsOut, "Wrong number of fields returned for CmObject.");
			}
			countAllFlids = countAllFlidsOut = 0;
			using (ArrayPtr flids = MarshalEx.ArrayToNative(500, typeof(uint)))
			{
				countAllFlidsOut = m_metaDataCache.GetFields(5035, true, (int)CellarModuleDefns.kgrfcptAll, 0, flids);
				countAllFlids = countAllFlidsOut;
				countAllFlidsOut = m_metaDataCache.GetFields(5035, true, (int)CellarModuleDefns.kgrfcptAll, countAllFlidsOut, flids);
				uIds = (uint[])MarshalEx.NativeToArray(flids, countAllFlidsOut, typeof(uint));
				Assert.AreEqual(9, countAllFlidsOut, "Wrong number of fields returned for 5035.");
			}
			countAllFlids = countAllFlidsOut = 0;
			using (ArrayPtr flids = MarshalEx.ArrayToNative(500, typeof(uint)))
			{
				countAllFlidsOut = m_metaDataCache.GetFields(5035, true, (int)CellarModuleDefns.kgrfcptReference, 0, flids);
				countAllFlids = countAllFlidsOut;
				countAllFlidsOut = m_metaDataCache.GetFields(5035, true, (int)CellarModuleDefns.kgrfcptReference, countAllFlidsOut, flids);
				uIds = (uint[])MarshalEx.NativeToArray(flids, countAllFlidsOut, typeof(uint));
				Assert.AreEqual(2, countAllFlidsOut, "Wrong number of fields returned for 5035.");
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void GetFieldsBadTest()
		{
			CheckDisposed();

			int countAllFlids;
			int countAllFlidsOut;
			using (ArrayPtr flids = MarshalEx.ArrayToNative(500, typeof(uint)))
			{
				countAllFlidsOut = m_metaDataCache.GetFields(5035, true, (int)CellarModuleDefns.kgrfcptReference, 0, flids);
				countAllFlids = countAllFlidsOut;
				countAllFlidsOut = m_metaDataCache.GetFields(5035, true, (int)CellarModuleDefns.kgrfcptAll, countAllFlidsOut, flids);
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
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		public void GetClassIdTest()
		{
			CheckDisposed();

			uint clid = m_metaDataCache.GetClassId("LexEntry");
			Assert.AreEqual(5002, clid, "Wrong class Id.");
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void GetClassIdBadTest()
		{
			CheckDisposed();

			uint clid;
			clid = m_metaDataCache.GetClassId("NonExistantClassName");
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		public void GetFieldIdSansSuperClassCheckTest()
		{
			CheckDisposed();

			uint flid;
			flid = m_metaDataCache.GetFieldId("LexEntry", "CitationForm", false);
			Assert.AreEqual(5002003, flid, "Wrong field Id.");
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		public void GetFieldIdWithSuperClassCheckTest()
		{
			CheckDisposed();

			uint flid;
			flid = m_metaDataCache.GetFieldId("PartOfSpeech", "Name", true);
			Assert.AreEqual(7001, flid, "Wrong field Id.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly.
		/// </summary>
		[Test]
		public void GetFieldIdSansSuperClassCheckBadTest1()
		{
			CheckDisposed();

			uint flid;
			flid = m_metaDataCache.GetFieldId("MoStemMsa", "CitationForm", false);
			Assert.AreEqual(0, flid, "Wrong field Id.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetFieldIdWithSuperClassCheckBadTest()
		{
			CheckDisposed();

			uint flid;
			flid = m_metaDataCache.GetFieldId("MoStemMsa", "CitationForm", true);
			Assert.AreEqual(0, flid, "Wrong field Id.");
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		public void GetFieldId2SansSuperClassCheckTest()
		{
			CheckDisposed();

			uint flid;
			flid = m_metaDataCache.GetFieldId2(5002, "CitationForm", false);
			Assert.AreEqual(5002003, flid, "Wrong field Id.");
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existant input pathname to the XML file.
		/// </summary>
		[Test]
		public void GetFieldId2WithSuperClassCheckTest()
		{
			CheckDisposed();

			uint flid;
			flid = m_metaDataCache.GetFieldId2(5049, "Name", true);
			Assert.AreEqual(7001, flid, "Wrong field Id.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly.
		/// </summary>
		[Test]
		public void GetFieldId2SansSuperClassCheckBadTest1()
		{
			CheckDisposed();

			uint flid;
			flid = m_metaDataCache.GetFieldId2(5001, "CitationForm", false);
			Assert.AreEqual(0, flid, "Wrong field Id.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetFieldId2WithSuperClassCheckBadTest()
		{
			CheckDisposed();

			uint flid;
			flid = m_metaDataCache.GetFieldId2(5001, "CitationForm", true);
			Assert.AreEqual(0, flid, "Wrong field Id.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetDirectSubclassesTest()
		{
			CheckDisposed();

			// Just use the count for all classes,
			// even though we know it will never be that high a number that can be returned.
			int countAllClasses = m_metaDataCache.ClassCount;
			int countDirectSubclasses = 0;
			using (ArrayPtr clids = MarshalEx.ArrayToNative(countAllClasses, typeof(uint)))
			{
				// Check PartOfSpeech.
				m_metaDataCache.GetDirectSubclasses(5049, countAllClasses, out countDirectSubclasses, clids);
				Assert.AreEqual(0, countDirectSubclasses, "Wrong number of subclasses returned.");
			}
			countDirectSubclasses = 0;
			using (ArrayPtr clids = MarshalEx.ArrayToNative(countAllClasses, typeof(uint)))
			{
				// Check MoForm (all of its direct subclasses).
				m_metaDataCache.GetDirectSubclasses(5035, countAllClasses, out countDirectSubclasses, clids);
				Assert.AreEqual(2, countDirectSubclasses, "Wrong number of subclasses returned.");
				uint[] uIds = (uint[])MarshalEx.NativeToArray(clids, countAllClasses, typeof(uint));
				for (int i = 0; i < uIds.Length; ++i)
				{
					uint clid = uIds[i];
					if (i < 2)
						Assert.IsTrue(((clid == 5028) || (clid == 5045)), "Clid should be 5028 or 5049 for direct subclasses of MoForm.");
					else
						Assert.AreEqual(0, clid, "Clid should be 0 from here on.");
				}
			}
			/* The method does not support getting some arbitrary subset of subclasses.
			 * The array must contain at least that many spaces, if not more.
			countDirectSubclasses = 0;
			using (ArrayPtr clids = MarshalEx.ArrayToNative(1, typeof(uint)))
			{
				// Check MoForm (but only 1 of its subclasses).
				m_mdc.GetDirectSubclasses(5035, 1, out countDirectSubclasses, clids);
				Assert.AreEqual(1, countDirectSubclasses, "Wrong number of subclasses returned.");
			}
			*/
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetDirectSubclassesCountUnknownTest()
		{
			CheckDisposed();

			int countAllClasses;
			m_metaDataCache.GetDirectSubclasses(5035, 0, out countAllClasses, null);
			Assert.AreEqual(2, countAllClasses, "Wrong number of subclasses returned.");
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetAllSubclassesPartOfSpeechTest()
		{
			CheckDisposed();

			// Just use the count for all classes,
			// even though we know it will never be that high a number that can be returned.
			int countAllClasses = m_metaDataCache.ClassCount;
			using (ArrayPtr clids = MarshalEx.ArrayToNative(countAllClasses, typeof(uint)))
			{
				// Check PartOfSpeech.
				int countAllSubclasses = 0;
				m_metaDataCache.GetAllSubclasses(5049, countAllClasses, out countAllSubclasses, clids);
				Assert.AreEqual(1, countAllSubclasses, "Wrong number of subclasses returned.");
			}
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetAllSubclassesMoFormAllTest()
		{
			CheckDisposed();

			// Just use the count for all classes,
			// even though we know it will never be that high a number that can be returned.
			int countAllClasses = m_metaDataCache.ClassCount;
			using (ArrayPtr clids = MarshalEx.ArrayToNative(countAllClasses, typeof(uint)))
			{
				// Check MoForm (all of its direct subclasses).
				int countAllSubclasses = 0;
				m_metaDataCache.GetAllSubclasses(5035, countAllClasses, out countAllSubclasses, clids);
				Assert.AreEqual(5, countAllSubclasses, "Wrong number of subclasses returned.");
			}
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetAllSubclassesMoFormLimitedTest()
		{
			CheckDisposed();

			// Just use the count for all classes,
			// even though we know it will never be that high a number that can be returned.
			int countAllClasses = m_metaDataCache.ClassCount;
			using (ArrayPtr clids = MarshalEx.ArrayToNative(2, typeof(uint)))
			{
				// Check MoForm (but get it and only 1 of its subclasses).
				int countAllSubclasses = 0;
				m_metaDataCache.GetAllSubclasses(5035, 2, out countAllSubclasses, clids);
				Assert.AreEqual(5, countAllSubclasses, "Wrong number of subclasses returned.");
			}
		}

		/// <summary>
		/// Check for case where the specified clid has no such field, directly, or on its superclasses.
		/// </summary>
		[Test]
		public void GetAllSubclassesCmObjectTest()
		{
			CheckDisposed();

			// Just use the count for all classes,
			// even though we know it will never be that high a number that can be returned.
			int countAllClasses = m_metaDataCache.ClassCount;
			using (ArrayPtr clids = MarshalEx.ArrayToNative(countAllClasses, typeof(uint)))
			{
				// Check CmObject.
				int countAllSubclasses = 0;
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
			CheckDisposed();

			uint flid = 2000000001;
			int type = (int)CellarModuleDefns.kcptImage;
			string className = "PartOfSpeech";
			string fieldName = "NewImageVP";
			m_metaDataCache.AddVirtualProp(className, fieldName, flid, type);
			// Check its flid.
			uint newFlid = m_metaDataCache.GetFieldId(className, fieldName, false);
			Assert.AreEqual(flid, newFlid, "Wrong field Id.");
			// Check its data type.
			Assert.AreEqual(type, m_metaDataCache.GetFieldType(flid), "Wrong field type.");
			// Check to see it is virtual.
			bool isVirtual = m_metaDataCache.get_IsVirtual(flid);
			Assert.IsTrue(isVirtual, "Wrong field virtual setting.");
			// Check the clid it was supposed to be placed in.
			uint clid = m_metaDataCache.GetClassId(className);
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
			CheckDisposed();

			bool isVirtual = m_metaDataCache.get_IsVirtual(101);
			Assert.IsFalse(isVirtual, "Wrong field virtual setting.");
		}

		/// <summary>
		/// Check for case where the specified class for the new virtual field doesn't exist.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void AddVirtualPropNoClassTest()
		{
			CheckDisposed();

			uint flid = 2000000002;
			int type = (int)CellarModuleDefns.kcptImage;
			string className = "BogusClass";
			string fieldName = "NewImageVP";
			m_metaDataCache.AddVirtualProp(className, fieldName, flid, type);
		}

		/// <summary>
		/// Check for case where the specified field name for the new virtual field exists.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void AddVirtualPropFieldExistsTest()
		{
			CheckDisposed();

			uint flid = 2000000003;
			int type = (int)CellarModuleDefns.kcptImage;
			string className = "CmPossibility";
			string fieldName = "Name";
			m_metaDataCache.AddVirtualProp(className, fieldName, flid, type);
		}

		/// <summary>
		/// Check for case where the specified flid for the new virtual field exists.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void AddVirtualPropFlidExistsTest()
		{
			CheckDisposed();

			uint flid = m_metaDataCache.GetFieldId("PartOfSpeech", "Name", true);
			int type = (int)CellarModuleDefns.kcptImage;
			string className = "PartOfSpeech";
			string fieldName = "NewName";
			m_metaDataCache.AddVirtualProp(className, fieldName, flid, type);
		}

		/// <summary>
		/// Check for case where the specified flid for the new virtual field exists.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void AddVirtualPropInvalidLowFieldTypeTest()
		{
			CheckDisposed();

			uint flid = 2000000004;
			int type = 0; // Below acceptable level.
			string className = "PartOfSpeech";
			string fieldName = "NewName";
			m_metaDataCache.AddVirtualProp(className, fieldName, flid, type);
		}

		/// <summary>
		/// Check for case where the specified flid for the new virtual field exists.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void AddVirtualPropInvalidHighFieldTypeTest()
		{
			CheckDisposed();

			uint flid = 2000000005;
			int type = 1000; // Above acceptable level.
			string className = "PartOfSpeech";
			string fieldName = "NewName";
			m_metaDataCache.AddVirtualProp(className, fieldName, flid, type);
		}
	}

	#endregion MetaDataCacheVirtualPropTests class
}
