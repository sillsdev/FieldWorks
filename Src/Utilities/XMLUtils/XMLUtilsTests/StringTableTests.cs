// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: StringTableTests.cs
// Authorship History: John Hatton
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using NUnit.Framework;
using System.Reflection;
using System.Xml;

namespace SIL.Utils
{
	/// <summary>
	/// Summary description for StringTableTests.
	/// </summary>
	[TestFixture]
	public class StringTableTests : TestBaseForTestsThatCreateTempFilesBasedOnResources
	{
		protected StringTable m_table;

		[TestFixtureSetUp]
		public void FixtureInit()
		{
			string baseFolder = CreateTempTestFiles(typeof(Properties.Resources), "food");
			m_table = new StringTable(System.IO.Path.Combine(baseFolder, "fruit/citrus"));
		}

		private Assembly GetExecutingAssembly()
		{
			throw new NotImplementedException();
		}

		[Test]
		public void InBaseFile()
		{
			Assert.AreEqual("orng", m_table.GetString("orange"));
		}
		[Test]
		public void InParentFile()
		{
			Assert.AreEqual("pssnfrt", m_table.GetString("passion fruit"));
			Assert.AreEqual("ppy", m_table.GetString("papaya"));
		}

		[Test]
		public void OmitTxtAttribute()
		{
			/* 		<!-- this one demonstrates that omiting the txt attribute just means that we should return the id value -->
					<string id="Banana"/>
			*/
			Assert.AreEqual("Banana", m_table.GetString("Banana"));
		}


		[Test]
		public void WithPath()
		{
			Assert.AreEqual(m_table.GetString("MyPineapple", "InPng/InMyYard"), "pnppl");
		}

		[Test]
		public void WithXPathFragment()
		{
			//find any group that contains a match
			//the leading '/' here will lead to a double slash,
			//	something like strings//group,
			//meaning that this can be found in any group.
			Assert.AreEqual(m_table.GetStringWithXPath("MyPineapple", "/group/"), "pnppl");
		}

		[Test]
		public void WithRootXPathFragment()
		{
			// Give the path of groups explicitly in a compact form.
			Assert.AreEqual(m_table.GetString("MyPineapple", "InPng/InMyYard"), "pnppl");
		}


		[Test]
		public void StringListXmlNode()
		{
			XmlDocument doc =  new XmlDocument();
			doc.LoadXml(@"<stringList group='InPng/InMyYard' ids='   MyPapaya, MyPineapple  '/>");
			XmlNode node = doc.FirstChild;
			string[] strings = m_table.GetStringsFromStringListNode(node);
			Assert.AreEqual(2, strings.Length);
			Assert.AreEqual(strings[1], "pnppl");
		}
	}
}
