// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Controls.DetailControls.Slices
{
	/// <summary />
	internal sealed class ReversalIndexEntryFormSlice : MultiStringSlice
	{
		/// <summary />
		internal ReversalIndexEntryFormSlice(int flid, ICmObject obj)
			: base(obj, flid, WritingSystemServices.kwsAllReversalIndex, 0, false, true, true)
		{
		}
	}
}
