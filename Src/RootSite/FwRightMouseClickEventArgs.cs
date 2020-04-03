// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Drawing;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary />
	internal delegate void FwRightMouseClickEventHandler(SimpleRootSite sender, FwRightMouseClickEventArgs e);

	/// <summary>
	/// This event argument class is used to handle right clicks in SimpleRootSite objects.
	/// If someone handles the event, then they should set EventHandled to true, so the regular mouse processing is skipped.
	/// </summary>
	internal sealed class FwRightMouseClickEventArgs
	{
		/// <summary />
		/// <param name="pt">The point of the right click.</param>
		/// <param name="selection">The selection at the right click point.</param>
		internal FwRightMouseClickEventArgs(Point pt, IVwSelection selection)
		{
			Debug.Assert(selection != null);
			Selection = selection;
			MouseLocation = pt;
		}

		/// <summary>
		/// Gets or sets whether the event was handled or not. Defaults to false.
		/// </summary>
		internal bool EventHandled { get; set; } = false;

		/// <summary>
		/// Get the selection from the sender.
		/// </summary>
		internal IVwSelection Selection { get; }

		/// <summary>
		/// Get the mouse pointer location.
		/// </summary>
		internal Point MouseLocation { get; }
	}
}