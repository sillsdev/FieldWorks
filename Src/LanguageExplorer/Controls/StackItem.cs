// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Controls
{
	/// <summary />
	internal sealed class StackItem
	{
		/// <summary>Hvo of the next higher item in the view hierarchy (usually the "owner" of
		/// the item)</summary>
		public int m_hvoOuter;
		/// <summary>Hvo of the current item</summary>
		public int m_hvo;
		/// <summary>Tag of the current item</summary>
		public int m_tag;
		/// <summary>Index of the current item</summary>
		public int m_ihvo;
		/// <summary>Handles counting of previous occurrences of properties</summary>
		public PrevPropCounter m_cpropPrev = new PrevPropCounter();

		/// <summary />
		public StackItem(int hvoOuter, int hvo, int tag, int ihvo)
		{
			m_hvoOuter = hvoOuter;
			m_hvo = hvo;
			m_tag = tag;
			m_ihvo = ihvo;
		}

		/// <summary />
		public SelLevInfo ToSelLevInfo()
		{
			return new SelLevInfo
			{
				hvo = m_hvo,
				tag = m_tag,
				ihvo = m_ihvo,
			};
		}
	}
}