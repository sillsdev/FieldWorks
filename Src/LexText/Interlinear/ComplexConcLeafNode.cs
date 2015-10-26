// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
