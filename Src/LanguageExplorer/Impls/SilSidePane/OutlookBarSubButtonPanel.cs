// Copyright (c) 2016 SIL International
// SilOutlookBar is licensed under the MIT license.

using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace LanguageExplorer.Impls.SilSidePane
{
	/// <summary>
	/// Color-themed panel
	/// </summary>
	internal class OutlookBarSubButtonPanel : Panel
	{
		/// <summary />
		public OutlookBarSubButtonPanel()
		{
			DoubleBuffered = true;
			ResizeRedraw = true;
			AutoScroll = true;
			BorderStyle = BorderStyle.None;
		}

		/// <summary />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "******* Missing Dispose() call for " + GetType() + ". *******");
			base.Dispose(disposing);
		}

		/// <summary />
		protected override void OnPaint(PaintEventArgs e)
		{
			using (var br = new LinearGradientBrush(ClientRectangle, ProfessionalColors.ToolStripGradientMiddle, ProfessionalColors.ToolStripGradientEnd, LinearGradientMode.Vertical))
			{
				e.Graphics.FillRectangle(br, ClientRectangle);
			}
		}

		/// <summary />
		protected override void OnScroll(ScrollEventArgs se)
		{
			base.OnScroll(se);
			Invalidate();
		}
	}
}