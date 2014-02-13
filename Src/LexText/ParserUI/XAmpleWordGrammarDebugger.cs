using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	public class XAmpleWordGrammarDebugger : WordGrammarDebugger
	{
		public XAmpleWordGrammarDebugger(Mediator mediator, XDocument parseResult)
			: base(mediator)
		{
			m_parseResult = parseResult;
		}

		protected override void CreateMorphNodes(XmlWriter writer, string nodeId)
		{
			string s = "//failure[@id=\"" + nodeId + "\"]/ancestor::parseNode[morph][1]/morph";
			XElement node = m_parseResult.XPathSelectElement(s);
			if (node != null)
				CreateMorphNode(writer, node);
		}

		private void CreateMorphNode(XmlWriter writer, XElement element)
		{
			// get the <morph> element closest up the chain to node
			XElement morph = element.XPathSelectElement("../ancestor::parseNode[morph][1]/morph");
			if (morph != null)
				CreateMorphNode(writer, morph);
			CreateMorphXElement(writer, element);
		}

		protected override void CreateMorphAffixAlloFeatsXElement(XmlWriter writer, XElement element)
		{
			XElement affixAlloFeatsNode = element.XPathSelectElement("affixAlloFeats");
			if (affixAlloFeatsNode != null)
				affixAlloFeatsNode.WriteTo(writer);
		}

		protected override void CreateMorphShortNameXElement(XmlWriter writer, XElement element)
		{
			XElement formNode = element.XPathSelectElement("shortName");
			if (formNode != null)
				writer.WriteElementString("shortName", formNode.Value);
		}
	}
}
