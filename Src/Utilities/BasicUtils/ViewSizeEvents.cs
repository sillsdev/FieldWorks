// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ViewSizeEvents.cs
// Responsibility:

using System;

namespace SIL.Utils
{
	/// <summary>
	/// Type declaration for FwViewSizeChangedEventHandler, which is used to handle changes in
	/// the size used by a view.
	/// </summary>
	public delegate void FwViewSizeChangedEventHandler (object sender, FwViewSizeEventArgs e);

	#region FwViewSizeEventArgs
	/// <remarks>
	/// This event argument class is used for events relating to a view changing its underlying
	/// size.
	/// </remarks>
	public class FwViewSizeEventArgs : EventArgs
	{
		private int m_height;
		private int m_width;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="height">new height of the view</param>
		/// <param name="width">new width of the view</param>
		public FwViewSizeEventArgs(int height, int width)
		{
			m_height= height;
			m_width = width;
		}

		/// <summary>
		/// The height of the view.
		/// </summary>
		public int Height
		{
			get
			{
				return m_height;
			}
		}

		/// <summary>
		/// The width of the view.
		/// </summary>
		public int Width
		{
			get
			{
				return m_width;
			}
		}
	}
	#endregion // FwViewSizeEventArgs
}
