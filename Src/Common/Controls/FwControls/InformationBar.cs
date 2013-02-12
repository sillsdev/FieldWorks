// ------------------------------------------------------------------------------
// <copyright from='2002' to='2002' company='SIL International'>
//    Copyright (c) 2002, SIL International. All Rights Reserved.
// </copyright>
//
// File: InformationBar.cs
// Responsibility: ToddJ
// Last reviewed:
//
// <remarks>Implementation of InformationBar and InfoBarTextButton</remarks>
// ------------------------------------------------------------------------------
//
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// An information bar works like a title bar for a pane within a window. It can display
	/// a title and a collection of buttons.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ToolboxBitmap(typeof(InformationBar), "resources.InformationBar.ico")]
	public class InformationBar : UserControl, IFWDisposable
	{
		private System.ComponentModel.IContainer components;
		private InformationBarButtonCollection m_buttons = null;
		private int m_buttonWidth;
		internal System.Windows.Forms.Panel InfoBarPanel;
		/// <summary>
		///
		/// </summary>
		public System.Windows.Forms.Label InfoBarLabel;
		private SIL.FieldWorks.Common.Drawing.BorderDrawing m_borderDrawing;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public InformationBar()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer |
				ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// The font size below looks random, but is used in InitializeComponent for the
			// corresponding font assignment.  I didn't want to change it here.
			if (MiscUtils.IsUnix)
				InfoBarLabel.Font = new System.Drawing.Font(MiscUtils.StandardSansSerif, 8.861538F,
					System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, (System.Byte)0);

			// TODO: Add any initialization after the InitForm call
			m_buttonWidth = 17;
			DockPadding.All = 5;
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.m_borderDrawing = new SIL.FieldWorks.Common.Drawing.BorderDrawing(this.components);
			this.InfoBarPanel = new System.Windows.Forms.Panel();
			this.InfoBarLabel = new System.Windows.Forms.Label();
			this.InfoBarPanel.SuspendLayout();
			this.SuspendLayout();
			//
			// m_borderDrawing
			//
			this.m_borderDrawing.Graphics = null;
			//
			// InfoBarPanel
			//
			this.InfoBarPanel.AccessibleName = "InfoBarPanel";
			this.InfoBarPanel.Anchor = (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
				| System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right);
			this.InfoBarPanel.BackColor = System.Drawing.SystemColors.ControlDark;
			this.InfoBarPanel.Controls.AddRange(new System.Windows.Forms.Control[] {
																					   this.InfoBarLabel});
			this.InfoBarPanel.Location = new System.Drawing.Point(5, 5);
			this.InfoBarPanel.Name = "InfoBarPanel";
			this.InfoBarPanel.Size = new System.Drawing.Size(742, 22);
			this.InfoBarPanel.TabIndex = 1;
			//
			// InfoBarLabel
			//
			this.InfoBarLabel.AccessibleName = "InfoBarLabel";
			this.InfoBarLabel.Anchor = (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
				| System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right);
			this.InfoBarLabel.BackColor = System.Drawing.Color.Transparent;
			this.InfoBarLabel.Font = new System.Drawing.Font("Arial", 8.861538F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.InfoBarLabel.ForeColor = System.Drawing.SystemColors.ControlLightLight;
			this.InfoBarLabel.Name = "InfoBarLabel";
			this.InfoBarLabel.Size = new System.Drawing.Size(632, 22);
			this.InfoBarLabel.TabIndex = 1;
			this.InfoBarLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// InformationBar
			//
			this.AccessibleName = "InformationBar";
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.InfoBarPanel});
			this.DockPadding.All = 5;
			this.Name = "InformationBar";
			this.Size = new System.Drawing.Size(752, 32);
			this.InfoBarPanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		#region Overriden functions and event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override OnPaint().
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			m_borderDrawing.Draw(e.Graphics, ClientRectangle,
				SIL.FieldWorks.Common.Drawing.BorderTypes.Single);
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
		/// Clean up any resources being used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
				if (m_buttons != null)
					m_buttons.Clear();
			}
			m_buttons = null;

			base.Dispose( disposing );
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the collection that contains the buttons that belong to this info bar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Category("Appearance")]
		[Description("The buttons that will be displayed in this information bar.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public InformationBarButtonCollection Buttons
		{
			get
			{
				CheckDisposed();

				if (m_buttons == null)
				{
					m_buttons = new InformationBarButtonCollection();
					m_buttons.BeforeInsert +=
						new InformationBarButtonCollection.CollectionChange(OnButtonInserting);
				}
				return m_buttons;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The button is about to be inserted to the button control. Set's the button's properties.
		/// </summary>
		/// TODO: Delete most code and set button size and position.
		/// ------------------------------------------------------------------------------------
		private void OnButtonInserting(int index, object value)
		{
			InformationBarButton btn = value as InformationBarButton;
			if (btn != null)
			{
				btn.SuspendLayout();
				btn.Dock = System.Windows.Forms.DockStyle.Right;
				btn.Width = ButtonWidth;
				btn.Text = "";
				// Setting all the colors to 'ControlDark' so the button will behave like an icon.
				btn.BackColor = System.Drawing.SystemColors.ControlDark;
				btn.BorderDarkColor = System.Drawing.SystemColors.ControlDark;
				btn.BorderDarkestColor = System.Drawing.SystemColors.ControlDark;
				btn.BorderLightColor = System.Drawing.SystemColors.ControlDark;
				btn.BorderLightestColor = System.Drawing.SystemColors.ControlDark;
				btn.ResumeLayout();
				this.InfoBarPanel.Controls.Add(btn);
				btn.AccessibilityObject.Name = "InformationBarButton";
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the width that all the info bar buttons have.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Category("Appearance")]
		[DefaultValue(17)]
		[Description("The default button width.")]
		public int ButtonWidth
		{
			get
			{
				CheckDisposed();

				return m_buttonWidth;
			}

			set
			{
				CheckDisposed();

				foreach (Button button in Buttons)
					button.Width = value;
				m_buttonWidth = value;
			}
		}
	}
}
