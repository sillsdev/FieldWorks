// Copyright (c) 2008-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	internal class ColumnMenuItem
	{
		internal ColumnMenuItem(ICmPossibility column)
		{
			Column = column;
		}

		public override string ToString()
		{
			return Column.Name.BestAnalysisAlternative.Text;
		}

		public ICmPossibility Column { get; }
	}
}