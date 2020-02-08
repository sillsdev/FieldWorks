// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel;

namespace LanguageExplorer
{
	internal interface ITreeBarHandler : IDisposable
	{
		string BestWritingSystem { get; }
		bool IncludeAbbreviation { get; }
		bool IsItemInTree(int hvo);
		void PopulateRecordBar(IRecordList list);
		void UpdateSelection(ICmObject currentObject);
		void ReloadItem(ICmObject currentObject);
		void ReleaseRecordBar();
	}
}