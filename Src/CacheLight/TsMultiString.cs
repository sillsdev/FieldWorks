// Copyright (c) 2016-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.CacheLight
{
	internal class TsMultiString : ITsMultiString
	{
		private readonly SmallDictionary<int, ITsString> m_strings;

		public TsMultiString()
		{
			m_strings = new SmallDictionary<int, ITsString>();
		}

		public int StringCount => m_strings.Count;

		public ITsString GetStringFromIndex(int iws, out int ws)
		{
			var idx = 0;
			foreach (var kvp in m_strings)
			{
				if (idx++ != iws)
				{
					continue;
				}
				ws = kvp.Key;
				return kvp.Value;
			}

			throw new IndexOutOfRangeException("'iws' is not a valid index");
		}

		public ITsString get_String(int ws)
		{
			ITsString tss;
			return m_strings.TryGetValue(ws, out tss) ? tss : TsStringUtils.EmptyString(ws);
		}

		public void set_String(int ws, ITsString tss)
		{
			tss = TsStringUtils.NormalizeNfd(tss);
			ITsString originalValue;
			m_strings.TryGetValue(ws, out originalValue);
			if (tss == originalValue)
			{
				return;
			}
			if (tss != null && originalValue != null && tss.Equals(originalValue))
			{
				return;
			}
			// If tss is null, then just remove ws from the dictionary.
			if (tss == null)
			{
				m_strings.Remove(ws);
			}
			else
			{
				m_strings[ws] = tss;
			}
		}
	}
}
