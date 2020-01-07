// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;

namespace LanguageExplorer.Controls
{
	/// <summary />
	internal sealed class PrevPropCounter
	{
		/// <summary>Count of occurrences of each property</summary>
		private Dictionary<int, int> m_cpropPrev = new Dictionary<int, int>();

		/// <summary>
		/// Gets the count of the previous occurrences of the given property.
		/// </summary>
		public int GetCount(int tag)
		{
			int value;
			return m_cpropPrev.TryGetValue(tag, out value) ? value : -1;
		}

		/// <summary>
		/// Increments the count of the previous occurrences of the given property.
		/// </summary>
		public void Increment(int tag)
		{
			if (m_cpropPrev.ContainsKey(tag))
			{
				m_cpropPrev[tag] += 1;
			}
			else
			{
				m_cpropPrev[tag] = 0;
			}
		}
	}
}