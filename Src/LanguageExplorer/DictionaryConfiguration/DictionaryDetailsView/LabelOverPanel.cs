// Copyright (c) 2016-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Windows.Forms;

namespace LanguageExplorer.DictionaryConfiguration.DictionaryDetailsView
{
	public partial class LabelOverPanel : UserControl
	{
		private Control m_panelContents;
		private readonly ToolTip m_tt = new ToolTip { AutoPopDelay = 32767 }; // We expect verbose tooltips; make them show as long as possible (33s)

		public LabelOverPanel()
		{
			InitializeComponent();
		}

		public string LabelText { set { label.Text = value; } }

		public string LabelToolTip { set { m_tt.SetToolTip(label, value); } }

		public UserControl PanelContents
		{
			set
			{
				if (m_panelContents != null)
				{
					panel.Controls.Remove(m_panelContents);
					m_panelContents.Dispose();
				}
				m_panelContents = value;
				if (m_panelContents != null)
				{
					m_panelContents.Dock = DockStyle.Fill;
					m_panelContents.Location = new Point(0, 0);
					panel.Controls.Add(m_panelContents);
				}
			}
		}

		public event EventHandler LabelClicked
		{
			add { label.Click += value; }
			remove { label.Click -= value; }
		}

		/// <summary>
		/// Make sure that on Mono, the height of LabelOverPanel is not too small to show its two controls, similar to LT-18097. Set the height to help Mono.
		/// </summary>
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			if (Height < panel.Location.Y + panel.Height)
			{
				Height = panel.Location.Y + panel.Height;
			}
		}
	}
}