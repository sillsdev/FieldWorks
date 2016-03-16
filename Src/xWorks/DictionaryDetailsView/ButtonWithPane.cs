// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Windows.Forms;

namespace SIL.FieldWorks.XWorks.DictionaryDetailsView
{
	public partial class ButtonWithPane : UserControl
	{
		private Control m_panelContents;

		public ButtonWithPane()
		{
			InitializeComponent();
		}

		public UserControl PaneContents
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
					// Set the initial size to whatever is available.
					SetPanelOptionsSize();
				}
			}
		}

		private void SetPanelOptionsSize()
		{
			//panel.Size = new Size(Width, Height - (m_deltaStyleLabel + 10));
		}

		public event EventHandler ButtonClicked
		{
			add { button.Click += value; }
			remove { button.Click -= value; }
		}
	}
}
