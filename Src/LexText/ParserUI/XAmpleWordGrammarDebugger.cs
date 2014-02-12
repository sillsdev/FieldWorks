using System.Xml;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	public class XAmpleWordGrammarDebugger : WordGrammarDebugger
	{
		public XAmpleWordGrammarDebugger(Mediator mediator, XmlDocument parseResult)
			: base(mediator)
		{
			m_parseResult = parseResult;
		}

		protected override void CreateMorphNodes(XmlWriter writer, string nodeId)
		{
			string s = "//failure[@id=\"" + nodeId + "\"]/ancestor::parseNode[morph][1]/morph";
			XmlNode node = m_parseResult.SelectSingleNode(s);
			if (node != null)
				CreateMorphNode(writer, node);
		}

		private void CreateMorphNode(XmlWriter writer, XmlNode node)
		{
			// get the <morph> element closest up the chain to node
			XmlNode morph = node.SelectSingleNode("../ancestor::parseNode[morph][1]/morph");
			if (morph != null)
				CreateMorphNode(writer, morph);
			CreateMorphXmlElement(writer, node);
		}

		protected override void CreateMorphAffixAlloFeatsXmlElement(XmlWriter writer, XmlNode node)
		{
			XmlNode affixAlloFeatsNode = node.SelectSingleNode("affixAlloFeats");
			if (affixAlloFeatsNode != null)
			{
				writer.WriteStartElement("affixAlloFeats");
				writer.WriteRaw(affixAlloFeatsNode.InnerXml);
				writer.WriteEndElement();
			}
		}

		protected override void CreateMorphShortNameXmlElement(XmlWriter writer, XmlNode node)
		{
			XmlNode formNode = node.SelectSingleNode("shortName");
			if (formNode != null)
				writer.WriteElementString("shortName", formNode.InnerText);
		}
	}
}
