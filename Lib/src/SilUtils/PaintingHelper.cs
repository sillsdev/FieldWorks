// ---------------------------------------------------------------------------------------------
using System.Diagnostics.CodeAnalysis;


#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2003' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: PaintingHelper.cs
// Responsibility: FW Team, especially David Olson (this is of interest to PA also)
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Runtime.InteropServices;

namespace SIL.Utils
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Possible painting states for DrawHotBackground
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public enum PaintState
	{
		/// <summary></summary>
		Normal,
		/// <summary></summary>
		Hot,
		/// <summary></summary>
		HotDown,
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Contains misc. static methods for various customized painting.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class PaintingHelper
	{
		#region Windows API imported methods
#if !__MonoCS__
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the window DC.
		/// </summary>
		/// <param name="hwnd">The HWND.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		[System.Runtime.InteropServices.DllImport("User32.dll")]
		extern static public IntPtr GetWindowDC(IntPtr hwnd);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases the DC.
		/// </summary>
		/// <param name="hwnd">The HWND.</param>
		/// <param name="hdc">The HDC.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		[System.Runtime.InteropServices.DllImport("User32.dll")]
		extern static public int ReleaseDC(IntPtr hwnd, IntPtr hdc);
#endif

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the parent.
		/// </summary>
		/// <param name="hWnd">The h WND.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr GetParent(IntPtr hWnd);

		#endregion

		/// <summary></summary>
		public static int WM_NCPAINT = 0x85;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws around the specified control, a fixed single border the color of text
		/// boxes in a themed environment. If themes are not enabled, the border is black.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void DrawCustomBorder(Control ctrl)
		{
			// TODO-Linux: VisualStyleInformation.TextControlBorder has a MonoTODO.
			DrawCustomBorder(ctrl, Application.RenderWithVisualStyles ?
				VisualStyleInformation.TextControlBorder : Color.Black);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws around the specified control, a fixed single border of the specified color.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void DrawCustomBorder(Control ctrl, Color clrBorder)
		{
#if !__MonoCS__
			IntPtr hdc = GetWindowDC(ctrl.Handle);

			using (Graphics g = Graphics.FromHdc(hdc))
#else
			using (Graphics g = ctrl.CreateGraphics())
#endif
			{
				Rectangle rc = new Rectangle(0, 0, ctrl.Width, ctrl.Height);
				ControlPaint.DrawBorder(g, rc, clrBorder, ButtonBorderStyle.Solid);
			}

#if !__MonoCS__
			ReleaseDC(ctrl.Handle, hdc);
#endif
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws a background in the specified rectangle that looks like a toolbar button
		/// when the mouse is over it, with consideration for whether the look should be like
		/// the mouse is down or not. Note, when a PaintState of normal is specified, this
		/// method does nothing. Normal background painting is up to the caller.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void DrawHotBackground(Graphics g, Rectangle rc, PaintState state)
		{
			// The caller has to handle painting when the state is normal.
			if (state == PaintState.Normal)
				return;

			// Determine the highlight color.
			// TODO-Linux: VisualStyleInformation.ControlHighlightHot has a MonoTODO.
			Color clrHot = (Application.RenderWithVisualStyles ?
				VisualStyleInformation.ControlHighlightHot : SystemColors.MenuHighlight);

			int alpha = (Application.RenderWithVisualStyles ? 95 : 120);

			// Determine the angle and one of the colors for the gradient highlight. When state is
			// hot down, the gradiant goes from bottom (lighter) to top (darker). When the state
			// is just hot, the gradient is from top (lighter) to bottom (darker).
			float angle = (state == PaintState.HotDown ? 270 : 90);
			Color clr2 = ColorHelper.CalculateColor(Color.White, clrHot, alpha);

			// Draw the label's background.
			if (state == PaintState.Hot)
			{
				using (LinearGradientBrush br = new LinearGradientBrush(rc, Color.White, clr2, angle))
					g.FillRectangle(br, rc);
			}
			else
			{
				using (LinearGradientBrush br = new LinearGradientBrush(rc, clr2, clrHot, angle))
					g.FillRectangle(br, rc);
			}

			// Draw a black border around the label.
			ControlPaint.DrawBorder(g, rc, Color.Black, ButtonBorderStyle.Solid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a value indicating whether or not visual style rendering is supported
		/// in the application and if the specified element can be rendered.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool CanPaintVisualStyle(VisualStyleElement element)
		{
			return (Application.RenderWithVisualStyles && VisualStyleRenderer.IsElementDefined(element));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Because the popup containers forces a little padding above and below, we need to get
		/// the popup's parent (which is the popup container) and paint its background to match
		/// the menu color.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "g gets disposed or returned depending on parameter returnGraphics")]
		public static Graphics PaintDropDownContainer(IntPtr hwnd, bool returnGraphics)
		{
			IntPtr hwndParent = GetParent(hwnd);
			Graphics g = Graphics.FromHwnd(hwndParent);
			RectangleF rc = g.VisibleClipBounds;
			rc.Inflate(-1, -1);
			g.FillRectangle(SystemBrushes.Menu, rc);

			if (!returnGraphics)
			{
				g.Dispose();
				g = null;
			}

			return g;
		}
	}
}
