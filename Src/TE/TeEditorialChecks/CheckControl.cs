// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CheckControl.cs
// Responsibility: TE Team
//

using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using ControlExtenders;
using Microsoft.Win32;
using SIL.Utils;
using SIL.FieldWorks.Common.Controls;

namespace SIL.FieldWorks.TE.TeEditorialChecks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Control containing the tool strip and the checking pane.
	/// </summary>
	/// <remarks><para>This class serves as a base class for specific implementations of the
	/// left pane of the checking views.</para>
	/// <para>It can be docked to the left, top, or right, or floating. If it is docked to the
	/// top the Content control is displayed in a combo box-like tool strip item, otherwise
	/// the Content control fills the client area of the CheckControl.</para>
	/// <para>The tool strip combo box uses the Text property and TextChanged event of the
	/// Content control to retrieve the text to display when the combo box is collapsed.</para>
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	public partial class CheckControl : UserControl, IChecksViewWrapperView
	{
		private IFloaty m_floaty;
		private Control m_Control;

		/// <summary></summary>
		protected Persistence m_persistence;
		/// <summary>If this separator is set to something by subclasses, then that separator
		/// will only be turned on when the toolstrip is docked to the top.</summary>
		protected ToolStripSeparator m_sepShowOnlyAtTop = null;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:CheckControl"/> class. This default
		/// constructor is required for Designer.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CheckControl()
		{
			InitializeComponent();
			pnlOuter.BringToFront();
			m_ComboBox.Dock = DockStyle.Fill;
			m_ComboBox.DropDownHeight = 350;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:CheckControl"/> class. This default
		/// constructor is required for Designer.
		/// </summary>
		/// <param name="sCaption">The caption to use when this control is displayed as a
		/// floating window</param>
		/// <param name="sProject">The name of the current project</param>
		/// ------------------------------------------------------------------------------------
		public CheckControl(string sCaption, string sProject)
			: this()
		{
			Text = string.Format(Text, sProject, sCaption);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing)
			{
				if (components != null)
					components.Dispose();

				if (m_Control != null)
					m_Control.Dispose();
			}

			base.Dispose(disposing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a button to the toolstrip.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <param name="buttonImage">The image for this button.</param>
		/// <param name="toolTip">The tool tip for this button.</param>
		/// ------------------------------------------------------------------------------------
		protected void AddToolStripButton(int index, Image buttonImage, string toolTip)
		{
			ToolStripButton button = new ToolStripButton();
			button.Image = buttonImage;
			button.ToolTipText = toolTip;
			m_ToolStrip.Items.Insert(index, button);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the tool strip.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ToolStrip ToolStrip
		{
			get { return m_ToolStrip; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The Text of the control
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(true)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		public override string Text
		{
#if !__MonoCS__
			get { return m_ToolStrip.Text; }
			set { m_ToolStrip.Text = value; }
#else
			// TODO-Linux: base constructor calls (indirecty) Control.CreateParam which queries the Text Attribute
			get { if (m_ToolStrip != null) return m_ToolStrip.Text; else return String.Empty; }
			set { if (m_ToolStrip != null) m_ToolStrip.Text = value; }
#endif
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the content (that contains the key terms/ed. checks tree).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Control MainPanelContent
		{
			get { return splitContainer.Panel1.Controls.Count > 0 ? splitContainer.Panel1.Controls[0] : null; }
			set
			{
				splitContainer.Panel1.Controls.Clear();
				splitContainer.Panel1.Controls.Add(value);
				value.Dock = DockStyle.Fill;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the subordinate pane with the detail controls
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SplitterPanel DetailsPane
		{
			get { return splitContainer.Panel2; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is really annoying. For some reason, even though the content panel is docked
		/// to fill, it will often move to the top, left of this user control, thus covering
		/// the tool strip control. This is the kludge necessary to remedy that.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void Content_LocationChanged(object sender, EventArgs e)
		{
			if (Dock != DockStyle.Top && m_Control.Top != m_ToolStrip.Height)
			{
				m_Control.LocationChanged -= Content_LocationChanged;
				m_Control.Top = m_ToolStrip.Height;
				m_Control.LocationChanged += Content_LocationChanged;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override this to provide a height other than the default DropDownHeight.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual int PreferredHeight
		{
			get { return -1; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the height to make the drop-down combo box that is used to show the
		/// content when the control is docked to the top of the parent.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual int DropDownHeight
		{
			get
			{
				// Figure out what 2/3 of the space is between the tool strip and
				// the bottom of the screen. That is the max height of the drop-down.
				Point pt = m_ToolStrip.PointToScreen(m_ToolStrip.Location);
				Screen scn = Screen.FromPoint(pt);
				int maxHeight = (int)((scn.Bounds.Height - pt.Y) * 0.667f);

				int prefHeight = PreferredHeight;

				if (prefHeight == -1)
					return maxHeight;

				return Math.Min(prefHeight, maxHeight);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the floaty that hosts this control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IFloaty Floaty
		{
			get { return m_floaty; }
			set
			{
				if (m_floaty != null)
				{
					m_floaty.Docking -= m_floaty_Docking;
					m_floaty.Undocked -= m_floaty_Undocked;
					m_floaty.DockUndockBegin -= OnDockUndockBegin;
					m_floaty.DockUndockEnd -= OnDockUndockEnd;
				}

				m_floaty = value;

				if (m_floaty != null)
				{
					m_floaty.Docking += m_floaty_Docking;
					m_floaty.Undocked += m_floaty_Undocked;
					m_floaty.DockUndockBegin += OnDockUndockBegin;
					m_floaty.DockUndockEnd += OnDockUndockEnd;
				}

				Control ctrl = m_floaty as Form;
				if (ctrl != null)
					ctrl.MinimumSize = new Size(125, 265);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnDockUndockBegin(object sender, DockingEventArgs e)
		{
			// Only need the separator when docked to the top.
			if (m_sepShowOnlyAtTop != null)
				m_sepShowOnlyAtTop.Visible = (e.DockStyle == DockStyle.Top);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnDockUndockEnd(object sender, DockingEventArgs e)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_floaty_Docking(object sender, DockingEventArgs e)
		{
			OnDocking(sender, e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_floaty_Undocked(object sender, EventArgs e)
		{
			OnUndocked(sender, e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called after being docked in the parent
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The DockingEventArgs instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		public void OnDocking(object sender, DockingEventArgs e)
		{
			if (Dock == DockStyle.Top)
			{
				// need to hide the tree
				Height = m_ToolStrip.Height;
				m_ComboBox.DropDownHeight = DropDownHeight;
				m_ComboBox.Visible = true;
				m_ComboBox.DropDownControl = pnlOuter;
				if (m_floaty != null)
					m_floaty.ShowSplitter = false;
			}
			else
			{
				m_ComboBox.Visible = false;
				if (pnlOuter != null)
				{
					pnlOuter.Show();
					pnlOuter.Dock = DockStyle.None;
					pnlOuter.Parent = this;
					pnlOuter.Dock = DockStyle.Fill;
					pnlOuter.BringToFront();
				}

				if (m_floaty != null)
					m_floaty.ShowSplitter = true;
			}
			RefreshControlsAfterDocking();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to allow inner controls to be reset after this control has been docked in
		/// the parent window. If overridden, base must be called first.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void RefreshControlsAfterDocking()
		{
			splitContainer.Orientation = Dock == DockStyle.Top ? Orientation.Vertical : Orientation.Horizontal;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called after being undocked - control is now floating.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The EventArgs instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void OnUndocked(object sender, EventArgs e)
		{
			m_ComboBox.Visible = false;

			if (pnlOuter != null)
			{
				pnlOuter.Show();
				pnlOuter.Dock = DockStyle.None;
				pnlOuter.Parent = this;
				pnlOuter.Dock = DockStyle.Fill;
				pnlOuter.BringToFront();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the user clicked the Top menu item. Should dock to the top.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnDockTopClicked(object sender, EventArgs e)
		{
			m_floaty.Dock(DockStyle.Top);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the user clicked the Left menu item. Should dock to the left.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnDockLeftClicked(object sender, EventArgs e)
		{
			m_floaty.Dock(DockStyle.Left);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the user clicked the Right menu item. Should dock to the right.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnDockRightClicked(object sender, EventArgs e)
		{
			m_floaty.Dock(DockStyle.Right);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the user clicked the Floating menu item.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnFloatingClicked(object sender, EventArgs e)
		{
			m_floaty.Float();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the drop down is opening.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnDropDownOpening(object sender, EventArgs e)
		{
			// While the drop down is showing the user shouldn't be able to drag the toolbar
			// around - otherwise we get surprising behavior if he clicks in the toolbar while
			// he drop down is shown.
			m_floaty.AllowFloating = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the drop down is closed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnDropDownClosed(object sender, EventArgs e)
		{
			m_floaty.AllowFloating = true;
		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		///
//		/// </summary>
//		/// ------------------------------------------------------------------------------------
//		private void splitOuter_SplitterMoving(object sender, SplitterCancelEventArgs e)
//		{
//			if (e.SplitX != 25)
//				e.Cancel = true;
//		}

		#region IChecksViewWrapperView Members
		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the persistence object.
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		public Persistence Persistence
		{
			get { return m_persistence; }
			set
			{
				if (m_persistence != null)
				{
					m_persistence.LoadSettings -= OnLoadSettings;
					m_persistence.SaveSettings -= OnSaveSettings;
				}

				m_persistence = value;

				if (m_persistence != null)
				{
					m_persistence.LoadSettings += OnLoadSettings;
					m_persistence.SaveSettings += OnSaveSettings;
				}
			}
		}

		#endregion

		#region Methods for loading and saving settings.
		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Load settings
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		protected virtual void OnLoadSettings(RegistryKey key)
		{
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Save settings
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		protected virtual void OnSaveSettings(RegistryKey key)
		{
		}

		#endregion
	}
}
