using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a trace record. All trace records inherit from this class.
	/// A morph trace is a tree structure where each node in the tree is a <c>Trace</c> object.
	/// </summary>
	public abstract class Trace
	{
		/// <summary>
		/// The type of trace record
		/// </summary>
		public enum TraceType
		{
			/// <summary>
			/// Word analysis trace
			/// </summary>
			WORD_ANALYSIS,
			/// <summary>
			/// Stratum synthesis trace
			/// </summary>
			STRATUM_SYNTHESIS,
			/// <summary>
			/// Stratum analysis trace
			/// </summary>
			STRATUM_ANALYSIS,
			/// <summary>
			/// Lexical lookup trace
			/// </summary>
			LEX_LOOKUP,
			/// <summary>
			/// Blocking trace
			/// </summary>
			BLOCKING,
			/// <summary>
			/// Word synthesis trace
			/// </summary>
			WORD_SYNTHESIS,
			/// <summary>
			/// Phonological rule analysis trace
			/// </summary>
			PHONOLOGICAL_RULE_ANALYSIS,
			/// <summary>
			/// Phonological rule synthesis trace
			/// </summary>
			PHONOLOGICAL_RULE_SYNTHESIS,
			/// <summary>
			/// Affix template analysis trace
			/// </summary>
			TEMPLATE_ANALYSIS,
			/// <summary>
			/// Affix template synthesis trace
			/// </summary>
			TEMPLATE_SYNTHESIS,
			/// <summary>
			/// Morphological rule analysis trace
			/// </summary>
			MORPHOLOGICAL_RULE_ANALYSIS,
			/// <summary>
			/// Morphological rule synthesis trace
			/// </summary>
			MORPHOLOGICAL_RULE_SYNTHESIS,
			/// <summary>
			/// Report success trace
			/// </summary>
			REPORT_SUCCESS
		}

		List<Trace> m_children;

		/// <summary>
		/// Initializes a new instance of the <see cref="Trace"/> class.
		/// </summary>
		internal Trace()
		{
			m_children = new List<Trace>();
		}

		/// <summary>
		/// Gets the trace record type.
		/// </summary>
		/// <value>The trace record type.</value>
		public abstract TraceType Type
		{
			get;
		}

		/// <summary>
		/// Gets the children of this trace record.
		/// </summary>
		/// <value>The children.</value>
		public IEnumerable<Trace> Children
		{
			get
			{
				return m_children;
			}
		}

		/// <summary>
		/// Gets the child count.
		/// </summary>
		/// <value>The child count.</value>
		public int ChildCount
		{
			get
			{
				return m_children.Count;
			}
		}

		/// <summary>
		/// Gets the child at the specified index.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns>The child.</returns>
		public Trace GetChildAt(int index)
		{
			return m_children[index];
		}

		/// <summary>
		/// Adds the child.
		/// </summary>
		/// <param name="tr">The trace record.</param>
		internal void AddChild(Trace tr)
		{
			m_children.Add(tr);
		}

		public override string ToString()
		{
			return ToString(true);
		}

		public abstract string ToString(bool includeInputs);
	}

	/// <summary>
	/// This represents the root of a word analysis trace tree.
	/// </summary>
	public class WordAnalysisTrace : Trace
	{
		string m_inputWord;
		PhoneticShape m_inputShape;

		/// <summary>
		/// Initializes a new instance of the <see cref="RootTrace"/> class.
		/// </summary>
		/// <param name="inputWord">The input word.</param>
		/// <param name="inputShape">The input shape.</param>
		internal WordAnalysisTrace(string inputWord, PhoneticShape inputShape)
		{
			m_inputWord = inputWord;
			m_inputShape = inputShape;
		}

		/// <summary>
		/// Gets the trace record type.
		/// </summary>
		/// <value>The trace record type.</value>
		public override TraceType Type
		{
			get
			{
				return TraceType.WORD_ANALYSIS;
			}
		}

		/// <summary>
		/// Gets the input word.
		/// </summary>
		/// <value>The input word.</value>
		public string InputWord
		{
			get
			{
				return m_inputWord;
			}
		}

		/// <summary>
		/// Gets the input shape.
		/// </summary>
		/// <value>The input shape.</value>
		public PhoneticShape InputShape
		{
			get
			{
				return m_inputShape;
			}
		}

		public override string ToString(bool includeInputs)
		{
			return string.Format(HCStrings.kstidTraceWordAnalysis, m_inputWord);
		}
	}

	/// <summary>
	/// This abstract class is used to represent all stratum-related trace records.
	/// </summary>
	public abstract class StratumTrace : Trace
	{
		protected Stratum m_stratum;
		protected bool m_input;

		/// <summary>
		/// Initializes a new instance of the <see cref="StratumTrace"/> class.
		/// </summary>
		/// <param name="stratum">The stratum.</param>
		/// <param name="input">if <c>true</c> this is an input record, if <c>false</c> this is an output record.</param>
		internal StratumTrace(Stratum stratum, bool input)
		{
			m_stratum = stratum;
			m_input = input;
		}

		/// <summary>
		/// Gets the stratum.
		/// </summary>
		/// <value>The stratum.</value>
		public Stratum Stratum
		{
			get
			{
				return m_stratum;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance is an input or output trace record.
		/// </summary>
		/// <value><c>true</c> if this instance is an input record; otherwise it is an output record.</value>
		public bool IsInput
		{
			get
			{
				return m_input;
			}
		}
	}

	/// <summary>
	/// This is used to represent information resulting from the tracing of strata
	/// during analysis. This trace record is produced before and after a stratum is
	/// unapplied to a word analysis.
	/// </summary>
	public class StratumAnalysisTrace : StratumTrace
	{
		WordAnalysis m_analysis;

		/// <summary>
		/// Initializes a new instance of the <see cref="StratumAnalysisTrace"/> class.
		/// </summary>
		/// <param name="stratum">The stratum.</param>
		/// <param name="input">if <c>true</c> this is an input record, if <c>false</c> this is an output record.</param>
		/// <param name="analysis">The input or output word analysis.</param>
		internal StratumAnalysisTrace(Stratum stratum, bool input, WordAnalysis analysis)
			: base(stratum, input)
		{
			m_analysis = analysis;
		}

		/// <summary>
		/// Gets the trace record type.
		/// </summary>
		/// <value>The trace record type.</value>
		public override TraceType Type
		{
			get
			{
				return TraceType.STRATUM_ANALYSIS;
			}
		}

		/// <summary>
		/// Gets the input or output word analysis.
		/// </summary>
		/// <value>The input or output word analysis.</value>
		public WordAnalysis Analysis
		{
			get
			{
				return m_analysis;
			}
		}

		public override string ToString(bool includeInputs)
		{
			if (m_input)
			{
				return string.Format(HCStrings.kstidTraceStratumAnalysisIn, m_stratum,
					m_analysis.Stratum.CharacterDefinitionTable.ToRegexString(m_analysis.Shape, ModeType.ANALYSIS, true));
			}
			else
			{
				return string.Format(HCStrings.kstidTraceStratumAnalysisOut, m_stratum,
					m_analysis.Stratum.CharacterDefinitionTable.ToRegexString(m_analysis.Shape, ModeType.ANALYSIS, true));
			}
		}
	}

	/// <summary>
	/// This is used to represent information resulting from the tracing of strata
	/// during synthesis. This trace record is produced every time a stratum is applied
	/// to a word synthesis.
	/// </summary>
	public class StratumSynthesisTrace : StratumTrace
	{
		WordSynthesis m_synthesis;

		/// <summary>
		/// Initializes a new instance of the <see cref="StratumSynthesisTrace"/> class.
		/// </summary>
		/// <param name="stratum">The stratum.</param>
		/// <param name="input">if <c>true</c> this is an input record, if <c>false</c> this is an output record.</param>
		/// <param name="synthesis">The input or output word synthesis.</param>
		internal StratumSynthesisTrace(Stratum stratum, bool input, WordSynthesis synthesis)
			: base(stratum, input)
		{
			m_synthesis = synthesis;
		}

		/// <summary>
		/// Gets the trace record type.
		/// </summary>
		/// <value>The trace record type.</value>
		public override TraceType Type
		{
			get
			{
				return TraceType.STRATUM_SYNTHESIS;
			}
		}

		/// <summary>
		/// Gets the input or output word synthesis.
		/// </summary>
		/// <value>The input or output word synthesis.</value>
		public WordSynthesis Synthesis
		{
			get
			{
				return m_synthesis;
			}
		}

		public override string ToString(bool includeInputs)
		{
			if (m_input)
			{
				return string.Format(HCStrings.kstidTraceStratumSynthesisIn, m_stratum,
					m_synthesis.Stratum.CharacterDefinitionTable.ToString(m_synthesis.Shape, ModeType.SYNTHESIS, true));
			}
			else
			{
				return string.Format(HCStrings.kstidTraceStratumSynthesisOut, m_stratum,
					m_synthesis.Stratum.CharacterDefinitionTable.ToString(m_synthesis.Shape, ModeType.SYNTHESIS, true));
			}
		}
	}

	/// <summary>
	/// This is used to represent information resulting from the tracing of lexical lookup in a
	/// stratum during analysis. This trace record is produced every time a lexical lookup is
	/// attempted. If the lookup successfully finds entries that match the input shape a word synthesis
	/// trace record will be created and added as a child.
	/// </summary>
	public class LexLookupTrace : Trace
	{
		PhoneticShape m_shape;
		Stratum m_stratum;

		/// <summary>
		/// Initializes a new instance of the <see cref="LexLookupTrace"/> class.
		/// </summary>
		/// <param name="stratum">The stratum.</param>
		/// <param name="shape">The shape.</param>
		internal LexLookupTrace(Stratum stratum, PhoneticShape shape)
		{
			m_stratum = stratum;
			m_shape = shape;
		}

		/// <summary>
		/// Gets the trace record type.
		/// </summary>
		/// <value>The trace record type.</value>
		public override TraceType Type
		{
			get
			{
				return TraceType.LEX_LOOKUP;
			}
		}

		/// <summary>
		/// Gets the stratum.
		/// </summary>
		/// <value>The stratum.</value>
		public Stratum Stratum
		{
			get
			{
				return m_stratum;
			}
		}

		/// <summary>
		/// Gets the input shape.
		/// </summary>
		/// <value>The shape.</value>
		public PhoneticShape Shape
		{
			get
			{
				return m_shape;
			}
		}

		public override string ToString(bool includeInputs)
		{
			return string.Format(HCStrings.kstidTraceLexLookup,
				m_stratum.CharacterDefinitionTable.ToRegexString(m_shape, ModeType.ANALYSIS, true), m_stratum);
		}
	}

	/// <summary>
	/// This represents the root of a word synthesis trace tree. This trace record is usually produced
	/// when a lexical lookup successfully returns a matching lexical entry.
	/// </summary>
	public class WordSynthesisTrace : Trace
	{
		LexEntry.RootAllomorph m_rootAllomorph;
		FeatureValues m_rzFeatures;
		List<MorphologicalRule> m_mrules;

		/// <summary>
		/// Initializes a new instance of the <see cref="WordSynthesisTrace"/> class.
		/// </summary>
		/// <param name="rootAllomorph">The root allomorph.</param>
		/// <param name="mrules">The morphological rules.</param>
		/// <param name="rzFeatures">The realizational features.</param>
		internal WordSynthesisTrace(LexEntry.RootAllomorph rootAllomorph, IEnumerable<MorphologicalRule> mrules, FeatureValues rzFeatures)
		{
			m_rootAllomorph = rootAllomorph;
			m_mrules = new List<MorphologicalRule>(mrules);
			m_rzFeatures = rzFeatures;
		}

		/// <summary>
		/// Gets the trace record type.
		/// </summary>
		/// <value>The trace record type.</value>
		public override TraceType Type
		{
			get
			{
				return TraceType.WORD_SYNTHESIS;
			}
		}

		/// <summary>
		/// Gets the root allomorph.
		/// </summary>
		/// <value>The root allomorph.</value>
		public LexEntry.RootAllomorph RootAllomorph
		{
			get
			{
				return m_rootAllomorph;
			}
		}

		/// <summary>
		/// Gets the morphological rules.
		/// </summary>
		/// <value>The morphological rules.</value>
		public IEnumerable<MorphologicalRule> MorphologicalRules
		{
			get
			{
				return m_mrules;
			}
		}

		/// <summary>
		/// Gets the realizational features.
		/// </summary>
		/// <value>The realizational features.</value>
		public FeatureValues RealizationalFeatures
		{
			get
			{
				return m_rzFeatures;
			}
		}

		public override string ToString(bool includeInputs)
		{
			StringBuilder sb = new StringBuilder();
			bool firstItem = true;
			foreach (MorphologicalRule rule in m_mrules)
			{
				if (!firstItem)
					sb.Append(", ");
				sb.Append(rule.Description);
				firstItem = false;
			}

			return string.Format(HCStrings.kstidTraceWordSynthesis, m_rootAllomorph.Morpheme,
				m_rootAllomorph.Morpheme.Stratum.CharacterDefinitionTable.ToString(m_rootAllomorph.Shape, ModeType.SYNTHESIS, true),
				sb, m_rzFeatures);
		}
	}

	/// <summary>
	/// This abstract class is used to represent all phonological rule-related trace records.
	/// </summary>
	public abstract class PhonologicalRuleTrace : Trace
	{
		protected PhonologicalRule m_rule;

		/// <summary>
		/// Initializes a new instance of the <see cref="PhonologicalRuleTrace"/> class.
		/// </summary>
		/// <param name="rule">The rule.</param>
		internal PhonologicalRuleTrace(PhonologicalRule rule)
		{
			m_rule = rule;
		}

		/// <summary>
		/// Gets the rule.
		/// </summary>
		/// <value>The rule.</value>
		public PhonologicalRule Rule
		{
			get
			{
				return m_rule;
			}
		}

	}

	/// <summary>
	/// This is used to represent information resulting from the tracing of phonological rules
	/// during analysis. This trace record is produced every time a phonological rule is unapplied
	/// to a word analysis.
	/// </summary>
	public class PhonologicalRuleAnalysisTrace : PhonologicalRuleTrace
	{
		WordAnalysis m_input;
		WordAnalysis m_output = null;

		/// <summary>
		/// Initializes a new instance of the <see cref="PhonologicalRuleAnalysisTrace"/> class.
		/// </summary>
		/// <param name="rule">The rule.</param>
		/// <param name="input">The input.</param>
		internal PhonologicalRuleAnalysisTrace(PhonologicalRule rule, WordAnalysis input)
			: base(rule)
		{
			m_input = input;
		}

		/// <summary>
		/// Gets the trace record type.
		/// </summary>
		/// <value>The trace record type.</value>
		public override TraceType Type
		{
			get
			{
				return TraceType.PHONOLOGICAL_RULE_ANALYSIS;
			}
		}

		/// <summary>
		/// Gets the input word analysis.
		/// </summary>
		/// <value>The input word analysis.</value>
		public WordAnalysis Input
		{
			get
			{
				return m_input;
			}
		}

		/// <summary>
		/// Gets or sets the output word analysis.
		/// </summary>
		/// <value>The output word analysis.</value>
		public WordAnalysis Output
		{
			get
			{
				return m_output;
			}

			internal set
			{
				m_output = value;
			}
		}

		public override string ToString(bool includeInputs)
		{
			if (includeInputs)
			{
				return string.Format(HCStrings.kstidTracePhonologicalRuleAnalysisInputs, m_rule,
					m_input.Stratum.CharacterDefinitionTable.ToRegexString(m_input.Shape, ModeType.ANALYSIS, true),
					m_output == null ? HCStrings.kstidTraceNoOutput
					: m_output.Stratum.CharacterDefinitionTable.ToRegexString(m_output.Shape, ModeType.ANALYSIS, true));
			}
			else
			{
				return string.Format(HCStrings.kstidTracePhonologicalRuleAnalysis, m_rule,
					m_output == null ? HCStrings.kstidTraceNoOutput
					: m_output.Stratum.CharacterDefinitionTable.ToRegexString(m_output.Shape, ModeType.ANALYSIS, true));
			}
		}
	}

	/// <summary>
	/// This is used to represent information resulting from the tracing of phonological rules
	/// during synthesis. This trace record is produced every time a phonological rule is applied
	/// to a word synthesis.
	/// </summary>
	public class PhonologicalRuleSynthesisTrace : PhonologicalRuleTrace
	{
		WordSynthesis m_input;
		WordSynthesis m_output = null;

		/// <summary>
		/// Initializes a new instance of the <see cref="PhonologicalRuleSynthesisTrace"/> class.
		/// </summary>
		/// <param name="rule">The rule.</param>
		/// <param name="input">The input.</param>
		internal PhonologicalRuleSynthesisTrace(PhonologicalRule rule, WordSynthesis input)
			: base(rule)
		{
			m_input = input;
		}

		/// <summary>
		/// Gets the trace record type.
		/// </summary>
		/// <value>The trace record type.</value>
		public override TraceType Type
		{
			get
			{
				return TraceType.PHONOLOGICAL_RULE_SYNTHESIS;
			}
		}

		/// <summary>
		/// Gets the input word synthesis.
		/// </summary>
		/// <value>The input word synthesis.</value>
		public WordSynthesis Input
		{
			get
			{
				return m_input;
			}
		}

		/// <summary>
		/// Gets or sets the output word synthesis.
		/// </summary>
		/// <value>The output word synthesis.</value>
		public WordSynthesis Output
		{
			get
			{
				return m_output;
			}

			internal set
			{
				m_output = value;
			}
		}

		public override string ToString(bool includeInputs)
		{
			if (includeInputs)
			{
				return string.Format(HCStrings.kstidTracePhonologicalRuleSynthesisInputs, m_rule,
					m_input.Stratum.CharacterDefinitionTable.ToString(m_input.Shape, ModeType.SYNTHESIS, true),
					m_output == null ? HCStrings.kstidTraceNoOutput
					: m_output.Stratum.CharacterDefinitionTable.ToString(m_output.Shape, ModeType.SYNTHESIS, true));
			}
			else
			{
				return string.Format(HCStrings.kstidTracePhonologicalRuleSynthesis, m_rule,
					m_output == null ? HCStrings.kstidTraceNoOutput
					: m_output.Stratum.CharacterDefinitionTable.ToString(m_output.Shape, ModeType.SYNTHESIS, true));
			}
		}
	}

	/// <summary>
	/// This abstract class is used to represent all affix template-related trace records.
	/// </summary>
	public abstract class TemplateTrace : Trace
	{
		protected AffixTemplate m_template;
		protected bool m_input;

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateTrace"/> class.
		/// </summary>
		/// <param name="template">The template.</param>
		/// <param name="input">if <c>true</c> this is an input record, if <c>false</c> this is an output record.</param>
		internal TemplateTrace(AffixTemplate template, bool input)
		{
			m_template = template;
			m_input = input;
		}

		/// <summary>
		/// Gets the affix template.
		/// </summary>
		/// <value>The affix template.</value>
		public AffixTemplate Template
		{
			get
			{
				return m_template;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance is an input or output trace record.
		/// </summary>
		/// <value><c>true</c> if this instance is an input record, otherwise it is an output record.</value>
		public bool IsInput
		{
			get
			{
				return m_input;
			}
		}
	}

	/// <summary>
	/// This is used to represent information resulting from the tracing of affix templates
	/// during analysis. This trace record is produced before and after an attempt to unapply an
	/// affix template to a word analysis is made.
	/// </summary>
	public class TemplateAnalysisTrace : TemplateTrace
	{
		WordAnalysis m_analysis;

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateAnalysisTrace"/> class.
		/// </summary>
		/// <param name="template">The template.</param>
		/// <param name="input">if <c>true</c> this is an input record, if <c>false</c> this is an output record.</param>
		/// <param name="analysis">The input or output word analysis.</param>
		internal TemplateAnalysisTrace(AffixTemplate template, bool input, WordAnalysis analysis)
			: base(template, input)
		{
			m_analysis = analysis;
		}

		/// <summary>
		/// Gets the trace record type.
		/// </summary>
		/// <value>The trace record type.</value>
		public override TraceType Type
		{
			get
			{
				return TraceType.TEMPLATE_ANALYSIS;
			}
		}

		/// <summary>
		/// Gets the input or output word analysis.
		/// </summary>
		/// <value>The input or output word analysis.</value>
		public WordAnalysis Analysis
		{
			get
			{
				return m_analysis;
			}
		}

		public override string ToString(bool includeInputs)
		{
			if (m_input)
			{
				return string.Format(HCStrings.kstidTraceTemplateAnalysisIn, m_template,
					m_analysis.Stratum.CharacterDefinitionTable.ToRegexString(m_analysis.Shape, ModeType.ANALYSIS, true));
			}
			else
			{
				return string.Format(HCStrings.kstidTraceTemplateAnalysisOut, m_template,
					(m_analysis == null ? HCStrings.kstidTraceNoOutput
					: m_analysis.Stratum.CharacterDefinitionTable.ToRegexString(m_analysis.Shape, ModeType.ANALYSIS, true)));
			}
		}
	}

	/// <summary>
	/// This is used to represent information resulting from the tracing of affix templates
	/// during synthesis. This trace record is produced before and after an attempt to apply an
	/// affix template to a word synthesis is made.
	/// </summary>
	public class TemplateSynthesisTrace : TemplateTrace
	{
		WordSynthesis m_synthesis;

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateSynthesisTrace"/> class.
		/// </summary>
		/// <param name="template">The template.</param>
		/// <param name="input">if <c>true</c> this is an input record, if <c>false</c> this is an output record.</param>
		/// <param name="synthesis">The input or output word synthesis.</param>
		internal TemplateSynthesisTrace(AffixTemplate template, bool input, WordSynthesis synthesis)
			: base(template, input)
		{
			m_synthesis = synthesis;
		}

		/// <summary>
		/// Gets the trace record type.
		/// </summary>
		/// <value>The trace record type.</value>
		public override TraceType Type
		{
			get
			{
				return TraceType.TEMPLATE_SYNTHESIS;
			}
		}

		/// <summary>
		/// Gets the input or output word synthesis.
		/// </summary>
		/// <value>The input or output word synthesis.</value>
		public WordSynthesis Synthesis
		{
			get
			{
				return m_synthesis;
			}
		}

		public override string ToString(bool includeInputs)
		{
			if (m_input)
			{
				return string.Format(HCStrings.kstidTraceTemplateSynthesisIn, m_template,
					m_synthesis.Stratum.CharacterDefinitionTable.ToString(m_synthesis.Shape, ModeType.SYNTHESIS, true));
			}
			else
			{
				return string.Format(HCStrings.kstidTraceTemplateSynthesisOut, m_template,
					(m_synthesis == null ? HCStrings.kstidTraceNoOutput
					: m_synthesis.Stratum.CharacterDefinitionTable.ToString(m_synthesis.Shape, ModeType.SYNTHESIS, true)));
			}
		}
	}

	/// <summary>
	/// This abstract class is used to represent all morphological rule-related trace records.
	/// </summary>
	public abstract class MorphologicalRuleTrace : Trace
	{
		protected MorphologicalRule m_rule;
		protected Allomorph m_ruleAllomorph = null;

		/// <summary>
		/// Initializes a new instance of the <see cref="MorphologicalRuleTrace"/> class.
		/// </summary>
		/// <param name="rule">The rule.</param>
		internal MorphologicalRuleTrace(MorphologicalRule rule)
		{
			m_rule = rule;
		}

		/// <summary>
		/// Gets the rule.
		/// </summary>
		/// <value>The rule.</value>
		public MorphologicalRule Rule
		{
			get
			{
				return m_rule;
			}
		}

		/// <summary>
		/// Gets or sets the rule allomorph.
		/// </summary>
		/// <value>The rule allomorph.</value>
		public Allomorph RuleAllomorph
		{
			get
			{
				return m_ruleAllomorph;
			}

			set
			{
				m_ruleAllomorph = value;
			}
		}
	}

	/// <summary>
	/// This is used to represent information resulting from the tracing of morphological rules
	/// during analysis. This trace record is produced every time an attempt to unapply a
	/// morphological rule to a word analysis is made. If the morphological rule was successfully
	/// unapplied the record will contain the output.
	/// </summary>
	public class MorphologicalRuleAnalysisTrace : MorphologicalRuleTrace
	{
		WordAnalysis m_input;
		WordAnalysis m_output = null;

		/// <summary>
		/// Initializes a new instance of the <see cref="MorphologicalRuleAnalysisTrace"/> class.
		/// </summary>
		/// <param name="rule">The rule.</param>
		/// <param name="input">The input.</param>
		internal MorphologicalRuleAnalysisTrace(MorphologicalRule rule, WordAnalysis input)
			: base(rule)
		{
			m_input = input;
		}

		/// <summary>
		/// Gets the trace record type.
		/// </summary>
		/// <value>The trace record type.</value>
		public override TraceType Type
		{
			get
			{
				return TraceType.MORPHOLOGICAL_RULE_ANALYSIS;
			}
		}

		/// <summary>
		/// Gets the input word analysis.
		/// </summary>
		/// <value>The input word analysis.</value>
		public WordAnalysis Input
		{
			get
			{
				return m_input;
			}
		}

		/// <summary>
		/// Gets or sets the output word analysis.
		/// </summary>
		/// <value>The output word analysis.</value>
		public WordAnalysis Output
		{
			get
			{
				return m_output;
			}

			internal set
			{
				m_output = value;
			}
		}

		public override string ToString(bool includeInputs)
		{
			if (includeInputs)
			{
				return string.Format(HCStrings.kstidTraceMorphologicalRuleAnalysisInputs, m_rule,
					m_input.Stratum.CharacterDefinitionTable.ToRegexString(m_input.Shape, ModeType.ANALYSIS, true),
					(m_output == null ? HCStrings.kstidTraceNoOutput
					: m_output.Stratum.CharacterDefinitionTable.ToRegexString(m_output.Shape, ModeType.ANALYSIS, true)));
			}
			else
			{
				return string.Format(HCStrings.kstidTraceMorphologicalRuleAnalysis, m_rule,
					(m_output == null ? HCStrings.kstidTraceNoOutput
					: m_output.Stratum.CharacterDefinitionTable.ToRegexString(m_output.Shape, ModeType.ANALYSIS, true)));
			}
		}
	}

	/// <summary>
	/// This is used to represent information resulting from the tracing of morphological rules
	/// during synthesis. This trace record is produced every time an attempt to apply a
	/// morphological rule to a word synthesis is made. If the morphological rule was successfully
	/// applied the record will contain the output.
	/// </summary>
	public class MorphologicalRuleSynthesisTrace : MorphologicalRuleTrace
	{
		WordSynthesis m_input;
		WordSynthesis m_output = null;

		/// <summary>
		/// Initializes a new instance of the <see cref="MorphologicalRuleSynthesisTrace"/> class.
		/// </summary>
		/// <param name="rule">The rule.</param>
		/// <param name="input">The input.</param>
		internal MorphologicalRuleSynthesisTrace(MorphologicalRule rule, WordSynthesis input)
			: base(rule)
		{
			m_input = input;
		}

		/// <summary>
		/// Gets the trace record type.
		/// </summary>
		/// <value>The trace record type.</value>
		public override TraceType Type
		{
			get
			{
				return TraceType.MORPHOLOGICAL_RULE_SYNTHESIS;
			}
		}

		/// <summary>
		/// Gets the input word synthesis.
		/// </summary>
		/// <value>The input word synthesis.</value>
		public WordSynthesis Input
		{
			get
			{
				return m_input;
			}
		}

		/// <summary>
		/// Gets or sets the output word synthesis.
		/// </summary>
		/// <value>The output word synthesis.</value>
		public WordSynthesis Output
		{
			get
			{
				return m_output;
			}

			internal set
			{
				m_output = value;
			}
		}

		public override string ToString(bool includeInputs)
		{
			if (includeInputs)
			{
				return string.Format(HCStrings.kstidTraceMorphologicalRuleSynthesisInputs, m_rule,
					m_input.Stratum.CharacterDefinitionTable.ToString(m_input.Shape, ModeType.SYNTHESIS, true),
					(m_output == null ? HCStrings.kstidTraceNoOutput
					: m_output.Stratum.CharacterDefinitionTable.ToString(m_output.Shape, ModeType.SYNTHESIS, true)));
			}
			else
			{
				return string.Format(HCStrings.kstidTraceMorphologicalRuleSynthesis, m_rule,
					(m_output == null ? HCStrings.kstidTraceNoOutput
					: m_output.Stratum.CharacterDefinitionTable.ToString(m_output.Shape, ModeType.SYNTHESIS, true)));
			}
		}
	}

	/// <summary>
	/// This is used to represent information resulting from the blocking of word synthesis by a lexical
	/// entry during synthesis. This trace record is produced every time a word synthesis is blocked.
	/// </summary>
	public class BlockingTrace : Trace
	{
		/// <summary>
		/// The block type
		/// </summary>
		public enum BlockType { RULE, TEMPLATE }

		BlockType m_blockingType;
		LexEntry m_blockingEntry;

		/// <summary>
		/// Initializes a new instance of the <see cref="BlockingTrace"/> class.
		/// </summary>
		/// <param name="blockingType">Type of the blocking.</param>
		/// <param name="blockingEntry">The blocking entry.</param>
		internal BlockingTrace(BlockType blockingType, LexEntry blockingEntry)
		{
			m_blockingType = blockingType;
			m_blockingEntry = blockingEntry;
		}

		/// <summary>
		/// Gets the trace record type.
		/// </summary>
		/// <value>The trace record type.</value>
		public override TraceType Type
		{
			get
			{
				return TraceType.BLOCKING;
			}
		}

		/// <summary>
		/// Gets the type of the blocking.
		/// </summary>
		/// <value>The type of the blocking.</value>
		public BlockType BlockingType
		{
			get
			{
				return m_blockingType;
			}
		}

		/// <summary>
		/// Gets the blocking entry.
		/// </summary>
		/// <value>The blocking entry.</value>
		public LexEntry BlockingEntry
		{
			get
			{
				return m_blockingEntry;
			}
		}

		public override string ToString(bool includeInputs)
		{
			string typeStr = null;
			switch (m_blockingType)
			{
				case BlockType.RULE:
					typeStr = "rule";
					break;

				case BlockType.TEMPLATE:
					typeStr = "template";
					break;
			}

			return string.Format(HCStrings.kstidTraceBlocking, typeStr, m_blockingEntry);
		}
	}
	/// <summary>
	/// This is used to represent information resulting from the tracing of lexical lookup in a
	/// stratum during analysis. This trace record is produced every time a lexical lookup is
	/// attempted. If the lookup successfully finds entries that match the input shape a word synthesis
	/// trace record will be created and added as a child.
	/// </summary>
	public class ReportSuccessTrace : Trace
	{
		WordSynthesis m_output;
		/// <summary>
		/// Initializes a new instance of the <see cref="ReportSuccessTrace"/> class.
		/// </summary>
		internal ReportSuccessTrace(WordSynthesis output)
		{
			m_output = output;
		}

		/// <summary>
		/// Gets the trace record type.
		/// </summary>
		/// <value>The trace record type.</value>
		public override TraceType Type
		{
			get
			{
				return TraceType.REPORT_SUCCESS;
			}
		}

		/// <summary>
		/// Gets the output.
		/// </summary>
		/// <value>The output.</value>
		public WordSynthesis Output
		{
			get
			{
				return m_output;
			}
		}

		public override string ToString(bool includeInputs)
		{
			return string.Format(HCStrings.kstidTraceReportSuccess,
				m_output.Stratum.CharacterDefinitionTable.ToString(m_output.Shape, ModeType.SYNTHESIS, true));
		}
	}

}