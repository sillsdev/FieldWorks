using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a match between a phonetic shape and a phonetic pattern.
	/// </summary>
	public class Match : IComparable<Match>
	{
		private readonly List<PhoneticShapeNode> m_entireMatch;
		private readonly Dictionary<int, List<PhoneticShapeNode>> m_partitions;
		private VariableValues m_instantiatedVars;
		private readonly PhoneticPattern m_pattern;

		/// <summary>
		/// Initializes a new instance of the <see cref="Match"/> class.
		/// </summary>
		/// <param name="pattern"></param>
		/// <param name="vars">The instantiated variables.</param>
		internal Match(PhoneticPattern pattern, VariableValues vars)
		{
			m_pattern = pattern;
			m_partitions = new Dictionary<int, List<PhoneticShapeNode>>();
			m_entireMatch = new List<PhoneticShapeNode>();
			m_instantiatedVars = vars.Clone();
		}

		/// <summary>
		/// Gets the pattern.
		/// </summary>
		public PhoneticPattern Pattern
		{
			get { return m_pattern; }
		}

		/// <summary>
		/// Gets the entire match.
		/// </summary>
		/// <value>The entire match.</value>
		public IList<PhoneticShapeNode> EntireMatch
		{
			get
			{
				return m_entireMatch;
			}
		}

		/// <summary>
		/// Gets or sets the instantiated variables.
		/// </summary>
		/// <value>The instantiated variables.</value>
		public VariableValues VariableValues
		{
			get
			{
				return m_instantiatedVars;
			}

			internal set
			{
				m_instantiatedVars = value;
			}
		}

		/// <summary>
		/// Adds the specified phonetic shape node.
		/// </summary>
		/// <param name="seg">The phonetic shape node.</param>
		internal void Add(PhoneticShapeNode node)
		{
			Add(node, -1);
		}

		/// <summary>
		/// Adds the specified phonetic shape node to the specified partition.
		/// </summary>
		/// <param name="seg">The phonetic shape node.</param>
		/// <param name="partition">The partition.</param>
		internal void Add(PhoneticShapeNode node, int partition)
		{
			if (partition >= 0)
			{
				List<PhoneticShapeNode> nodes = GetNodes(partition);
				nodes.Insert(0, node);
			}

			m_entireMatch.Insert(0, node);
		}

		List<PhoneticShapeNode> GetNodes(int partition)
		{
			List<PhoneticShapeNode> nodes;
			if (!m_partitions.TryGetValue(partition, out nodes))
			{
				nodes = new List<PhoneticShapeNode>();
				m_partitions[partition] = nodes;
			}
			return nodes;
		}

		/// <summary>
		/// Gets the subset of phonetic shape nodes that matched the specified partition.
		/// </summary>
		/// <param name="partition">The partition.</param>
		/// <returns>The list of matching nodes, or <c>null</c> if there were no matching nodes</returns>
		public IList<PhoneticShapeNode> GetPartition(int partition)
		{
			List<PhoneticShapeNode> nodes;
			if (m_partitions.TryGetValue(partition, out nodes))
				return nodes;
			return null;
		}

		public int CompareTo(Match other)
		{
			if (m_entireMatch.Count > other.m_entireMatch.Count)
				return -1;
			if (m_entireMatch.Count < other.m_entireMatch.Count)
				return 1;

			if (m_partitions.Count > other.m_partitions.Count)
				return -1;
			if (m_partitions.Count < other.m_partitions.Count)
				return 1;

			foreach (int partition in m_pattern.Partitions)
			{
				if (m_pattern.IsPartitionGreedy(partition))
				{
					int partitionCount = 0;
					List<PhoneticShapeNode> nodes;
					if (m_partitions.TryGetValue(partition, out nodes))
						partitionCount = nodes.Count;
					int otherPartitionCount = 0;
					List<PhoneticShapeNode> otherNodes;
					if (other.m_partitions.TryGetValue(partition, out otherNodes))
						otherPartitionCount = otherNodes.Count;

					if (partitionCount > otherPartitionCount)
						return -1;
					if (partitionCount < otherPartitionCount)
						return 1;
				}
			}

			return 0;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			foreach (Segment seg in m_entireMatch)
				sb.Append(seg.ToString());
			return sb.ToString();
		}
	}

	/// <summary>
	/// This is the abstract class that all phonetic pattern nodes extend.
	/// </summary>
	public abstract class PhoneticPatternNode : HCLinkedListNode<PhoneticPatternNode, PhoneticPattern>
	{
		/// <summary>
		/// This enumeration represents the node type.
		/// </summary>
		public enum NodeType { SIMP_CTXT, BDRY_CTXT, MARGIN_CTXT, PATTERN };

		/// <summary>
		/// Initializes a new instance of the <see cref="PhoneticPatternNode"/> class.
		/// </summary>
		public PhoneticPatternNode()
		{
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="node">The node.</param>
		public PhoneticPatternNode(PhoneticPatternNode node)
			: base(node)
		{
		}

		/// <summary>
		/// Gets the node type.
		/// </summary>
		/// <value>The node type.</value>
		public abstract NodeType Type
		{
			get;
		}

		/// <summary>
		/// Gets the features.
		/// </summary>
		/// <value>The features.</value>
		public abstract IEnumerable<Feature> Features
		{
			get;
		}

		/// <summary>
		/// Determines whether this node references the specified feature.
		/// </summary>
		/// <param name="feature">The feature.</param>
		/// <returns>
		/// 	<c>true</c> if the specified feature is referenced, otherwise <c>false</c>.
		/// </returns>
		public abstract bool IsFeatureReferenced(Feature feature);

		/// <summary>
		/// Checks if the specified phonetic shape node matches this phonetic pattern node.
		/// </summary>
		/// <param name="node">The phonetic shape node.</param>
		/// <param name="dir">The direction.</param>
		/// <param name="mode">The mode.</param>
		/// <param name="isTarget">if <c>true</c> the phonetic pattern is a target.</param>
		/// <param name="instantiatedVars">The instantiated variables.</param>
		/// <returns>All matches.</returns>
		internal abstract IList<Match> Match(PhoneticShapeNode node, Direction dir, ModeType mode,
			VariableValues instantiatedVars);

		protected IList<Match> MatchNext(PhoneticShapeNode node, Direction dir, ModeType mode,
			VariableValues instantiatedVars)
		{
			PhoneticPatternNode n = GetNext(dir);
			// skip boundaries in analysis mode
			if (mode == ModeType.ANALYSIS)
			{
				while (n != null && n.Type == NodeType.BDRY_CTXT)
					n = n.GetNext(dir);
			}

			List<Match> matches = new List<Match>();
			if (n == null)
			{
				// we are at the end of the pattern, so we have a match
				matches.Add(new Match(Owner, instantiatedVars));
			}
			else
			{
				// try skipping over optional shape nodes
				do
				{
					// try the next node in the pattern
					node = node.GetNext(dir);
					matches.AddRange(n.Match(node, dir, mode, instantiatedVars));
				}
				while (node != null && node.IsOptional);
			}

			return matches;
		}
	}

	/// <summary>
	/// This class represents a phonetic pattern that can be used to match against phonetic shapes. It
	/// can contain context objects which match against segments, boundaries, and margins. It can also
	/// contain nested patterns. It provides the ability to specify the minimum and maximum number of
	/// times to match against a shape. In practice, phonetic patterns are very similar in functionality
	/// to regular expressions and support most of the functionality provided by regular expressions,
	/// except the alternation operator (|).
	/// </summary>
	public class PhoneticPattern : HCLinkedList<PhoneticPatternNode, PhoneticPattern>, ICloneable
	{
		private readonly bool m_isTarget;
		private readonly Dictionary<int, bool> m_partitions = new Dictionary<int, bool>();

		/// <summary>
		/// Initializes a new instance of the <see cref="PhoneticPattern"/> class.
		/// </summary>
		public PhoneticPattern()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PhoneticPattern"/> class.
		/// </summary>
		/// <param name="isTarget">if <c>true</c> the phonetic pattern is a target.</param>
		public PhoneticPattern(bool isTarget)
		{
			m_isTarget = isTarget;
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="pattern">The phonetic pattern.</param>
		public PhoneticPattern(PhoneticPattern pattern)
			: base(pattern)
		{
			m_isTarget = pattern.m_isTarget;
		}

		/// <summary>
		/// Gets a value indicating whether this instance is a phonological target.
		/// Phonological targets are the LHS and RHS of the rule, and match somewhat differently
		/// than normal patterns.
		/// </summary>
		/// <value><c>true</c> if this instance is a target, otherwise <c>false</c>.</value>
		public bool IsTarget
		{
			get
			{
				return m_isTarget;
			}
		}

		/// <summary>
		/// Gets all of the features referenced in this phonetic pattern.
		/// </summary>
		/// <value>The features.</value>
		public IEnumerable<Feature> Features
		{
			get
			{
				HCObjectSet<Feature> features = new HCObjectSet<Feature>();
				foreach (PhoneticPatternNode node in this)
					features.AddMany(node.Features);
				return features;
			}
		}

		/// <summary>
		/// Determines whether the phonetic pattern references the specified feature.
		/// </summary>
		/// <param name="feature">The feature.</param>
		/// <returns>
		/// 	<c>true</c> if the specified feature is referenced, otherwise <c>false</c>.
		/// </returns>
		public bool IsFeatureReferenced(Feature feature)
		{
			foreach (PhoneticPatternNode node in this)
			{
				if (node.IsFeatureReferenced(feature))
					return true;
			}
			return false;
		}

		public IEnumerable<int> Partitions
		{
			get { return m_partitions.Keys; }
		}

		public bool IsPartitionGreedy(int partition)
		{
			return m_partitions[partition];
		}

		public void AddPartition(IEnumerable<PhoneticPatternNode> nodes, int partition, bool greedy)
		{
			AddPartition(nodes, partition);
			m_partitions[partition] = greedy;
		}

		public bool IsMatch(PhoneticShapeNode node, Direction dir, ModeType mode, out IList<Match> matches)
		{
			return IsMatch(node, dir, mode, new VariableValues(), out matches);
		}

		public bool IsMatch(PhoneticShapeNode node, Direction dir, ModeType mode)
		{
			return IsMatch(node, dir, mode, new VariableValues());
		}

		public bool IsMatch(PhoneticShapeNode node, Direction dir, ModeType mode, VariableValues instantiatedVars)
		{
			IList<Match> matches;
			return IsMatch(node, dir, mode, instantiatedVars, out matches);
		}

		/// <summary>
		/// Checks if a phonetic shape matches this pattern starting at the specified node.
		/// </summary>
		/// <param name="node">The phonetic shape node.</param>
		/// <param name="dir">The direction.</param>
		/// <param name="mode">The mode.</param>
		/// <param name="instantiatedVars">The instantiated variables.</param>
		/// <param name="matches">The matches.</param>
		/// <returns><c>true</c> if the shape successfully matched this pattern, otherwise <c>false</c></returns>
		public bool IsMatch(PhoneticShapeNode node, Direction dir, ModeType mode, VariableValues instantiatedVars,
			out IList<Match> matches)
		{
			List<Match> matchesList = new List<Match>();
			matches = matchesList;

			PhoneticPatternNode n = GetFirst(dir);
			// skip boundaries during analysis
			if (mode == ModeType.ANALYSIS)
			{
				while (n != null && n.Type == PhoneticPatternNode.NodeType.BDRY_CTXT)
					n = n.GetNext(dir);
			}

			if (n == null)
			{
				// we are at the end of the pattern, so it is a match
				matchesList.Add(new Match(this, instantiatedVars));
			}
			else
			{
				PhoneticShapeNode cur = node;
				PhoneticShapeNode prev = null;
				do
				{
					matchesList.AddRange(n.Match(cur, dir, mode, instantiatedVars));
					prev = cur;
					if (cur != null)
						cur = cur.GetNext(dir);
				}
				while (cur != null && prev.IsOptional);
			}

			//matchesList.Sort();
			return matchesList.Count > 0;
		}

		/// <summary>
		/// Combines this pattern with the specified pattern. This is used by phonological rules to
		/// generate the RHS target and by morphological rules to generate the RHS template for
		/// modify-from output records. The patterns must be the same size.
		/// </summary>
		/// <param name="pattern">The pattern.</param>
		/// <returns>The phonetic pattern which is a combination of this pattern and the specified pattern.</returns>
		/// <exception cref="System.ArgumentException">Thrown when the size of <c>pattern</c> does not match the size of this pattern.</exception>
		public PhoneticPattern Combine(PhoneticPattern pattern)
		{
			if (Count != pattern.Count)
				throw new ArgumentException(HCStrings.kstidPatternCombine, "pattern");

			PhoneticPattern result = new PhoneticPattern();
			PhoneticPatternNode lhsNode = pattern.First;
			foreach (PhoneticPatternNode rhsNode in this)
			{
				// combine the simple contexts
				if (rhsNode.Type == PhoneticPatternNode.NodeType.SIMP_CTXT
					&& lhsNode.Type == PhoneticPatternNode.NodeType.SIMP_CTXT)
				{
					SimpleContext rhsCtxt = rhsNode as SimpleContext;
					SimpleContext lhsCtxt = lhsNode as SimpleContext;

					result.Add(rhsCtxt.Combine(lhsCtxt));
				}
				lhsNode = lhsNode.GetNext();
			}

			return result;
		}

		object ICloneable.Clone()
		{
			return this.Clone();
		}

		public PhoneticPattern Clone()
		{
			return new PhoneticPattern(this);
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			foreach (PhoneticPatternNode node in this)
				sb.Append(node.ToString());
			return sb.ToString();
		}
	}

	/// <summary>
	/// This class represents a phonological environment.
	/// </summary>
	public class Environment
	{
		PhoneticPattern m_leftEnv = null;
		PhoneticPattern m_rightEnv = null;

		/// <summary>
		/// Initializes a new instance of the <see cref="Environment"/> class.
		/// </summary>
		public Environment()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Environment"/> class.
		/// </summary>
		/// <param name="leftEnv">The left environment.</param>
		/// <param name="rightEnv">The right environment.</param>
		public Environment(PhoneticPattern leftEnv, PhoneticPattern rightEnv)
		{
			m_leftEnv = leftEnv;
			m_rightEnv = rightEnv;
		}

		/// <summary>
		/// Gets the left environment.
		/// </summary>
		/// <value>The left environment.</value>
		public PhoneticPattern LeftEnvironment
		{
			get
			{
				return m_leftEnv;
			}
		}

		/// <summary>
		/// Gets the right environment.
		/// </summary>
		/// <value>The right environment.</value>
		public PhoneticPattern RightEnvironment
		{
			get
			{
				return m_rightEnv;
			}
		}

		/// <summary>
		/// Checks if the specified phonetic shape matches this environment.
		/// </summary>
		/// <param name="leftNode">The left node.</param>
		/// <param name="rightNode">The right node.</param>
		/// <param name="mode">The mode.</param>
		/// <returns>
		/// 	<c>true</c> if the specified left node is match; otherwise, <c>false</c>.
		/// </returns>
		public bool IsMatch(PhoneticShapeNode leftNode, PhoneticShapeNode rightNode, ModeType mode)
		{
			return IsMatch(leftNode, rightNode, mode, new VariableValues());
		}

		/// <summary>
		/// Checks if the specified phonetic shape matches this environment.
		/// </summary>
		/// <param name="leftNode">The left node.</param>
		/// <param name="rightNode">The right node.</param>
		/// <param name="mode">The mode.</param>
		/// <param name="instantiatedVars">The instantiated variables.</param>
		/// <returns>
		/// 	<c>true</c> if the shape successfully matched this pattern, otherwise <c>false</c>.
		/// </returns>
		public bool IsMatch(PhoneticShapeNode leftNode, PhoneticShapeNode rightNode, ModeType mode, VariableValues instantiatedVars)
		{
			VariableValues temp = instantiatedVars.Clone();
			// right environment
			if (m_rightEnv != null)
			{
				IList<Match> matches;
				if (m_rightEnv.IsMatch(rightNode, Direction.RIGHT, mode, temp, out matches))
					temp.ReplaceValues(matches[0].VariableValues);
				else
					return false;
			}

			// left environment
			if (m_leftEnv != null)
			{
				IList<Match> matches;
				if (m_leftEnv.IsMatch(leftNode, Direction.LEFT, mode, temp, out matches))
					temp.ReplaceValues(matches[0].VariableValues);
				else
					return false;
			}

			instantiatedVars.ReplaceValues(temp);
			return true;
		}

		public override bool Equals(object obj)
		{
			return obj != null && Equals(obj as Environment);
		}

		public bool Equals(Environment other)
		{
			if (other == null)
				return false;

			if (m_leftEnv == null)
			{
				if (other.m_leftEnv != null)
					return false;
			}
			else
			{
				if (!m_leftEnv.Equals(other.m_leftEnv))
					return false;
			}

			if (m_rightEnv == null)
			{
				if (other.m_rightEnv != null)
					return false;
			}
			else
			{
				if (!m_rightEnv.Equals(other.m_rightEnv))
					return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			return (m_leftEnv == null ? 0 : m_leftEnv.GetHashCode()) ^ (m_rightEnv == null ? 0 : m_rightEnv.GetHashCode());
		}
	}
}
