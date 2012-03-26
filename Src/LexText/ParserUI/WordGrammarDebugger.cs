using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	public abstract class WordGrammarDebugger : ParserTraceBase
	{
		/// <summary>
		/// Word Grammar step stack
		/// </summary>
		protected Stack<WordGrammarStepPair> m_XmlHtmlStack;

		protected const string m_ksWordGrammarDebugger = "WordGrammarDebugger";

		public WordGrammarDebugger()
		{
		}
		/// <summary>
		/// The real deal
		/// </summary>
		/// <param name="mediator"></param>
		public WordGrammarDebugger(Mediator mediator)
			: base(mediator)
		{
			m_XmlHtmlStack = new Stack<WordGrammarStepPair>();
		}


		/// <summary>
		/// Initialize what is needed to perform the word grammar debugging and
		/// produce an html page showing the results
		/// </summary>
		/// <param name="sNodeId">Id of the node to use</param>
		/// <param name="sForm">the wordform being tried</param>
		/// <returns>temporary html file showing the results of the first step</returns>
		public string SetUpWordGrammarDebuggerPage(string sNodeId, string sForm, string sLastURL)
		{
			m_XmlHtmlStack.Push(new WordGrammarStepPair(null, sLastURL));
			string sInitialAnalysisXml = CreateAnalysisXml(sNodeId, sForm);
			string sHtmlPage = CreateWordDebuggerPage(sInitialAnalysisXml);
			return sHtmlPage;
		}
		/// <summary>
		/// Perform another step in the word grammar debugging process and
		/// produce an html page showing the results
		/// </summary>
		/// <param name="sNodeId">Id of the selected node to use</param>
		/// <returns>temporary html file showing the results of the next step</returns>
		public string PerformAnotherWordGrammarDebuggerStepPage(string sNodeId, string sForm, string sLastURL)
		{
			m_XmlHtmlStack.Push(new WordGrammarStepPair(m_sWordGrammarDebuggerXmlFile, sLastURL));
			string sNextXml = CreateSelectedWordGrammarXml(sNodeId, sForm);
			string sHtmlPage = CreateWordDebuggerPage(sNextXml);
			return sHtmlPage;
		}
		public string PopWordGrammarStack()
		{
			WordGrammarStepPair wgsp;
			if (m_XmlHtmlStack.Count > 0)
			{
				wgsp = m_XmlHtmlStack.Pop(); // get the previous one
				m_sWordGrammarDebuggerXmlFile = wgsp.XmlFile;
				return wgsp.HtmlFile;
			}
			return "unknown";
		}
		protected string CreateAnalysisXml(string sNodeId, string sForm)
		{
			string sResult;
			if (m_parseResult != null)
			{
				XmlDocument doc = new XmlDocument();
				XmlNode wordNode = CreateXmlElement(doc, "word", doc);
				XmlNode formNode = CreateXmlElement(doc, "form", wordNode);
				formNode.InnerXml = sForm;
				XmlNode seqNode = CreateXmlElement(doc, "seq", wordNode);

				// following for debugging as needed
				sResult = CreateTempFile("ParseResult", "xml");
				m_parseResult.Save(sResult);
				CreateMorphNodes(doc, seqNode, sNodeId);

				sResult = CreateTempFile(CreateWordGrammarDebuggerFileName(), "xml");
				doc.Save(sResult);
			}
			else
				sResult = "error!";
			return sResult;
		}
		private string CreateSelectedWordGrammarXml(string sNodeId, string sForm)
		{
			string sResult;
			if (m_sWordGrammarDebuggerXmlFile != null)
			{
				XmlDocument lastDoc = new XmlDocument();
				lastDoc.Load(m_sWordGrammarDebuggerXmlFile);
				XmlDocument doc = new XmlDocument();
				XmlNode wordNode = CreateXmlElement(doc, "word", doc);
				XmlNode formNode = CreateXmlElement(doc, "form", wordNode);
				formNode.InnerXml = sForm;
				// Find the sNode'th seq node
				string sSelect = "//seq[position()='" + sNodeId + "']";
				XmlNode selectedSeqNode = lastDoc.SelectSingleNode(sSelect);
				// create the "result so far node"
				XmlNode resultSoFarNode = CreateXmlElement(doc, "resultSoFar", wordNode);
				resultSoFarNode.InnerXml = selectedSeqNode.InnerXml;
				// create the seq node
				XmlNode seqNode = CreateXmlElement(doc, "seq", wordNode);
				seqNode.InnerXml = selectedSeqNode.InnerXml;
				// save result
				sResult = CreateTempFile("SelectedWordGrammarXml", "xml");
				doc.Save(sResult);
			}
			else
				sResult = "error!";
			return sResult;
		}

		private string CreateWordDebuggerPage(string sXmlFile)
		{
			// apply word grammar step transform file
			string sXmlOutput = TransformToXml(sXmlFile);
			m_sWordGrammarDebuggerXmlFile = sXmlOutput;
			// format the result
			string sOutput = TransformToHtml(sXmlOutput);
			return sOutput;
		}

		private string CreateWordGrammarDebuggerFileName()
		{
			string sDepthLevel = m_XmlHtmlStack.Count.ToString();
			return m_ksWordGrammarDebugger + sDepthLevel;
		}

		protected string TransformToHtml(string sInputFile)
		{
			var args = new List<XmlUtils.XSLParameter>();
			string sOutput = TransformToHtml(sInputFile, CreateWordGrammarDebuggerFileName(),
									  "FormatXAmpleWordGrammarDebuggerResult.xsl", args);
			return sOutput;
		}
		private string TransformToXml(string sInputFile)
		{
			string sOutput = CreateTempFile(CreateWordGrammarDebuggerFileName(), "xml");
			string sName = m_sDataBaseName + "XAmpleWordGrammarDebugger" + ".xsl";
			string sTransform = Path.Combine(Path.GetDirectoryName(sOutput), sName);
			XmlUtils.TransformFileToFile(sTransform, new XmlUtils.XSLParameter[0], sInputFile, sOutput);
			return sOutput;
		}
	}
}
