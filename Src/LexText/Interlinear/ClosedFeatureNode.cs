// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using Aga.Controls.Tree;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.IText
{
	public class ClosedFeatureNode : Node
	{
		private readonly IFsClosedFeature m_feature;

		public ClosedFeatureNode(IFsClosedFeature feature)
		{
			m_feature = feature;
			Value = new SymbolicValue(null);
		}

		public IFsClosedFeature Feature
		{
			get { return m_feature; }
		}

		public SymbolicValue Value { get; set; }

		public override string Text
		{
			get { return m_feature.Name.BestAnalysisAlternative.Text; }

			set
			{
				throw new NotSupportedException();
			}
		}
	}
}
