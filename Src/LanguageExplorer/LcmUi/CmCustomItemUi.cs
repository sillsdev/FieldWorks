// Copyright (c) 2012-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.LcmUi
{
	public class CmCustomItemUi : CmPossibilityUi
	{
		public override string DisplayNameOfClass => StringTable.Table.GetString(MyCmObject.GetType().Name, "ClassNames");
	}
}