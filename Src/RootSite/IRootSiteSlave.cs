// Copyright (c) 2007-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Any class that implements this interface is suitable to be contained as a slave in a
	/// root site group.
	/// </summary>
	public interface IRootSiteSlave : IRefreshableRoot
	{
		/// <summary>
		/// Gets or sets the scrolling position for the control. When we're not the scrolling
		/// controller then we're part of a group then gets or sets the scrolling
		/// controller's value.
		/// </summary>
		Point ScrollPosition { get; set; }

		/// <summary>
		/// Gets or sets the zoom multiplier that magnifies (or shrinks) the view.
		/// </summary>
		float Zoom { get; set; }
	}
}