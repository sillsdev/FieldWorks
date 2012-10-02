using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a word synthesis record. It contains all of the information for
	/// the synthesis of a word.
	/// </summary>
	public class WordSynthesis : ICloneable, IComparable<WordSynthesis>
	{
		LexEntry m_root;
		PhoneticShape m_shape;
		Morphs m_morphs;
		PartOfSpeech m_pos;
		MPRFeatureSet m_mprFeatures;
		WordSynthesis m_nonHead = null;
		FeatureValues m_headFeatures;
		FeatureValues m_footFeatures;
		HCObjectSet<Feature> m_obligHeadFeatures;
		List<MorphologicalRule> m_mrules;
		int m_curRuleIndex = 0;
		FeatureValues m_rzFeatures;
		Trace m_curTrace = null;
		Stratum m_stratum;
		Dictionary<MorphologicalRule, int> m_mrulesApplied;

		/// <summary>
		/// Initializes a new instance of the <see cref="WordSynthesis"/> class.
		/// </summary>
		/// <param name="rootAllomorph">The root allomorph.</param>
		/// <param name="rzFeatures">The realizational features.</param>
		/// <param name="curTrace">The current trace record.</param>
		internal WordSynthesis(LexEntry.RootAllomorph rootAllomorph, FeatureValues rzFeatures, Trace curTrace)
			: this(rootAllomorph, null, rzFeatures, new MorphologicalRule[] {}, curTrace)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="WordSynthesis"/> class.
		/// </summary>
		/// <param name="wa">The word analysis.</param>
		internal WordSynthesis(WordAnalysis wa)
			: this(wa.RootAllomorph, wa.NonHead == null ? null : new WordSynthesis(wa.NonHead), wa.RealizationalFeatures.Clone(),
			wa.UnappliedMorphologicalRules, wa.CurrentTrace)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="WordSynthesis"/> class.
		/// </summary>
		/// <param name="rootAllomorph">The root allomorph.</param>
		/// <param name="nonHead">The non-head synthesis.</param>
		/// <param name="rzFeatures">The realizational features.</param>
		/// <param name="mrules">The morphological rules to apply.</param>
		/// <param name="curTrace">The current trace record.</param>
		internal WordSynthesis(LexEntry.RootAllomorph rootAllomorph, WordSynthesis nonHead, FeatureValues rzFeatures, IEnumerable<MorphologicalRule> mrules,
			Trace curTrace)
		{
			m_root = (LexEntry) rootAllomorph.Morpheme;
			m_mprFeatures = m_root.MPRFeatures != null ? m_root.MPRFeatures.Clone() : new MPRFeatureSet();
			m_headFeatures = m_root.HeadFeatures != null ? m_root.HeadFeatures.Clone() : new FeatureValues();
			m_footFeatures = m_root.FootFeatures != null ? m_root.FootFeatures.Clone() : new FeatureValues();
			m_pos = m_root.POS;
			m_stratum = m_root.Stratum;

			m_nonHead = nonHead;
			m_morphs = new Morphs();
			Morph morph = new Morph(rootAllomorph);
			morph.Shape.AddMany(rootAllomorph.Shape.Segments);
			m_morphs.Add(morph);
			m_shape = new PhoneticShape();
			m_shape.Add(new Margin(Direction.LEFT));
			m_shape.AddPartition(rootAllomorph.Shape.Segments, morph.Partition);
			m_shape.Add(new Margin(Direction.RIGHT));

			m_obligHeadFeatures = new HCObjectSet<Feature>();
			m_mrules = new List<MorphologicalRule>(mrules);
			m_rzFeatures = rzFeatures;
			m_curTrace = curTrace;
			m_mrulesApplied = new Dictionary<MorphologicalRule, int>();
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="ws">The word synthesis.</param>
		public WordSynthesis(WordSynthesis ws)
		{
			m_root = ws.m_root;
			if (ws.m_nonHead != null)
				m_nonHead = ws.m_nonHead.Clone();
			m_shape = ws.m_shape.Clone();
			m_morphs = ws.m_morphs.Clone();
			m_pos = ws.m_pos;
			m_mprFeatures = ws.m_mprFeatures.Clone();
			m_headFeatures = ws.m_headFeatures.Clone();
			m_footFeatures = ws.m_footFeatures.Clone();
			m_obligHeadFeatures = new HCObjectSet<Feature>(ws.m_obligHeadFeatures);
			m_mrules = new List<MorphologicalRule>(ws.m_mrules);
			m_curRuleIndex = ws.m_curRuleIndex;
			m_rzFeatures = ws.m_rzFeatures.Clone();
			m_curTrace = ws.m_curTrace;
			m_stratum = ws.m_stratum;
			m_mrulesApplied = new Dictionary<MorphologicalRule, int>(ws.m_mrulesApplied);
		}

		/// <summary>
		/// Gets the root lexical entry.
		/// </summary>
		/// <value>The root lexical entry.</value>
		public LexEntry Root
		{
			get
			{
				return m_root;
			}
		}

		/// <summary>
		/// Gets the non-head word synthesis.
		/// </summary>
		/// <value>The root lexical entries.</value>
		public WordSynthesis NonHead
		{
			get
			{
				return m_nonHead;
			}
		}

		/// <summary>
		/// Gets the phonetic shape.
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
		/// Gets the morphs.
		/// </summary>
		/// <value>The morphs.</value>
		public Morphs Morphs
		{
			get
			{
				return m_morphs;
			}
		}

		/// <summary>
		/// Gets or sets the part of speech.
		/// </summary>
		/// <value>The part of speech.</value>
		public PartOfSpeech POS
		{
			get
			{
				return m_pos;
			}

			internal set
			{
				m_pos = value;
			}
		}

		/// <summary>
		/// Gets the MPR features.
		/// </summary>
		/// <value>The MPR features.</value>
		public MPRFeatureSet MPRFeatures
		{
			get
			{
				return m_mprFeatures;
			}
		}

		/// <summary>
		/// Gets or sets the head features.
		/// </summary>
		/// <value>The head features.</value>
		public FeatureValues HeadFeatures
		{
			get
			{
				return m_headFeatures;
			}

			internal set
			{
				m_headFeatures = value;
			}
		}

		/// <summary>
		/// Gets or sets the foot features.
		/// </summary>
		/// <value>The foot features.</value>
		public FeatureValues FootFeatures
		{
			get
			{
				return m_footFeatures;
			}

			internal set
			{
				m_footFeatures = value;
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

		/// <summary>
		/// Gets a value that indicates whether this instance is valid. It ensures that this instance
		/// contains all of the required head features and that all of the morphs are valid.
		/// </summary>
		/// <value><c>true</c> if this instance is valid, otherwise <c>false</c>.</value>
		internal bool IsValid
		{
			get
			{
				foreach (Feature feature in m_obligHeadFeatures)
				{
					if (!m_headFeatures.ContainsFeature(feature))
						return false;
				}

				if (!IsMorphsValid)
					return false;

				return true;
			}
		}

		/// <summary>
		/// Gets a value indicating whether all of the morphs in this word synthesis are valid. It checks
		/// environments and allomorph/morpheme co-occurrence rules.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if all morphs are valid, otherwise <c>false</c>.
		/// </value>
		internal bool IsMorphsValid
		{
			get
			{
				foreach (Morph morph in m_morphs)
				{
					if (morph.Allomorph.ExcludedEnvironments != null || morph.Allomorph.RequiredEnvironments != null)
					{
						// get the left and right context nodes for this morph
						PhoneticShapeNode leftNode = null;
						for (PhoneticShapeNode node = m_shape.Begin; node != m_shape.Last; node = node.Next)
						{
							if (node.Partition == morph.Partition)
							{
								leftNode = node.Prev;
								break;
							}
						}
						PhoneticShapeNode rightNode = null;
						for (PhoneticShapeNode node = m_shape.End; node != m_shape.First; node = node.Prev)
						{
							if (node.Partition == morph.Partition)
							{
								rightNode = node.Next;
								break;
							}
						}

						// excluded environments
						if (morph.Allomorph.ExcludedEnvironments != null)
						{
							foreach (Environment env in morph.Allomorph.ExcludedEnvironments)
							{
								if (env.IsMatch(leftNode, rightNode, ModeType.SYNTHESIS))
									return false;
							}
						}

						// required environments
						if (morph.Allomorph.RequiredEnvironments != null)
						{
							bool match = false;
							foreach (Environment env in morph.Allomorph.RequiredEnvironments)
							{
								if (env.IsMatch(leftNode, rightNode, ModeType.SYNTHESIS))
								{
									match = true;
									break;
								}
							}
							if (!match)
								return false;
						}
					}

					// excluded morpheme co-occurrences
					if (morph.Allomorph.Morpheme.ExcludedMorphemeCoOccurrences != null)
					{
						foreach (MorphCoOccurrence coOccur in morph.Allomorph.Morpheme.ExcludedMorphemeCoOccurrences)
						{
							if (coOccur.CoOccurs(m_morphs, morph.Allomorph.Morpheme))
								return false;
						}
					}

					// required morpheme co-occurrences
					if (morph.Allomorph.Morpheme.RequiredMorphemeCoOccurrences != null)
					{
						bool match = false;
						foreach (MorphCoOccurrence coOccur in morph.Allomorph.Morpheme.RequiredMorphemeCoOccurrences)
						{
							if (coOccur.CoOccurs(m_morphs, morph.Allomorph.Morpheme))
							{
								match = true;
								break;
							}
						}
						if (!match)
							return false;
					}

					// excluded allomorph co-occurrences
					if (morph.Allomorph.ExcludedAllomorphCoOccurrences != null)
					{
						foreach (MorphCoOccurrence coOccur in morph.Allomorph.ExcludedAllomorphCoOccurrences)
						{
							if (coOccur.CoOccurs(m_morphs, morph.Allomorph))
								return false;
						}
					}

					// required allomorph co-occurrences
					if (morph.Allomorph.RequiredAllomorphCoOccurrences != null)
					{
						bool match = false;
						foreach (MorphCoOccurrence coOccur in morph.Allomorph.RequiredAllomorphCoOccurrences)
						{
							if (coOccur.CoOccurs(m_morphs, morph.Allomorph))
							{
								match = true;
								break;
							}
						}
						if (!match)
							return false;
					}

				}
				return true;
			}
		}

		/// <summary>
		/// Gets or sets the current trace record.
		/// </summary>
		/// <value>The current trace record.</value>
		internal Trace CurrentTrace
		{
			get
			{
				return m_curTrace;
			}

			set
			{
				m_curTrace = value;
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
		/// Gets the next rule to be applied.
		/// </summary>
		/// <value>The next rule.</value>
		internal MorphologicalRule NextRule
		{
			get
			{
				if (m_curRuleIndex >= m_mrules.Count)
					return null;
				return m_mrules[m_curRuleIndex];
			}
		}

		/// <summary>
		/// Adds the obligatory head feature.
		/// </summary>
		/// <param name="feature">The feature.</param>
		internal void AddObligatoryHeadFeature(Feature feature)
		{
			m_obligHeadFeatures.Add(feature);
		}

		/// <summary>
		/// Notifies this word synthesis that the specified morphological rule has applied.
		/// </summary>
		/// <param name="mrule">The morphological rule.</param>
		internal void MorphologicalRuleApplied(MorphologicalRule mrule)
		{
			int numApplies = GetNumAppliesForMorphologicalRule(mrule);
			m_mrulesApplied[mrule] = numApplies + 1;
			if (mrule == NextRule)
				m_curRuleIndex++;
		}

		/// <summary>
		/// Gets the number of times the specified morphological rule has been applied.
		/// </summary>
		/// <param name="mrule">The morphological rule.</param>
		/// <returns>The number of applications.</returns>
		internal int GetNumAppliesForMorphologicalRule(MorphologicalRule mrule)
		{
			int numApplies;
			if (!m_mrulesApplied.TryGetValue(mrule, out numApplies))
				numApplies = 0;
			return numApplies;
		}

		/// <summary>
		/// Determines if the specified word synthesis is a duplicate of this word synthesis. It differs from
		/// <c>Equals</c> in that it only checks the shape, root, and stratum. It is used for
		/// checking completed word synthesis records.
		/// </summary>
		/// <param name="other">The other.</param>
		/// <returns></returns>
		public bool Duplicates(WordSynthesis other)
		{
			return m_shape.Equals(other.m_shape) && m_morphs.Equals(other.m_morphs) && m_stratum == other.m_stratum;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as WordSynthesis);
		}

		public bool Equals(WordSynthesis other)
		{
			if (other == null)
				return false;

			if (m_mrules.Count != other.m_mrules.Count)
				return false;

			for (int i = 0; i < m_mrules.Count; i++)
			{
				if (m_mrules[i] != other.m_mrules[i])
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

			return m_shape.Equals(other.m_shape) && m_morphs.Equals(other.m_morphs)
				&& m_rzFeatures.Equals(other.m_rzFeatures)
				&& m_stratum == other.m_stratum && m_root == other.m_root;
		}

		public override int GetHashCode()
		{
			int mruleHashCode = 0;
			foreach (MorphologicalRule rule in m_mrules)
				mruleHashCode ^= rule.GetHashCode();

			return m_shape.GetHashCode() ^ m_morphs.GetHashCode()
				^ mruleHashCode ^ m_rzFeatures.GetHashCode()
				^ m_stratum.GetHashCode() ^ m_root.GetHashCode()
				^ (m_nonHead == null ? 0 : m_nonHead.GetHashCode());
		}

		public int CompareTo(WordSynthesis other)
		{
			IEnumerator<Morph> enum1 = Morphs.GetEnumerator();
			IEnumerator<Morph> enum2 = other.Morphs.GetEnumerator();
			while (enum1.MoveNext() && enum2.MoveNext())
			{
				int compare = enum1.Current.Allomorph.Morpheme.ID.CompareTo(enum2.Current.Allomorph.Morpheme.ID);
				if (compare != 0)
					return compare;

				compare = enum1.Current.Allomorph.CompareTo(enum2.Current.Allomorph);
				if (compare != 0)
					return compare;
			}
			return 0;
		}

		public override string ToString()
		{
			StringBuilder morphsSb = new StringBuilder();
			bool firstItem = true;
			foreach (Morph morph in m_morphs)
			{
				if (!firstItem)
					morphsSb.Append(" ");
				morphsSb.Append(morph.Allomorph.Morpheme.Gloss == null ? "?" : morph.Allomorph.Morpheme.Gloss.Description);
				firstItem = false;
			}

			return string.Format(HCStrings.kstidWordSynthesis, m_stratum.CharacterDefinitionTable.ToString(m_shape, ModeType.SYNTHESIS, true),
				m_pos, m_root.ID, m_stratum, morphsSb);
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public WordSynthesis Clone()
		{
			return new WordSynthesis(this);
		}
	}
}
