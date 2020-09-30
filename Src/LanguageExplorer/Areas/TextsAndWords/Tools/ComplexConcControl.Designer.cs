// Copyright (c) 2013-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls;

namespace LanguageExplorer.Areas.TextsAndWords.Tools
{
	partial class ComplexConcControl
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
				components?.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			ComponentResourceManager resources = new ComponentResourceManager(typeof(ComplexConcControl));
			this.m_view = new PatternView();
			this.m_insertControl = new InsertionControl();
			this.m_searchButton = new Button();
			this.m_panel = new TableLayoutPanel();
			this.m_panel.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_view
			// 
			this.m_view.AcceptsReturn = true;
			this.m_view.AcceptsTab = false;
			resources.ApplyResources(this.m_view, "m_view");
			this.m_view.BackColor = SystemColors.Window;
			this.m_view.DoSpellCheck = false;
			this.m_view.InSelectionChanged = false;
			this.m_view.IsTextBox = false;
			this.m_view.Name = "m_view";
			this.m_view.ReadOnlyView = false;
			this.m_view.ScrollMinSize = new Size(0, 0);
			this.m_view.ScrollPosition = new Point(0, 0);
			this.m_view.ShowRangeSelAfterLostFocus = false;
			this.m_view.SizeChangedSuppression = false;
			this.m_view.WritingSystemFactory = null;
			this.m_view.WsPending = -1;
			this.m_view.Zoom = 1F;
			this.m_view.LayoutSizeChanged += new EventHandler(this.m_view_LayoutSizeChanged);
			// 
			// m_insertControl
			// 
			resources.ApplyResources(this.m_insertControl, "m_insertControl");
			this.m_insertControl.Name = "m_insertControl";
			this.m_insertControl.NoOptionsMessage = null;
			this.m_insertControl.Insert += new EventHandler<InsertEventArgs>(this.m_insertControl_Insert);
			// 
			// m_searchButton
			// 
			resources.ApplyResources(this.m_searchButton, "m_searchButton");
			this.m_searchButton.Name = "m_searchButton";
			this.m_searchButton.UseVisualStyleBackColor = true;
			this.m_searchButton.Click += new EventHandler(this.m_searchButton_Click);
			// 
			// m_panel
			// 
			resources.ApplyResources(this.m_panel, "m_panel");
			this.m_panel.Controls.Add(this.m_view, 0, 0);
			this.m_panel.Controls.Add(this.m_insertControl, 0, 1);
			this.m_panel.Controls.Add(this.m_searchButton, 1, 0);
			this.m_panel.Name = "m_panel";
			// 
			// ComplexConcControl
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = AutoScaleMode.Font;
			this.Controls.Add(this.m_panel);
			this.Name = "ComplexConcControl";
			this.m_panel.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private PatternView m_view;
		private InsertionControl m_insertControl;
		private Button m_searchButton;
		private TableLayoutPanel m_panel;
	}
}
