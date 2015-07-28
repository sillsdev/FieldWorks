// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.Utils;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Summary description for SummaryCommandControl.
	/// </summary>
	public class SummaryCommandControl : UserControl, IFWDisposable
	{
		/// <summary>
		/// This menu contains the items that are displayed when the context menu icon is clicked,
		/// and which are displayed in buttons as space permits.
		/// </summary>
		SummarySlice m_slice;
		bool m_fInLayout = false;
		Font m_hotLinkFont;
		// list of menu items we are displaying as buttons.
		List<MenuItem> m_buttonMenuItems = new List<MenuItem>();
		bool[] m_buttonDrawnEnabled = null;
		// x coord of left of first button.
		int m_firstButtonOffset = 0;
		int m_lastWidth = 0;
		ContextMenu m_menu = null; // Menu last created by OnLayout. Need consistent one for OnClick.
		System.Windows.Forms.Timer m_timer;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public SummaryCommandControl(SummarySlice slice)
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			m_slice = slice;
			m_hotLinkFont = new Font(MiscUtils.StandardSansSerif, (float)10.0, FontStyle.Underline);
			m_timer = new System.Windows.Forms.Timer();
			m_timer.Interval = 400; // ms
			m_timer.Tick += new EventHandler(m_timer_Tick);
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

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if (m_timer != null)
				{
					m_timer.Stop();
					m_timer.Tick -= new EventHandler(m_timer_Tick);
					m_timer.Dispose();
				}
				if(components != null)
				{
					components.Dispose();
				}
				if (m_hotLinkFont != null)
					m_hotLinkFont.Dispose();
			}
			m_hotLinkFont = null;
			m_timer = null;
			m_menu = null; // Client is responsible for this.
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
				return;

			base.OnLayout (levent);
			if (m_fInLayout)
				return;

			Graphics g = this.CreateGraphics();
			try
			{
				m_fInLayout = true;
				// Clear out old collection of menu items,
				// since we are fixin to reset the menu.
				m_buttonMenuItems.Clear();
				m_menu = m_slice.RetrieveContextMenuForHotlinks();
				if (m_menu == null)
					return;

				int availButtonWidth = this.Width - 2;
				for (int i = 0; i < m_menu.MenuItems.Count; i++)
				{
					MenuItem item = m_menu.MenuItems[i];
					string label = item.Text.Replace("_","");
					int width = (int)(g.MeasureString(label, m_hotLinkFont).Width);
					if (width + kGapInBetweenButtons > availButtonWidth)
						break;
					m_buttonMenuItems.Add(item);
					availButtonWidth -= width + kGapInBetweenButtons;
				}
				m_firstButtonOffset = availButtonWidth;
				m_buttonDrawnEnabled = new bool[m_buttonMenuItems.Count];
			}
			finally
			{
				m_fInLayout = false;
				g.Dispose();
			}

		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			int xPos = m_firstButtonOffset;
			using (Graphics g = e.Graphics)
			using (Brush brush = new SolidBrush(Color.Blue))
			using (Brush disabledBrush = new SolidBrush(Color.Gray))
			{
				int i = 0;
				foreach (MenuItem item in m_buttonMenuItems)
				{
					string label = item.Text.Replace("_","");
					m_buttonDrawnEnabled[i] = item.Enabled;
					g.DrawString(label, m_hotLinkFont, (item.Enabled ? brush : disabledBrush), (float) xPos, (float)0);
					xPos += (int)g.MeasureString(label, m_hotLinkFont).Width + kGapInBetweenButtons;
					i++;
				}
			}
			if (m_buttonMenuItems.Count > 0)
				m_timer.Start(); // keep them up to date.
		}

		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged (e);
			if (!this.Visible)
				m_timer.Stop();  // no point invalidating while hidden!
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged (e);
			if (Width == m_lastWidth)
				return;
			m_lastWidth = Width;
			PerformLayout();
			Invalidate();
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e); // invoke any delegates.
			using (Graphics g = this.CreateGraphics())
			{
				if (m_menu == null)
					return;
				int targetPos = e.X;
				int xPos = m_firstButtonOffset;
				foreach (MenuItem item in m_buttonMenuItems)
				{
					if (item is XCore.AdapterMenuItem)
					{
						XCore.AdapterMenuItem xcoreMenuItem = (item as XCore.AdapterMenuItem);
						string label = xcoreMenuItem.Text;
						int width = (int)g.MeasureString(label, m_hotLinkFont).Width;
						if (targetPos > xPos && targetPos < xPos + width)
						{
							// some menu commands use the current slice to decide what to act on.
							// It had better be the one the command is expecting it to be.
							m_slice.ContainingDataTree.CurrentSlice = m_slice;
							if (xcoreMenuItem.Enabled)
							{
								ItemClicked(xcoreMenuItem);
							}
							else
							{
								MessageBox.Show(this, DetailControlsStrings.ksCmdUnavailable,
									DetailControlsStrings.ksCmdDisabled);
								this.Invalidate(); // Make sure command enabling is correct.
							}
							break;
						}
						else
							xPos += width + kGapInBetweenButtons;
					}
				}
			}
		}

		private void ItemClicked(XCore.AdapterMenuItem item)
		{
			XCore.ChoiceBase c = (XCore.ChoiceBase) item.Tag;
			c.OnClick(item, null);
		}

		private void m_timer_Tick(object sender, EventArgs e)
		{
			for (int i = 0; i < m_buttonDrawnEnabled.Length; i++)
			{
				if (m_buttonDrawnEnabled[i] != (m_buttonMenuItems[i] as MenuItem).Enabled)
				{
					this.Invalidate();
					break;
				}
			}
		}
	}
}
