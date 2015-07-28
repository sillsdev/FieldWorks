// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SIL.FieldWorks.SharpViews
{
	/// <summary>
	/// This interface provides some crucial call-backs which the host of a root box (typically SharpView) must provide.
	/// </summary>
	public interface ISharpViewSite
	{
		/// <summary>
		/// Returns the information needed for figuring out where to draw (or invalidate) things.
		/// </summary>
		IGraphicsHolder DrawingInfo { get; }

		/// <summary>
		/// Invalidate (mark as needing to be painted) the specified rectangle, which is in layout coords
		/// (relative to the top left of the root box).
		/// </summary>
		void InvalidateInRoot(Rectangle rect);

		/// <summary>
		/// Invalidate in paint coordinates, that is, relative to the control itself.
		/// </summary>
		void Invalidate(Rectangle rect);

		/// <summary>
		/// Perform the indicated task after all change events (or PropChanged notifications)
		/// that may be pending are complete. Clients not using the UnitOfWork pattern may perform
		/// the task immediately. If a unit of work is in progress, the action (typically to make
		/// a new selection) should be performed when the unit of work is complete, particularly after
		/// any change notifications have been sent.
		/// </summary>
		/// <param name="task"></param>
		void PerformAfterNotifications(Action task);

		/// <summary>
		/// Initiate a drag and drop operation dragging some text from the view.
		/// Typically implemented by inheritance from Control.
		/// </summary>
		DragDropEffects DoDragDrop(Object data, DragDropEffects allowedEffects);
	}
}
