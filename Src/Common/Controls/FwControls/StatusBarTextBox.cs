using System;
using System.Drawing;
using System.Windows.Forms;

using SIL.Utils;

namespace SIL.FieldWorks.Common.Controls
{

	/// <summary>
	/// Allows having a colored background, as for the "filtered" panel.
	/// </summary>
	public class StatusBarTextBox : StatusBarPanel, IFWDisposable
	{
		private System.Drawing.Brush m_backBrush;
		private string m_text = string.Empty;
		private StatusBar m_bar;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// constructor
		/// </summary>
		/// <param name="bar">Needed because the parent attr is null at this point</param>
		/// ------------------------------------------------------------------------------------
		public StatusBarTextBox(StatusBar bar): base()
		{
			Style = System.Windows.Forms.StatusBarPanelStyle.OwnerDraw;
			m_backBrush = System.Drawing.Brushes.Transparent;
			bar.DrawItem += new StatusBarDrawItemEventHandler(HandleDrawItem);
			m_bar = bar;
		}

		/// <summary>
		///
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		private bool m_isDisposed = false;

		/// <summary>
		///
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// This was disposing a system brush, so the next time it was used it was invalid.
				// It's invalid to Dispose of a system brush as it removes it from the main app
				// thread and all future refs will fail.
				//				if (m_backBrush != null)
				//					m_backBrush.Dispose();
				if (m_bar != null)
					m_bar.DrawItem -= new StatusBarDrawItemEventHandler(HandleDrawItem);
			}
			m_backBrush = null;
			m_text = null;
			m_bar = null;

			base.Dispose(disposing);

			m_isDisposed = true;
		}
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// use this to get a background color
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public System.Drawing.Brush BackBrush
		{
			set
			{
				CheckDisposed();

				//// This was disposing a system brush, so the next time it was used it was invalid.
				//// Using the reflector tool we could see that the static Brush Yellow method is
				//// returning a Brush that exists in a Thread context and disposing that doesn't
				//// remove the index entry from the ThreadData so the next one gets an invalid
				//// object and crashes.  This could be considerd a bug in .NET 2.0.
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Somehow the normal "Text" attr we inherit was getting cleared.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public  string TextForReal
		{
			set
			{
				CheckDisposed();

				m_text = value;
				// But we still need to set the Text property to get autosizing to work.
				this.Text = m_text;
				// Be very careful about taking this out. It will SOMETIMES work without it, and
				// apparently always if you are stepping through the code in a debugger, but not always.
				m_bar.Invalidate(); // Enhance JohnT: there should be some way to just invalidate this, but can't find it.
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the DrawItem Event from the parent bar
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="sbdevent"></param>
		/// ------------------------------------------------------------------------------------
		private void HandleDrawItem(object sender, StatusBarDrawItemEventArgs sbdevent)
		{
			if (sbdevent.Panel != this)
				return;

			Rectangle rect = sbdevent.Bounds;
			if (Application.RenderWithVisualStyles)
				rect.Width = rect.Width - SystemInformation.Border3DSize.Width;

			if (m_text.Trim() != string.Empty)
				sbdevent.Graphics.FillRectangle(m_backBrush, rect);

			using (StringFormat sf = new StringFormat())
			{
				sf.Alignment = StringAlignment.Center;
				Rectangle centered = rect;
				centered.Offset(0, (int)(rect.Height - sbdevent.Graphics.MeasureString(m_text, sbdevent.Font).Height) / 2);
				using (SolidBrush brush = new SolidBrush(sbdevent.ForeColor))
				{
					sbdevent.Graphics.DrawString(m_text, sbdevent.Font, brush, centered, sf);
				}
			}
		}
	}
}
