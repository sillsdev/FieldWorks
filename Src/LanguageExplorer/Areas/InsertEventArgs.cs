// Copyright (c) 2013-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer.Areas
{
	public class InsertEventArgs : EventArgs
	{
		public InsertEventArgs(object option, object suboption)
		{
			Option = option;
			Suboption = suboption;
		}

		public object Option { get; }

		public object Suboption { get; }
	}
}