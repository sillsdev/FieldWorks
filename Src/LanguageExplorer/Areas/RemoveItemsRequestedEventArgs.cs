// Copyright (c) 2009-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer.Areas
{
	public class RemoveItemsRequestedEventArgs : EventArgs
	{
		public RemoveItemsRequestedEventArgs(bool forward)
		{
			Forward = forward;
		}

		public bool Forward { get; }
	}
}