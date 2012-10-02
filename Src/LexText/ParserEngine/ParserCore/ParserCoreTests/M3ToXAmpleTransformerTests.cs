// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2005' company='SIL International'>
//		Copyright (c) 2003-2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: M3ToXAmpleTransformerTests.cs
// Responsibility: Andy Black
// Last reviewed:
//
// <remarks>
// Implements the M3ToXAmpleTransformerTests unit tests.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

using NUnit.Framework;

using SIL.FieldWorks.Common.Utils;

namespace ParserCoreTests
{
	/// <summary>
	/// Summary description for M3ToXAmpleTransformerTests.
	/// </summary>
	[TestFixture]
	public class M3ToXAmpleTransformerTests
	{
		XPathDocument m_docM3FXTDump;
		XPathDocument m_docM3FXTCircumfixDump;
		XPathDocument m_docM3FXTCircumfixInfixDump;
		XPathDocument m_docM3FXTCliticDump;
		XPathDocument m_docM3FXTFullRedupDump;
		XPathDocument m_docM3FXTConceptualIntroDump;
		XPathDocument m_docM3FXTStemNameDump;
		XPathDocument m_docM3FXTStemName2Dump;
		XPathDocument m_docM3FXTStemName3Dump;
		XPathDocument m_docM3FXTRootCliticEnvsDump;
		XPathDocument m_docM3FXTCliticEnvsDump;
		XPathDocument m_docM3FXTAffixAlloFeatsDump;
		XPathDocument m_docM3FXTLatinDump;
		XslCompiledTransform m_adTransform;
		XslCompiledTransform m_lexTransform;  // public so utility tool (like a results updater) can access it
		XslCompiledTransform m_gramTransform;

		/// <summary>
		/// Location of test files
		/// </summary>
		protected string m_sTestPath = Path.Combine(SIL.FieldWorks.Common.Utils.DirectoryFinder.FwSourceDirectory,
			@"LexText\ParserEngine\ParserCore\ParserCoreTests\M3ToXAmpleTransformerTestsDataFiles");
		protected string m_sTransformPath = Path.Combine(SIL.FieldWorks.Common.Utils.DirectoryFinder.FWCodeDirectory,
			@"Language Explorer\Transforms");
		protected string m_sADTransform;
		protected string m_sLexTransform;
		protected string m_sGramTransform;

		// Constructor is for utility program (like a results updater)
		public M3ToXAmpleTransformerTests()
		{
			SetUpXAmpleTransforms();
			SetUpM3FXTDump();
		}
		[TestFixtureSetUp]
		public void FixtureInit()
		{
			SetUpXAmpleTransforms();
			SetUpM3FXTDump();
		}

		private void SetUpM3FXTDump()
		{
			string sM3FXTDump = Path.Combine(m_sTestPath, "M3FXTDump.xml");
			m_docM3FXTDump = new XPathDocument(sM3FXTDump);
			sM3FXTDump = Path.Combine(m_sTestPath, "M3FXTCircumfixDump.xml");
			m_docM3FXTCircumfixDump = new XPathDocument(sM3FXTDump);
			sM3FXTDump = Path.Combine(m_sTestPath, "M3FXTCircumfixInfixDump.xml");
			m_docM3FXTCircumfixInfixDump = new XPathDocument(sM3FXTDump);
			sM3FXTDump = Path.Combine(m_sTestPath, "CliticParserFxtResult.xml");
			m_docM3FXTCliticDump = new XPathDocument(sM3FXTDump);
			sM3FXTDump = Path.Combine(m_sTestPath, "M3FXTFullRedupDump.xml");
			m_docM3FXTFullRedupDump = new XPathDocument(sM3FXTDump);
			sM3FXTDump = Path.Combine(m_sTestPath, "ConceptualIntroTestParserFxtResult.xml");
			m_docM3FXTConceptualIntroDump = new XPathDocument(sM3FXTDump);
			sM3FXTDump = Path.Combine(m_sTestPath, "M3FXTStemNameDump.xml");
			m_docM3FXTStemNameDump = new XPathDocument(sM3FXTDump);
			sM3FXTDump = Path.Combine(m_sTestPath, "OrizabaParserFxtResult.xml");
			m_docM3FXTStemName2Dump = new XPathDocument(sM3FXTDump);
			sM3FXTDump = Path.Combine(m_sTestPath, "StemName3ParserFxtResult.xml");
			m_docM3FXTStemName3Dump = new XPathDocument(sM3FXTDump);
			sM3FXTDump = Path.Combine(m_sTestPath, "RootCliticEnvParserFxtResult.xml");
			m_docM3FXTRootCliticEnvsDump = new XPathDocument(sM3FXTDump);
			sM3FXTDump = Path.Combine(m_sTestPath, "CliticEnvsParserFxtResult.xml");
			m_docM3FXTCliticEnvsDump = new XPathDocument(sM3FXTDump);
			sM3FXTDump = Path.Combine(m_sTestPath, "TestAffixAllomorphFeatsParserFxtResult.xml");
			m_docM3FXTAffixAlloFeatsDump = new XPathDocument(sM3FXTDump);
			sM3FXTDump = Path.Combine(m_sTestPath, "LatinParserFxtResult.xml");
			m_docM3FXTLatinDump = new XPathDocument(sM3FXTDump);
		}

		private void SetUpXAmpleTransforms()
		{
			SetUpTransform(ref m_adTransform, "FxtM3ParserToXAmpleADCtl.xsl");
			SetUpTransform(ref m_lexTransform, "FxtM3ParserToXAmpleLex.xsl");
			SetUpTransform(ref m_gramTransform, "FxtM3ParserToToXAmpleGrammar.xsl");
		}
		private void SetUpTransform(ref XslCompiledTransform transform, string sName)
		{
			string sTransformPath = Path.Combine(m_sTransformPath, sName);
			transform = new XslCompiledTransform();
			transform.Load(sTransformPath);
		}


		[TestFixtureTearDown]
		public void FixtureCleanUp()
		{
		}
		/// <summary>
		/// Test creating the AD Control file
		/// </summary>
		[Test]
		public void CreateXAmpleADControlFile()
		{
			ApplyTransform(m_docM3FXTDump, m_adTransform, "TestAdCtl.txt");
			ApplyTransform(m_docM3FXTStemNameDump, m_adTransform, "StemNameTestAdCtl.txt");
			ApplyTransform(m_docM3FXTStemName2Dump, m_adTransform, "Orizabaadctl.txt");
			ApplyTransform(m_docM3FXTStemName3Dump, m_adTransform, "StemName3adctl.txt");
			ApplyTransform(m_docM3FXTCliticDump, m_adTransform, "CliticAdCtl.txt");
			ApplyTransform(m_docM3FXTAffixAlloFeatsDump, m_adTransform, "AffixAlloFeatsAdCtl.txt");
			ApplyTransform(m_docM3FXTLatinDump, m_adTransform, "LatinAdCtl.txt");
		}
		/// <summary>
		/// Test creating the lexicon file
		/// </summary>
		[Test]
		public void CreateXAmpleLexiconFile()
		{
			ApplyTransform(m_docM3FXTDump, m_lexTransform, "TestLexicon.txt");
#if OnHoldUntilGetDBFixed
			ApplyTransform(m_docM3FXTCircumfixDump, m_lexTransform, "IndonCircumfixLexicon.txt");
#endif
			ApplyTransform(m_docM3FXTCircumfixInfixDump, m_lexTransform, "CircumfixInfixLexicon.txt");
			ApplyTransform(m_docM3FXTFullRedupDump, m_lexTransform, "FullRedupLexicon.txt");
			ApplyTransform(m_docM3FXTConceptualIntroDump, m_lexTransform, "ConceptualIntroTestlex.txt");
			ApplyTransform(m_docM3FXTStemNameDump, m_lexTransform, "StemNameTestlex.txt");
			ApplyTransform(m_docM3FXTStemName2Dump, m_lexTransform, "Orizabalex.txt");
			ApplyTransform(m_docM3FXTStemName3Dump, m_lexTransform, "StemName3lex.txt");
			ApplyTransform(m_docM3FXTCliticDump, m_lexTransform, "CliticLexicon.txt");
			ApplyTransform(m_docM3FXTCliticEnvsDump, m_lexTransform, "CliticEnvsLexicon.txt");
			ApplyTransform(m_docM3FXTRootCliticEnvsDump, m_lexTransform, "RootCliticEnvsLexicon.txt");
			ApplyTransform(m_docM3FXTAffixAlloFeatsDump, m_lexTransform, "AffixAlloFeatsLexicon.txt");
		}
		/// <summary>
		/// Test creating the word grammar file
		/// </summary>
		[Test]
		public void CreateXAmpleWordGrammarFile()
		{
			ApplyTransform(m_docM3FXTDump, m_gramTransform, "TestWordGrammar.txt");
			ApplyTransform(m_docM3FXTCircumfixDump, m_gramTransform, "IndonCircumfixWordGrammar.txt");
			ApplyTransform(m_docM3FXTCircumfixInfixDump, m_gramTransform, "CircumfixInfixWordGrammar.txt");
			ApplyTransform(m_docM3FXTStemNameDump, m_gramTransform, "StemNameWordGrammar.txt");
			ApplyTransform(m_docM3FXTStemName2Dump, m_gramTransform, "Orizabagram.txt");
			ApplyTransform(m_docM3FXTStemName3Dump, m_gramTransform, "StemName3gram.txt");
			ApplyTransform(m_docM3FXTCliticDump, m_gramTransform, "CliticWordGrammar.txt");
			ApplyTransform(m_docM3FXTAffixAlloFeatsDump, m_gramTransform, "AffixAlloFeatsWordGrammar.txt");
			ApplyTransform(m_docM3FXTLatinDump, m_gramTransform, "LatinWordGrammar.txt");
		}
		private void ApplyTransform(XPathDocument fxtDump, XslCompiledTransform transform, string sExpectedOutput)
		{
			string sOutput = CreateTempFile("txt");
			StreamWriter result = new StreamWriter(sOutput);
			transform.Transform(fxtDump, null, result);
			result.Close();
			string sExpectedResult = Path.Combine(m_sTestPath, sExpectedOutput);
			CheckXmlEquals(sExpectedResult, sOutput);
			File.Delete(sOutput);
		}
		private string CreateTempFile(string sExtension)
		{
			FwTempFile tmpFile = new FwTempFile(sExtension);
			string sInput = tmpFile.CloseAndGetPath();
			return sInput;
		}
		private void CheckXmlEquals(string sExpectedResultFile, string sActualResultFile)
		{
			StreamReader expected = new StreamReader(sExpectedResultFile);
			StreamReader actual = new StreamReader(sActualResultFile);
			string sExpected = expected.ReadToEnd();
			string sActual = actual.ReadToEnd();
			StringBuilder sb = new StringBuilder();
			sb.Append("Expected file was ");
			sb.Append(sExpectedResultFile);
			Assert.AreEqual(sExpected, sActual, sb.ToString());
			expected.Close();
			actual.Close();
		}

	}
}
