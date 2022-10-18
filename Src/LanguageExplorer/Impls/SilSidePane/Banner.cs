// Copyright (c) 2016 SIL International
// SilOutlookBar is licensed under the MIT license.

using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace LanguageExplorer.Impls.SilSidePane
{
	/// <summary>
	/// Banner at the top of a sidepane to display the name of the currently-selected tab.
	/// </summary>
	internal sealed class Banner : Label
	{
		/// <summary />
		internal Banner()
		{
			ForeColor = Color.White;
		}

		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " ******");
			base.Dispose(disposing);
		}

		/// <summary>
		/// Paint a blue gradient background
		/// </summary>
		protected override void OnPaint(PaintEventArgs e)
		{
			var rectangleToPaint = ClientRectangle;
			using (var brush = new LinearGradientBrush(rectangleToPaint, Color.FromArgb(0x58, 0x80, 0xd0), Color.FromArgb(0x08, 0x40, 0x98), LinearGradientMode.Vertical))
			{
				e.Graphics.FillRectangle(brush, rectangleToPaint);
			}
			// Then paint the label text on top
			base.OnPaint(e);
		}
	}
}