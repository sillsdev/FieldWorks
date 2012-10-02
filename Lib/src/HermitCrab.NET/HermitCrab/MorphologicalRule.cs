using System.Collections.Generic;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class should be extended by all morphological rules.
	/// </summary>
	public abstract class MorphologicalRule : Morpheme
	{
		bool m_traceAnalysis;
		bool m_traceSynthesis;
		bool m_isBlockable = true;

		protected MorphologicalRule(string id, string desc, Morpher morpher)
			: base(id, desc, morpher)
		{
		}

		/// <summary>
		/// Gets or sets a value indicating whether tracing of this morphological rule
		/// during analysis is on or off.
		/// </summary>
		/// <value><c>true</c> if tracing is on, <c>false</c> if tracing is off.</value>
		public bool TraceAnalysis
		{
			get
			{
				return m_traceAnalysis;
			}

			set
			{
				m_traceAnalysis = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether tracing of this morphological rule
		/// during synthesis is on or off.
		/// </summary>
		/// <value><c>true</c> if tracing is on, <c>false</c> if tracing is off.</value>
		public bool TraceSynthesis
		{
			get
			{
				return m_traceSynthesis;
			}

			set
			{
				m_traceSynthesis = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is blockable.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is blockable, otherwise <c>false</c>.
		/// </value>
		public bool IsBlockable
		{
			get
			{
				return m_isBlockable;
			}

			set
			{
				m_isBlockable = value;
			}
		}

		/// <summary>
		/// Gets the subrule count.
		/// </summary>
		/// <value>The subrule count.</value>
		public abstract int SubruleCount
		{
			get;
		}

		/// <summary>
		/// Performs any pre-processing required for unapplication of a word analysis. This must
		/// be called before <c>Unapply</c>. <c>Unapply</c> and <c>EndUnapplication</c> should only
		/// be called if this method returns <c>true</c>.
		/// </summary>
		/// <param name="input">The input word analysis.</param>
		/// <returns><c>true</c> if the specified input is unapplicable, otherwise <c>false</c>.</returns>
		public abstract bool BeginUnapplication(WordAnalysis input);

		/// <summary>
		/// Unapplies the specified subrule to the specified word analysis.
		/// </summary>
		/// <param name="input">The input word analysis.</param>
		/// <param name="srIndex">Index of the subrule.</param>
		/// <param name="output">All resulting word analyses.</param>
		/// <returns><c>true</c> if the subrule was successfully unapplied, otherwise <c>false</c></returns>
		public abstract bool Unapply(WordAnalysis input, int srIndex, out ICollection<WordAnalysis> output);

		/// <summary>
		/// Performs any post-processing required after the unapplication of a word analysis. This must
		/// be called after a successful <c>BeginUnapplication</c> call and any <c>Unapply</c> calls.
		/// </summary>
		/// <param name="input">The input word analysis.</param>
		/// <param name="unapplied">if set to <c>true</c> if the input word analysis was successfully unapplied.</param>
		public abstract void EndUnapplication(WordAnalysis input, bool unapplied);

		/// <summary>
		/// Determines whether this rule is applicable to the specified word synthesis.
		/// </summary>
		/// <param name="input">The input word synthesis.</param>
		/// <returns>
		/// 	<c>true</c> if the rule is applicable, otherwise <c>false</c>.
		/// </returns>
		public abstract bool IsApplicable(WordSynthesis input);

		/// <summary>
		/// Applies the rule to the specified word synthesis.
		/// </summary>
		/// <param name="input">The input word synthesis.</param>
		/// <param name="output">The output word syntheses.</param>
		/// <returns><c>true</c> if the rule was successfully applied, otherwise <c>false</c></returns>
		public abstract bool Apply(WordSynthesis input, out ICollection<WordSynthesis> output);

		/// <summary>
		/// Applies the rule to the specified word synthesis. This method is used by affix templates.
		/// </summary>
		/// <param name="input">The input word synthesis.</param>
		/// <param name="origHeadFeatures">The original head features before template application.</param>
		/// <param name="output">The output word syntheses.</param>
		/// <returns>
		/// 	<c>true</c> if the rule was successfully applied, otherwise <c>false</c>
		/// </returns>
		public abstract bool ApplySlotAffix(WordSynthesis input, FeatureValues origHeadFeatures, out ICollection<WordSynthesis> output);

		public virtual void Reset()
		{
			m_gloss = null;
			m_isBlockable = true;
		}

		protected WordSynthesis CheckBlocking(WordSynthesis ws)
		{
			if (!m_isBlockable || ws.Root.Family == null)
				return ws;

			// check all relatives
			foreach (LexEntry entry in ws.Root.Family.Entries)
			{
				// a relative will block if the part of speech, stratum, head features, and foot features match
				if (entry != ws.Root && ws.POS == entry.POS && entry.Stratum == ws.Stratum
					&& entry.HeadFeatures.Equals(ws.HeadFeatures)
					&& entry.FootFeatures.Equals(ws.FootFeatures))
				{
					if (Morpher.TraceBlocking)
						// create a blocking trace record, should this become the current trace?
						ws.CurrentTrace.AddChild(new BlockingTrace(BlockingTrace.BlockType.RULE, entry));
					return new WordSynthesis(entry.PrimaryAllomorph, ws.RealizationalFeatures, ws.CurrentTrace);
				}
			}

			return ws;
		}
	}

}