// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// Each enum value must correspond to an index into m_imageList (except for kSimpleLink
	/// which indicates that no icon is shown).
	/// </summary>
	public enum LinkType
	{
		/// <summary />
		kSimpleLink = -1,
		/// <summary />
		kGotoLink = 0,
		/// <summary />
		kDialogLink = 1
	}
}