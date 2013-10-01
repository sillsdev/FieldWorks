using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a simple XML representation of HC objects. It writes out the results to the provided
	/// XML writer.
	/// </summary>
	public class XmlOutput : IOutput
	{
		private readonly XmlWriter m_xmlWriter;
		private readonly XmlTraceManager m_trace;

		public XmlOutput(XmlWriter writer)
		{
			m_xmlWriter = writer;
			m_trace = new XmlTraceManager();
		}

		protected XmlOutput(XmlWriter writer, XmlTraceManager trace)
		{
			m_xmlWriter = writer;
			m_trace = trace;
		}

		public XmlWriter XmlWriter
		{
			get
			{
				return m_xmlWriter;
			}
		}

		public TraceManager TraceManager
		{
			get { return m_trace; }
		}

		protected XmlTraceManager XmlTraceManager
		{
			get { return m_trace; }
		}

		public virtual void MorphAndLookupWord(Morpher morpher, string word, bool prettyPrint, bool printTraceInputs)
		{
			m_xmlWriter.WriteStartElement("MorphAndLookupWord");
			m_xmlWriter.WriteElementString("Input", word);
			try
			{
				m_trace.WriteInputs = printTraceInputs;
				ICollection<WordSynthesis> results = morpher.MorphAndLookupWord(word, m_trace);
				m_xmlWriter.WriteStartElement("Output");
				foreach (WordSynthesis ws in results)
					Write(ws, prettyPrint);
				m_xmlWriter.WriteEndElement();

				if (m_trace.IsTracing)
					WriteTrace();
				m_trace.Reset();
			}
			catch (MorphException me)
			{
				Write(me);
			}
			m_xmlWriter.WriteEndElement();
		}

		public virtual void Write(WordSynthesis ws, bool prettyPrint)
		{
			m_xmlWriter.WriteStartElement("Result");
			Write("Root", ws.Root);
			m_xmlWriter.WriteElementString("POS", ws.POS.Description);

			m_xmlWriter.WriteStartElement("Morphs");
			foreach (Morph morph in ws.Morphs)
				Write("Allomorph", morph.Allomorph);
			m_xmlWriter.WriteEndElement();

			m_xmlWriter.WriteElementString("MPRFeatures", ws.MPRFeatures.ToString());
			m_xmlWriter.WriteElementString("HeadFeatures", ws.HeadFeatures.ToString());
			m_xmlWriter.WriteElementString("FootFeatures", ws.FootFeatures.ToString());

			m_xmlWriter.WriteEndElement();
		}

		protected virtual void WriteTrace()
		{
			m_xmlWriter.WriteStartElement("Trace");
			foreach (XElement trace in m_trace.WordAnalysisTraces)
				trace.WriteTo(m_xmlWriter);
			m_xmlWriter.WriteEndElement();
		}

		protected virtual void Write(string localName, Morpheme morpheme)
		{
			m_xmlWriter.WriteStartElement(localName);
			m_xmlWriter.WriteAttributeString("id", morpheme.ID);
			m_xmlWriter.WriteElementString("Description", morpheme.Description);
			if (morpheme.Gloss != null)
				m_xmlWriter.WriteElementString("Gloss", morpheme.Gloss.Description);
			m_xmlWriter.WriteEndElement();
		}

		protected virtual void Write(string localName, Allomorph allo)
		{
			m_xmlWriter.WriteStartElement(localName);
			m_xmlWriter.WriteAttributeString("id", allo.ID);
			m_xmlWriter.WriteElementString("Description", allo.Description);
			Write("Morpheme", allo.Morpheme);
			m_xmlWriter.WriteStartElement("Properties");
			foreach (KeyValuePair<string, string> prop in allo.Properties)
			{
				m_xmlWriter.WriteStartElement("Property");
				m_xmlWriter.WriteElementString("Key", prop.Key);
				m_xmlWriter.WriteElementString("Value", prop.Value);
				m_xmlWriter.WriteEndElement();
			}
			m_xmlWriter.WriteEndElement();
			m_xmlWriter.WriteEndElement();
		}

		public virtual void Write(LoadException le)
		{
			m_xmlWriter.WriteElementString("LoadError", le.Message);
		}

		public virtual void Write(MorphException me)
		{
			m_xmlWriter.WriteElementString("MorphError", me.Message);
		}

		public virtual void Flush()
		{
			m_xmlWriter.Flush();
		}

		public virtual void Close()
		{
			m_xmlWriter.Close();
		}
	}
}
