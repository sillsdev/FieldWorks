// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Windows.Forms;

namespace SIL.FieldWorks.XWorks.DictionaryDetailsView
{
	public partial class ButtonOverPanel : UserControl
	{
		private Control m_panelContents;
		private readonly ToolTip m_tt = new ToolTip();

		public ButtonOverPanel()
		{
			InitializeComponent();
			ButtonToolTip = xWorksStrings.ConfigureReferencedHeadwordsTooltip;
		}

		public string ButtonText { set { button.Text = value; } }

		public string ButtonToolTip { set { m_tt.SetToolTip(button, value); } }

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

		public event EventHandler ButtonClicked
		{
			add { button.Click += value; }
			remove { button.Click -= value; }
		}

		/// <summary>
		/// On Mono, the height of ButtonOverPanel can be too small to show its two controls, causing LT-18097. Set the height to help Mono.
		/// </summary>
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			if (Height < panel.Location.Y + panel.Height)
				Height = panel.Location.Y + panel.Height;
		}
	}
}
