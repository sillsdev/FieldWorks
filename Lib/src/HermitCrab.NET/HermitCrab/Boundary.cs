using System.Collections.Generic;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a morpheme boundary in a phonetic shape.
	/// </summary>
	public class Boundary : PhoneticShapeNode
	{
		BoundaryDefinition m_bdryDef;

		/// <summary>
		/// Initializes a new instance of the <see cref="Boundary"/> class.
		/// </summary>
		/// <param name="strRep">The string representation.</param>
		/// <param name="charDefTable">The character definition table.</param>
		/// <param name="morpher">The morpher.</param>
		public Boundary(BoundaryDefinition bdryDef)
		{
			m_bdryDef = bdryDef;
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="bdry">The bdry.</param>
		public Boundary(Boundary bdry)
			: base(bdry)
		{
			m_bdryDef = bdry.m_bdryDef;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Boundary"/> class from a boundary
		/// context.
		/// </summary>
		/// <param name="ctxt">The boundary context.</param>
		public Boundary(BoundaryContext ctxt)
		{
			m_bdryDef = ctxt.BoundaryDefinition;
		}

		/// <summary>
		/// Gets the phonetic shape node type.
		/// </summary>
		/// <value>The node type.</value>
		public override NodeType Type
		{
			get
			{
				return NodeType.BOUNDARY;
			}
		}

		public BoundaryDefinition BoundaryDefinition
		{
			get
			{
				return m_bdryDef;
			}
		}

		public override PhoneticShapeNode Clone()
		{
			return new Boundary(this);
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as Boundary);
		}

		public bool Equals(Boundary other)
		{
			if (other == null)
				return false;
			return m_bdryDef.Equals(other.m_bdryDef);
		}

		public override int GetHashCode()
		{
			return m_bdryDef.GetHashCode();
		}

		public override string ToString()
		{
			return m_bdryDef.StrRep;
		}
	}

	/// <summary>
	/// This class represents a boundary in a phonetic pattern. It is used to match against a boundary
	/// in a phonetic shape.
	/// </summary>
	public class BoundaryContext : PhoneticPatternNode
	{
		BoundaryDefinition m_bdryDef;

		/// <summary>
		/// Initializes a new instance of the <see cref="BoundaryContext"/> class.
		/// </summary>
		/// <param name="strRep">The string representation.</param>
		/// <param name="charDefTable">The character definition table.</param>
		/// <param name="morpher">The morpher.</param>
		public BoundaryContext(BoundaryDefinition bdryDef)
		{
			m_bdryDef = bdryDef;
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="ctxt">The boundary context.</param>
		public BoundaryContext(BoundaryContext ctxt)
			: base(ctxt)
		{
			m_bdryDef = ctxt.m_bdryDef;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BoundaryContext"/> class from a boundary
		/// object.
		/// </summary>
		/// <param name="bdry">The bdry.</param>
		public BoundaryContext(Boundary bdry)
		{
			m_bdryDef = bdry.BoundaryDefinition;
		}

		/// <summary>
		/// Gets the node type.
		/// </summary>
		/// <value>The node type.</value>
		public override NodeType Type
		{
			get
			{
				return NodeType.BDRY_CTXT;
			}
		}

		public override IEnumerable<Feature> Features
		{
			get
			{
				yield break;
			}
		}

		public BoundaryDefinition BoundaryDefinition
		{
			get
			{
				return m_bdryDef;
			}
		}

		public override bool IsFeatureReferenced(Feature feature)
		{
			return false;
		}

		/// <summary>
		/// Checks if the specified phonetic shape node matches this boundary context.
		/// </summary>
		/// <param name="node">The phonetic shape node.</param>
		/// <param name="dir">The direction.</param>
		/// <param name="mode">The mode.</param>
		/// <param name="instantiatedVars">The instantiated variables.</param>
		/// <returns>All matches.</returns>
		internal override IList<Match> Match(PhoneticShapeNode node, Direction dir, ModeType mode,
			VariableValues instantiatedVars)
		{
			// only match boundaries in synthesis mode
			if (mode == ModeType.SYNTHESIS)
			{
				switch (node.Type)
				{
					case PhoneticShapeNode.NodeType.BOUNDARY:
						Boundary bdry = node as Boundary;
						// check if string representations match
						if (m_bdryDef.StrRep != bdry.BoundaryDefinition.StrRep)
							return new List<Match>();
						// move to next node
						IList<Match> matches = MatchNext(node, dir, mode, instantiatedVars);
						foreach (Match match in matches)
							match.Add(node, m_partition);
						return matches;

					case PhoneticShapeNode.NodeType.MARGIN:
						Margin margin = node as Margin;
						if (dir == margin.MarginType)
							// we are at the end of the phonetic shape, so it does not match
							return new List<Match>();
						else
							return Match(GetNextShapeNode(node, dir), dir, mode, instantiatedVars);
				}

				return new List<Match>();
			}
			else
			{
				PhoneticPatternNode n = GetNext(dir);
				if (n == null)
				{
					// this was the last node in the pattern, so we have a match
					List<Match> matches = new List<Match>();
					matches.Add(new Match(Owner, instantiatedVars));
					return matches;
				}
				else
				{
					return n.Match(node, dir, mode, instantiatedVars);
				}
			}
		}

		public override PhoneticPatternNode Clone()
		{
			return new BoundaryContext(this);
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as BoundaryContext);
		}

		public bool Equals(BoundaryContext other)
		{
			if (other == null)
				return false;
			return m_bdryDef.Equals(other.m_bdryDef);
		}

		public override int GetHashCode()
		{
			return m_bdryDef.GetHashCode();
		}

		public override string ToString()
		{
			return m_bdryDef.StrRep;
		}
	}
}
