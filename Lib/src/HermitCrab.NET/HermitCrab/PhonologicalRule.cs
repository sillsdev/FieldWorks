using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class should be extended by all phonological rules.
	/// </summary>
	public abstract class PhonologicalRule : HCObject
	{
		/// <summary>
		/// The multiple application order for phonological rules.
		/// </summary>
		public enum MultAppOrder { LR_ITERATIVE, RL_ITERATIVE, SIMULTANEOUS };

		public PhonologicalRule(string id, string desc, Morpher morpher)
			: base(id, desc, morpher)
		{
		}

		/// <summary>
		/// Gets or sets the multiple application order.
		/// </summary>
		/// <value>The multiple application order.</value>
		public abstract MultAppOrder MultApplication
		{
			get;
			set;
		}

		/// <summary>
		/// Unapplies the rule to the specified word analysis.
		/// </summary>
		/// <param name="input">The input word analysis.</param>
		/// <param name="trace"></param>
		public abstract void Unapply(WordAnalysis input, TraceManager trace);

		/// <summary>
		/// Applies the rule to the specified word synthesis.
		/// </summary>
		/// <param name="input">The input word synthesis.</param>
		/// <param name="trace"></param>
		public abstract void Apply(WordSynthesis input, TraceManager trace);
	}
}
