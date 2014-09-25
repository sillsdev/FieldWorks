// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: XAmpleParserTests.cs
// Responsibility:

using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// Summary description for XAmpleParserTests.
	/// </summary>
	[TestFixture]
	public class XAmpleParserTests : BaseTest
	{
		[Test]
		public void ConvertFailures()
		{
			string testPath = Path.Combine(FwDirectoryFinder.SourceDirectory, "LexText", "ParserCore", "ParserCoreTests", "Failures.xml");
			XDocument doc = XDocument.Load(testPath);

			XElement[] failures = doc.Descendants("failure").ToArray();
			XElement[] anccFailures = failures.Where(e => ((string) e.Attribute("test")).StartsWith("ANCC_FT")).ToArray();
			Assert.That(anccFailures.Length, Is.EqualTo(2), "Two ANCC failures");
			XElement[] mccFailures = failures.Where(e => ((string) e.Attribute("test")).StartsWith("MCC_FT")).ToArray();
			Assert.That(mccFailures.Length, Is.EqualTo(2), "Two MCC failures");
			XElement[] secFailures = failures.Where(e => ((string) e.Attribute("test")).StartsWith("SEC_ST") && ((string) e.Attribute("test")).Contains("[")).ToArray();
			Assert.That(secFailures.Length, Is.EqualTo(8), "Eight SEC failures with classes");
			XElement[] infixFailures = failures.Where(e => ((string) e.Attribute("test")).StartsWith("InfixEnvironment") && ((string) e.Attribute("test")).Contains("[")).ToArray();
			Assert.That(infixFailures.Length, Is.EqualTo(8), "Eight Infix Environment failures with classes");

			XAmpleParser.ConvertFailures(doc, (classID, hvo) =>
			{
				string className = null;
				switch (classID)
				{
					case MoFormTags.kClassId:
						className = "Form";
						break;
					case MoMorphSynAnalysisTags.kClassId:
						className = "MSA";
						break;
					case PhNaturalClassTags.kClassId:
						className = "NC";
						break;
				}
				return string.Format("{0}-{1}", className, hvo);
			});

			AssertTestEquals(anccFailures[0], "ANCC_FT:  Form-6213   -/ _ ... Form-6279");
			AssertTestEquals(anccFailures[1], "ANCC_FT:  Form-6213   -/ Form-6279 ... _ ");

			AssertTestEquals(mccFailures[0], "MCC_FT:  MSA-6331   +/ ~_ MSA-6139");
			AssertTestEquals(mccFailures[1], "MCC_FT:  MSA-6331   +/ MSA-6139 ~_");

			AssertTestEquals(secFailures[0], "SEC_ST: dok __ ra   / _ [NC-7405]");
			AssertTestEquals(secFailures[1], "SEC_ST: dok __ ra   / _ [NC-7405][NC-7405]");
			AssertTestEquals(secFailures[2], "SEC_ST: migel __ ximura   / [NC-7405] _");
			AssertTestEquals(secFailures[3], "SEC_ST: migel __ ximura   / [NC-7405][NC-7405] _");
			AssertTestEquals(secFailures[4], "SEC_ST: migel __ ximura   / [NC-7405] _ [NC-7405]");
			AssertTestEquals(secFailures[5], "SEC_ST: migel __ ximura   / [NC-7405][NC-7405] _ [NC-7405]");
			AssertTestEquals(secFailures[6], "SEC_ST: migel __ ximura   / [NC-7405] _ [NC-7405][NC-7405]");
			AssertTestEquals(secFailures[7], "SEC_ST: migel __ ximura   / [NC-7405][NC-7405] _ [NC-7405][NC-7405]");

			AssertTestEquals(infixFailures[0], "InfixEnvironment: dok __ ra   / _ [NC-7405]");
			AssertTestEquals(infixFailures[1], "InfixEnvironment: dok __ ra   / _ [NC-7405][NC-7405]");
			AssertTestEquals(infixFailures[2], "InfixEnvironment: migel __ ximura   / [NC-7405] _");
			AssertTestEquals(infixFailures[3], "InfixEnvironment: migel __ ximura   / [NC-7405][NC-7405] _");
			AssertTestEquals(infixFailures[4], "InfixEnvironment: migel __ ximura   / [NC-7405] _ [NC-7405]");
			AssertTestEquals(infixFailures[5], "InfixEnvironment: migel __ ximura   / [NC-7405][NC-7405] _ [NC-7405]");
			AssertTestEquals(infixFailures[6], "InfixEnvironment: migel __ ximura   / [NC-7405] _ [NC-7405][NC-7405]");
			AssertTestEquals(infixFailures[7], "InfixEnvironment: migel __ ximura   / [NC-7405][NC-7405] _ [NC-7405][NC-7405]");
		}

		private void AssertTestEquals(XElement elem, string expected)
		{
			var test = (string) elem.Attribute("test");
			Assert.That(test, Is.EqualTo(expected));
		}

		[Test]
		public void ConvertNameToUseAnsiCharactersTest()
		{
			// plain, simple ASCII
			string name = "abc 123";
			string convertedName = XAmpleParser.ConvertNameToUseAnsiCharacters(name);
			Assert.AreEqual("abc 123", convertedName);
			// Using upper ANSI characters as well as ASCII
			name = "ÿýúadctl";
			convertedName = XAmpleParser.ConvertNameToUseAnsiCharacters(name);
			Assert.AreEqual("ÿýúadctl", convertedName);
			// Using characters just above ANSI as well as ASCII
			name = "ąćălex";
			convertedName = XAmpleParser.ConvertNameToUseAnsiCharacters(name);
			Assert.AreEqual("010501070103lex", convertedName);
			// Using Cyrillic characters as well as ASCII
			name = "Английский для семинараgram";
			convertedName = XAmpleParser.ConvertNameToUseAnsiCharacters(name);
			Assert.AreEqual("0410043D0433043B043804390441043A04380439 0434043B044F 04410435043C0438043D043004400430gram", convertedName);
		}
	}
}
