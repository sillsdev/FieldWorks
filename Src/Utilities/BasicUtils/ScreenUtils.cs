// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScreenUtils.cs
// Responsibility: DavidO
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.Drawing;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Misc. methods for dealing with screens (in the .Net world, that is).
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScreenUtils
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not the specified screen is a virtual device. Virtual displays
		/// can usually be ignored.
		/// </summary>
		/// <param name="scrn">Screen to test.</param>
		/// <returns>True if screen is virtual. Otherwise, guess</returns>
		/// ------------------------------------------------------------------------------------
		public static bool ScreenIsVirtual(Screen scrn)
		{
			// We're really looking for a string containing DISPLAYV, but something is *goofy*
			// in the DeviceName string. It will not compare correctly despite all common sense.
			// Best we can do is test for "ISPLAYV".
			return (scrn.DeviceName.ToUpper().IndexOf("ISPLAYV") >= 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the screen that is primary. This has the same result as returning
		/// Screen.PrimaryScreen except that we can avoid some problems with .Net's
		/// Screen object. We discovered that the FromRectangle method forces the screen object
		/// to be recreated which then gives accurate results if the taskbar has been moved.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Screen PrimaryScreen
		{
			get
			{
				try
				{
					return Screen.FromRectangle(new Rectangle(0, 0, 10, 10));
				}
				catch
				{
					return Screen.PrimaryScreen;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the X coordinate that is the left edge of the specified screen's working area.
		/// The coordinate is relative to the screen's working area, not it's bounds.
		/// </summary>
		/// <param name="scrn"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static int ScreenLeft(Screen scrn)
		{
			return (scrn.Primary ? 0 : scrn.WorkingArea.Left - TaskbarWidth);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Y coordinate that is the top edge of the specified screen's working area.
		/// The coordinate is relative to the screen's working area, not it's bounds.
		/// </summary>
		/// <param name="scrn"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static int ScreenTop(Screen scrn)
		{
			return (scrn.Primary ? 0 : scrn.WorkingArea.Top - TaskbarHeight);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the horizontal space the task bar (or any other 'bar' type of thing like the
		/// Office shortcut bar) occupies on the primary screen when the bar is on the left
		/// edge of the primary screen.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static int TaskbarWidth
		{
			get
			{
				return PrimaryScreen.WorkingArea.Left - PrimaryScreen.Bounds.Left;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the vertical space the task bar (or any other 'bar' type of thing like the
		/// Office shortcut bar) occupies on the primary screen when the bar is on the top
		/// edge of the primary screen.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static int TaskbarHeight
		{
			get
			{
				return PrimaryScreen.WorkingArea.Top - PrimaryScreen.Bounds.Top;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a screens working area with the location adjusted for the taskbar (or any
		/// other 'bar' type of thing like the Office shortcut bar).
		/// </summary>
		/// <param name="scrn">The screen whose working area will be adjusted.</param>
		/// <returns>A rectangle representing the adjusted working area.</returns>
		/// ------------------------------------------------------------------------------------
		public static Rectangle AdjustedWorkingArea(Screen scrn)
		{
			Rectangle rc = scrn.WorkingArea;
			rc.X -= TaskbarWidth;
			rc.Y -= TaskbarHeight;
			return rc;
		}

		///***********************************************************************************
		/// <summary>
		/// This method checks to see that rect is fully contained within a screen of the
		/// system. First determine which screen rect is mostly on and if it's not fully on
		/// that screen, move it to the top, left corner of the screen. If rect is too big
		/// to fit on the screen, it will be resized to fit the screen's working area.
		/// </summary>
		/// <param name='rect'>The rectangle for a window.</param>
		///***********************************************************************************
		static public void EnsureVisibleRect(ref Rectangle rect)
		{
			Rectangle rcAllowable =	AdjustedWorkingArea(Screen.FromRectangle(rect));

			// Make the rectangle's width fit in the screen's width
			if (rect.Width > rcAllowable.Width)
				rect.Width = rcAllowable.Width;

			// Make the rectangle's height fit in the screen's height
			if (rect.Height > rcAllowable.Height)
				rect.Height = rcAllowable.Height;

			// If the window is off the monitor, move it within the allowable window area.
			if (rect.Right > rcAllowable.Right)
				rect.X = rcAllowable.Right - rect.Width;
			if (rect.Left < rcAllowable.Left)
				rect.X = rcAllowable.X;
			if (rect.Bottom > rcAllowable.Bottom)
				rect.Y = rcAllowable.Bottom - rect.Height;
			if (rect.Top < rcAllowable.Top)
				rect.Y = rcAllowable.Y;

			// REVIEW: Is this adjustment below needed? It seems like if the window is still
			// outside the allowable area, then it should be re-sized instead of moved.
			// Check if the new window's location will cause its right or bottom edge to extend
			// outside its screen's working area. If either edge extends beyond its screen's
			// boundary or if the rectangle isn't in the primary screen and its top or left
			// side lies off the screen, then move the new window to the top, left corner of
			// its screen.
			if ((rect.Right > rcAllowable.Right || rect.Bottom > rcAllowable.Bottom) ||
				(!Screen.FromRectangle(rect).Primary &&
				(rect.X < rcAllowable.X || rect.Y < rcAllowable.Y)))
			{
				rect.X = rcAllowable.X;
				rect.Y = rcAllowable.Y;
			}
			else if (Screen.FromRectangle(rect).Primary && (rect.X < 0 || rect.Y < 0))
			{
				// Place the rectangle in the top, left corner of the primary screen
				rect.X = rect.Y = 0;
			}
		}
	}
}
