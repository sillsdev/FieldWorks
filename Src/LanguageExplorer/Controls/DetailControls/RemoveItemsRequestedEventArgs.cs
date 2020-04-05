// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer.Controls.DetailControls
{
	internal sealed class RemoveItemsRequestedEventArgs : EventArgs
	{
		internal RemoveItemsRequestedEventArgs(bool forward)
		{
			Forward = forward;
		}

		internal bool Forward { get; }
	}
}