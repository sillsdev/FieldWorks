using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	public class XAmpleWordGrammarDebugger : WordGrammarDebugger
	{
		public XAmpleWordGrammarDebugger()
		{ }

		public XAmpleWordGrammarDebugger(Mediator mediator, XmlDocument parseResult)
			: base(mediator)
		{
			m_parseResult = parseResult;
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
}
