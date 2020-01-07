// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace LanguageExplorer
{
	/// <summary>
	/// Allows having a colored background, as for the "filtered" panel.
	/// </summary>
	public class StatusBarTextBox : StatusBarPanel
	{
		private Brush m_backBrush;
		private string m_text = string.Empty;
		private StatusBar m_bar;

		/// <summary />
		public StatusBarTextBox(StatusBar bar)
		{
			Style = StatusBarPanelStyle.OwnerDraw;
			m_backBrush = Brushes.Transparent;
			bar.DrawItem += HandleDrawItem;
			m_bar = bar;
		}

		/// <summary />
		private bool IsDisposed { get; set; }

		/// <summary />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				if (m_bar != null)
				{
					m_bar.DrawItem -= HandleDrawItem;
				}
			}
			m_backBrush = null;
			m_text = null;
			m_bar = null;

			base.Dispose(disposing);

			IsDisposed = true;
		}

		/// <summary>
		/// use this to get a background color
		/// </summary>
		public Brush BackBrush
		{
			set
			{
				m_backBrush = value;
				// Be very careful about taking this out. It will SOMETIMES work without it, and
				// apparently always if you are stepping through the code in a debugger, but not always.
				m_bar.Invalidate(); // Enhance JohnT: there should be some way to just invalidate this, but can't find it.
			}
		}

		/// <summary>
		/// Somehow the normal "Text" attr we inherit was getting cleared.
		/// </summary>
		public string TextForReal
		{
			set
			{
				m_text = value;
				// But we still need to set the Text property to get autosizing to work.
				Text = m_text;
				// Be very careful about taking this out. It will SOMETIMES work without it, and
				// apparently always if you are stepping through the code in a debugger, but not always.
				m_bar.Invalidate(); // Enhance JohnT: there should be some way to just invalidate this, but can't find it.
			}
		}

		/// <summary>
		/// Handle the DrawItem Event from the parent bar
		/// </summary>
		private void HandleDrawItem(object sender, StatusBarDrawItemEventArgs sbdevent)
		{
			if (sbdevent.Panel != this)
			{
				return;
			}
			var rect = sbdevent.Bounds;
			if (Application.RenderWithVisualStyles)
			{
				rect.Width = rect.Width - SystemInformation.Border3DSize.Width;
			}
			if (m_text.Trim() != string.Empty)
			{
				sbdevent.Graphics.FillRectangle(m_backBrush, rect);
			}
			using (var sf = new StringFormat())
			{
				sf.Alignment = StringAlignment.Center;
				var centered = rect;
				centered.Offset(0, (int)(rect.Height - sbdevent.Graphics.MeasureString(m_text, sbdevent.Font).Height) / 2);
				using (var brush = new SolidBrush(sbdevent.ForeColor))
				{
					sbdevent.Graphics.DrawString(m_text, sbdevent.Font, brush, centered, sf);
				}
			}
		}
	}
}