// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Windows.Forms;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// Encapsulates a simple panel whose border, by default is 3D if visual styles aren't
	/// enabled and is a single line (painted using visual styles) when visual styles are
	/// enabled.
	/// </summary>
	public class FwPanel : FwTextPanel
	{
		private bool m_overrideBorderDrawing;

		/// <summary />
		public FwPanel()
		{
			BorderStyle = (Application.VisualStyleState == VisualStyleState.NoneEnabled ? BorderStyle.Fixed3D : BorderStyle.FixedSingle);
		}

		/// <summary />
		public new bool DoubleBuffered
		{
			get { return base.DoubleBuffered; }
			set { base.DoubleBuffered = value; }
		}

		/// <inheritdoc />
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

		/// <inheritdoc />
		protected override void OnClientSizeChanged(EventArgs e)
		{
			base.OnClientSizeChanged(e);

			if (m_overrideBorderDrawing)
			{
				Win32.SendMessage(Handle, PaintingHelper.WM_NCPAINT, 1, 0);
			}
		}

		/// <summary />
		public new BorderStyle BorderStyle
		{
			get { return base.BorderStyle; }
			set
			{
				base.BorderStyle = value;

				m_overrideBorderDrawing = (value == BorderStyle.FixedSingle &&
					(Application.VisualStyleState == VisualStyleState.NonClientAreaEnabled ||
					Application.VisualStyleState == VisualStyleState.ClientAndNonClientAreasEnabled));
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether or not the background of the panel will
		/// be painted using the visual style's explorer bar element.
		/// </summary>
		public bool PaintExplorerBarBackground { get; set; } = false;

		/// <summary />
		protected override void OnPaintBackground(PaintEventArgs e)
		{
			if (!DesignMode && PaintExplorerBarBackground)
			{
				var element = VisualStyleElement.ExplorerBar.NormalGroupBackground.Normal;
				if (PaintingHelper.CanPaintVisualStyle(element))
				{
					var renderer = new VisualStyleRenderer(element);
					renderer.DrawBackground(e.Graphics, ClientRectangle);
					return;
				}
			}

			base.OnPaintBackground(e);
		}
	}
}