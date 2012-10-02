using System;
using System.Collections.Generic;
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

	}
}
