// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Areas.Lexicon;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.Filters;

namespace LanguageExplorer.Areas.Lists.Tools.ReversalIndexPOS
{
	/// <summary>
	/// This clerk is used to deal with the POSes of a IReversalIndex.
	/// </summary>
	internal sealed class ReversalEntryPOSClerk : ReversalClerk
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
		internal ReversalEntryPOSClerk(IFdoServiceLocator serviceLocator, ISilDataAccessManaged decorator, IReversalIndex reversalIndex)
			: base("ReversalEntriesPOS", new ReversalIndexPOSRecordList(serviceLocator, decorator, reversalIndex), new PropertyRecordSorter("ShortName"), "Default", null, true, true)
		{
		}

		/// <summary />
		protected override ICmObject NewOwningObject(IReversalIndex ri)
		{
			return ri.PartsOfSpeechOA;
		}
	}
}
