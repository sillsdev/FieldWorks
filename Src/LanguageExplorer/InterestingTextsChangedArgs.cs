// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer
{
	internal sealed class InterestingTextsChangedArgs : EventArgs
	{
		internal InterestingTextsChangedArgs(int insertAt, int inserted, int deleted)
		{
			InsertedAt = insertAt;
			NumberInserted = inserted;
			NumberDeleted = deleted;
		}
		internal int InsertedAt { get; }
		internal int NumberInserted { get; }
		internal int NumberDeleted { get; }
	}
}