// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IRootSiteSlave.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Any class that implements this interface is suitable to be contained as a slave in a
	/// root site group.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IRootSiteSlave
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the scrolling position for the control. When we're not the scrolling
		/// controller then we're part of a group then gets or sets the scrolling
		/// controller's value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		Point ScrollPosition { get; set; }

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the scrolling range for the control. When we're not the scrolling
		/// controller then we're part of a group then gets or sets the scrolling
		/// controller's value.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		Size ScrollMinSize { get; set; }

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Scroll to the top
		/// </summary>
		/// -----------------------------------------------------------------------------------
		void ScrollToTop();

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Scroll to the bottom.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		void ScrollToEnd();

		/// <summary>This event gets fired when the AutoScrollPosition value changes</summary>
		event ScrollPositionChanged VerticalScrollPositionChanged;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The group that organizes several roots scrolling together.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IRootSiteGroup Group { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes the display :)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void RefreshDisplay();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the zoom multiplier that magnifies (or shrinks) the view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		float Zoom { get; set;}
	}
}
