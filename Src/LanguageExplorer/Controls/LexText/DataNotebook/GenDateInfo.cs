// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel.Core.Cellar;

namespace LanguageExplorer.Controls.LexText.DataNotebook
{
	internal struct GenDateInfo
	{
		public int mday;
		public int wday;
		public int ymon;
		public int year;
		public GenDate.PrecisionType prec;
		public bool error;
	}
}