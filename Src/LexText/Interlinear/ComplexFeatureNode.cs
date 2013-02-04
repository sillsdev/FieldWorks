using System;
using Aga.Controls.Tree;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.IText
{
	public class ComplexFeatureNode : Node
	{
		private readonly IFsComplexFeature m_feature;

		public ComplexFeatureNode(IFsComplexFeature feature)
		{
			m_feature = feature;
		}

		public IFsComplexFeature Feature
		{
			get { return m_feature; }
		}

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
