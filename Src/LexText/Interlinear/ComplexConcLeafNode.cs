using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SIL.FieldWorks.IText
{
	public abstract class ComplexConcLeafNode : ComplexConcPatternNode
	{
		private ReadOnlyCollection<ComplexConcPatternNode> m_nodes;

		public override IList<ComplexConcPatternNode> Children
		{
			get
			{
				if (m_nodes == null)
					m_nodes = new ReadOnlyCollection<ComplexConcPatternNode>(new ComplexConcPatternNode[0]);
				return m_nodes;
			}
		}

		public override bool IsLeaf
		{
			get { return true; }
		}
	}
}
