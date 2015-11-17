// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;
using SIL.FieldWorks.FDO;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="m_cache and m_mediator are references")]
	public class XAmpleWordGrammarDebugger
	{
		private static ParserTraceUITransform s_pageTransform;
		private static ParserTraceUITransform PageTransform
		{
			get
			{
				if (s_pageTransform == null)
					s_pageTransform = new ParserTraceUITransform("FormatXAmpleWordGrammarDebuggerResult");
				return s_pageTransform;
			}
		}

		/// <summary>
		/// Word Grammar step stack
		/// </summary>
		private readonly Stack<Tuple<XDocument, string>> m_xmlHtmlStack;

		/// <summary>
		/// the latest word grammar debugging step xml document
		/// </summary>
		private XDocument m_wordGrammarDebuggerXml;

		private readonly XslCompiledTransform m_intermediateTransform;
		private readonly Mediator m_mediator;
		private readonly FdoCache m_cache;
		private readonly XDocument m_parseResult;

		public XAmpleWordGrammarDebugger(Mediator mediator, XDocument parseResult)
		{
			m_mediator = mediator;
			m_parseResult = parseResult;
			m_cache = (FdoCache) m_mediator.PropertyTable.GetValue("cache");
			m_xmlHtmlStack = new Stack<Tuple<XDocument, string>>();
			m_intermediateTransform = new XslCompiledTransform();
			m_intermediateTransform.Load(Path.Combine(Path.GetTempPath(), m_cache.ProjectId.Name + "XAmpleWordGrammarDebugger.xsl"), new XsltSettings(true, false), new XmlUrlResolver());
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
			m_xmlHtmlStack.Push(Tuple.Create((XDocument) null, lastUrl));
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
			m_xmlHtmlStack.Push(Tuple.Create(m_wordGrammarDebuggerXml, lastUrl));
			var doc = new XDocument();
			using (XmlWriter writer = doc.CreateWriter())
				CreateSelectedWordGrammarXml(writer, nodeId, form);
			return CreateWordDebuggerPage(doc);
		}

		public string PopWordGrammarStack()
		{
			if (m_xmlHtmlStack.Count > 0)
			{
				Tuple<XDocument, string> wgsp = m_xmlHtmlStack.Pop();
				m_wordGrammarDebuggerXml = wgsp.Item1;
				return wgsp.Item2;
			}
			return "unknown";
		}

		private void CreateAnalysisXml(XmlWriter writer, string nodeId, string form)
		{
			writer.WriteStartElement("word");
			writer.WriteElementString("form", form);
			writer.WriteStartElement("seq");

			WriteMorphNodes(writer, nodeId);

			writer.WriteEndElement();
			writer.WriteEndElement();
		}

		private void CreateSelectedWordGrammarXml(XmlWriter writer, string nodeId, string form)
		{
			writer.WriteStartElement("word");
			writer.WriteElementString("form", form);

			// Find the sNode'th seq node
			Debug.Assert(m_wordGrammarDebuggerXml.Root != null);
			XElement selectedSeqNode = m_wordGrammarDebuggerXml.Root.Elements("seq").ElementAt(int.Parse(nodeId, CultureInfo.InvariantCulture) - 1);
			// create the "result so far node"
			writer.WriteStartElement("resultSoFar");
			foreach (XElement child in selectedSeqNode.Elements())
				child.WriteTo(writer);
			writer.WriteEndElement();
			// create the seq node
			selectedSeqNode.WriteTo(writer);
			writer.WriteEndElement();
		}

		private string CreateWordDebuggerPage(XDocument xmlDoc)
		{
			// apply word grammar step transform file
			var output = new XDocument();
			using (XmlWriter writer = output.CreateWriter())
				m_intermediateTransform.Transform(xmlDoc.CreateNavigator(), writer);
			m_wordGrammarDebuggerXml = output;
			// format the result
			return PageTransform.Transform(m_mediator, output, "WordGrammarDebugger" + m_xmlHtmlStack.Count);
		}

		private void WriteMorphNodes(XmlWriter writer, string nodeId)
		{
			XElement failureElem = m_parseResult.Descendants("failure").FirstOrDefault(e => ((string) e.Attribute("id")) == nodeId);
			if (failureElem != null)
			{
				foreach (XElement parseNodeElem in failureElem.Ancestors("parseNode").Where(e => e.Element("morph") != null).Reverse())
				{
					XElement morphElem = parseNodeElem.Element("morph");
					Debug.Assert(morphElem != null);
					morphElem.WriteTo(writer);
				}
			}
		}
	}
}
