// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer
{
	/// <summary>
	/// Interface for reversal record list classes.
	/// </summary>
	internal interface IReversalRecordList
	{
		/// <summary>
		/// Change the reversal list for the record list (if possible).
		/// </summary>
		void ChangeOwningObjectIfPossible();
	}
}