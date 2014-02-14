using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	public class HCWordGrammarDebugger : WordGrammarDebugger
	{
		public HCWordGrammarDebugger(Mediator mediator, XDocument parseResult)
			: base(mediator)
		{
			m_parseResult = parseResult;
		}

		protected override void CreateMorphNodes(XmlWriter writer, string nodeId)
		{
			Debug.Assert(m_parseResult.Root != null);
			XElement wordGrammarAttemptElem = m_parseResult.Root.Elements("WordGrammarTrace").Elements("WordGrammarAttempt").FirstOrDefault(e => ((string) e.Element("Id")) == nodeId);
			if (wordGrammarAttemptElem != null)
			{
				foreach (XElement morphElem in wordGrammarAttemptElem.Elements("morph"))
					morphElem.WriteTo(writer);
			}
		}
	}
}
