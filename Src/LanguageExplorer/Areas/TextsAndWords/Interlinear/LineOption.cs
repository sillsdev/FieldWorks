// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	internal class LineOption
	{
		public LineOption(int flid, string label)
		{
			Flid = flid;
			Label = label;
		}

		public override string ToString()
		{
			return Label;
		}

		public int Flid { get; }

		public string Label { get; }
	}
}