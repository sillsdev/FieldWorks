// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// An additional interface that may be implemented by an IBulkEditSpecControl if it needs to know
	/// when the text of its control has been set (typically by restoring from persistence).
	/// </summary>
	public interface ITextChangedNotification
	{
		/// <summary>
		/// Notifies that the control's text has changed.
		/// </summary>
		void ControlTextChanged();
	}
}