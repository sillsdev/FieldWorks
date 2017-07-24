// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
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
		/// <summary />
		internal ReversalEntryClerk(StatusBar statusBar, ILcmServiceLocator serviceLocator, ISilDataAccessManaged decorator, IReversalIndex reversalIndex)
			: base("AllReversalEntries", statusBar, new AllReversalEntriesRecordList(serviceLocator, decorator, reversalIndex), new PropertyRecordSorter("ShortName"), "Default", null, true, true)
		{
		}

		/// <summary />
		protected override ICmObject NewOwningObject(IReversalIndex ri)
		{
			return ri;
		}
	}
}