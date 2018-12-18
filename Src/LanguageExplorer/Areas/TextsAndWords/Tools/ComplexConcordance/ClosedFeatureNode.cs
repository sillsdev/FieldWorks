// Copyright (c) 2013-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using Aga.Controls.Tree;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.ComplexConcordance
{
	public class ClosedFeatureNode : Node
	{
		public ClosedFeatureNode(IFsClosedFeature feature)
		{
			Feature = feature;
			Value = new SymbolicValue(null);
		}

		public IFsClosedFeature Feature { get; }

		public SymbolicValue Value { get; set; }

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