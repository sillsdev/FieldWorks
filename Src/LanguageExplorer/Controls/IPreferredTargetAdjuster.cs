// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// Interface that may be implemented to adjust the object that we will try to jump to when it
	/// is clicked in the view.
	/// </summary>
	internal interface IPreferredTargetAdjuster
	{
		ICmObject AdjustTarget(ICmObject target);
	}
}