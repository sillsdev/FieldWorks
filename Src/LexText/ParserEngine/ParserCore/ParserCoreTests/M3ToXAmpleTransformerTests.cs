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
//
// <remarks>
// Implements the M3ToXAmpleTransformerTests unit tests.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;
using NUnit.Framework;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// Summary description for M3ToXAmpleTransformerTests.
	/// </summary>
	[TestFixture]
	public class M3ToXAmpleTransformerTests : BaseTest
	{
		string m_sM3FXTDump;
		string m_sM3FXTCircumfixDump;
		string m_sM3FXTCircumfixInfixDump;
		string m_sM3FXTCliticDump;
		string m_sM3FXTFullRedupDump;
		string m_sM3FXTConceptualIntroDump;
		string m_sM3FXTStemNameDump;
		string m_sM3FXTStemName2Dump;
		string m_sM3FXTStemName3Dump;
		string m_sM3FXTRootCliticEnvsDump;
		string m_sM3FXTCliticEnvsDump;
		string m_sM3FXTAffixAlloFeatsDump;
		string m_sM3FXTLatinDump;
		Dictionary<string, XPathDocument> m_mapXmlDocs = new Dictionary<string, XPathDocument>();
#if __MonoCS__
		IntPtr m_adTransform;
		IntPtr m_lexTransform;  // public so utility tool (like a results updater) can access it
		IntPtr m_gramTransform;
#else
		XslCompiledTransform m_adTransform;
		XslCompiledTransform m_lexTransform;  // public so utility tool (like a results updater) can access it
		XslCompiledTransform m_gramTransform;
#endif

		/// <summary>
		/// Location of test files
		/// </summary>
		protected string m_sTestPath;
		protected string m_sTransformPath;
		protected string m_sADTransform;
		protected string m_sLexTransform;
		protected string m_sGramTransform;

		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			m_sTestPath = Path.Combine(DirectoryFinder.FwSourceDirectory,
				Path.Combine("LexText",
				Path.Combine("ParserEngine",
				Path.Combine("ParserCore",
				Path.Combine("ParserCoreTests", "M3ToXAmpleTransformerTestsDataFiles")))));
			m_sTransformPath = Path.Combine(DirectoryFinder.FlexFolder, "Transforms");

			SetUpXAmpleTransforms();
			SetUpM3FXTDump();
		}

		private void SetUpM3FXTDump()
		{
			m_sM3FXTDump = Path.Combine(m_sTestPath, "M3FXTDump.xml");
			m_sM3FXTCircumfixDump = Path.Combine(m_sTestPath, "M3FXTCircumfixDump.xml");
			m_sM3FXTCircumfixInfixDump = Path.Combine(m_sTestPath, "M3FXTCircumfixInfixDump.xml");
			m_sM3FXTCliticDump = Path.Combine(m_sTestPath, "CliticParserFxtResult.xml");
			m_sM3FXTFullRedupDump = Path.Combine(m_sTestPath, "M3FXTFullRedupDump.xml");
			m_sM3FXTConceptualIntroDump = Path.Combine(m_sTestPath, "ConceptualIntroTestParserFxtResult.xml");
			m_sM3FXTStemNameDump = Path.Combine(m_sTestPath, "M3FXTStemNameDump.xml");
			m_sM3FXTStemName2Dump = Path.Combine(m_sTestPath, "OrizabaParserFxtResult.xml");
			m_sM3FXTStemName3Dump = Path.Combine(m_sTestPath, "StemName3ParserFxtResult.xml");
			m_sM3FXTRootCliticEnvsDump = Path.Combine(m_sTestPath, "RootCliticEnvParserFxtResult.xml");
			m_sM3FXTCliticEnvsDump = Path.Combine(m_sTestPath, "CliticEnvsParserFxtResult.xml");
			m_sM3FXTAffixAlloFeatsDump = Path.Combine(m_sTestPath, "TestAffixAllomorphFeatsParserFxtResult.xml");
			m_sM3FXTLatinDump = Path.Combine(m_sTestPath, "LatinParserFxtResult.xml");
#if !__MonoCS__
			SetupXmlDocument(m_sM3FXTDump);
			SetupXmlDocument(m_sM3FXTCircumfixDump);
			SetupXmlDocument(m_sM3FXTCircumfixInfixDump);
			SetupXmlDocument(m_sM3FXTCliticDump);
			SetupXmlDocument(m_sM3FXTFullRedupDump);
			SetupXmlDocument(m_sM3FXTConceptualIntroDump);
			SetupXmlDocument(m_sM3FXTStemNameDump);
			SetupXmlDocument(m_sM3FXTStemName2Dump);
			SetupXmlDocument(m_sM3FXTStemName3Dump);
			SetupXmlDocument(m_sM3FXTRootCliticEnvsDump);
			SetupXmlDocument(m_sM3FXTCliticEnvsDump);
			SetupXmlDocument(m_sM3FXTAffixAlloFeatsDump);
			SetupXmlDocument(m_sM3FXTLatinDump);
#endif
		}

		private void SetupXmlDocument(string filepath)
		{
			XPathDocument xdoc = new XPathDocument(filepath);
			m_mapXmlDocs.Add(filepath, xdoc);
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
		private void SetUpTransform(ref IntPtr transform, string sName)
		{
			string sTransformPath = Path.Combine(m_sTransformPath, sName);
			transform = SIL.Utils.LibXslt.CompileTransform(sTransformPath);
		}

		/// <summary>
		/// Test creating the AD Control file
		/// </summary>
		[Test]
		public void CreateXAmpleADControlFile()
		{
			ApplyTransform(m_sM3FXTDump, m_adTransform, "TestAdCtl.txt");
			ApplyTransform(m_sM3FXTStemNameDump, m_adTransform, "StemNameTestAdCtl.txt");
			ApplyTransform(m_sM3FXTStemName2Dump, m_adTransform, "Orizabaadctl.txt");
			ApplyTransform(m_sM3FXTStemName3Dump, m_adTransform, "StemName3adctl.txt");
			ApplyTransform(m_sM3FXTCliticDump, m_adTransform, "CliticAdCtl.txt");
			ApplyTransform(m_sM3FXTAffixAlloFeatsDump, m_adTransform, "AffixAlloFeatsAdCtl.txt");
			ApplyTransform(m_sM3FXTLatinDump, m_adTransform, "LatinAdCtl.txt");
		}
		/// <summary>
		/// Test creating the lexicon file
		/// </summary>
		[Test]
		public void CreateXAmpleLexiconFile()
		{
			ApplyTransform(m_sM3FXTDump, m_lexTransform, "TestLexicon.txt");
#if OnHoldUntilGetDBFixed
			ApplyTransform(m_sM3FXTCircumfixDump, m_lexTransform, "IndonCircumfixLexicon.txt");
#endif
			ApplyTransform(m_sM3FXTCircumfixInfixDump, m_lexTransform, "CircumfixInfixLexicon.txt");
			ApplyTransform(m_sM3FXTFullRedupDump, m_lexTransform, "FullRedupLexicon.txt");
			ApplyTransform(m_sM3FXTConceptualIntroDump, m_lexTransform, "ConceptualIntroTestlex.txt");
			ApplyTransform(m_sM3FXTStemNameDump, m_lexTransform, "StemNameTestlex.txt");
			ApplyTransform(m_sM3FXTStemName2Dump, m_lexTransform, "Orizabalex.txt");
			ApplyTransform(m_sM3FXTStemName3Dump, m_lexTransform, "StemName3lex.txt");
			ApplyTransform(m_sM3FXTCliticDump, m_lexTransform, "CliticLexicon.txt");
			ApplyTransform(m_sM3FXTCliticEnvsDump, m_lexTransform, "CliticEnvsLexicon.txt");
			ApplyTransform(m_sM3FXTRootCliticEnvsDump, m_lexTransform, "RootCliticEnvsLexicon.txt");
			ApplyTransform(m_sM3FXTAffixAlloFeatsDump, m_lexTransform, "AffixAlloFeatsLexicon.txt");
		}
		/// <summary>
		/// Test creating the word grammar file
		/// </summary>
		[Test]
		public void CreateXAmpleWordGrammarFile()
		{
			ApplyTransform(m_sM3FXTDump, m_gramTransform, "TestWordGrammar.txt");
			ApplyTransform(m_sM3FXTCircumfixDump, m_gramTransform, "IndonCircumfixWordGrammar.txt");
			ApplyTransform(m_sM3FXTCircumfixInfixDump, m_gramTransform, "CircumfixInfixWordGrammar.txt");
			ApplyTransform(m_sM3FXTStemNameDump, m_gramTransform, "StemNameWordGrammar.txt");
			ApplyTransform(m_sM3FXTStemName2Dump, m_gramTransform, "Orizabagram.txt");
			ApplyTransform(m_sM3FXTStemName3Dump, m_gramTransform, "StemName3gram.txt");
			ApplyTransform(m_sM3FXTCliticDump, m_gramTransform, "CliticWordGrammar.txt");
			ApplyTransform(m_sM3FXTAffixAlloFeatsDump, m_gramTransform, "AffixAlloFeatsWordGrammar.txt");
			ApplyTransform(m_sM3FXTLatinDump, m_gramTransform, "LatinWordGrammar.txt");
		}
		private void ApplyTransform(string sInput, XslCompiledTransform transform, string sExpectedOutput)
		{
			XPathDocument fxtDump = m_mapXmlDocs[sInput];
			string sOutput = FileUtils.GetTempFile("txt");
			using (StreamWriter result = new StreamWriter(sOutput))
			{
				transform.Transform(fxtDump, null, result);
				result.Close();
			}
			string sExpectedResult = Path.Combine(m_sTestPath, sExpectedOutput);
			CheckOutputEquals(sExpectedResult, sOutput);
			// by deleting it here instead of a finally block, when it fails, we can see what the result is.
			File.Delete(sOutput);
		}
		private void ApplyTransform(string sInput, IntPtr transform, string sExpectedOutput)
		{
			string sOutput = FileUtils.GetTempFile("txt");
			SIL.Utils.LibXslt.TransformFileToFile(transform, sInput, sOutput);
			string sExpectedResult = Path.Combine(m_sTestPath, sExpectedOutput);
			CheckOutputEquals(sExpectedResult, sOutput);
			// by deleting it here instead of a finally block, when it fails, we can see what the result is.
			File.Delete(sOutput);
		}

		private void CheckOutputEquals(string sExpectedResultFile, string sActualResultFile)
		{
			string sExpected, sActual;
			using (StreamReader expected = new StreamReader(sExpectedResultFile))
				sExpected = expected.ReadToEnd();
			using (StreamReader actual = new StreamReader(sActualResultFile))
				sActual = actual.ReadToEnd();
			StringBuilder sb = new StringBuilder();
			sb.Append("Expected file was ");
			sb.AppendLine(sExpectedResultFile);
			sb.Append("Actual file was ");
			sb.AppendLine(sActualResultFile);
			Assert.AreEqual(sExpected, sActual, sb.ToString());
		}
	}
}
