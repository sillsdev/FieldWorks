// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls.Resources;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// Summary description for SummaryCommandControl.
	/// </summary>
	internal class SummaryCommandControl : UserControl
	{
		/// <summary>
		/// This menu contains the items that are displayed when the context menu icon is clicked,
		/// and which are displayed in buttons as space permits.
		/// </summary>
		SummarySlice m_slice;
		bool m_fInLayout;
		Font m_hotLinkFont;
		// list of menu items we are displaying as buttons.
		List<ToolStripItem> m_buttonMenuItems = new List<ToolStripItem>();
		bool[] m_buttonDrawnEnabled;
		// x coord of left of first button.
		int m_firstButtonOffset = 0;
		int m_lastWidth = 0;
		List<Tuple<ToolStripMenuItem, EventHandler>> m_menuItems = null; // Menu last created by OnLayout. Need consistent one for OnClick.
		Timer m_timer;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private SliceContextMenuFactory SliceContextMenuFactory { get; set; }

		internal SummaryCommandControl(SummarySlice slice, SliceContextMenuFactory sliceContextMenuFactory)
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			m_slice = slice;
			SliceContextMenuFactory = sliceContextMenuFactory;
			m_hotLinkFont = new Font(MiscUtils.StandardSansSerif, (float)10.0, FontStyle.Underline);
			m_timer = new Timer
			{
				Interval = 400 // ms
			};

			m_timer.Tick += m_timer_Tick;
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException($"'{GetType().Name}' in use after being disposed.");
			}
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if( disposing )
			{
				SliceContextMenuFactory.DisposeHotLinksMenus(m_menuItems);
				if (m_timer != null)
				{
					m_timer.Stop();
					m_timer.Tick -= m_timer_Tick;
					m_timer.Dispose();
				}
				components?.Dispose();
				m_hotLinkFont?.Dispose();
			}
			m_menuItems = null;
			m_hotLinkFont = null;
			m_timer = null;
			m_slice = null; // Client is responsible for this.
			m_buttonDrawnEnabled = null;
			m_buttonMenuItems.Clear();
			m_buttonMenuItems = null;

			base.Dispose( disposing );
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SummaryCommandControl));
			this.SuspendLayout();
			//
			// SummaryCommandControl
			//
			this.Name = "SummaryCommandControl";
			resources.ApplyResources(this, "$this");
			this.ResumeLayout(false);

		}
		#endregion

		const int kGapInBetweenButtons = 5; // gap between buttons

		protected override void OnLayout(LayoutEventArgs levent)
		{
			// Too early for layout.
			if (m_slice.ContainingDataTree == null)
			{
				return;
			}

			base.OnLayout (levent);
			if (m_fInLayout)
			{
				return;
			}

			var graphics = CreateGraphics();
			try
			{
				m_fInLayout = true;
				// Clear out old collection of menu items,
				// since we are fixin to reset the menu.
				m_buttonMenuItems.Clear();
				if (m_menuItems != null)
				{
					// Dispose the old ones, since this class 'owns' them, so must dispose them, even if they are created elsewhere.
					SliceContextMenuFactory.DisposeHotLinksMenus(m_menuItems);
					m_menuItems = null;
				}
				var hotlinksMenuId = m_slice.HotlinksMenuId;
				m_menuItems = string.IsNullOrWhiteSpace(hotlinksMenuId) ? null : SliceContextMenuFactory.GetHotlinksMenuItems(m_slice, hotlinksMenuId);
				if (m_menuItems == null || !m_menuItems.Any())
				{
					return;
				}

				var availButtonWidth = Width - 2;
				foreach (var menuItemTuple in m_menuItems)
				{
					var item = menuItemTuple.Item1;
					var label = item.Text;
					var width = (int)graphics.MeasureString(label, m_hotLinkFont).Width;
					if (width + kGapInBetweenButtons > availButtonWidth)
					{
						break;
					}
					m_buttonMenuItems.Add(item);
					availButtonWidth -= width + kGapInBetweenButtons;
				}
				m_firstButtonOffset = availButtonWidth;
				m_buttonDrawnEnabled = new bool[m_buttonMenuItems.Count];
			}
			finally
			{
				m_fInLayout = false;
				graphics.Dispose();
			}

		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			var xPos = m_firstButtonOffset;
			using (var graphics = e.Graphics)
			using (Brush brush = new SolidBrush(Color.Blue))
			using (Brush disabledBrush = new SolidBrush(Color.Gray))
			{
				var i = 0;
				foreach (var item in m_buttonMenuItems)
				{
					var label = item.Text.Replace("_",string.Empty);
					m_buttonDrawnEnabled[i] = item.Enabled;
					graphics.DrawString(label, m_hotLinkFont, (item.Enabled ? brush : disabledBrush), xPos, 0);
					xPos += (int)graphics.MeasureString(label, m_hotLinkFont).Width + kGapInBetweenButtons;
					i++;
				}
			}
			if (m_buttonMenuItems.Count > 0)
			{
				m_timer.Start(); // keep them up to date.
			}
		}

		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged (e);
			if (!Visible)
			{
				m_timer.Stop();  // no point invalidating while hidden!
			}
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged (e);
			if (Width == m_lastWidth)
			{
				return;
			}
			m_lastWidth = Width;
			PerformLayout();
			Invalidate();
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e); // invoke any delegates.
			using (var graphics = CreateGraphics())
			{
				if (m_menuItems == null)
				{
					return;
				}
				var targetPos = e.X;
				var xPos = m_firstButtonOffset;
				foreach (var item in m_buttonMenuItems)
				{
					var label = item.Text;
					var width = (int)graphics.MeasureString(label, m_hotLinkFont).Width;
					if (targetPos > xPos && targetPos < xPos + width)
					{
						// some menu commands use the current slice to decide what to act on.
						// It had better be the one the command is expecting it to be.
						m_slice.ContainingDataTree.CurrentSlice = m_slice;
						if (item.Enabled)
						{
							item.PerformClick();
						}
						else
						{
							MessageBox.Show(this, DetailControlsStrings.ksCmdUnavailable, DetailControlsStrings.ksCmdDisabled);
							Invalidate(); // Make sure command enabling is correct.
						}
						break;
					}
					xPos += width + kGapInBetweenButtons;
				}
			}
		}

		private void m_timer_Tick(object sender, EventArgs e)
		{
			if (m_buttonDrawnEnabled.Where((t, i) => t != (m_buttonMenuItems[i]).Enabled).Any())
			{
				Invalidate();
			}
		}
	}
}
