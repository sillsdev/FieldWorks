using System.Diagnostics.CodeAnalysis;
using System.Xml;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	public class HCWordGrammarDebugger : WordGrammarDebugger
	{
		public HCWordGrammarDebugger(Mediator mediator, XmlDocument parseResult)
			: base(mediator)
		{
			m_parseResult = parseResult;
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		protected override void CreateMorphNodes(XmlWriter writer, string nodeId)
		{
			string s = "//WordGrammarAttempt[Id=\"" + nodeId + "\"]";
			XmlNode node = m_parseResult.SelectSingleNode(s);
			if (node != null)
			{
				XmlNodeList morphs = node.SelectNodes("Morphs");
				foreach (XmlNode morph in morphs)
					CreateMorphXmlElement(writer, morph);
			}
		}

		protected override void CreateMorphAffixAlloFeatsXmlElement(XmlWriter writer, XmlNode node)
		{
			XmlNode alloid = node.SelectSingleNode("@alloid");
			if (alloid != null)
			{
				XmlNode affixAlloFeatsNode = m_parseResult.SelectSingleNode("//Morph[MoForm/@DbRef='" + alloid.InnerText + "']/affixAlloFeats");
				if (affixAlloFeatsNode != null)
				{
					writer.WriteStartElement("affixAlloFeats");
					writer.WriteRaw(affixAlloFeatsNode.InnerXml);
					writer.WriteEndElement();
				}
			}
		}

		protected override void CreateMorphShortNameXmlElement(XmlWriter writer, XmlNode node)
		{
			XmlNode formNode = node.SelectSingleNode("shortName");
			if (formNode != null)
			{
				writer.WriteElementString("shortName", formNode.InnerText);
			}
			else
			{
				XmlNode alloFormNode = node.SelectSingleNode("alloform");
				XmlNode glossNode = node.SelectSingleNode("gloss");
				XmlNode citationFormNode = node.SelectSingleNode("citationForm");
				if (alloFormNode != null && glossNode != null && citationFormNode != null)
					writer.WriteElementString("shortName", string.Format("{0} ({1}): {2}", alloFormNode.InnerText, glossNode.InnerText, citationFormNode.InnerText));
			}
		}
	}
}
