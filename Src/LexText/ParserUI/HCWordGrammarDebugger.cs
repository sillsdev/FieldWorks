using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
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

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		protected override void CreateMorphNodes(XmlWriter writer, string nodeId)
		{
			string s = "//WordGrammarAttempt[Id=\"" + nodeId + "\"]";
			XElement element = m_parseResult.XPathSelectElement(s);
			if (element != null)
			{
				foreach (XElement morph in element.Elements("Morphs"))
					CreateMorphXElement(writer, morph);
			}
		}

		protected override void CreateMorphAffixAlloFeatsXElement(XmlWriter writer, XElement element)
		{
			XElement alloid = element.XPathSelectElement("@alloid");
			if (alloid != null)
			{
				XElement affixAlloFeatsNode = m_parseResult.XPathSelectElement("//Morph[MoForm/@DbRef='" + alloid.Value + "']/affixAlloFeats");
				if (affixAlloFeatsNode != null)
					affixAlloFeatsNode.WriteTo(writer);
			}
		}

		protected override void CreateMorphShortNameXElement(XmlWriter writer, XElement element)
		{
			XElement formNode = element.XPathSelectElement("shortName");
			if (formNode != null)
			{
				writer.WriteElementString("shortName", formNode.Value);
			}
			else
			{
				XElement alloFormNode = element.XPathSelectElement("alloform");
				XElement glossNode = element.XPathSelectElement("gloss");
				XElement citationFormNode = element.XPathSelectElement("citationForm");
				if (alloFormNode != null && glossNode != null && citationFormNode != null)
					writer.WriteElementString("shortName", string.Format("{0} ({1}): {2}", alloFormNode.Value, glossNode.Value, citationFormNode.Value));
			}
		}
	}
}
