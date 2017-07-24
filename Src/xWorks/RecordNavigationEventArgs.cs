// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.XWorks
{
	public delegate void RecordNavigationInfoEventHandler(object sender, RecordNavigationEventArgs e);

	/// <summary>
	/// Event args class for handling RecordNavigation events.
	/// </summary>
	public class RecordNavigationEventArgs : EventArgs
	{
		/// <summary />
		public RecordNavigationEventArgs(RecordNavigationInfo rni)
		{
			RecordNavigationInfo = rni;
		}

		/// <summary>
		/// Get the record navigation information related to the event.
		/// </summary>
		public RecordNavigationInfo RecordNavigationInfo { get; }
	}
}