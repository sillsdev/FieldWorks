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
		bool m_traceAnalysis = false;
		bool m_traceSynthesis = false;

		/// <summary>
		/// The multiple application order for phonological rules.
		/// </summary>
		public enum MultAppOrder { LR_ITERATIVE, RL_ITERATIVE, SIMULTANEOUS };

		public PhonologicalRule(string id, string desc, Morpher morpher)
			: base(id, desc, morpher)
		{
		}

		/// <summary>
		/// Gets or sets a value indicating whether tracing of this phonological rule
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
		/// Gets or sets a value indicating whether tracing of this phonological rule
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
		public abstract void Unapply(WordAnalysis input);

		/// <summary>
		/// Applies the rule to the specified word synthesis.
		/// </summary>
		/// <param name="input">The input word synthesis.</param>
		public abstract void Apply(WordSynthesis input);
	}
}
