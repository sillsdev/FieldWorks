// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DummyICU.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using NMock;

namespace SIL.FieldWorks.Common.FwUtils
{
	internal class DummyICU : IUnicodeCharacters
	{
		internal DynamicMock m_icu;
		internal IUnicodeCharacters m_mockICU;

		internal DummyICU()
		{
			m_icu = new DynamicMock(typeof(IUnicodeCharacters));
			m_mockICU = (IUnicodeCharacters)m_icu.MockInstance;
			m_icu.Expect("Init");
		}

		#region IUnicodeCharacters Members

		public string  GetExemplarCharacters(string icuLocale)
		{
			return m_mockICU.GetExemplarCharacters(icuLocale);
		}

		public void  Init()
		{
			m_mockICU.Init();
		}

		public string ToTitle(string s, string icuLocale)
		{
			return s.ToUpperInvariant();
		}

		#endregion
	}
}
