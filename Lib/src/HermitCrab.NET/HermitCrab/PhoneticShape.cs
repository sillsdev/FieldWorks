using System;
using System.Text;
using System.Collections.Generic;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This is the abstract class that all phonetic shape nodes extend.
	/// </summary>
	public abstract class PhoneticShapeNode : HCLinkedListNode<PhoneticShapeNode, PhoneticShape>
	{
		/// <summary>
		/// The phonetic shape node type
		/// </summary>
		public enum NodeType { SEGMENT, BOUNDARY, MARGIN };

		protected bool m_isOptional = false;

		/// <summary>
		/// Initializes a new instance of the <see cref="PhoneticShapeNode"/> class.
		/// </summary>
		public PhoneticShapeNode()
		{
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="node">The node.</param>
		public PhoneticShapeNode(PhoneticShapeNode node)
			: base(node)
		{
			m_isOptional = node.m_isOptional;
		}

		/// <summary>
		/// Gets the phonetic shape node type.
		/// </summary>
		/// <value>The node type.</value>
		public abstract NodeType Type
		{
			get;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this node is optional.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this node is optional, otherwise <c>false</c>.
		/// </value>
		public bool IsOptional
		{
			get
			{
				return m_isOptional;
			}

			set
			{
				m_isOptional = value;
			}
		}
	}

	/// <summary>
	/// This class represents a sequence of phonetic segments and boundaries. All phonetic shapes should
	/// contain a left and right margin. You can use the methods and attributes provided by the <see cref="HCLinkedList"/>
	/// class to retrieve the first and last nodes to retrieve the margins. If you wish to retrieve the first
	/// or last segment/boundary in the shape, you can use the begin and end methods and attributes provided
	/// by this class.
	/// </summary>
	public class PhoneticShape : HCLinkedList<PhoneticShapeNode, PhoneticShape>, ICloneable
	{
		public const int MAX_SIZE = 256;

		/// <summary>
		/// Initializes a new instance of the <see cref="PhoneticShape"/> class.
		/// </summary>
		public PhoneticShape()
		{
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="pshape">The pshape.</param>
		public PhoneticShape(PhoneticShape pshape)
			: base(pshape)
		{
		}

		public bool IsAllOptional
		{
			get
			{
				for (PhoneticShapeNode node = Begin; node != Last; node = node.Next)
				{
					if (!node.IsOptional)
						return false;
				}
				return true;
			}
		}

		public IEnumerable<PhoneticShapeNode> Segments
		{
			get
			{
				return GetSegments(Direction.RIGHT);
			}
		}

		/// <summary>
		/// Gets the first segment or boundary in this shape. It corresponds to the node to the right of the
		/// left margin.
		/// </summary>
		/// <value>The first segment or boundary.</value>
		public PhoneticShapeNode Begin
		{
			get
			{
				return First.Next;
			}
		}

		/// <summary>
		/// Gets the last segment or boundary in this shape. It corresponds to the node to the left of the
		/// right margin.
		/// </summary>
		/// <value>The last segment or boundary.</value>
		public PhoneticShapeNode End
		{
			get
			{
				return Last.Prev;
			}
		}

		public IEnumerable<PhoneticShapeNode> GetSegments()
		{
			return Segments;
		}

		public IEnumerable<PhoneticShapeNode> GetSegments(Direction dir)
		{
			for (PhoneticShapeNode node = GetBegin(dir); node != GetLast(dir); node = node.GetNext(dir))
				yield return node;
		}

		/// <summary>
		/// Gets the first segment or boundary in this shape. It corresponds to the node to the right of the
		/// left margin.
		/// </summary>
		/// <returns>The first segment or boundary.</returns>
		public PhoneticShapeNode GetBegin()
		{
			return Begin;
		}

		/// <summary>
		/// Gets the last segment or boundary in this shape. It corresponds to the node to the left of the
		/// right margin.
		/// </summary>
		/// <returns>The last segment or boundary.</returns>
		public PhoneticShapeNode GetEnd()
		{
			return End;
		}

		/// <summary>
		/// Gets the first segment or boundary in this shape according the specified direction.
		/// </summary>
		/// <param name="dir">The direction.</param>
		/// <returns>The first segment or boundary.</returns>
		public PhoneticShapeNode GetBegin(Direction dir)
		{
			if (dir == Direction.RIGHT)
				return Begin;
			else
				return End;
		}

		/// <summary>
		/// Gets the last segment or boundary in this shape according the specified direction.
		/// </summary>
		/// <param name="dir">The dir.</param>
		/// <returns>The last segment or boundary.</returns>
		public PhoneticShapeNode GetEnd(Direction dir)
		{
			if (dir == Direction.RIGHT)
				return End;
			else
				return Begin;
		}

		/// <summary>
		/// Adds the specified node to the end of this list.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <exception cref="System.InvalidOperationException">Thrown when the current size of this phonetic shape is
		/// equal to the maximum size.</exception>
		public override void Add(PhoneticShapeNode node)
		{
			if (Count == MAX_SIZE)
				throw new InvalidOperationException(string.Format(HCStrings.kstidPShapeMaxSize, "node"));

			base.Add(node);
		}

		/// <summary>
		/// Inserts <c>newNode</c> to the left or right of <c>node</c>.
		/// </summary>
		/// <param name="newNode">The new node.</param>
		/// <param name="node">The current node.</param>
		/// <param name="dir">The direction to insert the new node.</param>
		/// <exception cref="System.InvalidOperationException">Thrown when the current size of this phonetic shape is
		/// equal to the maximum size.</exception>
		public override void Insert(PhoneticShapeNode newNode, PhoneticShapeNode node, Direction dir)
		{
			if (Count == MAX_SIZE)
				throw new InvalidOperationException(string.Format(HCStrings.kstidPShapeMaxSize, "newNode"));

			base.Insert(newNode, node, dir);
		}

		/// <summary>
		/// Determines if this shape is a duplicate of the specified shape. A duplicate is defined as having
		/// the same shape as another output, excluding optional segments.
		/// </summary>
		/// <param name="shape">The shape.</param>
		/// <returns><c>true</c> if this shape is a duplicate of the specified shape, otherwise <c>false</c>.</returns>
		public bool Duplicates(PhoneticShape shape)
		{
			PhoneticShape longShape = null, shortShape = null;
			if (Count >= shape.Count)
			{
				longShape = this;
				shortShape = shape;
			}
			else
			{
				longShape = shape;
				shortShape = this;
			}
			PhoneticShapeNode longNode = longShape.Begin;
			PhoneticShapeNode shortNode = shortShape.Begin;

			bool duplicate = false;
			while (longNode != longShape.Last || shortNode != shortShape.Last)
			{
				if (longNode.Equals(shortNode))
				{
					longNode = longNode.Next;
					shortNode = shortNode.Next;
					duplicate = true;
				}
				else if (longNode.IsOptional)
				{
					longNode = longNode.Next;
				}
				else if (shortNode.IsOptional)
				{
					shortNode = shortNode.Next;
				}
				else
				{
					duplicate = false;
					break;
				}
			}
			return duplicate;
		}

		object ICloneable.Clone()
		{
			return this.Clone();
		}

		public PhoneticShape Clone()
		{
			return new PhoneticShape(this);
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			foreach (PhoneticShapeNode node in this)
				sb.Append(node.ToString());
			return sb.ToString();
		}
	}
}
