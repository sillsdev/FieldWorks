// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Controls.DetailControls
{
	internal sealed class ContextMenuRequestedEventArgs : EventArgs
	{
		internal ContextMenuRequestedEventArgs(IVwSelection selection)
		{
			Selection = selection;
		}

		internal IVwSelection Selection { get; }

		internal bool Handled { get; set; }
	}
}