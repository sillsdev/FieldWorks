using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a simple XML representation of HC objects. It writes out the results to the provided
	/// XML writer.
	/// </summary>
	public class XmlOutput : IOutput
	{
		protected XmlWriter m_xmlWriter;

		public XmlOutput(XmlWriter writer)
		{
			m_xmlWriter = writer;
		}

		public XmlWriter XmlWriter
		{
			get
			{
				return m_xmlWriter;
			}
		}

		public virtual void MorphAndLookupWord(Morpher morpher, string word, bool prettyPrint, bool printTraceInputs)
		{
			m_xmlWriter.WriteStartElement("MorphAndLookupWord");
			m_xmlWriter.WriteElementString("Input", word);
			WordAnalysisTrace trace;
			try
			{
				ICollection<WordSynthesis> results = morpher.MorphAndLookupWord(word, out trace);
				m_xmlWriter.WriteStartElement("Output");
				foreach (WordSynthesis ws in results)
					Write(ws, prettyPrint);
				m_xmlWriter.WriteEndElement();

				if (morpher.IsTracing)
				{
					Write(trace, prettyPrint, printTraceInputs);
				}
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

		public virtual void Write(Trace trace, bool prettyPrint, bool printTraceInputs)
		{
			m_xmlWriter.WriteStartElement("Trace");
			Write(trace, printTraceInputs);
			m_xmlWriter.WriteEndElement();
		}

		protected virtual void Write(Trace trace, bool printTraceInputs)
		{
			switch (trace.Type)
			{
				case Trace.TraceType.WORD_ANALYSIS:
					WordAnalysisTrace waTrace = trace as WordAnalysisTrace;
					m_xmlWriter.WriteStartElement(waTrace.GetType().Name);
					m_xmlWriter.WriteElementString("InputWord", waTrace.InputWord);
					break;

				case Trace.TraceType.STRATUM_ANALYSIS:
					StratumAnalysisTrace saTrace = trace as StratumAnalysisTrace;
					m_xmlWriter.WriteStartElement(saTrace.GetType().Name + (saTrace.IsInput ? "In" : "Out"));
					Write("Stratum", saTrace.Stratum);
					Write(saTrace.IsInput ? "Input" : "Output", saTrace.Analysis);
					break;

				case Trace.TraceType.STRATUM_SYNTHESIS:
					StratumSynthesisTrace ssTrace = trace as StratumSynthesisTrace;
					m_xmlWriter.WriteStartElement(ssTrace.GetType().Name + (ssTrace.IsInput ? "In" : "Out"));
					Write("Stratum", ssTrace.Stratum);
					Write(ssTrace.IsInput ? "Input" : "Output", ssTrace.Synthesis);
					break;

				case Trace.TraceType.LEX_LOOKUP:
					LexLookupTrace llTrace = trace as LexLookupTrace;
					m_xmlWriter.WriteStartElement(llTrace.GetType().Name);
					m_xmlWriter.WriteElementString("Stratum", llTrace.Stratum.Description);
					m_xmlWriter.WriteElementString("Shape", llTrace.Stratum.CharacterDefinitionTable.ToRegexString(llTrace.Shape,
						ModeType.ANALYSIS, true));
					break;

				case Trace.TraceType.WORD_SYNTHESIS:
					WordSynthesisTrace wsTrace = trace as WordSynthesisTrace;
					m_xmlWriter.WriteStartElement(wsTrace.GetType().Name);
					Write("RootAllomorph", wsTrace.RootAllomorph);
					m_xmlWriter.WriteStartElement("MorphologicalRules");
					foreach (MorphologicalRule rule in wsTrace.MorphologicalRules)
						Write("MorphologicalRule", rule);
					m_xmlWriter.WriteEndElement(); // MorphologicalRules
					m_xmlWriter.WriteElementString("RealizationalFeatures", wsTrace.RealizationalFeatures.ToString());
					break;

				case Trace.TraceType.PHONOLOGICAL_RULE_ANALYSIS:
					PhonologicalRuleAnalysisTrace paTrace = trace as PhonologicalRuleAnalysisTrace;
					m_xmlWriter.WriteStartElement(paTrace.GetType().Name);
					Write("PhonologicalRule", paTrace.Rule);
					if (printTraceInputs)
						Write("Input", paTrace.Input);
					Write("Output", paTrace.Output);
					break;

				case Trace.TraceType.PHONOLOGICAL_RULE_SYNTHESIS:
					PhonologicalRuleSynthesisTrace psTrace = trace as PhonologicalRuleSynthesisTrace;
					m_xmlWriter.WriteStartElement(psTrace.GetType().Name);
					Write("PhonologicalRule", psTrace.Rule);
					if (printTraceInputs)
						Write("Input", psTrace.Input);
					Write("Output", psTrace.Output);
					break;

				case Trace.TraceType.TEMPLATE_ANALYSIS:
					TemplateAnalysisTrace taTrace = trace as TemplateAnalysisTrace;
					m_xmlWriter.WriteStartElement(taTrace.GetType().Name + (taTrace.IsInput ? "In" : "Out"));
					Write("AffixTemplate", taTrace.Template);
					Write(taTrace.IsInput ? "Input" : "Output", taTrace.Analysis);
					break;

				case Trace.TraceType.TEMPLATE_SYNTHESIS:
					TemplateSynthesisTrace tsTrace = trace as TemplateSynthesisTrace;
					m_xmlWriter.WriteStartElement(tsTrace.GetType().Name + (tsTrace.IsInput ? "In" : "Out"));
					Write("AffixTemplate", tsTrace.Template);
					Write(tsTrace.IsInput ? "Input" : "Output", tsTrace.Synthesis);
					break;

				case Trace.TraceType.MORPHOLOGICAL_RULE_ANALYSIS:
					MorphologicalRuleAnalysisTrace maTrace = trace as MorphologicalRuleAnalysisTrace;
					m_xmlWriter.WriteStartElement(maTrace.GetType().Name);
					Write("MorphologicalRule", maTrace.Rule);
					if (maTrace.RuleAllomorph != null)
						Write("RuleAllomorph", maTrace.RuleAllomorph);
					if (printTraceInputs)
						Write("Input", maTrace.Input);
					Write("Output", maTrace.Output);
					break;

				case Trace.TraceType.MORPHOLOGICAL_RULE_SYNTHESIS:
					MorphologicalRuleSynthesisTrace msTrace = trace as MorphologicalRuleSynthesisTrace;
					m_xmlWriter.WriteStartElement(msTrace.GetType().Name);
					Write("MorphologicalRule", msTrace.Rule);
					if (msTrace.RuleAllomorph != null)
						Write("RuleAllomorph", msTrace.RuleAllomorph);
					if (printTraceInputs)
						Write("Input", msTrace.Input);
					Write("Output", msTrace.Output);
					break;

				case Trace.TraceType.BLOCKING:
					BlockingTrace bTrace = trace as BlockingTrace;
					m_xmlWriter.WriteStartElement(bTrace.GetType().Name);
					Write("BlockingEntry", bTrace.BlockingEntry);
					break;

				case Trace.TraceType.REPORT_SUCCESS:
					ReportSuccessTrace rsTrace = trace as ReportSuccessTrace;
					m_xmlWriter.WriteStartElement(rsTrace.GetType().Name);
					Write("Result", rsTrace.Output);
					break;
			}
			foreach (Trace child in trace.Children)
				Write(child, printTraceInputs);
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

		protected virtual void Write(string localName, WordAnalysis wa)
		{
			m_xmlWriter.WriteElementString(localName, wa == null ? HCStrings.kstidTraceNoOutput
				: wa.Stratum.CharacterDefinitionTable.ToRegexString(wa.Shape, ModeType.ANALYSIS, true));
		}

		protected virtual void Write(string localName, WordSynthesis ws)
		{
			m_xmlWriter.WriteElementString(localName, ws == null ? HCStrings.kstidTraceNoOutput
				: ws.Stratum.CharacterDefinitionTable.ToString(ws.Shape, ModeType.SYNTHESIS, true));
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

		protected virtual void Write(string localName, HCObject obj)
		{
			m_xmlWriter.WriteStartElement(localName);
			m_xmlWriter.WriteAttributeString("id", obj.ID);
			m_xmlWriter.WriteElementString("Description", obj.Description);
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
