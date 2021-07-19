// Copyright (c) 2016 SIL International
// SilOutlookBar is licensed under the MIT license.

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SIL.SilSidePane
{
	/// <summary>
	/// Banner at the top of a sidepane to display the name of the currently-selected tab.
	/// </summary>
	internal class Banner : Label
	{
		public Banner()
		{
			ForeColor = Color.White;
		}

		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " ******");
			base.Dispose(disposing);
		}

		/// <summary>
		/// Paint a blue gradient background
		/// </summary>
		protected override void OnPaint(PaintEventArgs e)
		{
			var rectangleToPaint = ClientRectangle;
			var beginColor = Color.FromArgb(0x58, 0x80, 0xd0);
			var endColor = Color.FromArgb(0x08, 0x40, 0x98);

			using (var brush = new LinearGradientBrush(rectangleToPaint, beginColor, endColor,
				LinearGradientMode.Vertical))
			{
				e.Graphics.FillRectangle(brush, rectangleToPaint);
			}

			// Then paint the label text on top
			base.OnPaint(e);
		}
	}
}