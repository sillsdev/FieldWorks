using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.XPath;
using System.Xml.Xsl;
using NUnit.Framework;
using SIL.LCModel.Utils;
using SIL.FieldWorks.Common.FwUtils;
using System.Text.RegularExpressions;


namespace SIL.FieldWorks.IText
{
	internal class XLingPaperExporterTests
	{
		/// <summary>
		/// Location of test files
		/// </summary>
		protected string m_sTestPath;
		protected string m_sTransformPath;
		protected XslCompiledTransform m_xmlTransform;
		readonly Dictionary<string, XPathDocument> m_mapXmlDocs = new Dictionary<string, XPathDocument>();

		[OneTimeSetUp]
		public void FixtureSetup()
		{
			m_sTestPath = Path.Combine(FwDirectoryFinder.SourceDirectory, "LexText", "Interlinear", "ITextDllTests", "XLingPaperTransformerTestsDataFiles");
			m_sTransformPath = Path.Combine(FwDirectoryFinder.FlexFolder, "Export Templates", "Interlinear", "xml2XLingPapConcatMorphemes.xsl");
			m_xmlTransform = new XslCompiledTransform();
			m_xmlTransform.Load(m_sTransformPath);
		}

		[TestCase("BruceCoxEmpty")]
		[TestCase("Gilaki01")]
		[TestCase("HalbiBUD2")]
		[TestCase("HalbiCS3")]
		[TestCase("HalbiST1")]
		[TestCase("Jibiyal2Texts")]
		[TestCase("Jibiyal3Text")]
		[TestCase("nszEnglishWords")]
		[TestCase("SETCorn")]
		[TestCase("Urim2Kids")]
		public void RunXmlTest(string testName)
		{
			string inputFileName = Path.Combine(m_sTestPath, "Phase1-" + testName + ".xml");
			string goldFileName = Path.Combine(m_sTestPath, testName + "Old.xml");
			ApplyTransform(inputFileName, m_xmlTransform, goldFileName);
		}

		private void ApplyTransform(string sInput, XslCompiledTransform transform, string sExpectedOutput)
		{
			XPathDocument inputXdoc = new XPathDocument(sInput);
			string sOutput = FileUtils.GetTempFile("xml");
			using (var result = new StreamWriter(sOutput))
			{
				transform.Transform(inputXdoc, null, result);
				result.Close();
			}
			CheckOutputEquals(sExpectedOutput, sOutput);
			// by deleting it here instead of a finally block, when it fails, we can see what the result is.
			File.Delete(sOutput);
		}

		private void CheckOutputEquals(string sExpectedResultFile, string sActualResultFile)
		{
			string sExpected, sActual;
			using (var expected = new StreamReader(sExpectedResultFile))
				sExpected = expected.ReadToEnd();
			using (var actual = new StreamReader(sActualResultFile))
				sActual = actual.ReadToEnd();
			// Eliminate spurious differences.
			sActual = NormalizeXmlString(sActual);
			sExpected = NormalizeXmlString(sExpected);
			var sb = new StringBuilder();
			sb.Append("Expected file was ");
			sb.AppendLine(sExpectedResultFile);
			sb.Append("Actual file was ");
			sb.AppendLine(sActualResultFile);
			Assert.That(sActual, Is.EqualTo(sExpected), sb.ToString());
		}

		private string NormalizeXmlString(string xmlString)
		{
			xmlString = xmlString.Replace("\r", "");
			xmlString = xmlString.Replace("\n", "");
			xmlString = xmlString.Replace("UTF", "utf");
			xmlString = Regex.Replace(xmlString, @"\s+<", "<");
			xmlString = Regex.Replace(xmlString, @"\s+/>", "/>");
			xmlString = xmlString.Replace("<gloss lang=\"en-lexGloss\"></gloss>", "<gloss lang=\"en-lexGloss\"/>");
			xmlString = xmlString.Replace("<gloss lang=\"es-lexGloss\"></gloss>", "<gloss lang=\"es-lexGloss\"/>");
			xmlString = xmlString.Replace("<gloss lang=\"en-wordGloss\"></gloss>", "<gloss lang=\"en-wordGloss\"/>");
			xmlString = xmlString.Replace("<langData lang=\"stp-x-stp-morpheme\"></langData>", "<langData lang=\"stp-x-stp-morpheme\"/>");
			xmlString = xmlString.Replace("<object type=\"tVariantTypes\"></object>", "<object type=\"tVariantTypes\"/>");
			xmlString = xmlString.Replace("<shortTitle></shortTitle>", "<shortTitle/>");
			return xmlString;
		}
	}
}
