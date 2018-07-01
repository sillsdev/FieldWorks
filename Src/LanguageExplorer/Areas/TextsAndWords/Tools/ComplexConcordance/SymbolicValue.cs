// Copyright (c) 2013-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.ComplexConcordance
{
	public class SymbolicValue : IEquatable<SymbolicValue>
	{
		public SymbolicValue(IFsSymFeatVal value)
		{
			FeatureValue = value;
		}

		public IFsSymFeatVal FeatureValue { get; }

		public bool Equals(SymbolicValue other)
		{
			return other != null && FeatureValue == other.FeatureValue;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as SymbolicValue);
		}

		public override int GetHashCode()
		{
			return FeatureValue == null ? 0 : FeatureValue.GetHashCode();
		}

		public override string ToString()
		{
			return FeatureValue == null ? ComplexConcordanceResources.ksComplexConcInflFeatAny : FeatureValue.Name.BestAnalysisAlternative.Text;
		}

	}
}
