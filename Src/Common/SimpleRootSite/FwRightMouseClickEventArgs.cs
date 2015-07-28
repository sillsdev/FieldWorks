// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Diagnostics;

using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	///
	/// </summary>
	public delegate void FwRightMouseClickEventHandler(SimpleRootSite sender, FwRightMouseClickEventArgs e);

	/// <summary>
	/// This event argument class is used to handle right clicks in SimpleRootSite objects.
	/// If someone handles the event, then they should set EventHandled to true, so the regular mouse processing is skipped.
	/// </summary>
	public class FwRightMouseClickEventArgs
	{
		private bool m_eventHandled = false;
		private IVwSelection m_selection;
		private Point m_pt;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="pt">The point of the right click.</param>
		/// <param name="selection">The selection at the right click point.</param>
		public FwRightMouseClickEventArgs(Point pt, IVwSelection selection)
		{
			Debug.Assert(selection != null);
			m_selection = selection;
			m_pt = pt;
		}

		/// <summary>
		/// Gets or sets whether the event was handled or not. Defaults to false.
		/// </summary>
		public bool EventHandled
		{
			get { return m_eventHandled; }
			set { m_eventHandled = value; }
		}

		/// <summary>
		/// Get the selection from the sender.
		/// </summary>
		public IVwSelection Selection
		{
			get { return m_selection; }
		}

		/// <summary>
		/// Get the mouse pointer loaction.
		/// </summary>
		public Point MouseLocation
		{
			get { return m_pt; }
		}
	}
}
