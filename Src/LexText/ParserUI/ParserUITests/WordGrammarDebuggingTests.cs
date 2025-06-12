// Copyright (c) 2003-2024 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;

using NUnit.Framework;

using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Utils;
using SIL.Utils;

namespace SIL.FieldWorks.LexText.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for WordGrammarDebuggingTests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class WordGrammarDebuggingTests
	{
		private XPathDocument m_doc;

		private XslCompiledTransform m_masterTransform;
		private XslCompiledTransform m_resultTransform;
		private XslCompiledTransform m_resultTransformNoCompoundRules;
		private XslCompiledTransform m_resultTransformStemNames;
		private XslCompiledTransform m_resultTransformAffixAlloFeats;
		private XslCompiledTransform m_UnificationViaXsltTransform;
		private XslCompiledTransform m_SameSlotTwiceTransform;
		private XslCompiledTransform m_RequiredOptionalPrefixSlotsTransform;

		/// <summary>
		/// Location of test files
		/// </summary>
		protected string m_sTestPath;
		/// <summary></summary>
		protected string m_sResultTransform;
		/// <summary></summary>
		protected string m_sResultTransformNoCompoundRules;
		/// <summary></summary>
		protected string m_sResultTransformStemNames;
		/// <summary></summary>
		protected string m_sResultTransformAffixAlloFeats;
		/// <summary></summary>
		protected string m_sRequiredOptionalPrefixSlotsTransform;
		/// <summary></summary>
		protected string m_sM3FXTDump;
		/// <summary></summary>
		protected string m_sM3FXTDumpNoCompoundRules;
		/// <summary></summary>
		protected string m_sM3FXTDumpStemNames;
		/// <summary></summary>
		protected string m_sM3FXTDumpAffixAlloFeats;
		/// <summary>Set to true to be able to debug into stylesheets</summary>
		private readonly bool m_fDebug;
		/// <summary>path to the standard directory for temporary files.</summary>
		private readonly string m_sTempPath;
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:WordGrammarDebuggingTests"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public WordGrammarDebuggingTests()
		{
#if DEBUG
			// We set this to true so that we can debug into XSLT.
			m_fDebug = true;
#else
			m_fDebug = false;
#endif
			m_sTempPath = Path.GetTempPath();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fixtures setup method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[OneTimeSetUp]
		public void FixtureSetup()
		{
			m_sTestPath = Path.Combine(FwDirectoryFinder.SourceDirectory,
				"LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults");

			SetUpMasterTransform();
			CreateResultTransform("M3FXTDump.xml", out m_sResultTransform);
			SetUpResultTransform(m_sResultTransform, out m_resultTransform);
			SetUpUnificationViaXsltTransform();
			SetUpSameSlotTwiceTransform();
			CreateResultTransform("M3FXTDumpNoCompoundRules.xml", out m_sResultTransformNoCompoundRules);
			SetUpResultTransform(m_sResultTransformNoCompoundRules, out m_resultTransformNoCompoundRules);
			CreateResultTransform("M3FXTDumpStemNames.xml", out m_sResultTransformStemNames);
			SetUpResultTransform(m_sResultTransformStemNames, out m_resultTransformStemNames);
			CreateResultTransform("M3FXTDumpAffixAlloFeats.xml", out m_sResultTransformAffixAlloFeats);
			SetUpResultTransform(m_sResultTransformAffixAlloFeats, out m_resultTransformAffixAlloFeats);
			SetUpRequiredOptionalPrefixSlotsTransform();
			CreateResultTransform("M3FXTRequiredOptionalPrefixSlots.xml", out m_sRequiredOptionalPrefixSlotsTransform);
			SetUpResultTransform(m_sRequiredOptionalPrefixSlotsTransform, out m_RequiredOptionalPrefixSlotsTransform);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete any files that we may have created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[OneTimeTearDown]
		public void FixtureTeardown()
		{
			if (File.Exists(m_sResultTransform))
				File.Delete(m_sResultTransform);
			if (File.Exists(m_sResultTransformNoCompoundRules))
				File.Delete(m_sResultTransformNoCompoundRules);
			if (File.Exists(m_sResultTransformStemNames))
				File.Delete(m_sResultTransformStemNames);
			if (File.Exists(m_sResultTransformAffixAlloFeats))
				File.Delete(m_sResultTransformAffixAlloFeats);
			if (File.Exists(Path.Combine(m_sTempPath, "UnifyTwoFeatureStructures.xsl")))
				File.Delete(Path.Combine(m_sTempPath, "UnifyTwoFeatureStructures.xsl"));
			if (File.Exists(Path.Combine(m_sTempPath, "TestUnificationViaXSLT-Linux.xsl")))
				File.Delete(Path.Combine(m_sTempPath, "TestUnificationViaXSLT-Linux.xsl"));
			if (File.Exists(m_sRequiredOptionalPrefixSlotsTransform))
				File.Delete(m_sRequiredOptionalPrefixSlotsTransform);
		}

		#region Helper methods for setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up the result transform.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetUpResultTransform(string sResultTransform, out XslCompiledTransform resultTransform)
		{
			resultTransform = new XslCompiledTransform(m_fDebug);
			resultTransform.Load(sResultTransform);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up the unification via XSLT transform.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetUpUnificationViaXsltTransform()
		{
			m_UnificationViaXsltTransform = new XslCompiledTransform(m_fDebug);
			string sUnificationViaXsltTransform = Path.Combine(m_sTestPath,
				"../TestUnificationViaXSLT.xsl");
			m_UnificationViaXsltTransform.Load(sUnificationViaXsltTransform);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up the same slot twice XSLT transform.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetUpSameSlotTwiceTransform()
		{
			string sSameSlotTwiceTransform = Path.Combine(m_sTestPath,
				@"TLPSameSlotTwiceWordGrammarDebugger.xsl");
			m_SameSlotTwiceTransform = new XslCompiledTransform(m_fDebug);
			m_SameSlotTwiceTransform.Load(sSameSlotTwiceTransform);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up the Required Optional Prefix Slots XSLT transform.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetUpRequiredOptionalPrefixSlotsTransform()
		{
			string sRequiredOptionalPrefixSlotsTransform = Path.Combine(m_sTestPath,
				@"RequiredOptionalPrefixSlotsWordGrammarDebugger.xsl");
			m_RequiredOptionalPrefixSlotsTransform = new XslCompiledTransform(m_fDebug);
			m_RequiredOptionalPrefixSlotsTransform.Load(sRequiredOptionalPrefixSlotsTransform);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a result transform.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CreateResultTransform(string fxtDumpFile, out string resultTransform)
		{
			resultTransform = FileUtils.GetTempFile("xsl");
			string fxtDump = Path.Combine(m_sTestPath, fxtDumpFile);
			using (var writer = new StreamWriter(resultTransform))
				m_masterTransform.Transform(fxtDump, null, writer);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up the master transform.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetUpMasterTransform()
		{
			m_masterTransform = XmlUtils.CreateTransform("FxtM3ParserToXAmpleWordGrammarDebuggingXSLT", "ApplicationTransforms");
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Applies the transform.
		/// </summary>
		/// <param name="sInputFile">The input file filename.</param>
		/// <param name="sExpectedOutput">The expected output filename.</param>
		/// ------------------------------------------------------------------------------------
		private void ApplyTransform(string sInputFile, string sExpectedOutput)
		{
			ApplyTransform(sInputFile, sExpectedOutput, m_resultTransform);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Applies the transform.
		/// </summary>
		/// <param name="sInputFile">The input file filename.</param>
		/// <param name="sExpectedOutput">The expected output filename.</param>
		/// <param name="transform">The transform.</param>
		/// ------------------------------------------------------------------------------------
		private void ApplyTransform(string sInputFile, string sExpectedOutput, XslCompiledTransform transform)
		{
			string sInput = Path.Combine(m_sTestPath, sInputFile);
			m_doc = new XPathDocument(sInput);
			string sOutput = FileUtils.GetTempFile("xml");
			using (var result = new StreamWriter(sOutput))
			{
				transform.Transform(m_doc, null, result);
				result.Close();
				string sExpectedResult = Path.Combine(m_sTestPath, sExpectedOutput);
				CheckXmlEquals(sExpectedResult, sOutput);
				// by deleting it here instead of a finally block, when it fails, we can see what the result is.
				File.Delete(sOutput);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verfifies that the result XML file is identical to the expected XML file.
		/// </summary>
		/// <param name="sExpectedResultFile">The expected result filename.</param>
		/// <param name="sActualResultFile">The actual result filename.</param>
		/// ------------------------------------------------------------------------------------
		private void CheckXmlEquals(string sExpectedResultFile, string sActualResultFile)
		{
			string sExpected, sActual;
			using (var expected = new StreamReader(sExpectedResultFile))
				sExpected = expected.ReadToEnd();
			using (var actual = new StreamReader(sActualResultFile))
				sActual = actual.ReadToEnd();
			var sb = new StringBuilder();
			sb.Append("Expected file was ");
			sb.AppendLine(sExpectedResultFile);
			sb.Append("Actual file was ");
			sb.AppendLine(sActualResultFile);
			XElement xeActual = XElement.Parse(sActual, LoadOptions.None);
			XElement xeExpected = XElement.Parse(sExpected, LoadOptions.None);
			bool ok = XmlHelper.EqualXml(xeExpected, xeActual, sb);
			Assert.IsTrue(ok, sb.ToString());
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// "Stem = root" production
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void StemEqualsRoot()
		{
			// Single root
			ApplyTransform("ximuStep00.xml", "ximuStep01.xml");
			ApplyTransform("ximukesraStep00.xml", "ximukesraStep01.xml");
			ApplyTransform("nixobiliraStep00.xml", "nixobiliraStep01.xml");
			ApplyTransform("nihimbiliguStep00.xml", "nihimbiliguStep01.xml");
			ApplyTransform("nihimbilitikoStep00.xml", "nihimbilitikoStep01.xml");
			ApplyTransform("niyuxobilitikoStep00.xml", "niyuxobilitikoStep01.xml");
			ApplyTransform("mixonipustikespeStep00.xml", "mixonipustikespeStep01.xml");
			ApplyTransform("dembilikesguStep00.xml", "dembilikesguStep01.xml");
			ApplyTransform("bilikesziStep00.xml", "bilikesziStep01.xml");
			ApplyTransform("keyaloStep00.xml", "keyaloStep01.xml");
			ApplyTransform("yalozoStep00.xml", "yalozoStep01.xml");
			ApplyTransform("kesuyalolozoStep00.xml", "kesuyalolozoStep01.xml");
			ApplyTransform("nihimbiliraBadInflectionClassInTenseStep00.xml",
				"nihimbiliraBadInflectionClassInTenseStep01.xml");
			ApplyTransform("nihimbiliraBadInflectionClassesInTenseStep00.xml",
				"nihimbiliraBadInflectionClassesInTenseStep01.xml");
			// Compound roots
				// Left-headed
			ApplyTransform("nihinlikximuraStep00.xml", "nihinlikximuraStep01.xml");
			ApplyTransform("nihinlikximuraStep01.xml", "nihinlikximuraStep02.xml");
			// Right-headed
			ApplyTransform("niyalonadkoStep00.xml", "niyalonadkoStep01.xml");
			ApplyTransform("niyalonadkoStep01.xml", "niyalonadkoStep02.xml");
			// Exocentric
			ApplyTransform("niyaloximuraStep00.xml", "niyaloximuraStep01.xml");
			ApplyTransform("niyaloximuraStep01.xml", "niyaloximuraStep02.xml");
			// Inflectional templates
			ApplyTransform("biliStep00BadInflection.xml", "biliStep01BadInflection.xml");
			// required prefix slot, optional prefix slot, stem, optional suffix slots
			// but no prefix is in the form
			ApplyTransform("manahomiaStep00.xml", "manahomiaStep01.xml", m_RequiredOptionalPrefixSlotsTransform);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// "Stem_1 = derivPfx Stem_2" production
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void StemEqualsDerivPfxStem()
		{
			// nixobilira (one derivational prefix)
			ApplyTransform("nixobiliraStep01.xml", "nixobiliraStep02.xml");
			ApplyTransform("nixobiliraStep01BadFromCat.xml", "nixobiliraStep02BadFromCat.xml");
			ApplyTransform("nixobiliraStep01BadFromInflClass.xml",
				"nixobiliraStep02BadFromInflClass.xml");
			//ApplyTransform("nixobiliraStep01BadEnvCat.xml", "nixobiliraStep02BadEnvCat.xml");
			ApplyTransform("nixobiliraStep01BadFromCatBadFromInflClass.xml",
				"nixobiliraStep02BadFromCatBadFromInflClass.xml");
			// niyuxobilitiko (one derivational prefix, one derivational suffix)
			// N.B. the stem = stem derivSfx applies first, then the derivational prefix
			ApplyTransform("niyuxobilitikoStep02.xml", "niyuxobilitikoStep03.xml");
			ApplyTransform("niyuxobilitikoStep02aBadFromCat.xml",
				"niyuxobilitikoStep03aBadFromCat.xml");
			ApplyTransform("niyuxobilitikoStep02aBadFromInflClass.xml",
				"niyuxobilitikoStep03aBadFromInflClass.xml");
			ApplyTransform("niyuxobilitikoStep02aBadFromCatBadFromInflClass.xml",
				"niyuxobilitikoStep03aBadFromCatBadFromInflClass.xml");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// "Stem_1 = Stem_2 derivSfx" production
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void StemEqualsStemDerivSfx()
		{
			// nihimbilitiko (one derivational suffix)
			ApplyTransform("nihimbilitikoStep01.xml", "nihimbilitikoStep02.xml");
			ApplyTransform("nihimbilitikoStep01BadFromCat.xml",
				"nihimbilitikoStep02BadFromCat.xml");
			ApplyTransform("nihimbilitikoStep01BadFromInflClass.xml",
				"nihimbilitikoStep02BadFromInflClass.xml");
			ApplyTransform("nihimbilitikoStep01BadFromCatBadFromInflClass.xml",
				"nihimbilitikoStep02BadFromCatBadFromInflClass.xml");
			// niyuxobilitiko (one derivational prefix, one derivational suffix)
			// N.B. the stem = stem derivSfx applies first
			ApplyTransform("niyuxobilitikoStep01.xml", "niyuxobilitikoStep02.xml");
			ApplyTransform("niyuxobilitikoStep01BadFromCat.xml",
				"niyuxobilitikoStep02BadFromCat.xml");
			ApplyTransform("niyuxobilitikoStep01BadFromInflClass.xml",
				"niyuxobilitikoStep02BadFromInflClass.xml");
			ApplyTransform("niyuxobilitikoStep01BadFromCatBadFromInflClass.xml",
				"niyuxobilitikoStep02BadFromCatBadFromInflClass.xml");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// "Stem_1 = Stem_2 Stem_3" productions
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void StemEqualsStemStem()
		{
			// Left-headed
			ApplyTransform("nihinlikximuraStep02.xml", "nihinlikximuraStep03.xml");
			ApplyTransform("nihinlikximuraStep02BadCats.xml", "nihinlikximuraStep03BadCats.xml");
			// Right-headed
			ApplyTransform("niyalonadkoStep02.xml", "niyalonadkoStep03.xml");
			ApplyTransform("niyalonadkoStep02BadCats.xml", "niyalonadkoStep03BadCats.xml");
			// Exocentric
			ApplyTransform("niyaloximuraStep02.xml", "niyaloximuraStep03.xml");
			ApplyTransform("niyaloximuraStep02BadCats.xml", "niyaloximuraStep03BadCats.xml");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// "Full = Stem" production
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FullEqualsStem()
		{
			ApplyTransform("ximuStep01.xml", "ximuStep02.xml");
			ApplyTransform("ximukesraStep01.xml", "ximukesraStep02.xml");
			ApplyTransform("nihimbiliguStep01.xml", "nihimbiliguStep02.xml");
			ApplyTransform("nihimbiliguStep01BadInflectionClass.xml",
				"nihimbiliguStep02BadInflectionClass.xml");
			ApplyTransform("biliStep01BadInflection.xml", "biliStep02BadInflection.xml");
			ApplyTransform("mixonipustikespeStep01.xml", "mixonipustikespeStep02.xml");
			ApplyTransform("dembilikesguStep01.xml", "dembilikesguStep02.xml");
			ApplyTransform("bilikesziStep01.xml", "bilikesziStep02.xml");
			ApplyTransform("keyaloStep01.xml", "keyaloStep02.xml");
			ApplyTransform("yalozoStep01.xml", "yalozoStep02.xml");
			ApplyTransform("kesuyalolozoStep01.xml", "kesuyalolozoStep02.xml");
			ApplyTransform("nihimbiliraBadInflectionClassInTenseStep01.xml",
				"nihimbiliraBadInflectionClassInTenseStep02.xml");
			ApplyTransform("nihimbiliraBadInflectionClassesInTenseStep01.xml",
				"nihimbiliraBadInflectionClassesInTenseStep02.xml");
			ApplyTransform("yalotetuStep01.xml", "yalotetuStep02.xml", m_SameSlotTwiceTransform);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// "Partial = Prefixes* Full Suffixes*" production
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PartialEqualsFull()
		{
			ApplyTransform("ximukesraStep02.xml", "ximukesraStep03.xml");
			ApplyTransform("mixonipustikespeStep02.xml", "mixonipustikespeStep03.xml");
			ApplyTransform("dembilikesguStep02.xml", "dembilikesguStep03.xml");
			ApplyTransform("bilikesziStep02.xml", "bilikesziStep03.xml");
			ApplyTransform("kasakesPartialCatUnclassCatStep02.xml",
				"kasakesPartialCatUnclassCatStep03.xml");
			ApplyTransform("sedembilikesraStep02.xml", "sedembilikesraStep03.xml");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// "Partial = root" production (unmarked root)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PartialEqualsRoot()
		{
			ApplyTransform("katStep00.xml", "katStep01.xml");
			ApplyTransform("xokattikesraStep00.xml", "xokattikesraStep01.xml");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// "Partial = Prefs* Roos Sufs*" production (no compound rules)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PartialEqualsRootsNoCompoundRules()
		{
			ApplyTransform("nihinlikximuraNoCompoundRulesStep00.xml",
				"nihinlikximuraNoCompoundRulesStep01.xml", m_resultTransformNoCompoundRules);
			ApplyTransform("nihinxolikximukestiraNoCompoundRulesStep00.xml",
				"nihinxolikximukestiraNoCompoundRulesStep01.xml", m_resultTransformNoCompoundRules);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// "Partial = Prefs* Partial Sufs*" production (unmarked root)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PartialEqualsPartial()
		{
			ApplyTransform("xokattikesraStep01.xml", "xokattikesraStep02.xml");
			ApplyTransform("dembilikesguStep03.xml", "dembilikesguStep04.xml");
			ApplyTransform("bilikesziStep03.xml", "bilikesziStep04.xml");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// "Word = Full" production and other Full productions
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void WordEqualsFull()
		{
			ApplyTransform("nihimbiliguStep02.xml", "nihimbiliguStep03.xml");
			ApplyTransform("keyaloStep02.xml", "keyaloStep03.xml");
			ApplyTransform("yalozoStep02.xml", "yalozoStep03.xml");
			ApplyTransform("kesuyalolozoStep02.xml", "kesuyalolozoStep03.xml");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// "Word = Partial" production and other Full productions
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void WordEqualsPartial()
		{
			ApplyTransform("katStep01.xml", "katStep02.xml");
			ApplyTransform("bilikesziStep04.xml", "bilikesziStep05.xml");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// "OrthographicWord = Word" production
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OrthographicWordEqualsWord()
		{
			ApplyTransform("nihimbiliguStep03.xml", "nihimbiliguStep04.xml");
			ApplyTransform("katStep02.xml", "katStep03.xml");
			ApplyTransform("katStep03.xml", "katStep04.xml");
			ApplyTransform("bilikesziStep05.xml", "bilikesziStep06.xml");
			ApplyTransform("keyaloStep03.xml", "keyaloStep04.xml");
			ApplyTransform("yalozoStep03.xml", "yalozoStep04.xml");
			ApplyTransform("kesuyalolozoStep03.xml", "kesuyalolozoStep04.xml");
			ApplyTransform("ximuzoStep03.xml", "ximuzoStep04.xml");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove sequences with failures whenever apply again
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveFailuresWhenApplyAgain()
		{
			// niyuxobilitiko (one derivational prefix, one derivational suffix)
			// deriv prefix fails, so it gets removed
			ApplyTransform("niyuxobilitikoStep02.xml", "niyuxobilitikoStep03.xml");
			ApplyTransform("niyuxobilitikoStep02BadFromCat.xml", "niyuxobilitikoStep03BadFromCat.xml");
			// when everything is a failure, it goes to just <word></word>
			ApplyTransform("nihinlikximuraStep03BadCats.xml", "nihinlikximuraStep04BadCats.xml");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Affix allomorph conditioned by features-oriented tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AffixAllomorphConditionedByFeatures()
		{
			// an inflectional affix allomorph has features and parse succeeds
			DoWordGrammarDebuggerSteps("msmsAffixAlloFeats", 3);
			// an inflectional affix allomorph has features and parse fails
			DoWordGrammarDebuggerSteps("mpmsAffixAlloFeats", 2);
			// an inflectional affix allomorph without features is in an entry that has features and parse succeeds
			DoWordGrammarDebuggerSteps("fsinflAffixAlloFeats", 3);
			// an inflectional affix allomorph without features is in an entry that has features and parse fails
			DoWordGrammarDebuggerSteps("msinflAffixAlloFeats", 2);
			// a derivational affix allomorph has features and parse succeeds
			DoWordGrammarDebuggerSteps("fsfstovAffixAlloFeats", 3);
			// a derivational affix allomorph has features and parse fails
			DoWordGrammarDebuggerSteps("fpfstovAffixAlloFeats", 2);
			// a derivational affix allomorph without features is in an entry that has features and parse succeeds
			DoWordGrammarDebuggerSteps("fpfptovAffixAlloFeats", 3);
			// a derivational affix allomorph without features is in an entry that has features and parse fails
			DoWordGrammarDebuggerSteps("fsfptovAffixAlloFeats", 2);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Stem name-oriented tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void StemNames()
		{
			// stem name is set and parse succeeds
			DoWordGrammarDebuggerSteps("niyuyiywusaStemNameSet", 3);
			// stem name is set but no features in regions and parse succeeds
			DoWordGrammarDebuggerSteps("niyumamwupeStemNameSetNoFs", 3);
			// stem name is set and parse fails
			DoWordGrammarDebuggerSteps("niyuyiywupeStemNameFail", 3);
			// stem name not set (but other allomorph in lex entry is) and it succeeds
			DoWordGrammarDebuggerSteps("niyuwowwupeStemNameNotSet", 3);
			// stem name not set (but other allomorph in lex entry is) and it fails
			DoWordGrammarDebuggerSteps("niyuhohwusaStemNameNotSetFail", 3);
			// stem name not set (but two other allomorps in lex entry are) and it fails
			DoWordGrammarDebuggerSteps("niyuwowwukoStemNameNotSetMultiFail", 3);
			// stem name set and has two FsFeatStrucs and it succeeds
			DoWordGrammarDebuggerSteps("timikikwusaStemNameSetMultiFs", 3);
			// stem name set and has two FsFeatStrucs and it fails
			DoWordGrammarDebuggerSteps("timikikwupeStemNameSetMultiFsFail", 3);
			// stem name is set, has compound and parse succeeds
			DoWordGrammarDebuggerSteps("niyuyiyximuwusaStemNameSetCompound", 5);
			// stem name not set (but other allomorph in lex entry is), has compound and it fails
			DoWordGrammarDebuggerSteps("niyuyiyximuwupeStemNameNotSetCompoundFail", 5);
		}

		// These methods have a seeming bug in them: the transform argument is not used,
		// but rather a hard-coded transform.  However, the test output obviously comes
		// from applying the hard-coded transform, so fixing this requires knowing what
		// the proper output is for the given data and the proper (other) transform.
		private void DoWordGrammarDebuggerSteps(string sName, int count)
		{
			for (int i = 0; i < count; i++)
				ApplyTransform(sName + "Step0" + i.ToString(CultureInfo.InvariantCulture) + ".xml", sName + "Step0" + i.ToString(CultureInfo.InvariantCulture) + "Result.xml", m_resultTransformStemNames);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test unification process using XSLT
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UnificationViaXslt()
		{
			ApplyTransform("TestFeatureStructureUnification.xml",
				"TestFeatureStructureUnification.xml", m_UnificationViaXsltTransform);
		}
	}
}
