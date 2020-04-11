// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.DetailControls
{
	internal sealed class LineOption
	{
		internal LineOption(int flid, string label)
		{
			Flid = flid;
			Label = label;
		}

		public override string ToString()
		{
			return Label;
		}

		internal int Flid { get; }

		internal string Label { get; }
	}
}