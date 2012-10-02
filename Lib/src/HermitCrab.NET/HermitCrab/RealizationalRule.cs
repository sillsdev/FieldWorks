using System.Collections.Generic;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a realizational rule. A realizational rule is a affixal morphological rule
	/// that realizes a set of morphosyntactic features. It is typically used to model inflectional morphology.
	/// </summary>
	public class RealizationalRule : AffixalMorphologicalRule
	{
		public RealizationalRule(string id, string desc, Morpher morpher)
			: base(id, desc, morpher)
		{
		}

		public FeatureValues RealizationalFeatures
		{
			get
			{
				return OutHeadFeatures;
			}

			set
			{
				OutHeadFeatures = value;
			}
		}

		public override bool BeginUnapplication(WordAnalysis input)
		{
			return true;
		}

		public override bool Unapply(WordAnalysis input, int srIndex, out ICollection<WordAnalysis> output)
		{
			output = null;
			FeatureValues rzFeats;
			if (!RealizationalFeatures.Unify(input.RealizationalFeatures, out rzFeats))
				return false;

			if (base.Unapply(input, srIndex, out output))
			{
				foreach (WordAnalysis wa in output)
					wa.RealizationalFeatures = rzFeats;
				return true;
			}
			return false;
		}

		public override bool IsApplicable(WordSynthesis input)
		{
			return RealizationalFeatures.IsMatch(input.RealizationalFeatures);
		}

		public override bool ApplySlotAffix(WordSynthesis input, FeatureValues origHeadFeatures, out ICollection<WordSynthesis> output)
		{
			output = null;
			if (IsBlockedSlotAffix(origHeadFeatures))
				return false;

			return base.ApplySlotAffix(input, origHeadFeatures, out output);
		}

		/// <summary>
		/// Check for if this slot affix was blocked.
		/// </summary>
		/// <param name="headFeats">The head features.</param>
		/// <returns>
		/// 	<c>true</c> if this slot affix was blocked, otherwise <c>false</c>.
		/// </returns>
		bool IsBlockedSlotAffix(FeatureValues headFeats)
		{
			if (RealizationalFeatures.NumFeatures == 0 || headFeats == null)
				return false;

			foreach (Feature feature in RealizationalFeatures.Features)
			{
				if (!headFeats.ContainsFeature(feature))
					return false;
			}
			return true;
		}
	}
}
