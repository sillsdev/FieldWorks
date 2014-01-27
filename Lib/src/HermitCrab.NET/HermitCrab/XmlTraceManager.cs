using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SIL.HermitCrab
{
	public class XmlTraceManager : TraceManager
	{
		private readonly List<XElement> m_traces;

		private XElement m_currentAnalysisPruleTrace;
		private XElement m_currentSynthesisPruleTrace;

		public XmlTraceManager()
		{
			m_traces = new List<XElement>();
		}

		public IEnumerable<XElement> WordAnalysisTraces
		{
			get { return m_traces; }
		}

		public bool WriteInputs { get; set; }

		public void Reset()
		{
			m_traces.Clear();
		}

		public override void BeginAnalyzeWord(string inputWord, WordAnalysis input)
		{
			if (IsTracing)
			{
				var trace = new XElement("WordAnalysisTrace", new XElement("InputWord", inputWord));
				m_traces.Add(trace);
				input.CurrentTraceObject = trace;
			}
		}

		public override void BeginUnapplyStratum(Stratum stratum, WordAnalysis input)
		{
			if (TraceStrataAnalysis)
			{
				((XElement) input.CurrentTraceObject).Add(new XElement("StratumAnalysisTraceIn",
					Write("Stratum", stratum),
					Write("Input", input)));
			}
		}

		public override void EndUnapplyStratum(Stratum stratum, WordAnalysis output)
		{
			if (TraceStrataAnalysis)
			{
				((XElement) output.CurrentTraceObject).Add(new XElement("StratumAnalysisTraceOut",
					Write("Stratum", stratum),
					Write("Output", output)));
			}
		}

		public override void BeginUnapplyPhonologicalRule(PhonologicalRule rule, WordAnalysis input)
		{
			if (IsAnalysisTracingEnabled(rule.ID))
			{
				m_currentAnalysisPruleTrace = new XElement("PhonologicalRuleAnalysisTrace",
					Write("PhonologicalRule", rule));
				if (WriteInputs)
					m_currentAnalysisPruleTrace.Add(Write("Input", input));
				((XElement) input.CurrentTraceObject).Add(m_currentAnalysisPruleTrace);
			}
		}

		public override void EndUnapplyPhonologicalRule(PhonologicalRule rule, WordAnalysis output)
		{
			if (m_currentAnalysisPruleTrace != null)
			{
				m_currentAnalysisPruleTrace.Add(Write("Output", output));
				m_currentAnalysisPruleTrace = null;
			}
		}

		public override void BeginUnapplyTemplate(AffixTemplate template, WordAnalysis input)
		{
			if (TraceTemplatesAnalysis)
			{
				((XElement) input.CurrentTraceObject).Add(new XElement("TemplateAnalysisTraceIn",
					Write("AffixTemplate", template),
					Write("Input", input)));
			}
		}

		public override void EndUnapplyTemplate(AffixTemplate template, WordAnalysis output, bool unapplied)
		{
			if (TraceTemplatesAnalysis)
			{
				((XElement) output.CurrentTraceObject).Add(new XElement("TemplateAnalysisTraceOut",
					Write("AffixTemplate", template),
					Write("Output", unapplied ? output : null)));
			}
		}

		public override void MorphologicalRuleUnapplied(MorphologicalRule rule, WordAnalysis input, WordAnalysis output, Allomorph allomorph)
		{
			if (IsAnalysisTracingEnabled(rule.ID))
			{
				var trace = new XElement("MorphologicalRuleAnalysisTrace",
					Write("MorphologicalRule", rule));
				if (allomorph != null)
					trace.Add(Write("RuleAllomorph", allomorph));
				if (WriteInputs)
					trace.Add(Write("Input", input));
				trace.Add(Write("Output", output));
				((XElement) output.CurrentTraceObject).Add(trace);
				output.CurrentTraceObject = trace;
			}
		}

		public override void MorphologicalRuleNotUnapplied(MorphologicalRule rule, WordAnalysis input)
		{
			if (IsAnalysisTracingEnabled(rule.ID))
			{
				var trace = new XElement("MorphologicalRuleAnalysisTrace",
					Write("MorphologicalRule", rule));
				if (WriteInputs)
					trace.Add(Write("Input", input));
				((XElement) input.CurrentTraceObject).Add(trace);
			}
		}

		public override void LexLookup(Stratum stratum, WordAnalysis input)
		{
			if (TraceLexLookup)
			{
				var trace = new XElement("LexLookupTrace",
					new XElement("Stratum", stratum.Description),
					new XElement("Shape", stratum.CharacterDefinitionTable.ToRegexString(input.Shape, ModeType.ANALYSIS, true)));
				((XElement) input.CurrentTraceObject).Add(trace);
			}
		}

		public override void BeginSynthesizeWord(WordAnalysis input)
		{
			if (TraceLexLookup)
			{
				var trace = new XElement("WordSynthesisTrace",
					Write("RootAllomorph", input.RootAllomorph),
					new XElement("MorphologicalRules", input.UnappliedMorphologicalRules.Select(r => Write("MorphologicalRule", r))),
					new XElement("RealizationalFeatures", input.RealizationalFeatures.ToString()));
				var curTrace = (XElement) input.CurrentTraceObject;
				var lookupTrace = (XElement) curTrace.LastNode;
				lookupTrace.Add(trace);
				input.CurrentTraceObject = trace;
			}
		}

		public override void BeginApplyStratum(Stratum stratum, WordSynthesis input)
		{
			if (TraceStrataSynthesis)
			{
				((XElement) input.CurrentTraceObject).Add(new XElement("StratumSynthesisTraceIn",
					Write("Stratum", stratum),
					Write("Input", input)));
			}
		}

		public override void EndApplyStratum(Stratum stratum, WordSynthesis output)
		{
			if (TraceStrataSynthesis)
			{
				((XElement) output.CurrentTraceObject).Add(new XElement("StratumSynthesisTraceOut",
					Write("Stratum", stratum),
					Write("Output", output)));
			}
		}

		public override void BeginApplyPhonologicalRule(PhonologicalRule rule, WordSynthesis input)
		{
			if (IsSynthesisTracingEnabled(rule.ID))
			{
				m_currentSynthesisPruleTrace = new XElement("PhonologicalRuleSynthesisTrace",
					Write("PhonologicalRule", rule));
				if (WriteInputs)
					m_currentSynthesisPruleTrace.Add(Write("Input", input));
				((XElement) input.CurrentTraceObject).Add(m_currentSynthesisPruleTrace);
			}
		}

		public override void PhonologicalRuleNotApplicablePOS(WordSynthesis input, HCObjectSet<PartOfSpeech> requiredPOSs)
		{
			if (m_currentSynthesisPruleTrace != null)
			{
				m_currentSynthesisPruleTrace.Add(new XElement("PhonologicalRuleSynthesisRequiredPOSTrace",
					Write("PhonologicalRuleStemPOS", input.POS),
					new XElement("PhonologicalRuleRequiredPOSes", requiredPOSs.Select(pos => Write("PhonologicalRuleRequiredPOS", pos)))));
			}
		}

		public override void PhonologicalRuleNotApplicableMPRFeatures(MPRFeaturesType type, WordSynthesis input, MPRFeatureSet mprFeatures)
		{
			if (m_currentSynthesisPruleTrace != null)
			{
				m_currentSynthesisPruleTrace.Add(new XElement("PhonologicalRuleSynthesisMPRFeaturesTrace", new XAttribute("type", type == MPRFeaturesType.EXCLUDED ? "excluded" : "required"),
					new XElement("PhonologicalRuleMPRFeatures", input.MPRFeatures.Select(f => Write("PhonologicalRuleMPRFeature", f))),
					new XElement("PhonologicalRuleConstrainingMPRFeatrues", mprFeatures.Select(f => Write("PhonologicalRuleMPRFeature", f)))));
			}
		}

		public override void EndApplyPhonologicalRule(PhonologicalRule rule, WordSynthesis output)
		{
			if (m_currentSynthesisPruleTrace != null)
			{
				m_currentSynthesisPruleTrace.Add(Write("Output", output));
				m_currentSynthesisPruleTrace = null;
			}
		}

		public override void BeginApplyTemplate(AffixTemplate template, WordSynthesis input)
		{
			if (TraceTemplatesSynthesis)
			{
				((XElement) input.CurrentTraceObject).Add(new XElement("TemplateSynthesisTraceIn",
					Write("AffixTemplate", template),
					Write("Input", input)));
			}
		}

		public override void EndApplyTemplate(AffixTemplate template, WordSynthesis output, bool applied)
		{
			if (TraceTemplatesSynthesis)
			{
				((XElement) output.CurrentTraceObject).Add(new XElement("TemplateSynthesisTraceOut",
					Write("AffixTemplate", template),
					Write("Output", applied ? output : null)));
			}
		}

		public override void MorphologicalRuleApplied(MorphologicalRule rule, WordSynthesis input, WordSynthesis output, Allomorph allomorph)
		{
			if (IsSynthesisTracingEnabled(rule.ID))
			{
				var trace = new XElement("MorphologicalRuleSynthesisTrace",
					Write("MorphologicalRule", rule));
				if (allomorph != null)
					trace.Add(Write("RuleAllomorph", allomorph));
				if (WriteInputs)
					trace.Add(Write("Input", input));
				trace.Add(Write("Output", output));
				((XElement) output.CurrentTraceObject).Add(trace);
				output.CurrentTraceObject = trace;
			}
		}

		public override void MorphologicalRuleNotApplied(MorphologicalRule rule, WordSynthesis input)
		{
			if (IsSynthesisTracingEnabled(rule.ID))
			{
				var trace = new XElement("MorphologicalRuleSynthesisTrace",
					Write("MorphologicalRule", rule));
				if (WriteInputs)
					trace.Add(Write("Input", input));
				((XElement) input.CurrentTraceObject).Add(trace);
			}
		}

		public override void MorphCooccurrenceRuleFailed(MorphCoOccurrence cooccurrence, string usage, WordSynthesis input)
		{
			if (TraceTemplatesSynthesis)
			{
				((XElement)input.CurrentTraceObject).Add(Write("MorphCooccurrenceRuleFailed", cooccurrence, usage));
			}
		}

		public override void Blocking(BlockType blockingType, WordSynthesis input, LexEntry blockingEntry)
		{
			if (TraceBlocking)
				((XElement) input.CurrentTraceObject).Add(new XElement("BlockingTrace", Write("BlockingEntry", blockingEntry)));
		}

		public override void ReportSuccess(WordSynthesis output)
		{
			if (TraceSuccess)
				((XElement) output.CurrentTraceObject).Add(new XElement("ReportSuccessTrace", Write("Result", output)));
		}

		protected virtual XElement Write(string name, Morpheme morpheme)
		{
			XElement elem = Write(name, (HCObject) morpheme);
			if (morpheme.Gloss != null)
				elem.Add(new XElement("Gloss", morpheme.Gloss.Description));
			return elem;
		}

		protected virtual XElement Write(string name, Allomorph allomorph)
		{
			XElement elem = Write(name, (HCObject) allomorph);
			elem.Add(Write("Morpheme", allomorph.Morpheme));
			elem.Add(new XElement("Properties", allomorph.Properties.Select(prop => new XElement("Property", new XElement("Key", prop.Key), new XElement("Value", prop.Value)))));
			return elem;
		}

		protected virtual XElement Write(string name, HCObject obj)
		{
			return new XElement(name, new XAttribute("id", obj.ID), new XElement("Description", obj.Description));
		}

		protected virtual XElement Write(string name, WordAnalysis wa)
		{
			return new XElement(name, wa == null ? HCStrings.kstidTraceNoOutput
				: wa.Stratum.CharacterDefinitionTable.ToRegexString(wa.Shape, ModeType.ANALYSIS, true));
		}

		protected virtual XElement Write(string name, WordSynthesis ws)
		{
			return new XElement(name, ws == null ? HCStrings.kstidTraceNoOutput
				: ws.Stratum.CharacterDefinitionTable.ToString(ws.Shape, ModeType.SYNTHESIS, true));
		}

		protected virtual XElement Write(string name, MorphCoOccurrence coOccurrence, string usage)
		{
			XElement elem = new XElement(name);
			elem.Add(new XElement("Usage", usage));
			elem.Add(new XElement("Type", coOccurrence.Type));
			var others = new XElement("Others");
			foreach (var item in coOccurrence.Others)
			{
				var morpheme = item as Morpheme;
				if (morpheme != null)
				{
					others.Add(Write("Morpheme", morpheme));
					continue;
				}
				var allomorph = item as Allomorph;
				if (allomorph != null)
				{
					others.Add(Write("Allomorph", allomorph));
				}
			}
			elem.Add(others);
			elem.Add(new XElement("Adjacency", coOccurrence.Adjacency));
			return elem;
		}

	}
}
