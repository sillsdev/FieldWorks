// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: KeyTermRenderingsControl.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using SIL.FieldWorks.TE.TeEditorialChecks;
namespace SIL.FieldWorks.TE
{
	partial class KeyTermRenderingsControl
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(KeyTermRenderingsControl));
			this.m_dataGridView = new SIL.FieldWorks.TE.KeyTermsGrid();
			this.m_Reference = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.m_Rendering = new SIL.FieldWorks.Common.Widgets.FwTextBoxColumn();
			this.m_Status = new SIL.FieldWorks.TE.TeEditorialChecks.CheckGridStatusColumn();
			this.m_OriginalTerm = new SIL.FieldWorks.Common.Widgets.FwTextBoxColumn();
			((System.ComponentModel.ISupportInitialize)(this.m_dataGridView)).BeginInit();
			this.SuspendLayout();
			//
			// m_dataGridView
			//
			this.m_dataGridView.AllowUserToAddRows = false;
			this.m_dataGridView.AllowUserToDeleteRows = false;
			this.m_dataGridView.AllowUserToOrderColumns = true;
			this.m_dataGridView.AllowUserToResizeRows = false;
			this.m_dataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_dataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
			this.m_Reference,
			this.m_Rendering,
			this.m_Status,
			this.m_OriginalTerm});
			dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Lucida Sans", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.World);
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.m_dataGridView.DefaultCellStyle = dataGridViewCellStyle1;
			resources.ApplyResources(this.m_dataGridView, "m_dataGridView");
			this.m_dataGridView.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
			this.m_dataGridView.MultiSelect = false;
			this.m_dataGridView.Name = "m_dataGridView";
			this.m_dataGridView.ReadOnly = true;
			this.m_dataGridView.RowHeadersVisible = false;
			this.m_dataGridView.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
			this.m_dataGridView.RowHeight = 20;
			this.m_dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_dataGridView.SettingsKey = null;
			this.m_dataGridView.ShowCellErrors = false;
			this.m_dataGridView.ShowCellToolTips = false;
			this.m_dataGridView.ShowEditingIcon = false;
			this.m_dataGridView.ShowRowErrors = false;
			this.m_dataGridView.VirtualMode = true;
			//
			// m_Reference
			//
			this.m_Reference.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.m_Reference.DataPropertyName = "Reference";
			resources.ApplyResources(this.m_Reference, "m_Reference");
			this.m_Reference.Name = "m_Reference";
			this.m_Reference.ReadOnly = true;
			this.m_Reference.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
			//
			// m_Rendering
			//
			this.m_Rendering.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.m_Rendering.DataPropertyName = "Rendering";
			this.m_Rendering.FillWeight = 500F;
			resources.ApplyResources(this.m_Rendering, "m_Rendering");
			this.m_Rendering.Name = "m_Rendering";
			this.m_Rendering.ReadOnly = true;
			this.m_Rendering.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
			//
			// m_Status
			//
			this.m_Status.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.m_Status.DataPropertyName = "RenderingStatus";
			this.m_Status.FillWeight = 50F;
			resources.ApplyResources(this.m_Status, "m_Status");
			this.m_Status.Name = "m_Status";
			this.m_Status.ReadOnly = true;
			this.m_Status.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this.m_Status.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
			//
			// m_OriginalTerm
			//
			this.m_OriginalTerm.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.m_OriginalTerm.DataPropertyName = "KeyWordString";
			this.m_OriginalTerm.FillWeight = 500F;
			resources.ApplyResources(this.m_OriginalTerm, "m_OriginalTerm");
			this.m_OriginalTerm.Name = "m_OriginalTerm";
			this.m_OriginalTerm.ReadOnly = true;
			this.m_OriginalTerm.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
			//
			// KeyTermRenderingsControl
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.m_dataGridView);
			this.Name = "KeyTermRenderingsControl";
			((System.ComponentModel.ISupportInitialize)(this.m_dataGridView)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		/// <summary></summary>
		protected KeyTermsGrid m_dataGridView;
		private System.Windows.Forms.DataGridViewTextBoxColumn m_Reference;
		private SIL.FieldWorks.Common.Widgets.FwTextBoxColumn m_Rendering;
		private CheckGridStatusColumn m_Status;
		private SIL.FieldWorks.Common.Widgets.FwTextBoxColumn m_OriginalTerm;
	}
}
