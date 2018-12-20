// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// A control within a slice may implement this in order to receive notification when the slice
	/// becomes active.
	/// It's crazy to define this over in LexTextControls, but then, it's crazy for ButtonLauncher
	/// and most of its subclasses to be here, either. It's a historical artifact resulting from
	/// the fact that LexTextControls doesn't reference DetailControls; rather, DetailControls
	/// references LexTextControls. We need references both ways, but can't achieve it.
	/// </summary>
	public interface INotifyControlInCurrentSlice
	{
		bool SliceIsCurrent { set; }
	}
}