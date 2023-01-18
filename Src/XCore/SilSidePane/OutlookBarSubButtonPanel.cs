// Copyright (c) 2016 SIL International
// SilOutlookBar is licensed under the MIT license.

using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SIL.SilSidePane
{
	/// <summary>
	/// Color-themed panel
	/// </summary>
	internal class OutlookBarSubButtonPanel : Panel
	{
		/// <summary></summary>
		public OutlookBarSubButtonPanel()
		{
			DoubleBuffered = true;
			ResizeRedraw = true;
			AutoScroll = true;
			BorderStyle = BorderStyle.None;
		}

		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "******* Missing Dispose() call for " + GetType() + ". *******");
			base.Dispose(disposing);
		}

		/// <summary></summary>
		protected override void OnPaint(PaintEventArgs e)
		{
			using (LinearGradientBrush br = new LinearGradientBrush(ClientRectangle,
				ProfessionalColors.ToolStripGradientMiddle,
				ProfessionalColors.ToolStripGradientEnd, LinearGradientMode.Vertical))
			{
				e.Graphics.FillRectangle(br, ClientRectangle);
			}
		}

		/// <summary></summary>
		protected override void OnScroll(ScrollEventArgs se)
		{
			base.OnScroll(se);
			Invalidate();
		}
	}
}