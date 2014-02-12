using System.Collections.Generic;
using System.IO;
using System.Xml;
using SIL.FieldWorks.WordWorks.Parser;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	public class HCTrace : ParserTrace
	{
		/// <summary>
		/// The real deal
		/// </summary>
		/// <param name="mediator"></param>
		public HCTrace(Mediator mediator)
			: base(mediator)
		{
			m_sParse = "HCParse";
			m_sTrace = "HCTrace";
			m_sFormatParse = "FormatXAmpleParse.xsl"; // the XAmple one works fine with Hermit Crab parse output
			m_sFormatTrace = "FormatHCTrace.xsl";
		}

		/// <summary>
		/// Initialize what is needed to perform the word grammar debugging and
		/// produce an html page showing the results
		/// </summary>
		/// <param name="nodeId">Id of the node to use</param>
		/// <param name="form">the wordform being tried</param>
		/// <param name="lastUrl"></param>
		/// <returns>temporary html file showing the results of the first step</returns>
		public override string SetUpWordGrammarDebuggerPage(string nodeId, string form, string lastUrl)
		{
			m_wordGrammarDebugger = new HCWordGrammarDebugger(m_mediator, m_parseResult);
			return m_wordGrammarDebugger.SetUpWordGrammarDebuggerPage(nodeId, form, lastUrl);
		}

		public override string CreateResultPage(string result)
		{
			bool fIsTrace = result.Contains("<Trace>");

			m_parseResult = new XmlDocument();
			m_parseResult.LoadXml(result);
			ConvertHvosToStrings(fIsTrace);

			AddMsaNodes(false);

			string sInput = CreateTempFile(m_sTrace, "xml");
			m_parseResult.Save(sInput);

			TransformKind kind = (fIsTrace ? TransformKind.kcptTrace : TransformKind.kcptParse);
			string sOutput = TransformToHtml(sInput, kind);
			return sOutput;
		}

		private void ConvertHvosToStrings(bool fIsTrace)
		{
			ParserXmlGenerator.ConvertMorphs(m_parseResult, fIsTrace ? "//RuleAllomorph/Morph | //RootAllomorph/Morph | //Morphs/Morph" : "//Morphs/Morph", false, m_cache);
		}

		protected override void AddParserSpecificArguments(List<XmlUtils.XSLParameter> args)
		{
			string sLoadErrorFile = Path.Combine(Path.GetTempPath(), m_sDataBaseName + "HCLoadErrors.xml");
			args.Add(new XmlUtils.XSLParameter("prmHCTraceLoadErrorFile", sLoadErrorFile));
		}
	}
}
