// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ConvertFailuresTests.cs
// Responsibility:

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// Summary description for ConvertFailuresTests.
	/// </summary>
	[TestFixture]
	public class ConvertFailuresTests : BaseTest
	{
		private static readonly string[] SaTesting = { "A", "AB", "ABC", "ABCD", "ABCDE", "ABCDEF" };

		private XmlDocument m_doc;
		private int m_testingCount;

		/// <summary>
		/// Location of simple test FXT files
		/// </summary>
		protected string m_sTestPath;

		[TestFixtureSetUp]
		public void FixtureInit()
		{
			m_sTestPath = Path.Combine(FwDirectoryFinder.SourceDirectory,
				"LexText/ParserUI/ParserUITests");
			string sFailureDocPath = Path.Combine(m_sTestPath, "Failures.xml");
			m_doc = new XmlDocument();
			m_doc.Load(sFailureDocPath);
		}


		[TestFixtureTearDown]
		public void FixtureCleanUp()
		{
		}

		private string GetRepresentation(int hvo)
		{
			if (m_testingCount > 5)
				m_testingCount = 0;
			return SaTesting[m_testingCount++];
		}

		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		public void ConvertANCCFailureStrings()
		{
			XmlNodeList nl = m_doc.SelectNodes("//failure[contains(@test,'ANCC_FT')]");
			Assert.IsTrue(nl.Count == 2, "Two ANCC failures");
			m_testingCount = 0;
			XAmpleParser.ConvertAdHocFailures(m_doc, "ANCC_FT", GetRepresentation);
			int i = 1;
			foreach (XmlNode node in nl)
			{
				XmlNode test = node.Attributes.GetNamedItem("test");
				string s = test.InnerText;
				switch (i)
				{
					case 1:
						Assert.AreEqual("ANCC_FT:  A   -/ _ ... AB", s);
						break;
					case 2:
						Assert.AreEqual("ANCC_FT:  ABC   -/ ABCD ... _ ", s);
						break;
				}
				i++;
			}
		}

		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		public void ConvertMCCFailureStrings()
		{
			XmlNodeList nl = m_doc.SelectNodes("//failure[contains(@test,'MCC_FT')]");
			Assert.IsTrue(nl.Count == 2, "Two MCC failures");
			m_testingCount = 0;
			XAmpleParser.ConvertAdHocFailures(m_doc, "MCC_FT", GetRepresentation);
			int i = 1;
			foreach (XmlNode node in nl)
			{
				XmlNode test = node.Attributes.GetNamedItem("test");
				string s = test.InnerText;
				switch (i)
				{
					case 1:
						Assert.AreEqual("MCC_FT:  A   +/ ~_ AB", s);
						break;
					case 2:
						Assert.AreEqual("MCC_FT:  ABC   +/ ABCD ~_", s);
						break;
				}
				i++;
			}
		}

		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		public void ConvertSECFailureStrings()
		{
			XmlNodeList nl = m_doc.SelectNodes("//failure[contains(@test,'SEC_ST') and contains(@test,'[')]");
			Assert.IsTrue(nl.Count == 8, "Eight SEC failures with classes");
			m_testingCount = 0;
			XAmpleParser.ConvertNaturalClasses(m_doc, "SEC_ST", GetRepresentation);
			int i = 1;
			foreach (XmlNode node in nl)
			{
				XmlNode test = node.Attributes.GetNamedItem("test");
				string s = test.InnerText;
				switch (i)
				{
					case 1:
						Assert.AreEqual("SEC_ST: dok __ ra   / _ [A]", s);
						break;
					case 2:
						Assert.AreEqual("SEC_ST: dok __ ra   / _ [AB][ABC]", s);
						break;
					case 3:
						Assert.AreEqual("SEC_ST: migel __ ximura   / [ABCD] _", s);
						break;
					case 4:
						Assert.AreEqual("SEC_ST: migel __ ximura   / [ABCDE][ABCDEF] _", s);
						break;
					case 5:
						Assert.AreEqual("SEC_ST: migel __ ximura   / [A] _ [AB]", s);
						break;
					case 6:
						Assert.AreEqual("SEC_ST: migel __ ximura   / [ABC][ABCD] _ [ABCDE]", s);
						break;
					case 7:
						Assert.AreEqual("SEC_ST: migel __ ximura   / [ABCDEF] _ [A][AB]", s);
						break;
					case 8:
						Assert.AreEqual("SEC_ST: migel __ ximura   / [ABC][ABCD] _ [ABCDE][ABCDEF]", s);
						break;
				}
				i++;
			}
		}
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		public void ConvertInfixEnvironmentFailureStrings()
		{
			XmlNodeList nl = m_doc.SelectNodes("//failure[contains(@test,'InfixEnvironment') and contains(@test,'[')]");
			Assert.IsTrue(nl.Count == 8, "Eight Infix Environment failures with classes");
			m_testingCount = 0;
			XAmpleParser.ConvertNaturalClasses(m_doc, "InfixEnvironment", GetRepresentation);
			int i = 1;
			foreach (XmlNode node in nl)
			{
				XmlNode test = node.Attributes.GetNamedItem("test");
				string s = test.InnerText;
				switch (i)
				{
					case 1:
						Assert.AreEqual("InfixEnvironment: dok __ ra   / _ [A]", s);
						break;
					case 2:
						Assert.AreEqual("InfixEnvironment: dok __ ra   / _ [AB][ABC]", s);
						break;
					case 3:
						Assert.AreEqual("InfixEnvironment: migel __ ximura   / [ABCD] _", s);
						break;
					case 4:
						Assert.AreEqual("InfixEnvironment: migel __ ximura   / [ABCDE][ABCDEF] _", s);
						break;
					case 5:
						Assert.AreEqual("InfixEnvironment: migel __ ximura   / [A] _ [AB]", s);
						break;
					case 6:
						Assert.AreEqual("InfixEnvironment: migel __ ximura   / [ABC][ABCD] _ [ABCDE]", s);
						break;
					case 7:
						Assert.AreEqual("InfixEnvironment: migel __ ximura   / [ABCDEF] _ [A][AB]", s);
						break;
					case 8:
						Assert.AreEqual("InfixEnvironment: migel __ ximura   / [ABC][ABCD] _ [ABCDE][ABCDEF]", s);
						break;
				}
				i++;
			}
		}
	}
}
