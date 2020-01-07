// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer.Areas
{
	public class InterestingTextsChangedArgs : EventArgs
	{
		public InterestingTextsChangedArgs(int insertAt, int inserted, int deleted)
		{
			InsertedAt = insertAt;
			NumberInserted = inserted;
			NumberDeleted = deleted;
		}
		public int InsertedAt { get; }
		public int NumberInserted { get; }
		public int NumberDeleted { get; }
	}
}