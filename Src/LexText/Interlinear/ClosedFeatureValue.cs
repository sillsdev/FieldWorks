// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.IText
{
	public class ClosedFeatureValue
	{
		private readonly IFsSymFeatVal m_symbol;
		private readonly bool m_negate;

		public ClosedFeatureValue(IFsSymFeatVal symbol, bool negate)
		{
			m_symbol = symbol;
			m_negate = negate;
		}

		public IFsSymFeatVal Symbol
		{
			get { return m_symbol; }
		}

		public bool Negate
		{
			get { return m_negate; }
		}
	}
}
