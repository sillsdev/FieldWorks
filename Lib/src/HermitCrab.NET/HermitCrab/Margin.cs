using System.Collections.Generic;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This represents a right or left word boundary in a phonetic shape.
	/// </summary>
	public class Margin : PhoneticShapeNode
	{
		Direction m_marginType;

		/// <summary>
		/// Initializes a new instance of the <see cref="Margin"/> class.
		/// </summary>
		/// <param name="marginType">Type of the margin.</param>
		public Margin(Direction marginType)
		{
			m_marginType = marginType;
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="margin">The margin.</param>
		public Margin(Margin margin)
			: base(margin)
		{
			m_marginType = margin.m_marginType;
		}

		/// <summary>
		/// Gets the phonetic shape node type.
		/// </summary>
		/// <value>The node type.</value>
		public override NodeType Type
		{
			get
			{
				return NodeType.MARGIN;
			}
		}

		/// <summary>
		/// Gets the type of the margin.
		/// </summary>
		/// <value>The type of the margin.</value>
		public Direction MarginType
		{
			get
			{
				return m_marginType;
			}
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as Margin);
		}

		public bool Equals(Margin other)
		{
			if (other == null)
				return false;
			return m_marginType == other.m_marginType;
		}

		public override int GetHashCode()
		{
			return m_marginType == Direction.RIGHT ? 0 : 1;
		}

		public override PhoneticShapeNode Clone()
		{
			return new Margin(this);
		}

		public override string ToString()
		{
			return "";
		}
	}

	/// <summary>
	/// This represents a left or right word boundary in a phonetic pattern.
	/// </summary>
	public class MarginContext : PhoneticPatternNode
	{
		Direction m_marginType;

		/// <summary>
		/// Initializes a new instance of the <see cref="MarginContext"/> class.
		/// </summary>
		/// <param name="marginType">Type of the margin.</param>
		public MarginContext(Direction marginType)
		{
			m_marginType = marginType;
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="margin">The margin context.</param>
		public MarginContext(MarginContext ctxt)
			: base(ctxt)
		{
			m_marginType = ctxt.m_marginType;
		}

		/// <summary>
		/// Gets the phonetic sequence node type.
		/// </summary>
		/// <value>The node type.</value>
		public override NodeType Type
		{
			get
			{
				return NodeType.MARGIN_CTXT;
			}
		}

		public override IEnumerable<Feature> Features
		{
			get
			{
				yield break;
			}
		}

		/// <summary>
		/// Gets the type of the margin.
		/// </summary>
		/// <value>The type of the margin.</value>
		public Direction MarginType
		{
			get
			{
				return m_marginType;
			}
		}

		public override bool IsFeatureReferenced(Feature feature)
		{
			return false;
		}

		/// <summary>
		/// Checks if the specified phonetic shape node matches this margin context.
		/// </summary>
		/// <param name="node">The phonetic shape node.</param>
		/// <param name="dir">The direction.</param>
		/// <param name="mode">The mode.</param>
		/// <param name="instantiatedVars">The instantiated variables.</param>
		/// <returns>All matches.</returns>
		internal override IList<Match> Match(PhoneticShapeNode node, Direction dir, ModeType mode,
			VariableValues instantiatedVars)
		{
			switch (node.Type)
			{
				case PhoneticShapeNode.NodeType.MARGIN:
					Margin margin = node as Margin;
					if (m_marginType != margin.MarginType)
						return new List<Match>();

					// move to next node
					return MatchNext(node, dir, mode, instantiatedVars);

				case PhoneticShapeNode.NodeType.BOUNDARY:
					return Match(GetNextShapeNode(node, dir), dir, mode, instantiatedVars);
			}

			return new List<Match>();
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as MarginContext);
		}

		public bool Equals(MarginContext other)
		{
			if (other == null)
				return false;
			return m_marginType == other.m_marginType;
		}

		public override int GetHashCode()
		{
			return m_marginType == Direction.RIGHT ? 0 : 1;
		}

		public override PhoneticPatternNode Clone()
		{
			return new MarginContext(this);
		}

		public override string ToString()
		{
			return "";
		}
	}
}
