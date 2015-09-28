// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.Filters;

namespace LanguageExplorer.Areas.Lexicon
{
	/// <summary>
	/// This clerk is used to deal with the entries of a IReversalIndex.
	/// </summary>
	internal sealed class ReversalEntryClerk : ReversalClerk
	{
#if RANDYTODO
	// TODO: Only used in:
/*
<clerk id="AllReversalEntries">
	<dynamicloaderinfo assemblyPath="LanguageExplorer.dll" class="LanguageExplorer.Dumpster.ReversalEntryClerk" />
	<recordList owner="ReversalIndex" property="AllEntries">
		<dynamicloaderinfo assemblyPath="LanguageExplorer.dll" class="LanguageExplorer.Areas.Lexicon.AllReversalEntriesRecordList" />
	</recordList>
	<filters />
	<sortMethods>
		<sortMethod label="Form" assemblyPath="Filters.dll" class="SIL.FieldWorks.Filters.PropertyRecordSorter" sortProperty="ShortName" />
	</sortMethods>
</clerk>
*/
#endif
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
		internal ReversalEntryClerk(IFdoServiceLocator serviceLocator, ISilDataAccessManaged decorator, IReversalIndex reversalIndex)
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