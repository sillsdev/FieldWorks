// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary />
	public delegate void ScrollPositionChanged(object sender, int oldPos, int newPos);

	/// <summary>
	/// This interface allows us to deal with common features of IVwRootSite's and RootSiteGroup's.
	/// </summary>
	public interface IRootSite : IRefreshableRoot
	{
		/// <summary>
		/// Allows the IRootSite to be cast as an IVwRootSite
		/// </summary>
		IVwRootSite CastAsIVwRootSite();

		/// <summary>
		/// Allows forcing the closing of root boxes. This is necessary when an instance of a
		/// SimpleRootSite is created but never shown so it's handle doesn't get created.
		/// </summary>
		void CloseRootBox();

		/// <summary>
		/// Gets the editing helper for this IRootsite.
		/// </summary>
		EditingHelper EditingHelper { get; }

		/// <summary>
		/// Gets sets whether or not to allow painting on the view
		/// </summary>
		bool AllowPainting
		{
			get;
			set;
		}

		/// <summary>
		/// Returns the complete list of rootboxes used within this IRootSite control.
		/// The resulting list may contain zero or more items.
		/// </summary>
		List<IVwRootBox> AllRootBoxes();

		/// <summary>
		/// Scroll the selection in view and set the IP at the given client position.
		/// </summary>
		/// <param name="sel">The selection</param>
		/// <param name="dyPos">Position from top of client window where IP should be set</param>
		/// <returns>True if the selection was scrolled into view, false if this function did
		/// nothing</returns>
		bool ScrollSelectionToLocation(IVwSelection sel, int dyPos);
	}
}