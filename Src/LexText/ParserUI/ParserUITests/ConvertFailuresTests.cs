// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ConvertFailuresTests.cs
// Responsibility:
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;
using System.Xml.Xsl;

using SIL.FieldWorks.Common.FwUtils;

using NUnit.Framework;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for ConvertFailuresTests.
	/// </summary>
	[TestFixture]
	public class ConvertFailuresTests: BaseTest
	{
		private XmlDocument m_doc;

		private XAmpleTrace m_xampleTrace = new XAmpleTrace();

		/// <summary>
		/// Location of simple test FXT files
		/// </summary>
		protected string m_sTestPath;

		[TestFixtureSetUp]
		public void FixtureInit()
		{
			m_sTestPath = Path.Combine(DirectoryFinder.FwSourceDirectory,
				"LexText/ParserUI/ParserUITests");
			string sFailureDocPath = Path.Combine(m_sTestPath, "Failures.xml");
			m_doc = new XmlDocument();
			m_doc.Load(sFailureDocPath);
		}


		[TestFixtureTearDown]
		public void FixtureCleanUp()
		{
		}

		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		public void ConvertANCCFailureStrings()
		{
			XmlNodeList nl = m_doc.SelectNodes("//failure[contains(@test,'ANCC_FT')]");
			Assert.IsTrue(nl.Count == 2, "Two ANCC failures");
			m_xampleTrace.ConvertANCCFailures(m_doc, true);
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
			m_xampleTrace.ConvertMCCFailures(m_doc, true);
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
			m_xampleTrace.ConvertSECFailures(m_doc, true);
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
			m_xampleTrace.ConvertInfixEnvironmentFailures(m_doc, true);
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
