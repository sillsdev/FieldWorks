using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
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

			m_parseResult = XDocument.Parse(result);

			TransformKind kind = (fIsTrace ? TransformKind.kcptTrace : TransformKind.kcptParse);
			return TransformToHtml(m_parseResult, kind);
		}

		protected override void AddParserSpecificArguments(XsltArgumentList argumentList)
		{
			string sLoadErrorFile = Path.Combine(Path.GetTempPath(), m_sDataBaseName + "HCLoadErrors.xml");
			argumentList.AddParam("prmHCTraceLoadErrorFile", "", sLoadErrorFile);
		}
	}
}
