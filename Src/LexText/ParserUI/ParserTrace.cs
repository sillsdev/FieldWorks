using System.Text;
using System.Xml.Linq;
using System.Xml.Xsl;
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
		/// <param name="result">XML of the parse trace output</param>
		/// <returns>URL of the resulting HTML page</returns>
		public abstract string CreateResultPage(XDocument result);

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

		protected string TransformToHtml(XDocument xDocument, TransformKind kind)
		{
			string sOutput = null;
			var argumentList = new XsltArgumentList();

			switch (kind)
			{
				case TransformKind.kcptParse:
					sOutput = TransformToHtml(xDocument, m_sParse, m_sFormatParse, argumentList);
					break;
				case TransformKind.kcptTrace:
					string sIconPath = CreateIconPath();
					argumentList.AddParam("prmIconPath", "", sIconPath);
					sOutput = TransformToHtml(xDocument, m_sTrace, m_sFormatTrace, argumentList);
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
