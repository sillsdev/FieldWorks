// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer
{
	/// <summary>
	/// Actions to take on a ListChanged event.
	/// </summary>
	internal enum ListChangedActions
	{
		/// <summary>
		/// Skip broadcasting OnRecordNavigation.
		/// </summary>
		SkipRecordNavigation,
		/// <summary>
		/// Broadcast OnRecordNavigation, but not save the cache (not saving the cache preserves the Undo/Redo stack).
		/// </summary>
		SuppressSaveOnChangeRecord,
		/// <summary>
		/// Broadcast OnRecordNavigation and save the cache.
		/// </summary>
		Normal,
		/// <summary>
		/// Simply reload the record tree bar item for the CurrentObject.
		/// </summary>
		UpdateListItemName
	}
}