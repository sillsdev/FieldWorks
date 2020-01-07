// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// An interface common to classes that 'handle' combo boxes that appear when something in
	/// IText is clicked.
	/// </summary>
	internal interface IComboHandler : IDisposable
	{
		/// <summary>
		/// Initialize the combo contents.
		/// </summary>
		void SetupCombo();

		/// <summary>
		/// Get rid of the combo, typically when the user clicks outside it.
		/// </summary>
		void Hide();

		/// <summary>
		/// Handle a return key press in an editable combo.
		/// </summary>
		bool HandleReturnKey();

		/// <summary>
		/// Activate the combo-handler's control.
		/// If the control is a combo make it visible at the indicated location.
		/// If it is a ComboListBox pop it up at the relevant place for the indicated location.
		/// </summary>
		void Activate(Rect loc);

		/// <summary>
		/// This one is a bit awkward in this interface, but it simplifies things. It's OK to
		/// just answer zero if the handler has no particular morpheme selected.
		/// </summary>
		int SelectedMorphHvo { get; }

		/// <summary>
		/// Act as if the user selected the current item.
		/// </summary>
		void HandleSelectIfActive();
	}
}