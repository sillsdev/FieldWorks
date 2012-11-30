using System.Collections.Generic;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a slot in an affix template. It encapsulates a list of
	/// affixal morphological rules.
	/// </summary>
	public class Slot : HCObject
	{
		HCObjectSet<MorphologicalRule> m_rules;
		bool m_isOptional;

		/// <summary>
		/// Initializes a new instance of the <see cref="Slot"/> class.
		/// </summary>
		/// <param name="id">The ID.</param>
		/// <param name="desc">The description.</param>
		/// <param name="morpher">The morpher.</param>
		public Slot(string id, string desc, Morpher morpher)
			: base(id, desc, morpher)
		{
			m_rules = new HCObjectSet<MorphologicalRule>();
		}

		/// <summary>
		/// Gets or sets a value indicating whether this slot is optional.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is optional, otherwise <c>false</c>.
		/// </value>
		public bool IsOptional
		{
			get
			{
				if (m_rules.Count == 0)
					return true;

				return m_isOptional;
			}

			set
			{
				m_isOptional = value;
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
				return m_rules;
			}
		}

		/// <summary>
		/// Adds the morphological rule.
		/// </summary>
		/// <param name="rule">The morphological rule.</param>
		public void AddRule(MorphologicalRule rule)
		{
			m_rules.Add(rule);
		}
	}

	/// <summary>
	/// This class represents an affix template. It is normally used to model inflectional
	/// affixation.
	/// </summary>
	public class AffixTemplate : HCObject
	{
		HCObjectSet<Slot> m_slots;
		HCObjectSet<PartOfSpeech> m_requiredPOSs = null;

		/// <summary>
		/// Initializes a new instance of the <see cref="AffixTemplate"/> class.
		/// </summary>
		/// <param name="id">The ID.</param>
		/// <param name="desc">The description.</param>
		/// <param name="morpher">The morpher.</param>
		public AffixTemplate(string id, string desc, Morpher morpher)
			: base(id, desc, morpher)
		{
			m_slots = new HCObjectSet<Slot>();
		}

		/// <summary>
		/// Gets or sets the required parts of speech.
		/// </summary>
		/// <value>The required parts of speech.</value>
		public IEnumerable<PartOfSpeech> RequiredPOSs
		{
			get
			{
				return m_requiredPOSs;
			}

			set
			{
				m_requiredPOSs = new HCObjectSet<PartOfSpeech>(value);
			}
		}

		/// <summary>
		/// Adds the slot.
		/// </summary>
		/// <param name="slot">The slot.</param>
		public void AddSlot(Slot slot)
		{
			m_slots.Add(slot);
		}

		public bool IsUnapplicable(WordAnalysis input)
		{
			foreach (PartOfSpeech pos in m_requiredPOSs)
			{
				if (input.MatchPOS(pos))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Unapplies this affix template to specified input word analysis.
		/// </summary>
		/// <param name="input">The input word analysis.</param>
		/// <param name="output">The output word analyses.</param>
		/// <returns>The resulting word analyses.</returns>
		public bool Unapply(WordAnalysis input, out IEnumerable<WordAnalysis> output, string[] selectTraceMorphs)
		{
			Set<WordAnalysis> results = new Set<WordAnalysis>();
			if (Morpher.TraceTemplatesAnalysis)
			{
				// create the template analysis trace input record
				TemplateAnalysisTrace tempTrace = new TemplateAnalysisTrace(this, true, input.Clone());
				input.CurrentTrace.AddChild(tempTrace);
			}
			UnapplySlots(input.Clone(), m_slots.Count - 1, results, selectTraceMorphs);
			foreach (WordAnalysis wa in results)
			{
				foreach (PartOfSpeech pos in m_requiredPOSs)
					wa.AddPOS(pos);
			}

			if (results.Count > 0)
			{
				output = results;
				return true;
			}
			else
			{
				output = null;
				return false;
			}
		}

		void UnapplySlots(WordAnalysis input, int sIndex, Set<WordAnalysis> output, string[] selectTraceMorphs)
		{
			for (int i = sIndex; i >= 0; i--)
			{
				foreach (MorphologicalRule rule in m_slots[i].MorphologicalRules)
				{
					if (rule.BeginUnapplication(input))
					{
						bool ruleUnapplied = false;
						for (int j = 0; j < rule.SubruleCount; j++)
						{
							ICollection<WordAnalysis> analyses;
							if (rule.Unapply(input, j, out analyses, selectTraceMorphs))
							{
								ruleUnapplied = true;
								foreach (WordAnalysis wa in analyses)
								{
									if (wa.Shape.Count > 2)
										UnapplySlots(wa, i - 1, output, selectTraceMorphs);
								}
							}
						}
						rule.EndUnapplication(input, ruleUnapplied);
					}
				}
				// we can skip this slot if it is optional
				if (!m_slots[i].IsOptional)
				{
					if (Morpher.TraceTemplatesAnalysis)
						input.CurrentTrace.AddChild(new TemplateAnalysisTrace(this, false, null));
					return;
				}
			}

			if (Morpher.TraceTemplatesAnalysis)
				input.CurrentTrace.AddChild(new TemplateAnalysisTrace(this, false, input.Clone()));
			output.Add(input);
		}

		public bool IsApplicable(WordSynthesis input)
		{
			return m_requiredPOSs.Contains(input.POS);
		}

		/// <summary>
		/// Applies this affix template to the specified input word synthesis.
		/// </summary>
		/// <param name="input">The input word synthesis.</param>
		/// <param name="output">The output word synthesis.</param>
		/// <returns><c>true</c> if the affix template applied, otherwise <c>false</c>.</returns>
		public bool Apply(WordSynthesis input, out IEnumerable<WordSynthesis> output)
		{
			FeatureValues headFeatures = input.HeadFeatures.Clone();
			Set<WordSynthesis> results = new Set<WordSynthesis>();
			if (Morpher.TraceTemplatesSynthesis)
			{
				// create the template synthesis input trace record
				TemplateSynthesisTrace tempTrace = new TemplateSynthesisTrace(this, true, input.Clone());
				input.CurrentTrace.AddChild(tempTrace);
			}
			ApplySlots(input.Clone(), 0, headFeatures, results);

			if (results.Count > 0)
			{
				output = results;
				return true;
			}
			else
			{
				output = null;
				return false;
			}
		}

		void ApplySlots(WordSynthesis input, int sIndex, FeatureValues origHeadFeatures, Set<WordSynthesis> output)
		{
			for (int i = sIndex; i < m_slots.Count; i++)
			{
				foreach (MorphologicalRule rule in m_slots[i].MorphologicalRules)
				{
					if (rule.IsApplicable(input))
					{
						// this is the slot affix that realizes the features
						ICollection<WordSynthesis> syntheses;
						if (rule.ApplySlotAffix(input, origHeadFeatures, out syntheses))
						{
							foreach (WordSynthesis ws in syntheses)
								ApplySlots(ws, i + 1, origHeadFeatures, output);
						}
					}
				}

				if (!m_slots[i].IsOptional)
				{
					if (Morpher.TraceTemplatesSynthesis)
						input.CurrentTrace.AddChild(new TemplateSynthesisTrace(this, false, null));
					return;
				}
			}

			if (Morpher.TraceTemplatesSynthesis)
				input.CurrentTrace.AddChild(new TemplateSynthesisTrace(this, false, input.Clone()));
			output.Add(input);
		}
	}
}
