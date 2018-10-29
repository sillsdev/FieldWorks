// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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

		/// <summary>
		///
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				// This was disposing a system brush, so the next time it was used it was invalid.
				// It's invalid to Dispose of a system brush as it removes it from the main app
				// thread and all future refs will fail.
				//				if (m_backBrush != null)
				//					m_backBrush.Dispose();
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
				//// This was disposing a system brush, so the next time it was used it was invalid.
				//// Using the reflector tool we could see that the static Brush Yellow method is
				//// returning a Brush that exists in a Thread context and disposing that doesn't
				//// remove the index entry from the ThreadData so the next one gets an invalid
				//// object and crashes.  This could be considered a bug in .NET 2.0.
				//// Just for fun and out of interest to others, here's the property in the
				//// System.Drawing.Brushes class: (retrieved by Lutz Roeder's .NET Reflector)
				//// Thanks to Randy for finding this very useful utility: the find of the week!
				////
				//// public static Brush Yellow
				////{
				////    get
				////    {
				////        Brush brush1 = (Brush) SafeNativeMethods.Gdip.ThreadData[Brushes.YellowKey];
				////        if (brush1 == null)
				////        {
				////            brush1 = new SolidBrush(Color.Yellow);
				////            SafeNativeMethods.Gdip.ThreadData[Brushes.YellowKey] = brush1;
				////        }
				////        return brush1;
				////    }
				////}
				//if (m_backBrush != null && m_backBrush != value)
				//	m_backBrush.Dispose();

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