using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents all of the information for the analysis of a word.
	/// </summary>
	public class WordAnalysis : ICloneable
	{
		PhoneticShape m_shape;
		readonly HCObjectSet<PartOfSpeech> m_pos;
		LexEntry.RootAllomorph m_rootAllomorph;
		WordAnalysis m_nonHead;
		readonly List<MorphologicalRule> m_mrules;
		readonly Dictionary<MorphologicalRule, int> m_mrulesUnapplied;
		FeatureValues m_rzFeatures;
		object m_curTraceObject;
		Stratum m_stratum;

		/// <summary>
		/// Initializes a new instance of the <see cref="WordAnalysis"/> class.
		/// </summary>
		/// <param name="shape">The shape.</param>
		/// <param name="stratum"></param>
		internal WordAnalysis(PhoneticShape shape, Stratum stratum)
		{
			m_shape = shape;
			m_pos = new HCObjectSet<PartOfSpeech>();
			m_mrules = new List<MorphologicalRule>();
			m_mrulesUnapplied = new Dictionary<MorphologicalRule, int>();
			m_rzFeatures = new FeatureValues();
			m_stratum = stratum;
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="wa">The word analysis.</param>
		public WordAnalysis(WordAnalysis wa)
		{
			m_shape = wa.m_shape.Clone();
			m_pos = new HCObjectSet<PartOfSpeech>(wa.m_pos);
			m_rootAllomorph = wa.m_rootAllomorph;
			if (wa.m_nonHead != null)
				m_nonHead = wa.m_nonHead.Clone();
			m_mrules = new List<MorphologicalRule>(wa.m_mrules);
			m_mrulesUnapplied = new Dictionary<MorphologicalRule, int>(wa.m_mrulesUnapplied);
			m_rzFeatures = wa.m_rzFeatures.Clone();
			m_curTraceObject = wa.m_curTraceObject;
			m_stratum = wa.m_stratum;
		}

		/// <summary>
		/// Gets or sets the phonetic shape.
		/// </summary>
		/// <value>The phonetic shape.</value>
		public PhoneticShape Shape
		{
			get
			{
				return m_shape;
			}

			internal set
			{
				m_shape = value;
			}
		}

		/// <summary>
		/// Gets or sets the root allomorph.
		/// </summary>
		/// <value>The root allomorph.</value>
		public LexEntry.RootAllomorph RootAllomorph
		{
			get
			{
				return m_rootAllomorph;
			}

			internal set
			{
				m_rootAllomorph = value;
			}
		}

		/// <summary>
		/// Gets or sets the non-head analysis.
		/// </summary>
		/// <value>The non-head analysis.</value>
		public WordAnalysis NonHead
		{
			get
			{
				return m_nonHead;
			}

			internal set
			{
				m_nonHead = value;
			}
		}

		/// <summary>
		/// Gets the morphological rules.
		/// </summary>
		/// <value>The morphological rules.</value>
		public IEnumerable<MorphologicalRule> UnappliedMorphologicalRules
		{
			get
			{
				return m_mrules;
			}
		}

		/// <summary>
		/// Gets or sets the realizational features.
		/// </summary>
		/// <value>The realizational features.</value>
		public FeatureValues RealizationalFeatures
		{
			get
			{
				return m_rzFeatures;
			}

			internal set
			{
				m_rzFeatures = value;
			}
		}

		/// <summary>
		/// Gets or sets the current trace record.
		/// </summary>
		/// <value>The current trace record.</value>
		public object CurrentTraceObject
		{
			get
			{
				return m_curTraceObject;
			}

			set
			{
				m_curTraceObject = value;
			}
		}

		/// <summary>
		/// Gets or sets the stratum.
		/// </summary>
		/// <value>The stratum.</value>
		public Stratum Stratum
		{
			get
			{
				return m_stratum;
			}

			internal set
			{
				m_stratum = value;
			}
		}

		/// <summary>
		/// Adds the part of speech.
		/// </summary>
		/// <param name="pos">The part of speech.</param>
		internal void AddPOS(PartOfSpeech pos)
		{
			m_pos.Add(pos);
		}

		/// <summary>
		/// Checks if the specified part of speech matches the set of instantiated parts of speech.
		/// </summary>
		/// <param name="pos">The part of speech.</param>
		/// <returns><c>true</c> if the specified part of speech matches, otherwise <c>false</c>.</returns>
		public bool MatchPOS(PartOfSpeech pos)
		{
			return m_pos.Count == 0 || m_pos.Contains(pos);
		}

		/// <summary>
		/// Uninstantiates the part of speech.
		/// </summary>
		internal void UninstantiatePOS()
		{
			m_pos.Clear();
		}

		/// <summary>
		/// Notifies this analysis that the specified morphological rule was unapplied.
		/// </summary>
		/// <param name="mrule">The morphological rule.</param>
		internal void MorphologicalRuleUnapplied(MorphologicalRule mrule)
		{
			int numUnapplies = GetNumUnappliesForMorphologicalRule(mrule);
			m_mrulesUnapplied[mrule] = numUnapplies + 1;
			m_mrules.Insert(0, mrule);
		}

		/// <summary>
		/// Gets the number of times the specified morphological rule has been unapplied.
		/// </summary>
		/// <param name="mrule">The morphological rule.</param>
		/// <returns>The number of unapplications.</returns>
		internal int GetNumUnappliesForMorphologicalRule(MorphologicalRule mrule)
		{
			int numUnapplies;
			if (!m_mrulesUnapplied.TryGetValue(mrule, out numUnapplies))
				numUnapplies = 0;
			return numUnapplies;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as WordAnalysis);
		}

		public bool Equals(WordAnalysis other)
		{
			if (other == null)
				return false;

			if (m_mrulesUnapplied.Count != other.m_mrulesUnapplied.Count)
				return false;

			foreach (KeyValuePair<MorphologicalRule, int> kvp in m_mrulesUnapplied)
			{
				int numUnapplies;
				if (!other.m_mrulesUnapplied.TryGetValue(kvp.Key, out numUnapplies) || numUnapplies != kvp.Value)
					return false;
			}

			if (m_nonHead != null)
			{
				if (!m_nonHead.Equals(other.m_nonHead))
					return false;
			}
			else if (other.m_nonHead != null)
			{
				return false;
			}

			return m_shape.Equals(other.m_shape) && m_rzFeatures.Equals(other.m_rzFeatures);
		}

		public override int GetHashCode()
		{
			int mruleHashCode = 0;
			foreach (KeyValuePair<MorphologicalRule, int> kvp in m_mrulesUnapplied)
				mruleHashCode ^= kvp.Key.GetHashCode() ^ kvp.Value.GetHashCode();

			return mruleHashCode ^ m_shape.GetHashCode() ^ m_rzFeatures.GetHashCode()
				^ (m_nonHead == null ? 0 : m_nonHead.GetHashCode());
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			bool firstItem = true;
			foreach (MorphologicalRule rule in m_mrules)
			{
				if (!firstItem)
					sb.Append(", ");
				sb.Append(rule);
				firstItem = false;
			}

			return string.Format(HCStrings.kstidWordAnalysis,
				m_stratum == null ? m_shape.ToString() : m_stratum.CharacterDefinitionTable.ToRegexString(m_shape, ModeType.ANALYSIS, true),
				m_pos, sb, m_stratum);
		}

		object ICloneable.Clone()
		{
			return this.Clone();
		}

		public WordAnalysis Clone()
		{
			return new WordAnalysis(this);
		}
	}
}
