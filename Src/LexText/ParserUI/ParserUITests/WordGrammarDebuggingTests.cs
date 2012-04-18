// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, 2012, SIL International. All Rights Reserved.
// <copyright from='2003' to='2012' company='SIL International'>
//		Copyright (c) 2003, 2012, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: WordGrammarDebuggingTests.cs
// Responsibility:
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;

using NUnit.Framework;

using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;

namespace SIL.FieldWorks.LexText.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for WordGrammarDebuggingTests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithNativeFieldsShouldBeDisposableRule",
		Justification="Unit test - IntPtr get disposed in fixture teardown")]
	public class WordGrammarDebuggingTests : BaseTest
	{
		private XPathDocument m_doc;
#if __MonoCS__
		private IntPtr m_masterTransform;
		private IntPtr m_resultTransform;
		private IntPtr m_resultTransformNoCompoundRules;
		private IntPtr m_resultTransformStemNames;
		private IntPtr m_resultTransformAffixAlloFeats;
		private IntPtr m_UnificationViaXsltTransform;
		private IntPtr m_SameSlotTwiceTransform;
#else
		private XslCompiledTransform m_masterTransform;
		private XslCompiledTransform m_resultTransform;
		private XslCompiledTransform m_resultTransformNoCompoundRules;
		private XslCompiledTransform m_resultTransformStemNames;
		private XslCompiledTransform m_resultTransformAffixAlloFeats;
		private XslCompiledTransform m_UnificationViaXsltTransform;
		private XslCompiledTransform m_SameSlotTwiceTransform;
#endif

		/// <summary>
		/// Location of test files
		/// </summary>
		protected string m_sTestPath;
		/// <summary></summary>
		protected string m_sTransformPath;
		/// <summary></summary>
		protected string m_sMasterTransform;
		/// <summary></summary>
		protected string m_sResultTransform;
		/// <summary></summary>
		protected string m_sResultTransformNoCompoundRules;
		/// <summary></summary>
		protected string m_sResultTransformStemNames;
		/// <summary></summary>
		protected string m_sResultTransformAffixAlloFeats;
		/// <summary></summary>
		protected string m_sM3FXTDump;
		/// <summary></summary>
		protected string m_sM3FXTDumpNoCompoundRules;
		/// <summary></summary>
		protected string m_sM3FXTDumpStemNames;
		/// <summary></summary>
		protected string m_sM3FXTDumpAffixAlloFeats;
		/// <summary>Set to true to be able to debug into stylesheets</summary>
		private bool m_fDebug;
		/// <summary>path to the standard directory for temporary files.</summary>
		private string m_sTempPath;
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
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			m_sTestPath = Path.Combine(DirectoryFinder.FwSourceDirectory,
				"LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults");
			m_sTransformPath = Path.Combine(DirectoryFinder.FWCodeDirectory,
				"Language Explorer/Transforms");

			SetUpMasterTransform();
			CreateResultTransform("M3FXTDump.xml", ref m_sResultTransform);
			SetUpResultTransform(m_sResultTransform, out m_resultTransform);
			SetUpUnificationViaXsltTransform();
			SetUpSameSlotTwiceTransform();
			CreateResultTransform("M3FXTDumpNoCompoundRules.xml", ref m_sResultTransformNoCompoundRules);
			SetUpResultTransform(m_sResultTransformNoCompoundRules, out m_resultTransformNoCompoundRules);
			CreateResultTransform("M3FXTDumpStemNames.xml", ref m_sResultTransformStemNames);
			SetUpResultTransform(m_sResultTransformStemNames, out m_resultTransformStemNames);
			CreateResultTransform("M3FXTDumpAffixAlloFeats.xml", ref m_sResultTransformAffixAlloFeats);
			SetUpResultTransform(m_sResultTransformAffixAlloFeats, out m_resultTransformAffixAlloFeats);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete any files that we may have created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public override void FixtureTeardown()
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
#if __MonoCS__
			if (m_masterTransform != IntPtr.Zero)
			{
				SIL.Utils.LibXslt.FreeCompiledTransform(m_masterTransform);
				m_masterTransform = IntPtr.Zero;
			}
			if (m_resultTransform != IntPtr.Zero)
			{
				SIL.Utils.LibXslt.FreeCompiledTransform(m_resultTransform);
				m_resultTransform = IntPtr.Zero;
			}
			if (m_resultTransformNoCompoundRules != IntPtr.Zero)
			{
				SIL.Utils.LibXslt.FreeCompiledTransform(m_resultTransformNoCompoundRules);
				m_resultTransformNoCompoundRules = IntPtr.Zero;
			}
			if (m_resultTransformStemNames != IntPtr.Zero)
			{
				SIL.Utils.LibXslt.FreeCompiledTransform(m_resultTransformStemNames);
				m_resultTransformStemNames = IntPtr.Zero;
			}
			if (m_resultTransformAffixAlloFeats != IntPtr.Zero)
			{
				SIL.Utils.LibXslt.FreeCompiledTransform(m_resultTransformAffixAlloFeats);
				m_resultTransformAffixAlloFeats = IntPtr.Zero;
			}
			if (m_UnificationViaXsltTransform != IntPtr.Zero)
			{
				SIL.Utils.LibXslt.FreeCompiledTransform(m_UnificationViaXsltTransform);
				m_UnificationViaXsltTransform = IntPtr.Zero;
			}
			if (m_SameSlotTwiceTransform != IntPtr.Zero)
			{
				SIL.Utils.LibXslt.FreeCompiledTransform(m_SameSlotTwiceTransform);
				m_SameSlotTwiceTransform = IntPtr.Zero;
			}
#endif
			base.FixtureTeardown();
		}

		#region Helper methods for setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the up result transform.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetUpResultTransform(string sResultTransform, out XslCompiledTransform resultTransform)
		{
			resultTransform = new XslCompiledTransform(m_fDebug);
			resultTransform.Load(sResultTransform);
		}
		private void SetUpResultTransform(string sResultTransform, out IntPtr resultTransform)
		{
			resultTransform = SIL.Utils.LibXslt.CompileTransform(sResultTransform);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the up unification via XSLT transform.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetUpUnificationViaXsltTransform()
		{
#if __MonoCS__
			// TestUnificationViaXSLT.xsl contains an xsl:include href value that chokes libxslt.
			// (libxslt apparently doesn't like .. leading off a file path.)
			if (!File.Exists(Path.Combine(m_sTempPath, "UnifyTwoFeatureStructures.xsl")))
			{
				File.Copy(Path.Combine(m_sTransformPath, "UnifyTwoFeatureStructures.xsl"),
						  Path.Combine(m_sTempPath, "UnifyTwoFeatureStructures.xsl"));
			}
			if (!File.Exists(Path.Combine(m_sTempPath, "TestUnificationViaXSLT-Linux.xsl")))
			{
				File.Copy(Path.Combine(Path.GetDirectoryName(m_sTestPath), "TestUnificationViaXSLT-Linux.xsl"),
						  Path.Combine(m_sTempPath, "TestUnificationViaXSLT-Linux.xsl"));
			}
			string sUnificationViaXsltTransform = Path.Combine(m_sTempPath, "TestUnificationViaXSLT-Linux.xsl");
			SetUpResultTransform(sUnificationViaXsltTransform, out m_UnificationViaXsltTransform);
#else
			m_UnificationViaXsltTransform = new XslCompiledTransform(m_fDebug);
			string sUnificationViaXsltTransform = Path.Combine(m_sTestPath,
				"../TestUnificationViaXSLT.xsl");
			m_UnificationViaXsltTransform.Load(sUnificationViaXsltTransform);
#endif
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the up unification via XSLT transform.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetUpSameSlotTwiceTransform()
		{
#if __MonoCS__
			// TLPSameSlotTwiceWordGrammarDebugger.xsl contains a namespace declaration for
			// auto-ns1 that is Microsoft-specific.
			string sSameSlotTwiceTransform = Path.Combine(m_sTestPath,
				"TLPSameSlotTwiceWordGrammarDebugger-Linux.xsl");
			SetUpResultTransform(sSameSlotTwiceTransform, out m_SameSlotTwiceTransform);
#else
			m_SameSlotTwiceTransform = new XslCompiledTransform(m_fDebug);
			string sSameSlotTwiceTransform = Path.Combine(m_sTestPath,
				@"TLPSameSlotTwiceWordGrammarDebugger.xsl");
			m_SameSlotTwiceTransform.Load(sSameSlotTwiceTransform);
#endif
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a result transform.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CreateResultTransform(string sFXTDumpFile, ref string sResultTransform)
		{
			sResultTransform = FileUtils.GetTempFile("xsl");
			string sFXTDump = Path.Combine(m_sTestPath, sFXTDumpFile);
			SIL.Utils.XmlUtils.TransformFileToFile(m_sMasterTransform, sFXTDump,
				sResultTransform);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up the master transform.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetUpMasterTransform()
		{
			m_sMasterTransform = Path.Combine(m_sTransformPath,
				"FxtM3ParserToXAmpleWordGrammarDebuggingXSLT.xsl");
#if __MonoCS__
			m_masterTransform = SIL.Utils.LibXslt.CompileTransform(m_sMasterTransform);
#else
			m_masterTransform = new XslCompiledTransform();
			m_masterTransform.Load(m_sMasterTransform);
#endif
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
		private void ApplyTransform(string sInputFile, string sExpectedOutput,
			XslCompiledTransform transform)
		{
			ApplyTransform(sInputFile, sExpectedOutput, transform, true);
		}
		private void ApplyTransform(string sInputFile, string sExpectedOutput,
			IntPtr transform)
		{
			ApplyTransform(sInputFile, sExpectedOutput, transform, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Applies the transform.
		/// </summary>
		/// <param name="sInputFile">The input file filename.</param>
		/// <param name="sExpectedOutput">The expected output filename.</param>
		/// <param name="transform">The transform.</param>
		/// <param name="fFixMsxslNameSpace">remove Msxsl namespace</param>
		/// ------------------------------------------------------------------------------------
		private void ApplyTransform(string sInputFile, string sExpectedOutput,
			XslCompiledTransform transform, bool fFixMsxslNameSpace)
		{
			string sInput = Path.Combine(m_sTestPath, sInputFile);
			m_doc = new XPathDocument(sInput);
			string sOutput = FileUtils.GetTempFile("xml");
			using (StreamWriter result = new StreamWriter(sOutput))
			{
				transform.Transform(m_doc, null, result);
				result.Close();
				string sExpectedResult = Path.Combine(m_sTestPath, sExpectedOutput);
				CheckXmlEquals(sExpectedResult, sOutput, fFixMsxslNameSpace);
				// by deleting it here instead of a finally block, when it fails, we can see what the result is.
				File.Delete(sOutput);
			}
		}
		private void ApplyTransform(string sInputFile, string sExpectedOutput,
			IntPtr transform, bool fFixMsxslNameSpace)
		{
			string sInput = Path.Combine(m_sTestPath, sInputFile);
			string sOutput = FileUtils.GetTempFile("xml");
			SIL.Utils.LibXslt.TransformFileToFile(transform, sInput, sOutput);
			string sExpectedResult = Path.Combine(m_sTestPath, sExpectedOutput);
			CheckXmlEquals(sExpectedResult, sOutput, fFixMsxslNameSpace);
			// by deleting it here instead of a finally block, when it fails, we can see what the result is.
			File.Delete(sOutput);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verfifies that the result XML file is identical to the expected XML file.
		/// </summary>
		/// <param name="sExpectedResultFile">The expected result filename.</param>
		/// <param name="sActualResultFile">The actual result filename.</param>
		/// <param name="fFixMsxslNameSpace">remove Msxsl namespace</param>
		/// ------------------------------------------------------------------------------------
		private void CheckXmlEquals(string sExpectedResultFile, string sActualResultFile, bool fFixMsxslNameSpace)
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
#if __MonoCS__
			// REVIEW: Perhaps we should always use the fancy compare method using XElement objects?
			if (fFixMsxslNameSpace)
				sActual = sActual.Replace(" xmlns:auto-ns1=\"urn:schemas-microsoft-com:xslt\"", "");
			XElement xeActual = XElement.Parse(sActual, LoadOptions.None);
			XElement xeExpected = XElement.Parse(sExpected, LoadOptions.None);
			bool ok = XmlHelper.EqualXml(xeExpected, xeActual, sb);
			Assert.IsTrue(ok, sb.ToString());
#else
			if (fFixMsxslNameSpace)
			{
				string sFixMsxslNameSpace =
					sActual.Replace(" xmlns:auto-ns1=\"urn:schemas-microsoft-com:xslt\"", "");
				Assert.AreEqual(sExpected, sFixMsxslNameSpace, sb.ToString());
			}
			else
			{
				Assert.AreEqual(sExpected, sActual, sb.ToString());
			}
#endif
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
			DoWordGrammarDebuggerSteps("msmsAffixAlloFeats", 3, m_resultTransformAffixAlloFeats);
			// an inflectional affix allomorph has features and parse fails
			DoWordGrammarDebuggerSteps("mpmsAffixAlloFeats", 2, m_resultTransformAffixAlloFeats);
			// an inflectional affix allomorph without features is in an entry that has features and parse succeeds
			DoWordGrammarDebuggerSteps("fsinflAffixAlloFeats", 3, m_resultTransformAffixAlloFeats);
			// an inflectional affix allomorph without features is in an entry that has features and parse fails
			DoWordGrammarDebuggerSteps("msinflAffixAlloFeats", 2, m_resultTransformAffixAlloFeats);
			// a derivational affix allomorph has features and parse succeeds
			DoWordGrammarDebuggerSteps("fsfstovAffixAlloFeats", 3, m_resultTransformAffixAlloFeats);
			// a derivational affix allomorph has features and parse fails
			DoWordGrammarDebuggerSteps("fpfstovAffixAlloFeats", 2, m_resultTransformAffixAlloFeats);
			// a derivational affix allomorph without features is in an entry that has features and parse succeeds
			DoWordGrammarDebuggerSteps("fpfptovAffixAlloFeats", 3, m_resultTransformAffixAlloFeats);
			// a derivational affix allomorph without features is in an entry that has features and parse fails
			DoWordGrammarDebuggerSteps("fsfptovAffixAlloFeats", 2, m_resultTransformAffixAlloFeats);
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
			DoWordGrammarDebuggerSteps("niyuyiywusaStemNameSet", 3, m_resultTransformStemNames);
			// stem name is set but no features in regions and parse succeeds
			DoWordGrammarDebuggerSteps("niyumamwupeStemNameSetNoFs", 3, m_resultTransformStemNames);
			// stem name is set and parse fails
			DoWordGrammarDebuggerSteps("niyuyiywupeStemNameFail", 3, m_resultTransformStemNames);
			// stem name not set (but other allomorph in lex entry is) and it succeeds
			DoWordGrammarDebuggerSteps("niyuwowwupeStemNameNotSet", 3, m_resultTransformStemNames);
			// stem name not set (but other allomorph in lex entry is) and it fails
			DoWordGrammarDebuggerSteps("niyuhohwusaStemNameNotSetFail", 3, m_resultTransformStemNames);
			// stem name not set (but two other allomorps in lex entry are) and it fails
			DoWordGrammarDebuggerSteps("niyuwowwukoStemNameNotSetMultiFail", 3, m_resultTransformStemNames);
			// stem name set and has two FsFeatStrucs and it succeeds
			DoWordGrammarDebuggerSteps("timikikwusaStemNameSetMultiFs", 3, m_resultTransformStemNames);
			// stem name set and has two FsFeatStrucs and it fails
			DoWordGrammarDebuggerSteps("timikikwupeStemNameSetMultiFsFail", 3, m_resultTransformStemNames);
			// stem name is set, has compound and parse succeeds
			DoWordGrammarDebuggerSteps("niyuyiyximuwusaStemNameSetCompound", 5, m_resultTransformStemNames);
			// stem name not set (but other allomorph in lex entry is), has compound and it fails
			DoWordGrammarDebuggerSteps("niyuyiyximuwupeStemNameNotSetCompoundFail", 5, m_resultTransformStemNames);
		}

		// These methods have a seeming bug in them: the transform argument is not used,
		// but rather a hard-coded transform.  However, the test output obviously comes
		// from applying the hard-coded transform, so fixing this requires knowing what
		// the proper output is for the given data and the proper (other) transform.
		private void DoWordGrammarDebuggerSteps(string sName, int count, XslCompiledTransform transform)
		{
			for (int i = 0; i < count; i++)
				ApplyTransform(sName + "Step0" + i.ToString() + ".xml", sName + "Step0" + i.ToString() + "Result.xml", m_resultTransformStemNames, false);
		}
		private void DoWordGrammarDebuggerSteps(string sName, int count, IntPtr transform)
		{
			for (int i = 0; i < count; i++)
			{
				var inputName = String.Format("{0}Step0{1}.xml", sName, i);
				var outputName = String.Format("{0}Step0{1}Result.xml", sName, i);
				ApplyTransform(inputName, outputName, m_resultTransformStemNames, false);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test unification process using XSLT
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UnificationViaXSLT()
		{
			ApplyTransform("TestFeatureStructureUnification.xml",
				"TestFeatureStructureUnification.xml", m_UnificationViaXsltTransform);
		}
	}
}
