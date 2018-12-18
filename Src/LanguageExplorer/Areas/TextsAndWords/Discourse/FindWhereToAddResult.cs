// Copyright (c) 2008-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	/// <summary>
	/// Indicates how to interpret the other results of FindWhereToAddChOrph and FindWhereToAddWords,
	/// namely, 'whereToInsert' and 'existingWordGroup'. A (different) set of 3 of these results apply to each of
	/// the 2 'FindWhereToAddX' methods. 'FWTAWords' uses the first 3. 'FWTAChOrph' uses all but kMakeNewRow.
	/// </summary>
	internal enum FindWhereToAddResult
	{
		kAppendToExisting,      // append (word or ChOrph) as last occurrence(s) of 'existingWordGroup'
		// (ignore 'whereToInsert')

		kInsertWordGrpInRow,    // 'whereToInsert' specifies the index in the row's CellsOS of the new WordGroup to be
		// created (from Words or ChOrphs) in the (at this time, anyway) previously empty cell
		// (ignore 'existingWordGroup')

		kMakeNewRow,            // Make a new WordGroup in a new row. (ignore both 'whereToInsert and 'existingWordGroup')
		// (Not used for FindWhereToAddChOrph)

		kInsertChOrphInWordGrp  // Insert ChOrph word(s) into 'existingWordGroup'; whereToInsert is now index in WordGroup's
		// list of occurrences (Not used for FindWhereToAddWords)
	}
}