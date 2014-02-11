using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
		/// Temp File names
		/// </summary>
		const string m_ksHCParse = "HCParse";
		const string m_ksHCTrace = "HCTrace";

		/// <summary>
		/// For testing
		/// </summary>
		public HCTrace()
		{
		}
		/// <summary>
		/// The real deal
		/// </summary>
		/// <param name="mediator"></param>
		public HCTrace(Mediator mediator)
			: base(mediator)
		{
			m_sParse = m_ksHCParse;
			m_sTrace = m_ksHCTrace;
			m_sFormatParse = "FormatXAmpleParse.xsl"; // the XAmple one works fine with Hermit Crab parse output
			m_sFormatTrace = "FormatHCTrace.xsl";

		}

		/// <summary>
		/// Initialize what is needed to perform the word grammar debugging and
		/// produce an html page showing the results
		/// </summary>
		/// <param name="sNodeId">Id of the node to use</param>
		/// <param name="sForm">the wordform being tried</param>
		/// <param name="sLastURL"></param>
		/// <returns>temporary html file showing the results of the first step</returns>
		public override string SetUpWordGrammarDebuggerPage(string sNodeId, string sForm, string sLastURL)
		{
			m_wordGrammarDebugger = new HCWordGrammarDebugger(m_mediator, m_parseResult);
			return m_wordGrammarDebugger.SetUpWordGrammarDebuggerPage(sNodeId, sForm, sLastURL);
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
			if (fIsTrace)
			{
				ParserXMLGenerator.ConvertMorphs(m_parseResult, "//RuleAllomorph/Morph | //RootAllomorph/Morph | //Morphs/Morph", false, m_cache);
			}
			else
			{
				ParserXMLGenerator.ConvertMorphs(m_parseResult, "//Morphs/Morph", false, m_cache);
			}
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		protected override void CreateMorphNodes(XmlDocument doc, XmlNode seqNode, string sNodeId)
		{
			string s = "//WordGrammarAttempt[Id=\"" + sNodeId + "\"]";
			XmlNode node = m_parseResult.SelectSingleNode(s);
			if (node != null)
			{
				XmlNodeList morphs = node.SelectNodes("Morphs");
				foreach (XmlNode morph in morphs)
				{
					CreateMorphXmlElement(doc, seqNode, morph);
				}
			}
		}

		protected override void AddParserSpecificArguments(List<XmlUtils.XSLParameter> args)
		{
			string sLoadErrorFile = Path.Combine(Path.GetTempPath(), m_sDataBaseName + "HCLoadErrors.xml");
			args.Add(new XmlUtils.XSLParameter("prmHCTraceLoadErrorFile", sLoadErrorFile));
		}
	}
}
