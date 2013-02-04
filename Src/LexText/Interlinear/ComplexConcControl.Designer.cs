using SIL.FieldWorks.LexText.Controls;

namespace SIL.FieldWorks.IText
{
	partial class ComplexConcControl
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ComplexConcControl));
			this.m_view = new SIL.FieldWorks.IText.ComplexConcPatternView();
			this.m_insertControl = new SIL.FieldWorks.LexText.Controls.InsertionControl();
			this.m_searchButton = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// m_view
			// 
			this.m_view.AcceptsReturn = true;
			this.m_view.AcceptsTab = false;
			resources.ApplyResources(this.m_view, "m_view");
			this.m_view.BackColor = System.Drawing.SystemColors.Window;
			this.m_view.DoSpellCheck = false;
			this.m_view.Group = null;
			this.m_view.InSelectionChanged = false;
			this.m_view.IsTextBox = false;
			this.m_view.Mediator = null;
			this.m_view.Name = "m_view";
			this.m_view.ReadOnlyView = false;
			this.m_view.ScrollMinSize = new System.Drawing.Size(0, 0);
			this.m_view.ScrollPosition = new System.Drawing.Point(0, 0);
			this.m_view.ShowRangeSelAfterLostFocus = false;
			this.m_view.SizeChangedSuppression = false;
			this.m_view.WritingSystemFactory = null;
			this.m_view.WsPending = -1;
			this.m_view.Zoom = 1F;
			this.m_view.LayoutSizeChanged += new System.EventHandler(this.m_view_LayoutSizeChanged);
			// 
			// m_insertControl
			// 
			resources.ApplyResources(this.m_insertControl, "m_insertControl");
			this.m_insertControl.Name = "m_insertControl";
			this.m_insertControl.NoOptionsMessage = null;
			this.m_insertControl.Insert += new System.EventHandler<SIL.FieldWorks.LexText.Controls.InsertEventArgs>(this.m_insertControl_Insert);
			// 
			// m_searchButton
			// 
			resources.ApplyResources(this.m_searchButton, "m_searchButton");
			this.m_searchButton.Name = "m_searchButton";
			this.m_searchButton.UseVisualStyleBackColor = true;
			this.m_searchButton.Click += new System.EventHandler(this.m_searchButton_Click);
			// 
			// ComplexConcordanceControl
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_searchButton);
			this.Controls.Add(this.m_insertControl);
			this.Controls.Add(this.m_view);
			this.Name = "ComplexConcControl";
			this.ResumeLayout(false);

		}

		#endregion

		private ComplexConcPatternView m_view;
		private InsertionControl m_insertControl;
		private System.Windows.Forms.Button m_searchButton;
	}
}
