using System.Collections.Generic;

namespace SIL.HermitCrab
{
	public enum BlockType { RULE, TEMPLATE }
	public enum MPRFeaturesType { REQUIRED, EXCLUDED }

	public abstract class TraceManager
	{
		private bool m_analysisTraceAllRules;
		private bool m_synthesisTraceAllRules;
		private readonly HashSet<string> m_analysisRulesToTrace;
		private readonly HashSet<string> m_synthesisRulesToTrace;

		protected TraceManager()
		{
			m_analysisRulesToTrace = new HashSet<string>();
			m_synthesisRulesToTrace = new HashSet<string>();
		}


		/// <summary>
		/// Gets a value indicating whether this morpher is tracing.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this morpher is tracing, otherwise <c>false</c>.
		/// </value>
		public bool IsTracing
		{
			get
			{
				return TraceStrataAnalysis || TraceStrataSynthesis || TraceTemplatesAnalysis || TraceTemplatesSynthesis
					|| TraceLexLookup || TraceBlocking || TraceSuccess || m_analysisTraceAllRules || m_synthesisTraceAllRules
					|| m_analysisRulesToTrace.Count > 0 || m_synthesisRulesToTrace.Count > 0;
			}
		}

		/// <summary>
		/// Turns tracing on and off for all parts of the morpher.
		/// </summary>
		/// <value><c>true</c> to turn tracing on, <c>false</c> to turn tracing off.</value>
		public bool TraceAll
		{
			set
			{
				TraceStrataAnalysis = value;
				TraceStrataSynthesis = value;
				TraceTemplatesAnalysis = value;
				TraceTemplatesSynthesis = value;
				TraceLexLookup = value;
				TraceBlocking = value;
				TraceSuccess = value;
				SetTraceRules(value, value);
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether tracing of strata during analysis is
		/// on or off.
		/// </summary>
		/// <value><c>true</c> if tracing is on, <c>false</c> if tracing is off.</value>
		public bool TraceStrataAnalysis { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether tracing of strata during synthesis is
		/// on or off.
		/// </summary>
		/// <value><c>true</c> if tracing is on, <c>false</c> if tracing is off.</value>
		public bool TraceStrataSynthesis { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether tracing of affix templates during analysis
		/// is on or off.
		/// </summary>
		/// <value><c>true</c> if tracing is on, <c>false</c> if tracing is off.</value>
		public bool TraceTemplatesAnalysis { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether tracing of affix templates during synthesis
		/// is on or off.
		/// </summary>
		/// <value><c>true</c> if tracing is on, <c>false</c> if tracing is off.</value>
		public bool TraceTemplatesSynthesis { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether tracing of lexical lookup is
		/// on or off.
		/// </summary>
		/// <value><c>true</c> if tracing is on, <c>false</c> if tracing is off.</value>
		public bool TraceLexLookup { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether tracing of blocking is
		/// on or off.
		/// </summary>
		/// <value><c>true</c> if tracing is on, <c>false</c> if tracing is off.</value>
		public bool TraceBlocking { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether tracing of successful parses is
		/// on or off.
		/// </summary>
		/// <value><c>true</c> if tracing is on, <c>false</c> if tracing is off.</value>
		public bool TraceSuccess { get; set; }

		/// <summary>
		/// Turns tracing of all rules on or off.
		/// </summary>
		/// <param name="traceAnalysis"><c>true</c> if tracing during analysis is on, <c>false</c>
		/// if tracing during analysis is off.</param>
		/// <param name="traceSynthesis"><c>true</c> if tracing during synthesis is on, <c>false</c>
		/// if tracing during synthesis is off.</param>
		public void SetTraceRules(bool traceAnalysis, bool traceSynthesis)
		{
			m_analysisRulesToTrace.Clear();
			m_analysisTraceAllRules = traceAnalysis;
			m_synthesisRulesToTrace.Clear();
			m_synthesisTraceAllRules = traceSynthesis;
		}

		/// <summary>
		/// Turns tracing of a rule on or off.
		/// </summary>
		/// <param name="id">The rule ID.</param>
		/// <param name="traceAnalysis"><c>true</c> if tracing during analysis is on, <c>false</c>
		/// if tracing during analysis is off.</param>
		/// <param name="traceSynthesis"><c>true</c> if tracing during synthesis is on, <c>false</c>
		/// if tracing during synthesis is off.</param>
		public void SetTraceRule(string id, bool traceAnalysis, bool traceSynthesis)
		{
			if (traceAnalysis)
				m_analysisRulesToTrace.Add(id);
			else
				m_analysisRulesToTrace.Remove(id);

			if (traceSynthesis)
				m_synthesisRulesToTrace.Add(id);
			else
				m_synthesisRulesToTrace.Remove(id);
		}

		protected bool IsAnalysisTracingEnabled(string id)
		{
			return m_analysisTraceAllRules || m_analysisRulesToTrace.Contains(id);
		}

		protected bool IsSynthesisTracingEnabled(string id)
		{
			return m_synthesisTraceAllRules || m_synthesisRulesToTrace.Contains(id);
		}

		public abstract void BeginAnalyzeWord(string inputWord, WordAnalysis input);

		public abstract void BeginUnapplyStratum(Stratum stratum, WordAnalysis input);
		public abstract void EndUnapplyStratum(Stratum stratum, WordAnalysis output);

		public abstract void BeginUnapplyPhonologicalRule(PhonologicalRule rule, WordAnalysis input);
		public abstract void EndUnapplyPhonologicalRule(PhonologicalRule rule, WordAnalysis output);

		public abstract void BeginUnapplyTemplate(AffixTemplate template, WordAnalysis input);
		public abstract void EndUnapplyTemplate(AffixTemplate template, WordAnalysis output, bool unapplied);

		public abstract void MorphologicalRuleUnapplied(MorphologicalRule rule, WordAnalysis input, WordAnalysis output, Allomorph allomorph);
		public abstract void MorphologicalRuleNotUnapplied(MorphologicalRule rule, WordAnalysis input);

		public abstract void LexLookup(Stratum stratum, WordAnalysis input);

		public abstract void BeginSynthesizeWord(WordAnalysis input);

		public abstract void BeginApplyStratum(Stratum stratum, WordSynthesis input);
		public abstract void EndApplyStratum(Stratum stratum, WordSynthesis output);

		public abstract void BeginApplyPhonologicalRule(PhonologicalRule rule, WordSynthesis input);
		public abstract void PhonologicalRuleNotApplicablePOS(WordSynthesis input, HCObjectSet<PartOfSpeech> requiredPOSs);
		public abstract void PhonologicalRuleNotApplicableMPRFeatures(MPRFeaturesType type, WordSynthesis input, MPRFeatureSet mprFeatures);
		public abstract void EndApplyPhonologicalRule(PhonologicalRule rule, WordSynthesis output);

		public abstract void BeginApplyTemplate(AffixTemplate template, WordSynthesis input);
		public abstract void EndApplyTemplate(AffixTemplate template, WordSynthesis output, bool applied);

		public abstract void MorphologicalRuleApplied(MorphologicalRule rule, WordSynthesis input, WordSynthesis output, Allomorph allomorph);
		public abstract void MorphologicalRuleNotApplied(MorphologicalRule rule, WordSynthesis input);

		public abstract void MorphCooccurrenceRuleFailed(MorphCoOccurrence cooccurrence, string usage, WordSynthesis input);

		public abstract void Blocking(BlockType blockingType, WordSynthesis input, LexEntry blockingEntry);
		public abstract void ReportSuccess(WordSynthesis output);
	}
}
