// Copyright (c) 2013-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer.Controls.DetailControls
{
	internal sealed class InsertEventArgs : EventArgs
	{
		internal InsertEventArgs(object option, object suboption)
		{
			Option = option;
			Suboption = suboption;
		}

		internal object Option { get; }

		internal object Suboption { get; }
	}
}