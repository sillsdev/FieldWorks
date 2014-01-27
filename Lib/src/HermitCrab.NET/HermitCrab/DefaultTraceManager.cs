using System.Collections.Generic;

namespace SIL.HermitCrab
{
	public class DefaultTraceManager : TraceManager
	{
		private readonly List<WordAnalysisTrace> m_traces;

		private PhonologicalRuleAnalysisTrace m_currentAnalysisPruleTrace;
		private PhonologicalRuleSynthesisTrace m_currentSynthesisPruleTrace;

		public DefaultTraceManager()
		{
			m_traces = new List<WordAnalysisTrace>();
		}

		public IEnumerable<WordAnalysisTrace> WordAnalysisTraces
		{
			get { return m_traces; }
		}

		public void Reset()
		{
			m_traces.Clear();
		}

		public override void BeginAnalyzeWord(string inputWord, WordAnalysis input)
		{
			if (IsTracing)
			{
				var trace = new WordAnalysisTrace(inputWord, input.Shape.Clone());
				m_traces.Add(trace);
				input.CurrentTraceObject = trace;
			}
		}

		public override void BeginUnapplyStratum(Stratum stratum, WordAnalysis input)
		{
			if (TraceStrataAnalysis)
				((Trace) input.CurrentTraceObject).AddChild(new StratumAnalysisTrace(stratum, true, input.Clone()));
		}

		public override void EndUnapplyStratum(Stratum stratum, WordAnalysis output)
		{
			if (TraceStrataAnalysis)
				((Trace) output.CurrentTraceObject).AddChild(new StratumAnalysisTrace(stratum, false, output.Clone()));
		}

		public override void BeginUnapplyPhonologicalRule(PhonologicalRule rule, WordAnalysis input)
		{
			if (IsAnalysisTracingEnabled(rule.ID))
			{
				m_currentAnalysisPruleTrace = new PhonologicalRuleAnalysisTrace(rule, input.Clone());
				((Trace) input.CurrentTraceObject).AddChild(m_currentAnalysisPruleTrace);
			}
		}

		public override void EndUnapplyPhonologicalRule(PhonologicalRule rule, WordAnalysis output)
		{
			if (m_currentAnalysisPruleTrace != null)
			{
				m_currentAnalysisPruleTrace.Output = output.Clone();
				m_currentAnalysisPruleTrace = null;
			}
		}

		public override void BeginUnapplyTemplate(AffixTemplate template, WordAnalysis input)
		{
			if (TraceTemplatesAnalysis)
				((Trace) input.CurrentTraceObject).AddChild(new TemplateAnalysisTrace(template, true, input.Clone()));
		}

		public override void EndUnapplyTemplate(AffixTemplate template, WordAnalysis output, bool unapplied)
		{
			if (TraceTemplatesAnalysis)
				((Trace) output.CurrentTraceObject).AddChild(new TemplateAnalysisTrace(template, false, unapplied ? output.Clone() : null));
		}

		public override void MorphologicalRuleUnapplied(MorphologicalRule rule, WordAnalysis input, WordAnalysis output, Allomorph allomorph)
		{
			if (IsAnalysisTracingEnabled(rule.ID))
			{
				// create the morphological rule analysis trace record for each output analysis
				var trace = new MorphologicalRuleAnalysisTrace(rule, input.Clone()) {RuleAllomorph = allomorph, Output = output.Clone()};
				((Trace) output.CurrentTraceObject).AddChild(trace);
				// set current trace record to the morphological rule trace record for each
				// output analysis
				output.CurrentTraceObject = trace;
			}
		}

		public override void MorphologicalRuleNotUnapplied(MorphologicalRule rule, WordAnalysis input)
		{
			if (IsAnalysisTracingEnabled(rule.ID))
			{
				// create the morphological rule analysis trace record for a rule that did not succesfully unapply
				((Trace) input.CurrentTraceObject).AddChild(new MorphologicalRuleAnalysisTrace(rule, input.Clone()));
			}
		}

		public override void LexLookup(Stratum stratum, WordAnalysis input)
		{
			if (TraceLexLookup)
			{
				// create lexical lookup trace record
				var trace = new LexLookupTrace(stratum, input.Shape.Clone());
				((Trace) input.CurrentTraceObject).AddChild(trace);
			}
		}

		public override void BeginSynthesizeWord(WordAnalysis input)
		{
			if (TraceLexLookup)
			{
				var trace = new WordSynthesisTrace(input.RootAllomorph, input.UnappliedMorphologicalRules, input.RealizationalFeatures.Clone());
				var curTrace = (Trace) input.CurrentTraceObject;
				Trace lookupTrace = curTrace.GetChildAt(curTrace.ChildCount - 1);
				lookupTrace.AddChild(trace);
				input.CurrentTraceObject = trace;
			}
		}

		public override void BeginApplyStratum(Stratum stratum, WordSynthesis input)
		{
			if (TraceStrataSynthesis)
				((Trace) input.CurrentTraceObject).AddChild(new StratumSynthesisTrace(stratum, true, input.Clone()));
		}

		public override void EndApplyStratum(Stratum stratum, WordSynthesis output)
		{
			if (TraceStrataSynthesis)
				((Trace) output.CurrentTraceObject).AddChild(new StratumSynthesisTrace(stratum, false, output.Clone()));
		}

		public override void BeginApplyPhonologicalRule(PhonologicalRule rule, WordSynthesis input)
		{
			if (IsSynthesisTracingEnabled(rule.ID))
			{
				m_currentSynthesisPruleTrace = new PhonologicalRuleSynthesisTrace(rule, input.Clone());
				((Trace) input.CurrentTraceObject).AddChild(m_currentSynthesisPruleTrace);
			}
		}

		public override void PhonologicalRuleNotApplicablePOS(WordSynthesis input, HCObjectSet<PartOfSpeech> requiredPOSs)
		{
			if (m_currentSynthesisPruleTrace != null)
				m_currentSynthesisPruleTrace.AddChild(new PhonologicalRuleSynthesisRequiredPOSTrace(input.POS, requiredPOSs));
		}

		public override void PhonologicalRuleNotApplicableMPRFeatures(MPRFeaturesType type, WordSynthesis input, MPRFeatureSet mprFeatures)
		{
			if (m_currentSynthesisPruleTrace != null)
				m_currentSynthesisPruleTrace.AddChild(new PhonologicalRuleSynthesisMPRFeaturesTrace(type, input.MPRFeatures, mprFeatures));
		}

		public override void EndApplyPhonologicalRule(PhonologicalRule rule, WordSynthesis output)
		{
			if (m_currentSynthesisPruleTrace != null)
			{
				m_currentSynthesisPruleTrace.Output = output.Clone();
				m_currentSynthesisPruleTrace = null;
			}
		}

		public override void BeginApplyTemplate(AffixTemplate template, WordSynthesis input)
		{
			if (TraceTemplatesSynthesis)
				((Trace) input.CurrentTraceObject).AddChild(new TemplateSynthesisTrace(template, true, input.Clone()));
		}

		public override void EndApplyTemplate(AffixTemplate template, WordSynthesis output, bool applied)
		{
			if (TraceTemplatesSynthesis)
				((Trace) output.CurrentTraceObject).AddChild(new TemplateSynthesisTrace(template, false, applied ? output.Clone() : null));
		}

		public override void MorphologicalRuleApplied(MorphologicalRule rule, WordSynthesis input, WordSynthesis output, Allomorph allomorph)
		{
			if (IsSynthesisTracingEnabled(rule.ID))
			{
				var trace = new MorphologicalRuleSynthesisTrace(rule, input.Clone()) {RuleAllomorph = allomorph, Output = output.Clone()};
				((Trace) output.CurrentTraceObject).AddChild(trace);
				// set current trace record to the morphological rule trace record for each
				// output analysis
				output.CurrentTraceObject = trace;
			}
		}

		public override void MorphologicalRuleNotApplied(MorphologicalRule rule, WordSynthesis input)
		{
			if (IsSynthesisTracingEnabled(rule.ID))
				((Trace) input.CurrentTraceObject).AddChild(new MorphologicalRuleSynthesisTrace(rule, input.Clone()));
		}

		public override void MorphCooccurrenceRuleFailed(MorphCoOccurrence cooccurrence, string usage, WordSynthesis input)
		{
			if (TraceTemplatesSynthesis)
			{
				var trace = new MorphCoOccurrenceTrace(cooccurrence, usage);
				((Trace)input.CurrentTraceObject).AddChild(trace);

			}
		}

		public override void Blocking(BlockType blockingType, WordSynthesis input, LexEntry blockingEntry)
		{
			if (TraceBlocking)
				// create blocking trace record, should this become the current trace?
				((Trace) input.CurrentTraceObject).AddChild(new BlockingTrace(BlockingTrace.BlockType.TEMPLATE, blockingEntry));
		}

		public override void ReportSuccess(WordSynthesis output)
		{
			if (TraceSuccess)
				((Trace) output.CurrentTraceObject).AddChild(new ReportSuccessTrace(output));
		}
	}
}
