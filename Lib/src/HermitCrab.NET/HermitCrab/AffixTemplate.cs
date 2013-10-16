using System.Collections.Generic;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a slot in an affix template. It encapsulates a list of
	/// affixal morphological rules.
	/// </summary>
	public class Slot : HCObject
	{
		readonly HCObjectSet<MorphologicalRule> m_rules;
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
		readonly HCObjectSet<Slot> m_slots;
		HCObjectSet<PartOfSpeech> m_requiredPOSs;

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
		/// <param name="selectTraceMorphs"></param>
		/// <param name="output">The output word analyses.</param>
		/// <param name="trace"></param>
		/// <returns>The resulting word analyses.</returns>
		public bool Unapply(WordAnalysis input, TraceManager trace, string[] selectTraceMorphs, out IEnumerable<WordAnalysis> output)
		{
			var results = new Set<WordAnalysis>();
			if (trace != null)
				trace.BeginUnapplyTemplate(this, input);
			UnapplySlots(input.Clone(), m_slots.Count - 1, trace, selectTraceMorphs, results);
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

			output = null;
			return false;
		}

		void UnapplySlots(WordAnalysis input, int sIndex, TraceManager trace, string[] selectTraceMorphs, Set<WordAnalysis> output)
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
							if (rule.Unapply(input, j, trace, selectTraceMorphs, out analyses))
							{
								ruleUnapplied = true;
								foreach (WordAnalysis wa in analyses)
								{
									if (wa.Shape.Count > 2)
										UnapplySlots(wa, i - 1, trace, selectTraceMorphs, output);
								}
							}
						}
						rule.EndUnapplication(input, trace, ruleUnapplied);
					}
				}
				// we can skip this slot if it is optional
				if (!m_slots[i].IsOptional)
				{
					if (trace != null)
						trace.EndUnapplyTemplate(this, input, false);
					return;
				}
			}

			if (trace != null)
				trace.EndUnapplyTemplate(this, input, true);

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
		/// <param name="trace"></param>
		/// <param name="output">The output word synthesis.</param>
		/// <returns><c>true</c> if the affix template applied, otherwise <c>false</c>.</returns>
		public bool Apply(WordSynthesis input, TraceManager trace, out IEnumerable<WordSynthesis> output)
		{
			FeatureValues headFeatures = input.HeadFeatures.Clone();
			var results = new Set<WordSynthesis>();

			if (trace != null)
				trace.BeginApplyTemplate(this, input);

			ApplySlots(input.Clone(), 0, headFeatures, trace, results);

			if (results.Count > 0)
			{
				output = results;
				return true;
			}

			output = null;
			return false;
		}

		void ApplySlots(WordSynthesis input, int sIndex, FeatureValues origHeadFeatures, TraceManager trace, Set<WordSynthesis> output)
		{
			for (int i = sIndex; i < m_slots.Count; i++)
			{
				foreach (MorphologicalRule rule in m_slots[i].MorphologicalRules)
				{
					if (rule.IsApplicable(input))
					{
						// this is the slot affix that realizes the features
						ICollection<WordSynthesis> syntheses;
						if (rule.ApplySlotAffix(input, origHeadFeatures, trace, out syntheses))
						{
							foreach (WordSynthesis ws in syntheses)
								ApplySlots(ws, i + 1, origHeadFeatures, trace, output);
						}
					}
				}

				if (!m_slots[i].IsOptional)
				{
					if (trace != null)
						trace.EndApplyTemplate(this, input, false);
					return;
				}
			}

			if (trace != null)
				trace.EndApplyTemplate(this, input, true);

			output.Add(input);
		}
	}
}
