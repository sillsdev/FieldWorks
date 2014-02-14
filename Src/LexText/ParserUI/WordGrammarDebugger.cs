using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	public abstract class WordGrammarDebugger : ParserTraceBase
	{
		/// <summary>
		/// Word Grammar step stack
		/// </summary>
		private readonly Stack<WordGrammarStepPair> m_xmlHtmlStack;

		/// <summary>
		/// the latest word grammar debugging step xml document
		/// </summary>
		private string m_wordGrammarDebuggerXmlFile;

		/// <summary>
		/// The real deal
		/// </summary>
		/// <param name="mediator"></param>
		protected WordGrammarDebugger(Mediator mediator)
			: base(mediator)
		{
			m_xmlHtmlStack = new Stack<WordGrammarStepPair>();
		}

		/// <summary>
		/// Initialize what is needed to perform the word grammar debugging and
		/// produce an html page showing the results
		/// </summary>
		/// <param name="nodeId">Id of the node to use</param>
		/// <param name="form">the wordform being tried</param>
		/// <param name="lastUrl"></param>
		/// <returns>temporary html file showing the results of the first step</returns>
		public string SetUpWordGrammarDebuggerPage(string nodeId, string form, string lastUrl)
		{
			m_xmlHtmlStack.Push(new WordGrammarStepPair(null, lastUrl));
			var doc = new XDocument();
			using (XmlWriter writer = doc.CreateWriter())
				CreateAnalysisXml(writer, nodeId, form);
			return CreateWordDebuggerPage(doc);
		}

		/// <summary>
		/// Perform another step in the word grammar debugging process and
		/// produce an html page showing the results
		/// </summary>
		/// <param name="nodeId">Id of the selected node to use</param>
		/// <param name="form"></param>
		/// <param name="lastUrl"></param>
		/// <returns>temporary html file showing the results of the next step</returns>
		public string PerformAnotherWordGrammarDebuggerStepPage(string nodeId, string form, string lastUrl)
		{
			m_xmlHtmlStack.Push(new WordGrammarStepPair(m_wordGrammarDebuggerXmlFile, lastUrl));
			var doc = new XDocument();
			using (XmlWriter writer = doc.CreateWriter())
				CreateSelectedWordGrammarXml(writer, nodeId, form);
			return CreateWordDebuggerPage(doc);
		}

		public string PopWordGrammarStack()
		{
			if (m_xmlHtmlStack.Count > 0)
			{
				WordGrammarStepPair wgsp = m_xmlHtmlStack.Pop();
				m_wordGrammarDebuggerXmlFile = wgsp.XmlFile;
				return wgsp.HtmlFile;
			}
			return "unknown";
		}

		private void CreateAnalysisXml(XmlWriter writer, string nodeId, string form)
		{
			writer.WriteStartDocument();

			writer.WriteStartElement("word");
			writer.WriteElementString("form", form);
			writer.WriteStartElement("seq");

			CreateMorphNodes(writer, nodeId);

			writer.WriteEndElement();
			writer.WriteEndElement();

			writer.WriteEndDocument();
		}

		protected abstract void CreateMorphNodes(XmlWriter writer, string nodeId);

		private void CreateSelectedWordGrammarXml(XmlWriter writer, string nodeId, string form)
		{
			var lastDoc = XDocument.Load(m_wordGrammarDebuggerXmlFile);

			writer.WriteStartDocument();

			writer.WriteStartElement("word");
			writer.WriteElementString("form", form);

			// Find the sNode'th seq node
			Debug.Assert(lastDoc.Root != null);
			XElement selectedSeqNode = lastDoc.Root.Elements("seq").ElementAt(int.Parse(nodeId, CultureInfo.InvariantCulture) - 1);
			// create the "result so far node"
			writer.WriteStartElement("resultSoFar");
			foreach (XElement child in selectedSeqNode.Elements())
				child.WriteTo(writer);
			writer.WriteEndElement();
			// create the seq node
			selectedSeqNode.WriteTo(writer);
			writer.WriteStartElement("seq");
			writer.WriteEndElement();
		}

		private string CreateWordDebuggerPage(XDocument xmlDoc)
		{
			// apply word grammar step transform file
			string xmlOutput = TransformToXml(xmlDoc);
			m_wordGrammarDebuggerXmlFile = xmlOutput;
			// format the result
			return TransformToHtml(xmlOutput);
		}

		private string CreateWordGrammarDebuggerFileName()
		{
			string depthLevel = m_xmlHtmlStack.Count.ToString(CultureInfo.InvariantCulture);
			return "WordGrammarDebugger" + depthLevel;
		}

		protected string TransformToHtml(string inputPath)
		{
			return TransformToHtml(inputPath, CreateWordGrammarDebuggerFileName(),
									  "FormatXAmpleWordGrammarDebuggerResult.xsl", new XsltArgumentList());
		}

		protected string TransformToHtml(XDocument inputDoc)
		{
			return TransformToHtml(inputDoc, CreateWordGrammarDebuggerFileName(),
									  "FormatXAmpleWordGrammarDebuggerResult.xsl", new XsltArgumentList());
		}

		private string TransformToXml(XDocument inputDoc)
		{
			// Don't overwrite the input file before transforming it! (why +"A" on the next line)
			string outputPath = CreateTempFile(CreateWordGrammarDebuggerFileName() + "A", "xml");
			string xslFileName = m_sDataBaseName + "XAmpleWordGrammarDebugger" + ".xsl";
			string dir = Path.GetDirectoryName(outputPath);
			Debug.Assert(dir != null);
			string transform = Path.Combine(dir, xslFileName);
			XslCompiledTransformUtil.Instance.TransformXDocumentToFile(transform, inputDoc, outputPath, new XsltArgumentList());
			return outputPath;
		}
	}
}
