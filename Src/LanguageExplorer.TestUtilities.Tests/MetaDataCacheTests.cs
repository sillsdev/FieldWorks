// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Xml;
using NUnit.Framework;
using SIL.LCModel.Utils;

namespace LanguageExplorer.TestUtilities.Tests
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
		[OneTimeSetUp]
		public void FixtureSetup()
		{
			FileUtils.Manager.SetFileAdapter(new MockFileOS());
		}

		/// <summary />
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
			Assert.That(() => MetaDataCache.CreateMetaDataCache(null),
				Throws.ArgumentNullException);
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existent input pathname to the XML file.
		/// </summary>
		[Test]
		public void CreateMetaDataCacheEmptyPathname()
		{
			Assert.That(() => MetaDataCache.CreateMetaDataCache(string.Empty),
				Throws.ArgumentNullException);
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existent input pathname to the XML file.
		/// </summary>
		[Test]
		public void CreateMetaDataCacheBadPathname()
		{
			Assert.That(() => MetaDataCache.CreateMetaDataCache(MDCTestUtils.GetPathToTestFile("MyBadpathname")),
				Throws.InvalidOperationException);
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existent input pathname to the XML file.
		/// </summary>
		[Test]
		public void CreateMetaDataCacheNotXMLData()
		{
			Assert.That(() => MetaDataCache.CreateMetaDataCache(MDCTestUtils.GetPathToTestFile("NotXml.txt")),
				Throws.Exception.TypeOf<XmlException>());
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-existent input pathname to the XML file.
		/// </summary>
		[Test]
		public void CreateMetaDataCacheEntireModelXMLData()
		{
			Assert.That(() => MetaDataCache.CreateMetaDataCache(MDCTestUtils.GetPathToTestFile("Non-existent.xml")),
				Throws.InvalidOperationException);
		}

		/// <summary>
		/// Tests creating a MetaDataCache with an XML file containing no classes.
		/// </summary>
		[Test]
		public void CreateMetaDataCacheCellarModuleXMLData()
		{
			Assert.That(() => MetaDataCache.CreateMetaDataCache(MDCTestUtils.GetPathToTestFile("NoClasses.xml")),
				Throws.ArgumentException);
		}

		/// <summary>
		/// Tests attempting to initialize a MetaDataCache twice with the same XML file.
		/// </summary>
		[Test]
		public void CreateMetaDataCacheDuplicateFiles()
		{
			var mdc = MetaDataCache.CreateMetaDataCache(MDCTestUtils.GetPathToTestFile("Good.xml"));
			Assert.That(() => mdc.InitXml(MDCTestUtils.GetPathToTestFile("Good.xml"), false),
				Throws.ArgumentException);
		}

		/// <summary>
		/// Tests creating a MetaDataCache with duplicate classes defined in an XML file.
		/// </summary>
		[Test]
		public void CreateMetaDataCacheDuplicateClassesInXmlFile()
		{
			Assert.That(() => MetaDataCache.CreateMetaDataCache(MDCTestUtils.GetPathToTestFile("Bogus_DuplicateClassNames.xml")),
				Throws.ArgumentException);
		}

		/// <summary>
		/// Tests creating a MetaDataCache with duplicate classes defined in different XML files.
		/// </summary>
		[Test]
		public void CreateMetaDataCacheDuplicateClassesInTwoFiles()
		{
			var mdc = MetaDataCache.CreateMetaDataCache(MDCTestUtils.GetPathToTestFile("Good.xml"));
			Assert.That(() => mdc.InitXml(MDCTestUtils.GetPathToTestFile("ReallyGood.xml"), false),
				Throws.ArgumentException);
		}

		/// <summary>
		/// Tests creating a MetaDataCache with non-duplicate classes.
		/// </summary>
		[Test]
		public void CreateMetaDataCacheNonDuplicateClasses()
		{
			Assert.That(() => MetaDataCache.CreateMetaDataCache(MDCTestUtils.GetPathToTestFile("Good_2.xml")),
				Throws.Nothing);
		}
	}
}