// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Controls.DetailControls;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.Lexicon.Tools.ReversalIndexes
{
	/// <summary>
	/// Summary description for ReversalIndexEntryFormSlice.
	/// </summary>
	internal sealed class ReversalIndexEntryFormSlice : MultiStringSlice
	{
		/// <summary />
		public ReversalIndexEntryFormSlice(int flid, ICmObject obj)
			: base(obj, flid, WritingSystemServices.kwsAllReversalIndex, 0, false, true, true)
		{
		}
	}
}
