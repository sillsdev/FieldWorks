using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	public class XAmpleWordGrammarDebugger : WordGrammarDebugger
	{
		private readonly XDocument m_parseResult;

		public XAmpleWordGrammarDebugger(Mediator mediator, XDocument parseResult)
			: base(mediator)
		{
			m_parseResult = parseResult;
		}

		protected override void WriteMorphNodes(XmlWriter writer, string nodeId)
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
