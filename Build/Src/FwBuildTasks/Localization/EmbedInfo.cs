// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Build.Tasks.Localization
{
	public class EmbedInfo : Tuple<string, string>
	{
		public string Resource => Item1;
		public string Name => Item2;

		public EmbedInfo(string resource, string name)
			: base(resource, name)
		{
		}
	}
}
