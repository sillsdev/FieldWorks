// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ViewSizeEvents.cs
// Responsibility:
// --------------------------------------------------------------------------------------------
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
