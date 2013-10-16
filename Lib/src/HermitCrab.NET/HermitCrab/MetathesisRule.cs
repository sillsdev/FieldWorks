using System.Collections.Generic;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a metathesis rule. Metathesis rules are phonlogical rules that
	/// reorder segments.
	/// </summary>
	public class MetathesisRule : PhonologicalRule
	{
		MultAppOrder m_multApplication = MultAppOrder.LR_ITERATIVE;
		PhoneticPattern m_lhsTemp;
		PhoneticPattern m_rhsTemp;

		/// <summary>
		/// Initializes a new instance of the <see cref="MetathesisRule"/> class.
		/// </summary>
		/// <param name="id">The ID.</param>
		/// <param name="desc">The description.</param>
		/// <param name="morpher">The morpher.</param>
		public MetathesisRule(string id, string desc, Morpher morpher)
			: base(id, desc, morpher)
		{
		}

		/// <summary>
		/// Gets or sets the multiple application order.
		/// </summary>
		/// <value>The multiple application order.</value>
		public override MultAppOrder MultApplication
		{
			get
			{
				return m_multApplication;
			}

			set
			{
				m_multApplication = value;
			}
		}

		/// <summary>
		/// Gets or sets the LHS pattern. This pattern should be marked with partition
		/// numbers ordered according to how the partitions will be reordered during
		/// application.
		/// </summary>
		/// <value>The pattern.</value>
		public PhoneticPattern Pattern
		{
			get
			{
				return m_lhsTemp;
			}

			set
			{
				m_lhsTemp = value;

				// generate RHS template
				m_rhsTemp = new PhoneticPattern();
				List<int> parts = new List<int>();
				int i = 0;
				// sort each node in the LHS template by partition number and copy them
				// to the RHS template
				foreach (PhoneticPatternNode lhsNode in m_lhsTemp)
				{
					// simply copy over margin contexts
					if (lhsNode.Type == PhoneticPatternNode.NodeType.MARGIN_CTXT)
					{
						m_rhsTemp.Add(lhsNode.Clone());
						continue;
					}

					bool added = false;
					int j = 0;
					foreach (PhoneticPatternNode rhsNode in m_rhsTemp)
					{
						if (rhsNode.Type == PhoneticPatternNode.NodeType.MARGIN_CTXT)
							continue;

						if ((j == 0 || lhsNode.Partition > parts[j - 1]) && lhsNode.Partition < parts[j])
						{
							PhoneticPatternNode newNode = lhsNode.Clone();
							newNode.Partition = i;
							rhsNode.Insert(newNode, Direction.LEFT);
							parts.Insert(j, lhsNode.Partition);
							added = true;
							break;
						}
						j++;
					}
					if (!added)
					{
						PhoneticPatternNode newNode = lhsNode.Clone();
						newNode.Partition = i;
						m_rhsTemp.Add(newNode);
						parts.Add(lhsNode.Partition);
					}
					i++;
				}
			}
		}

		/// <summary>
		/// Unapplies the rule to the specified word analysis.
		/// </summary>
		/// <param name="input">The input word analysis.</param>
		/// <param name="trace"></param>
		public override void Unapply(WordAnalysis input, TraceManager trace)
		{
			// I don't think there is any difference between iterative and
			// simultaneous application
			var dir = Direction.RIGHT;
			switch (m_multApplication)
			{
				case MultAppOrder.LR_ITERATIVE:
				case MultAppOrder.SIMULTANEOUS:
					dir = Direction.LEFT;
					break;

				case MultAppOrder.RL_ITERATIVE:
					dir = Direction.RIGHT;
					break;
			}

			ProcessIterative(input.Shape, dir, m_rhsTemp, ModeType.ANALYSIS);
		}

		/// <summary>
		/// Applies the rule to the specified word synthesis.
		/// </summary>
		/// <param name="input">The word synthesis.</param>
		/// <param name="trace"></param>
		public override void Apply(WordSynthesis input, TraceManager trace)
		{
			// I don't think there is any difference between iterative and
			// simultaneous application
			var dir = Direction.RIGHT;
			switch (m_multApplication)
			{
				case MultAppOrder.LR_ITERATIVE:
				case MultAppOrder.SIMULTANEOUS:
					dir = Direction.RIGHT;
					break;

				case MultAppOrder.RL_ITERATIVE:
					dir = Direction.LEFT;
					break;
			}

			ProcessIterative(input.Shape, dir, m_lhsTemp, ModeType.SYNTHESIS);
		}

		bool ProcessIterative(PhoneticShape input, Direction dir, PhoneticPattern ptemp, ModeType mode)
		{
			bool reordered = false;
			PhoneticShapeNode node = input.GetFirst(dir);
			Match match;
			// iterate thru each match
			while (FindNextMatch(node, dir, ptemp, mode, out match))
			{
				// reorder the matching segments
				Reorder(dir, match);
				reordered = true;
				IList<PhoneticShapeNode> nodes = match.EntireMatch;
				node = nodes[nodes.Count - 1].GetNext(dir);
			}

			return reordered;
		}

		bool FindNextMatch(PhoneticShapeNode node, Direction dir, PhoneticPattern ptemp, ModeType mode,
			out Match match)
		{
			for (; node != node.Owner.GetLast(dir); node = node.GetNext(dir))
			{
				if (mode == ModeType.ANALYSIS && node.Type == PhoneticShapeNode.NodeType.BOUNDARY)
					continue;

				IList<Match> matches;
				if (ptemp.IsMatch(node, dir, mode, out matches))
				{
					match = matches[0];
					return true;
				}
			}

			match = null;
			return false;
		}

		void Reorder(Direction dir, Match match)
		{
			if (match.EntireMatch.Count == 0)
				return;

			PhoneticShapeNode first = null;
			PhoneticShapeNode last = null;
			switch (dir)
			{
				case Direction.RIGHT:
					first = match.EntireMatch[0];
					last = match.EntireMatch[match.EntireMatch.Count - 1];
					break;

				case Direction.LEFT:
					first = match.EntireMatch[match.EntireMatch.Count - 1];
					last = match.EntireMatch[0];
					break;
			}

			// remove the matching segments, so that we can reinsert them in the
			// new order
			PhoneticShapeNode cur = first.Prev;
			for (PhoneticShapeNode node = first; node != last.Next; node = node.Next)
				node.Remove();

			// reinsert the segments in the new order
			for (int i = 0; i < m_lhsTemp.Count; i++)
			{
				IList<PhoneticShapeNode> partNodes = match.GetPartition(i);
				if (partNodes == null)
					continue;

				IEnumerable<PhoneticShapeNode> partEnum = dir == Direction.RIGHT ? partNodes
					: ReverseNodes(partNodes);
				foreach (PhoneticShapeNode node in partEnum)
				{
					cur.Insert(node, Direction.RIGHT);
					cur = node;
				}
			}
		}

		IEnumerable<PhoneticShapeNode> ReverseNodes(IList<PhoneticShapeNode> nodes)
		{
			for (int i = nodes.Count - 1; i >= 0; i--)
				yield return nodes[i];
		}
	}
}
