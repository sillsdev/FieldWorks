// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer.MGA
{
	internal delegate void GlossListEventHandler(object sender, GlossListEventArgs e);

	/// <summary />
	internal sealed class GlossListEventArgs : EventArgs
	{
		internal GlossListEventArgs(GlossListBoxItem glbi)
		{
			GlossListBoxItem = glbi;
		}

		/// <summary>
		/// Gets the item.
		/// </summary>
		internal GlossListBoxItem GlossListBoxItem { get; }
	}
}