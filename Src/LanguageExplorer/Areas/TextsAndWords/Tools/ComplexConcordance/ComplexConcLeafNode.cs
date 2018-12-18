// Copyright (c) 2013-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.ComplexConcordance
{
	public abstract class ComplexConcLeafNode : ComplexConcPatternNode
	{
		private ReadOnlyCollection<ComplexConcPatternNode> m_nodes;

		public override IList<ComplexConcPatternNode> Children => m_nodes ?? (m_nodes = new ReadOnlyCollection<ComplexConcPatternNode>(new ComplexConcPatternNode[0]));

		public override bool IsLeaf => true;
	}
}