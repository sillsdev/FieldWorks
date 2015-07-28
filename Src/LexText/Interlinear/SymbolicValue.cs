// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.IText
{
	public class SymbolicValue : IEquatable<SymbolicValue>
	{
		private readonly IFsSymFeatVal m_value;

		public SymbolicValue(IFsSymFeatVal value)
		{
			m_value = value;
		}

		public IFsSymFeatVal FeatureValue
		{
			get { return m_value; }
		}

		public bool Equals(SymbolicValue other)
		{
			if (other == null)
				return false;

			return m_value == other.m_value;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as SymbolicValue);
		}

		public override int GetHashCode()
		{
			return m_value == null ? 0 : m_value.GetHashCode();
		}

		public override string ToString()
		{
			return m_value == null ? ITextStrings.ksComplexConcInflFeatAny : m_value.Name.BestAnalysisAlternative.Text;
		}

	}
}
