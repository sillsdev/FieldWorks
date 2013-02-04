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
