// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Controls.LexText
{
	public class ContextMenuRequestedEventArgs : EventArgs
	{
		public ContextMenuRequestedEventArgs(IVwSelection selection)
		{
			Selection = selection;
		}

		public IVwSelection Selection { get; }

		public bool Handled { get; set; }
	}
}