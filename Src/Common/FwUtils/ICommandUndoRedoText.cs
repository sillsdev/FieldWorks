// Copyright (c) 2003-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Interface for undo/redo text
	/// </summary>
	public interface ICommandUndoRedoText
	{
		/// <summary>
		/// Get the Undo text.
		/// </summary>
		string UndoText { get; }
		/// <summary>
		/// Get the Redo text
		/// </summary>
		string RedoText { get; }
	}
}
