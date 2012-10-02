using System;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Encapsulates a simple panel whose border, by default is 3D if visual styles aren't
	/// enabled and is a single line (painted using visual styles) when visual styles are
	/// enabled.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwPanel : FwTextPanel
	{
		private bool m_overrideBorderDrawing = false;
		private bool m_paintExplorerBarBackground = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwPanel()
		{
			BorderStyle = (Application.VisualStyleState == VisualStyleState.NoneEnabled ?
				BorderStyle.Fixed3D : BorderStyle.FixedSingle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new bool DoubleBuffered
		{
			get { return base.DoubleBuffered; }
			set { base.DoubleBuffered = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Catch the non client area paint message so we can paint a border around the
		/// explorer bar that isn't black.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void WndProc(ref Message m)
		{
			base.WndProc(ref m);

			if (m.Msg == PaintingHelper.WM_NCPAINT && m_overrideBorderDrawing)
			{
				PaintingHelper.DrawCustomBorder(this);
				m.Result = IntPtr.Zero;
				m.Msg = 0;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// After the panel has been resized, force the border to be repainted. I found that
		/// often, after resizing the panel at runtime (e.g. when it's docked inside a
		/// splitter panel and the splitter moved), the portion of the border that was newly
		/// repainted didn't show the overriden border color handled by the WndProc above.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnClientSizeChanged(EventArgs e)
		{
			base.OnClientSizeChanged(e);

			if (m_overrideBorderDrawing)
				Utils.Win32.SendMessage(Handle, PaintingHelper.WM_NCPAINT, 1, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new BorderStyle BorderStyle
		{
			get {return base.BorderStyle;}
			set
			{
				base.BorderStyle = value;

				m_overrideBorderDrawing = (value == BorderStyle.FixedSingle &&
					(Application.VisualStyleState == VisualStyleState.NonClientAreaEnabled ||
					Application.VisualStyleState == VisualStyleState.ClientAndNonClientAreasEnabled));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not the background of the panel will
		/// be painted using the visual style's explorer bar element.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool PaintExplorerBarBackground
		{
			get { return m_paintExplorerBarBackground; }
			set { m_paintExplorerBarBackground = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaintBackground(PaintEventArgs e)
		{
			if (!DesignMode && m_paintExplorerBarBackground)
			{
				VisualStyleElement element = VisualStyleElement.ExplorerBar.NormalGroupBackground.Normal;
				if (PaintingHelper.CanPaintVisualStyle(element))
				{
					VisualStyleRenderer renderer = new VisualStyleRenderer(element);
					renderer.DrawBackground(e.Graphics, ClientRectangle);
					return;
				}
			}

			base.OnPaintBackground(e);
		}
	}
}
