// Copyright (c) 2013-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using Aga.Controls.Tree;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.ComplexConcordance
{
	public class ComplexFeatureNode : Node
	{
		public ComplexFeatureNode(IFsComplexFeature feature)
		{
			Feature = feature;
		}

		public IFsComplexFeature Feature { get; }

		public override string Text
		{
			get { return Feature.Name.BestAnalysisAlternative.Text; }

			set
			{
				throw new NotSupportedException();
			}
		}
	}
}