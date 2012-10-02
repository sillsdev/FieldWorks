using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a nested phonetic pattern within another phonetic pattern.
	/// </summary>
	public class NestedPhoneticPattern : PhoneticPatternNode
	{
		private readonly int m_minOccur;
		private readonly int m_maxOccur;
		private readonly PhoneticPattern m_pattern;

		/// <summary>
		/// Initializes a new instance of the <see cref="NestedPhoneticPattern"/> class.
		/// </summary>
		/// <param name="pattern">The phonetic pattern.</param>
		/// <param name="minOccur">The minimum number of occurrences.</param>
		/// <param name="maxOccur">The maximum number of occurrences.</param>
		public NestedPhoneticPattern(PhoneticPattern pattern, int minOccur, int maxOccur)
		{
			m_pattern = pattern;
			m_minOccur = minOccur;
			m_maxOccur = maxOccur;
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="nestedPattern">The nested pattern.</param>
		public NestedPhoneticPattern(NestedPhoneticPattern nestedPattern)
			: base(nestedPattern)
		{
			m_pattern = new PhoneticPattern(nestedPattern.m_pattern);
			m_minOccur = nestedPattern.m_minOccur;
			m_maxOccur = nestedPattern.m_maxOccur;
		}

		/// <summary>
		/// Gets the node type.
		/// </summary>
		/// <value>The node type.</value>
		public override NodeType Type
		{
			get
			{
				return NodeType.PATTERN;
			}
		}

		/// <summary>
		/// Gets the features.
		/// </summary>
		/// <value>The features.</value>
		public override IEnumerable<Feature> Features
		{
			get
			{
				return m_pattern.Features;
			}
		}

		/// <summary>
		/// Gets the phonetic pattern.
		/// </summary>
		/// <value>The phonetic pattern.</value>
		public PhoneticPattern Pattern
		{
			get
			{
				return m_pattern;
			}
		}

		/// <summary>
		/// Gets the minimum number of occurrences of this pattern.
		/// </summary>
		/// <value>The minimum number of occurrences.</value>
		public int MinOccur
		{
			get
			{
				return m_minOccur;
			}
		}

		/// <summary>
		/// Gets the maximum number of occurrences of this pattern.
		/// </summary>
		/// <value>The maximum number of occurrences.</value>
		public int MaxOccur
		{
			get
			{
				return m_maxOccur;
			}
		}

		/// <summary>
		/// Determines whether this node references the specified feature.
		/// </summary>
		/// <param name="feature">The feature.</param>
		/// <returns>
		/// 	<c>true</c> if the specified feature is referenced, otherwise <c>false</c>.
		/// </returns>
		public override bool IsFeatureReferenced(Feature feature)
		{
			return m_pattern.IsFeatureReferenced(feature);
		}

		/// <summary>
		/// Checks if the specified phonetic shape node matches this nested phonetic pattern.
		/// </summary>
		/// <param name="node">The phonetic shape node.</param>
		/// <param name="dir">The direction.</param>
		/// <param name="mode">The mode.</param>
		/// <param name="instantiatedVars">The instantiated variables.</param>
		/// <returns>All matches.</returns>
		internal override IList<Match> Match(PhoneticShapeNode node, Direction dir, ModeType mode,
			VariableValues instantiatedVars)
		{
			Set<Match> candidates = new Set<Match>(new MatchComparer());
			Queue<KeyValuePair<Match, int>> queue = new Queue<KeyValuePair<Match, int>>();
			IList<Match> firstMatches;
			// collect first set of matches and add them to the queue
			m_pattern.IsMatch(node, dir, mode, instantiatedVars, out firstMatches);
			foreach (Match match in firstMatches)
			{
				if (match.EntireMatch.Count == 0)
					continue;

				// only add it to the list of candidates if it long enough
				if (m_minOccur <= 1)
				{
					if (!candidates.Contains(match))
					{
						candidates.Add(match);
						queue.Enqueue(new KeyValuePair<Match, int>(match, 1));
					}
				}
				else
				{
					queue.Enqueue(new KeyValuePair<Match, int>(match, 1));
				}
			}

			while (queue.Count > 0)
			{
				KeyValuePair<Match, int> pair = queue.Dequeue();
				// if we hit upper limit then do not process this match any longer
				if (m_maxOccur > -1 && pair.Value >= m_maxOccur)
					continue;

				IList<PhoneticShapeNode> nodes = pair.Key.EntireMatch;
				PhoneticShapeNode n = nodes[nodes.Count - 1].GetNext(dir);

				IList<Match> curMatches;
				m_pattern.IsMatch(n, dir, mode, pair.Key.VariableValues, out curMatches);
				foreach (Match match in curMatches)
				{
					if (match.EntireMatch.Count == 0)
						continue;

					for (PhoneticShapeNode curNode = nodes[nodes.Count - 1]; curNode != nodes[0].GetPrev(dir); curNode = curNode.GetPrev(dir))
						match.Add(curNode);

					// only add to the list of candidates if it is long enough
					if (m_minOccur <= pair.Value + 1)
					{
						if (!candidates.Contains(match))
						{
							candidates.Add(match);
							queue.Enqueue(new KeyValuePair<Match, int>(match, pair.Value + 1));
						}
					}
					else
					{
						queue.Enqueue(new KeyValuePair<Match, int>(match, pair.Value + 1));
					}
				}
			}

			// iterate thru list of candidates and see if they match the rest of the pattern
			List<Match> matches = new List<Match>();
			foreach (Match candidate in candidates)
			{
				IList<PhoneticShapeNode> nodes = candidate.EntireMatch;
				IList<Match> curMatches = MatchNext(nodes[nodes.Count - 1], dir, mode, candidate.VariableValues);
				foreach (Match match in curMatches)
				{
					for (PhoneticShapeNode curNode = nodes[nodes.Count - 1]; curNode != nodes[0].GetPrev(dir); curNode = curNode.GetPrev(dir))
						match.Add(curNode, m_partition);
					matches.Add(match);
				}
			}

			// finally, if this pattern can occur 0 times, then collect any 0 length matches
			if (m_minOccur == 0)
			{
				PhoneticPatternNode n = GetNext(dir);
				if (n == null)
					matches.Add(new Match(Owner, instantiatedVars));
				else
					matches.AddRange(n.Match(node, dir, mode, instantiatedVars));
			}
			matches.Sort();

			return matches;
		}

		public override string ToString()
		{
			return "(" + m_pattern + ")";
		}

		public override int GetHashCode()
		{
			return m_pattern.GetHashCode() ^ m_minOccur ^ m_maxOccur;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as NestedPhoneticPattern);
		}

		public bool Equals(NestedPhoneticPattern other)
		{
			if (other == null)
				return false;
			return m_pattern.Equals(other.m_pattern) && m_minOccur == other.m_minOccur
				&& m_maxOccur == other.m_maxOccur;
		}

		public override PhoneticPatternNode Clone()
		{
			return new NestedPhoneticPattern(this);
		}

		class MatchComparer : IEqualityComparer<Match>
		{
			public bool Equals(Match x, Match y)
			{
				if (ReferenceEquals(x, y))
					return true;

				if (x == null || y == null)
					return false;

				if (x.EntireMatch.Count == 0 && y.EntireMatch.Count == 0)
					return true;

				if (x.EntireMatch.Count == 0 || y.EntireMatch.Count == 0)
					return false;

				return x.EntireMatch[x.EntireMatch.Count - 1] == y.EntireMatch[y.EntireMatch.Count - 1];
			}

			public int GetHashCode(Match obj)
			{
				return obj.EntireMatch.Count == 0 ? 0 : RuntimeHelpers.GetHashCode(obj.EntireMatch[obj.EntireMatch.Count - 1]);
			}
		}
	}
}
