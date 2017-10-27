// Copyright (c) 2007-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer
{
	/// <summary>
	/// Interface for controls that cna handle the File-Undo/Redo events.
	/// </summary>
	internal interface IUndoRedoHandler
	{
		/// <summary>
		/// Get the text for the Undo menu.
		/// </summary>
		string UndoText { get; }

		/// <summary>
		/// Get the enabled condition for the Undo menu.
		/// </summary>
		bool UndoEnabled(bool callerEnableOpinion);

		/// <summary>
		/// Handle Undo event
		/// </summary>
		/// <returns>'true' if the event was handled, ortherwise 'false' which has caller deal with it.</returns>
		bool HandleUndo(object sender, EventArgs e);

		/// <summary>
		/// Get the text for the Redo menu.
		/// </summary>
		string RedoText { get; }

		/// <summary>
		/// Get the enabled condition for the Undo menu.
		/// </summary>
		bool RedoEnabled(bool callerEnableOpinion);

		/// <summary>
		/// Handle Redo event
		/// </summary>
		/// <returns>'true' if the event was handled, ortherwise 'false' which has caller deal with it.</returns>
		bool HandleRedo(object sender, EventArgs e);
	}
}
