#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
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
