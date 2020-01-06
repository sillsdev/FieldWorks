// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.CacheLight;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.CacheLightTests
{
	/// <summary>
	/// Test cache initialization.
	/// </summary>
	[TestFixture]
	public class MetaDataCacheInitializationTests
	{
		/// <summary>
		/// Don't use the real file system
		/// </summary>
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			FileUtils.Manager.SetFileAdapter(new MockFileOS());
		}

		/// <summary />
		[TestFixtureTearDown]
		public void FixtureTeardown()
		{
			FileUtils.Manager.Reset();
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
		/// Tests creating a MetaDataCache with non-existent input pathname to the XML file.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void CreateMetaDataCacheEmptyPathname()
		{
			MetaDataCache.CreateMetaDataCache("");
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existent input pathname to the XML file.
		/// </summary>
		[Test]
		[ExpectedException(typeof(InvalidOperationException))]
		public void CreateMetaDataCacheBadPathname()
		{
			MetaDataCache.CreateMetaDataCache("MyBadpathname");
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existent input pathname to the XML file.
		/// </summary>
		[Test]
		[ExpectedException(typeof(XmlException))]
		public void CreateMetaDataCacheNotXMLData()
		{
			MetaDataCache.CreateMetaDataCache("NotXml.txt");
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existent input pathname to the XML file.
		/// </summary>
		[Test]
		[ExpectedException(typeof(InvalidOperationException))]
		public void CreateMetaDataCacheEntireModelXMLData()
		{
			MetaDataCache.CreateMetaDataCache("Non-existent.xml");
		}

		/// <summary>
		/// Tests creating a MetaDataCache with an XML file containing no classes.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void CreateMetaDataCacheCellarModuleXMLData()
		{
			MetaDataCache.CreateMetaDataCache("NoClasses.xml");
		}

		/// <summary>
		/// Tests attempting to initialize a MetaDataCache twice with the same XML file.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void CreateMetaDataCacheDuplicateFiles()
		{
			var mdc = MetaDataCache.CreateMetaDataCache("Good.xml");
			mdc.InitXml("Good.xml", false);
		}

		/// <summary>
		/// Tests creating a MetaDataCache with duplicate classes defined in an XML file.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void CreateMetaDataCacheDuplicateClassesInXmlFile()
		{
			MetaDataCache.CreateMetaDataCache("Bogus_DuplicateClassNames.xml");
		}

		/// <summary>
		/// Tests creating a MetaDataCache with duplicate classes defined in different XML files.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void CreateMetaDataCacheDuplicateClassesInTwoFiles()
		{
			var mdc = MetaDataCache.CreateMetaDataCache("Good.xml");
			mdc.InitXml("ReallyGood.xml", false);
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-duplicate classes.
		/// </summary>
		[Test]
		public void CreateMetaDataCacheNonDuplicateClasses()
		{
			MetaDataCache.CreateMetaDataCache("Good_2.xml");
		}
	}
}
