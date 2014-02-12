using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.WordWorks.Parser;
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
			string initialAnalysisFile = CreateTempFile(CreateWordGrammarDebuggerFileName(), "xml");
			using (var writer = XmlWriter.Create(initialAnalysisFile))
				CreateAnalysisXml(writer, nodeId, form);
			return CreateWordDebuggerPage(initialAnalysisFile);
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
			string nextFile = CreateTempFile("SelectedWordGrammarXml", "xml");
			using (var writer = XmlWriter.Create(nextFile))
				CreateSelectedWordGrammarXml(writer, nodeId, form);
			return CreateWordDebuggerPage(nextFile);
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
			var lastDoc = new XmlDocument();
			lastDoc.Load(m_wordGrammarDebuggerXmlFile);

			writer.WriteStartDocument();

			writer.WriteStartElement("word");
			writer.WriteElementString("form", form);

			// Find the sNode'th seq node
			string sSelect = "//seq[position()='" + nodeId + "']";
			XmlNode selectedSeqNode = lastDoc.SelectSingleNode(sSelect);
			// create the "result so far node"
			writer.WriteStartElement("resultsSoFar");
			writer.WriteRaw(selectedSeqNode.InnerXml);
			writer.WriteEndElement();
			// create the seq node
			writer.WriteStartElement("seq");
			writer.WriteRaw(selectedSeqNode.InnerXml);
			writer.WriteEndElement();
		}

		protected void CreateMorphXmlElement(XmlWriter writer, XmlNode node)
		{
			writer.WriteStartElement("morph");
			XmlAttribute alloIdAttr = node.Attributes["alloid"];
			if (alloIdAttr != null)
				writer.WriteAttributeString("alloid", alloIdAttr.Value);
			XmlAttribute morphnameAttr = node.Attributes["morphname"];
			if (morphnameAttr != null)
				writer.WriteAttributeString("morphname", morphnameAttr.Value);
			XmlAttribute typeAttr = node.Attributes["type"];
			if (typeAttr != null)
				writer.WriteAttributeString("type", typeAttr.Value);
			XmlAttribute wordTypeAttr = node.Attributes["wordType"];
			if (wordTypeAttr != null)
				writer.WriteAttributeString("wordType", wordTypeAttr.Value);
			CreateMorphShortNameXmlElement(writer, node);
			XmlNode formNode = node.SelectSingleNode("alloform");
			if (formNode != null)
				writer.WriteElementString("alloform", formNode.InnerText);
			XmlNode stemNameNode = node.SelectSingleNode("stemName");
			if (stemNameNode != null)
			{
				XmlAttribute idAttr = stemNameNode.Attributes["id"];
				if (idAttr != null)
				{
					writer.WriteStartElement("stemName");
					writer.WriteAttributeString("id", idAttr.Value);
					writer.WriteString(stemNameNode.InnerText);
					writer.WriteEndElement();
				}
			}
			CreateMorphAffixAlloFeatsXmlElement(writer, node);
			XmlNode glossNode = node.SelectSingleNode("gloss");
			if (glossNode != null)
				writer.WriteElementString("gloss", glossNode.InnerText);
			XmlNode citationFormNode = node.SelectSingleNode("citationForm");
			if (citationFormNode != null)
				writer.WriteElementString("citationForm", citationFormNode.InnerText);

			if (alloIdAttr != null)
			{
				int hvo = Convert.ToInt32(alloIdAttr.Value);
				ICmObject obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
				var form = obj as IMoAffixForm;  // only for affix forms
				if (form != null)
				{
					if (form.InflectionClassesRC.Count > 0)
					{
						writer.WriteStartElement("inflClasses");
						CreateInflectionClassesAndSubclassesXmlElement(writer, form.InflectionClassesRC);
					}
				}
			}
			if (morphnameAttr != null)
			{
				ParserXmlGenerator.CreateMsaXmlElement(writer, morphnameAttr.Value, alloIdAttr == null ? 0 : Convert.ToInt32(alloIdAttr.Value),
					typeAttr == null ? null : typeAttr.Value, wordTypeAttr == null ? null : wordTypeAttr.Value, m_cache);
			}
		}

		protected abstract void CreateMorphShortNameXmlElement(XmlWriter writer, XmlNode node);

		protected abstract void CreateMorphAffixAlloFeatsXmlElement(XmlWriter writer, XmlNode node);

		private void CreateInflectionClassesAndSubclassesXmlElement(XmlWriter writer, IEnumerable<IMoInflClass> inflectionClasses)
		{
			foreach (IMoInflClass ic in inflectionClasses)
			{
				writer.WriteStartElement("inflClass");
				writer.WriteAttributeString("hvo", ic.Hvo.ToString(CultureInfo.InvariantCulture));
				writer.WriteAttributeString("abbr", ic.Abbreviation.BestAnalysisAlternative.Text);
				writer.WriteEndElement();
				CreateInflectionClassesAndSubclassesXmlElement(writer, ic.SubclassesOC);
			}
		}

		private string CreateWordDebuggerPage(string sXmlFile)
		{
			// apply word grammar step transform file
			string sXmlOutput = TransformToXml(sXmlFile);
			m_wordGrammarDebuggerXmlFile = sXmlOutput;
			// format the result
			string sOutput = TransformToHtml(sXmlOutput);
			return sOutput;
		}

		private string CreateWordGrammarDebuggerFileName()
		{
			string sDepthLevel = m_xmlHtmlStack.Count.ToString();
			return "WordGrammarDebugger" + sDepthLevel;
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
			// Don't overwrite the input file before transforming it! (why +"A" on the next line)
			string sOutput = CreateTempFile(CreateWordGrammarDebuggerFileName()+"A", "xml");
			string sName = m_sDataBaseName + "XAmpleWordGrammarDebugger" + ".xsl";
			string sTransform = Path.Combine(Path.GetDirectoryName(sOutput), sName);
			XmlUtils.TransformFileToFile(sTransform, new XmlUtils.XSLParameter[0], sInputFile, sOutput);
			return sOutput;
		}
	}
}
