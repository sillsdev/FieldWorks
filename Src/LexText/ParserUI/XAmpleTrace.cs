// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// THIS NEEDS TO BE REFACTORED!!
//
// File: XAmpleTrace.cs
// Responsibility: Andy Black
// Last reviewed:
//
// <remarks>
// Implementation of:
//		XAmpleTrace - Deal with results of an XAmple trace
// </remarks>

using System.Xml;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for XAmpleTrace.
	/// </summary>
	public class XAmpleTrace : ParserTrace
	{
		/// <summary>
		/// The real deal
		/// </summary>
		/// <param name="mediator"></param>
		public XAmpleTrace(Mediator mediator) : base(mediator)
		{
			m_sParse = "XAmpleParse";
			m_sTrace = "XAmpleTrace";
			m_sFormatParse = "FormatXAmpleParse.xsl";
			m_sFormatTrace = "FormatXAmpleTrace.xsl";
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
			m_wordGrammarDebugger = new XAmpleWordGrammarDebugger(m_mediator, m_parseResult);
			return m_wordGrammarDebugger.SetUpWordGrammarDebuggerPage(nodeId, form, lastUrl);
		}

		/// <summary>
		/// Create an HTML page of the results
		/// </summary>
		/// <param name="result">XML string of the XAmple trace output</param>
		/// <returns>URL of the resulting HTML page</returns>
		public override string CreateResultPage(string result)
		{
			m_parseResult = new XmlDocument();
			m_parseResult.LoadXml(result);

			string sInput = CreateTempFile(m_sTrace, "xml");
			m_parseResult.Save(sInput);
			TransformKind kind = (m_parseResult.DocumentElement.Name == "AmpleTrace" ? TransformKind.kcptTrace : TransformKind.kcptParse);
			string sOutput = TransformToHtml(sInput, kind);
			return sOutput;
		}
	}
}
///
/// Note for Andy
///
#if Later
using mshtml;

IHTMLDocument2 doc;
object boxDoc = m_browser.Document;
doc = (IHTMLDocument2)boxDoc;
string sHtml = doc.body.innerHTML;
#endif
