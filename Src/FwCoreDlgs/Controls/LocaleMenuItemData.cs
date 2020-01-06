// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SIL.FieldWorks.FwCoreDlgs.Controls
{
	internal class LocaleMenuItemData : IComparable<LocaleMenuItemData>
	{
		internal LocaleMenuItemData(string id, string displayName)
		{
			m_id = id;
			m_displayName = displayName;
		}
		// Locale id, as returned by Locale.getName. For submenus, this is the id
		// of the language, which is also the id of the base locale.
		internal string m_id;
		internal string m_displayName; // corresponding display name, from Locale.getDisplayName.

		internal List<LocaleMenuItemData> m_subitems;

		public int CompareTo(LocaleMenuItemData obj)
		{
			Debug.Assert(obj != null);
			return m_displayName.CompareTo(obj.m_displayName);
		}
	}
}