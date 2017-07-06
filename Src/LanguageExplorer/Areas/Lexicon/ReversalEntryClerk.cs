// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Filters;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Lexicon
{
	/// <summary>
	/// This clerk is used to deal with the entries of a IReversalIndex.
	/// </summary>
	internal sealed class ReversalEntryClerk : ReversalClerk
	{
		///// <summary>
		///// Contructor.
		///// </summary>
		///// <param name="id">Clerk id/name.</param>
		///// <param name="recordList">Record list for the clerk.</param>
		///// <param name="defaultSorter">The default record sorter.</param>
		///// <param name="defaultSortLabel"></param>
		///// <param name="defaultFilter">The default filter to use.</param>
		///// <param name="allowDeletions"></param>
		///// <param name="shouldHandleDeletion"></param>
		internal ReversalEntryClerk(ILcmServiceLocator serviceLocator, ISilDataAccessManaged decorator, IReversalIndex reversalIndex)
			: base("AllReversalEntries", new AllReversalEntriesRecordList(serviceLocator, decorator, reversalIndex), new PropertyRecordSorter("ShortName"), "Default", null, true, true)
		{
		}

		/// <summary />
		protected override ICmObject NewOwningObject(IReversalIndex ri)
		{
			return ri;
		}
	}
}