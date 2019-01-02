// Copyright (c) 2004-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer
{
	/// <summary>
	/// Type declaration for FwViewSizeChangedEventHandler, which is used to handle changes in
	/// the size used by a view.
	/// </summary>
	internal delegate void FwViewSizeChangedEventHandler(object sender, FwViewSizeEventArgs e);

	/// <remarks>
	/// This event argument class is used for events relating to a view changing its underlying
	/// size.
	/// </remarks>
	internal class FwViewSizeEventArgs : EventArgs
	{
		/// <summary />
		/// <param name="height">new height of the view</param>
		/// <param name="width">new width of the view</param>
		internal FwViewSizeEventArgs(int height, int width)
		{
			Height = height;
			Width = width;
		}

		/// <summary>
		/// The height of the view.
		/// </summary>
		public int Height { get; }

		/// <summary>
		/// The width of the view.
		/// </summary>
		public int Width { get; }
	}
}
