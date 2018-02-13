// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Encapsulates a control used to drag as an indicator where the edge of a resized control
	/// will end up.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SizingLine : Label
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:SizingLine"/> class.
		/// </summary>
		/// <param name="width">The width.</param>
		/// <param name="height">The height.</param>
		/// ------------------------------------------------------------------------------------
		public SizingLine(int width, int height)
		{
			DoubleBuffered = true;
			AutoSize = false;
			Size = new Size(width, height);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
		}

		/// <summary/>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ******");
			base.Dispose(disposing);
		}

		/// <summary/>
		protected override void OnPaint(PaintEventArgs e)
		{
			using (HatchBrush br = new HatchBrush(HatchStyle.Percent50, Color.Black, BackColor))
				e.Graphics.FillRectangle(br, ClientRectangle);
		}
	}
}
