// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary />
	public enum SpellCheckStatus
	{
		/// <summary>spell checking is enabled but word is in dictionary</summary>
		WordInDictionary,
		/// <summary>spell checking enabled with word not in dictionary,
		/// whether or not suggestions exist</summary>
		Enabled,
		/// <summary>spell checking disabled</summary>
		Disabled
	}
}