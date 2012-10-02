// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IRootSite.cs
// Responsibility: TE Team
//
// <remarks>
// This interface allows us to deal with common features of IVwRootSite's and RootSiteGroup's.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary></summary>
	public delegate void ScrollPositionChanged(object sender, int oldPos, int newPos);

	/// <summary>
	/// Summary description for IRootSite.
	/// </summary>
	public interface IRootSite
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes the display :)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void RefreshDisplay();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Commits all outstanding changes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void Commit();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows the IRootSite to be cast as an IVwRootSite
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		IVwRootSite CastAsIVwRootSite();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows forcing the closing of root boxes. This is necessary when an instance of a
		/// SimpleRootSite is created but never shown so it's handle doesn't get created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void CloseRootBox();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the editing helper for this IRootsite.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		EditingHelper EditingHelper{ get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets sets whether or not to allow painting on the view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool AllowPainting
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the complete list of rootboxes used within this IRootSite control.
		/// The resulting list may contain zero or more items.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		List<IVwRootBox> AllRootBoxes();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scroll the selection in view and set the IP at the given client position.
		/// </summary>
		/// <param name="sel">The selection</param>
		/// <param name="dyPos">Position from top of client window where IP should be set</param>
		/// ------------------------------------------------------------------------------------
		void ScrollSelectionToLocation(IVwSelection sel, int dyPos);

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the width available for laying things out in the view.
		/// Return the layout width for the window, depending on whether or not there is a
		/// scroll bar. If there is no scroll bar, we pretend that there is, so we don't have
		/// to keep adjusting the width back and forth based on the toggling on and off of
		/// vertical and horizontal scroll bars and their interaction.
		/// The return result is in pixels.
		/// The only common reason to override this is to answer instead a very large integer,
		/// which has the effect of turning off line wrap, as everything apparently fits on
		/// a line.
		/// </summary>
		/// <param name="prootb">The root box</param>
		/// <returns>Width available for layout</returns>
		/// -----------------------------------------------------------------------------------
		int GetAvailWidth(IVwRootBox prootb);
	}
}
