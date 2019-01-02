// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel;

namespace LanguageExplorer
{
	public delegate void SelectObjectEventHandler(object sender, SelectObjectEventArgs e);

	/// <summary>
	/// Event args class for handling RecordNavigation events.
	/// </summary>
	public class SelectObjectEventArgs : EventArgs
	{
		/// <summary />
		public SelectObjectEventArgs(ICmObject currentObject)
		{
			CurrentObject = currentObject;
		}

		/// <summary>
		/// Get the record navigation information related to the event.
		/// </summary>
		public ICmObject CurrentObject { get; }
	}
}