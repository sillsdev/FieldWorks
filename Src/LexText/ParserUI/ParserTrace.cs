using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml;
using SIL.FieldWorks.WordWorks.Parser;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Base class common to all parser trace processing
	/// </summary>
	public abstract class ParserTrace : ParserTraceBase
	{
		protected enum TransformKind
		{
			kcptParse = 0,
			kcptTrace = 1,
			kcptWordGrammarDebugger = 2,
		}
		/// <summary>
		/// Temp File names
		/// </summary>
		protected string m_sParse;
		protected string m_sTrace;

		protected WordGrammarDebugger m_wordGrammarDebugger;
		/// <summary>
		/// Transform file names
		/// </summary>
		protected string m_sFormatParse;
		protected string m_sFormatTrace;

		/// <summary>
		/// The real deal
		/// </summary>
		/// <param name="mediator"></param>
		protected ParserTrace(Mediator mediator)
			: base(mediator)
		{
		}

		/// <summary>
		/// Create an HTML page of the results
		/// </summary>
		/// <param name="result">XML string of the XAmple trace output</param>
		/// <returns>URL of the resulting HTML page</returns>
		public abstract string CreateResultPage(string result);

		/// <summary>
		/// Initialize what is needed to perform the word grammar debugging and
		/// produce an html page showing the results
		/// </summary>
		/// <param name="nodeId">Id of the node to use</param>
		/// <param name="form">the wordform being tried</param>
		/// <param name="lastUrl"></param>
		/// <returns>temporary html file showing the results of the first step</returns>
		public abstract string SetUpWordGrammarDebuggerPage(string nodeId, string form, string lastUrl);

		/// <summary>
		/// Perform another step in the word grammar debugging process and
		/// produce an html page showing the results
		/// </summary>
		/// <param name="sNodeId">Id of the selected node to use</param>
		/// <param name="form"></param>
		/// <param name="lastUrl"></param>
		/// <returns>temporary html file showing the results of the next step</returns>
		public string PerformAnotherWordGrammarDebuggerStepPage(string sNodeId, string form, string lastUrl)
		{
			return m_wordGrammarDebugger.PerformAnotherWordGrammarDebuggerStepPage(sNodeId, form, lastUrl);
		}

		public string PopWordGrammarStack()
		{
			return m_wordGrammarDebugger.PopWordGrammarStack();
		}

		protected string TransformToHtml(string sInputFile, TransformKind kind)
		{
			string sOutput = null;
			var args = new List<XmlUtils.XSLParameter>();

			switch (kind)
			{
				case TransformKind.kcptParse:
					sOutput = TransformToHtml(sInputFile, m_sParse, m_sFormatParse, args);
					break;
				case TransformKind.kcptTrace:
					string sIconPath = CreateIconPath();
					args.Add(new XmlUtils.XSLParameter("prmIconPath", sIconPath));
					sOutput = TransformToHtml(sInputFile, m_sTrace, m_sFormatTrace, args);
					break;
			}
			return sOutput;
		}

		private string CreateIconPath()
		{
			var sb = new StringBuilder();
			sb.Append("file:///");
			sb.Append(TransformPath.Replace(@"\", "/"));
			sb.Append("/");
			return sb.ToString();
		}
	}
}
