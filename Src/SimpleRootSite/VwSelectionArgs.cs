// Copyright (c) 2008-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Class for passing rootbox and new selection to handler.
	/// </summary>
	public class VwSelectionArgs : EventArgs
	{
		/// <summary />
		public VwSelectionArgs(IVwRootBox rootb, IVwSelection vwsel)
		{
			RootBox = rootb;
			Selection = vwsel;
		}

		/// <summary>
		/// The Rootbox whose selection has changed.
		/// </summary>
		public IVwRootBox RootBox { get; }

		/// <summary>
		/// The new selection for the rootbox.
		/// </summary>
		public IVwSelection Selection { get; }
	}
}