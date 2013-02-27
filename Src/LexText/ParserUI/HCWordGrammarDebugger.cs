using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Xml;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	public class HCWordGrammarDebugger : WordGrammarDebugger
	{
		public HCWordGrammarDebugger()
		{}

		public HCWordGrammarDebugger(Mediator mediator, XmlDocument parseResult)
			: base(mediator)
		{
			m_parseResult = parseResult;
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
		protected override void CreateMorphAffixAlloFeatsXmlElement(XmlNode node, XmlNode morphNode)
		{
			XmlNode alloid = node.SelectSingleNode("@alloid");
			if (alloid != null)
			{
				XmlNode affixAlloFeatsNode = m_parseResult.SelectSingleNode("//Morph[MoForm/@DbRef='" + alloid.InnerText + "']/affixAlloFeats");
				if (affixAlloFeatsNode != null)
				{
					morphNode.InnerXml += affixAlloFeatsNode.OuterXml;
				}
			}
		}
		protected override void CreateMorphShortNameXmlElement(XmlNode node, XmlNode morphNode)
		{
			XmlNode formNode = node.SelectSingleNode("shortName");
			if (formNode != null)
				morphNode.InnerXml = "<shortName>" + formNode.InnerXml + "</shortName>";
			else
			{
				XmlNode alloFormNode = node.SelectSingleNode("alloform");
				XmlNode glossNode = node.SelectSingleNode("gloss");
				XmlNode citationFormNode = node.SelectSingleNode("citationForm");
				if (alloFormNode != null && glossNode != null && citationFormNode != null)
				morphNode.InnerXml = "<shortName>" + alloFormNode.InnerXml + " (" + glossNode.InnerText + "): " + citationFormNode.InnerText + "</shortName>";
			}
		}
	}
}
