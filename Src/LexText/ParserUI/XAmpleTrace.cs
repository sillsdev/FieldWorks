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
		/// Temp File names
		/// </summary>
		const string m_ksXAmpleParse = "XAmpleParse";
		const string m_ksXAmpleTrace = "XAmpleTrace";

		/// <summary>
		/// For testing
		/// </summary>
		public XAmpleTrace()
		{
		}
		/// <summary>
		/// The real deal
		/// </summary>
		/// <param name="mediator"></param>
		public XAmpleTrace(Mediator mediator) : base(mediator)
		{
			m_sParse = m_ksXAmpleParse;
			m_sTrace = m_ksXAmpleTrace;
			m_sFormatParse = "FormatXAmpleParse.xsl";
			m_sFormatTrace = "FormatXAmpleTrace.xsl";

		}
		/// <summary>
		/// Initialize what is needed to perform the word grammar debugging and
		/// produce an html page showing the results
		/// </summary>
		/// <param name="sNodeId">Id of the node to use</param>
		/// <param name="sForm">the wordform being tried</param>
		/// <returns>temporary html file showing the results of the first step</returns>
		public override string SetUpWordGrammarDebuggerPage(string sNodeId, string sForm, string sLastUrl)
		{
			m_wordGrammarDebugger = new XAmpleWordGrammarDebugger(m_mediator, m_parseResult);
			return m_wordGrammarDebugger.SetUpWordGrammarDebuggerPage(sNodeId, sForm, sLastUrl);
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

			string sInput = CreateTempFile(m_ksXAmpleTrace, "xml");
			m_parseResult.Save(sInput);
			TransformKind kind = (m_parseResult.DocumentElement.Name == "AmpleTrace" ? TransformKind.kcptTrace : TransformKind.kcptParse);
			string sOutput = TransformToHtml(sInput, kind);
			return sOutput;
		}

		protected override void CreateMorphNodes(XmlDocument doc, XmlNode seqNode, string sNodeId)
		{
			string s = "//failure[@id=\"" + sNodeId + "\"]/ancestor::parseNode[morph][1]/morph";
			XmlNode node = m_parseResult.SelectSingleNode(s);
			if (node != null)
			{
				CreateMorphNode(doc, seqNode, node);
			}
		}
		private void CreateMorphNode(XmlDocument doc, XmlNode seqNode, XmlNode node)
		{
			// get the <morph> element closest up the chain to node
			XmlNode morph = node.SelectSingleNode("../ancestor::parseNode[morph][1]/morph");
			if (morph != null)
			{
				CreateMorphNode(doc, seqNode, morph);
			}
			CreateMorphXmlElement(doc, seqNode, node);
		}
	}

	public class WordGrammarStepPair
	{
		protected string m_sXmlFile;
		protected string m_sHtmlFile;
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="sXmlFile">Xml file</param>
		/// <param name="sHtmlFile">Html file</param>
		public WordGrammarStepPair(string sXmlFile, string sHtmlFile)
		{
			m_sXmlFile = sXmlFile;
			m_sHtmlFile = sHtmlFile;
		}
		/// <summary>
		/// Gete/set XmlFile
		/// </summary>
		public string XmlFile
		{
			get
			{
				return m_sXmlFile;
			}
			set
			{
				m_sXmlFile = value;
			}
		}
		/// <summary>
		/// Gete/set HtmlFile
		/// </summary>
		public string HtmlFile
		{
			get
			{
				return m_sHtmlFile;
			}
			set
			{
				m_sHtmlFile = value;
			}
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
