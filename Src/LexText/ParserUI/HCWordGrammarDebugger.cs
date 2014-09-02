using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	public class HCWordGrammarDebugger : WordGrammarDebugger
	{
		private readonly Dictionary<string, XElement> m_attempts;

		public HCWordGrammarDebugger(Mediator mediator, XDocument parseResult)
			: base(mediator)
		{
			Debug.Assert(parseResult.Root != null);
			m_attempts = parseResult.Root.Elements("WordGrammarTrace").Elements("WordGrammarAttempt").ToDictionary(e => (string) e.Element("Id"), e => new XElement(e));
		}

		protected override void WriteMorphNodes(XmlWriter writer, string nodeId)
		{
			XElement wordGrammarAttemptElem;
			if (m_attempts.TryGetValue(nodeId, out wordGrammarAttemptElem))
			{
				foreach (XElement morphElem in wordGrammarAttemptElem.Elements("morph"))
					morphElem.WriteTo(writer);
			}
		}
	}
}
