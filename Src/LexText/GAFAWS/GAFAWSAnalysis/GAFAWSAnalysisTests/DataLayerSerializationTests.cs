// --------------------------------------------------------------------------------------------
// <copyright from='2003' to='2003' company='SIL International'>
//    Copyright (c) 2003, SIL International. All Rights Reserved.
// </copyright>
//
// File: DataLayerSerializationTests.cs
// Responsibility: RandyR
// Last reviewed:
//
// <remarks>
// Unit tests for data layer serialization.
// </remarks>
//
// --------------------------------------------------------------------------------------------
using System;
using System.IO;

using NUnit.Framework;

namespace SIL.WordWorks.GAFAWS
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// GAFAWS data layer serialization testing class.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	[TestFixture]
	public class SerializationTests : DataLayerBase
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// A known set of data.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private string m_dataBefore = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
			"<GAFAWSData xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"" +
				" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n" +
			"  <WordRecords>\r\n" +
			"    <WordRecord WRID=\"WR1\">\r\n" +
			"      <Stem MIDREF=\"M1\" />\r\n" +
			"    </WordRecord>\r\n" +
			"  </WordRecords>\r\n" +
			"  <Morphemes>\r\n" +
			"    <Morpheme MID=\"M1\" type=\"s\" />\r\n" +
			"  </Morphemes>\r\n" +
			"  <Classes>\r\n" +
			"    <PrefixClasses />\r\n" +
			"    <SuffixClasses />\r\n" +
			"  </Classes>\r\n" +
			"  <Challenges />\r\n" +
			"</GAFAWSData>";

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initialize a class before each test is run.
		/// This is called by NUnit before each test.
		/// It ensures each test will have a brand new GAFAWSData object to work with.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			m_gd = GAFAWSData.Create();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public SerializationTests()
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Try to save with null as the pathname.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void SaveDataWithNullPathname()
		{
			m_gd.SaveData(null);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Save a good set of data.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void SaveData()
		{
			string fileName = null;
			StreamReader reader = null;
			try
			{
				Classes cls = m_gd.Classes;
				Assert.IsNotNull(cls);
				Morpheme m = new Morpheme(MorphemeType.stem, "M1");
				m_gd.Morphemes.Add(m);
				WordRecord wr = new WordRecord();
				m_gd.WordRecords.Add(wr);
				wr.WRID = "WR1";
				wr.Stem = new Stem();
				wr.Stem.MIDREF = m.MID;
				fileName = MakeFile();
				m_gd.SaveData(fileName);
				reader = new StreamReader(fileName);
				string dataAfter = reader.ReadToEnd();
				Assert.AreEqual(m_dataBefore, dataAfter, "Before and After");
			}
			finally
			{
				if (reader != null)
					reader.Close();
				DeleteFile(fileName);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Try loading a null pathname.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void LoadDataWithNullPathname()
		{
			m_gd = GAFAWSData.LoadData(null);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Load good data set.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void LoadGoodData()
		{
			string fileName = null;
			try
			{
				Assert.AreEqual(0, m_gd.Morphemes.Count);	// Shouldn't have any at this point.
				fileName = MakeFile(m_dataBefore);
				m_gd = GAFAWSData.LoadData(fileName);
				Assert.AreEqual(1, m_gd.Morphemes.Count);	// Should be 1 of them now.
				Assert.AreEqual(1, m_gd.WordRecords.Count, "Wrong word record count.");
				WordRecord wr = m_gd.WordRecords[0];
				Assert.IsNull(wr.Prefixes, "Should have null preffix collection.");
				Assert.IsNull(wr.Suffixes, "Should have null suffix collection.");
			}
			finally
			{
				DeleteFile(fileName);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Try loading data file that is empty.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(InvalidOperationException))]
		public void LoadDataWithEmptyFile()
		{
			string fileName = null;
			try
			{
				fileName = MakeFile();
				m_gd = GAFAWSData.LoadData(fileName);
			}
			finally
			{
				DeleteFile(fileName);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Try loading XML data that isn't in the right model of data.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(InvalidOperationException))]
		public void LoadDataWrongXML()
		{
			string fileName = null;
			try
			{
				fileName = MakeFile("<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
					"<NOTGAFAWSData xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"" +
						" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">\r\n" +
					"</NOTGAFAWSData>");
				m_gd = GAFAWSData.LoadData(fileName);
			}
			finally
			{
				DeleteFile(fileName);
			}
		}
	}
}
