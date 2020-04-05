// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.LCModel.Core.WritingSystems;

namespace LanguageExplorer
{
	internal interface IMultiStringSlice : ISlice
	{
		void SelectAt(int ws, int ich);
		IEnumerable<CoreWritingSystemDefinition> WritingSystemsSelectedForDisplay { get; set; }
	}
}