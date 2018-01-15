// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.ObjectModel;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// Compares CmObjects using their SortKey property.
	/// </summary>
	internal class CmObjectComparer : DisposableBase, IComparer<int>
	{
		private IntPtr m_col = IntPtr.Zero;
		private readonly LcmCache m_cache;

		public CmObjectComparer(LcmCache cache)
		{
			m_cache = cache;
		}

		public int Compare(int x, int y)
		{
			if (x == y)
			{
				return 0;
			}

			var xobj = m_cache.ServiceLocator.ObjectRepository.GetObject(x);
			var yobj = m_cache.ServiceLocator.ObjectRepository.GetObject(y);
			var xkeyStr = xobj.SortKey;
			var ykeyStr = yobj.SortKey;
			if (string.IsNullOrEmpty(xkeyStr) && string.IsNullOrEmpty(ykeyStr))
			{
				return 0;
			}
			if (string.IsNullOrEmpty(xkeyStr))
			{
				return -1;
			}
			if (string.IsNullOrEmpty(ykeyStr))
			{
				return 1;
			}

			if (m_col == IntPtr.Zero)
			{
				var ws = xobj.SortKeyWs;
				if (string.IsNullOrEmpty(ws))
				{
					ws = yobj.SortKeyWs;
				}
				var icuLocale = Icu.GetName(ws);
				m_col = Icu.OpenCollator(icuLocale);
			}

			var xkey = Icu.GetSortKey(m_col, xkeyStr);
			var ykey = Icu.GetSortKey(m_col, ykeyStr);
			// Simulate strcmp on the two NUL-terminated byte strings.
			// This avoids marshalling back and forth.
			// JohnT: but apparently the strings are not null-terminated if the input was empty.
			int nVal;
			if (xkey.Length == 0)
			{
				nVal = -ykey.Length; // zero if equal, neg if ykey is longer (considered larger)
			}
			else if (ykey.Length == 0)
			{
				nVal = 1; // xkey is longer and considered larger.
			}
			else
			{
				// Normal case, null termination should be present.
				int ib;
				for (ib = 0; xkey[ib] == ykey[ib] && xkey[ib] != 0; ++ib)
				{
					// skip merrily along until strings differ or end.
				}
				nVal = xkey[ib] - ykey[ib];
			}
			if (nVal == 0)
			{
				// Need to get secondary sort keys.
				var xkey2 = xobj.SortKey2;
				var ykey2 = yobj.SortKey2;
				return xkey2 - ykey2;
			}

			return nVal;
		}

		protected override void DisposeUnmanagedResources()
		{
			if (m_col == IntPtr.Zero)
			{
				return;
			}
			Icu.CloseCollator(m_col);
			m_col = IntPtr.Zero;
		}
	}
}