// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: EditorialChecksRenderingsControl.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System.Diagnostics.CodeAnalysis;
namespace SIL.FieldWorks.TE.TeEditorialChecks
{
	partial class EditorialChecksRenderingsControl
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
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="TODO-Linux: VirtualMode is not supported in Mono")]
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditorialChecksRenderingsControl));
			this.m_dataGridView = new SIL.FieldWorks.TE.TeEditorialChecks.EditorialChecksGrid();
			this.m_Reference = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.m_TypeOfCheck = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.m_Message = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.m_Details = new SIL.FieldWorks.Common.Widgets.FwTextBoxColumn();
			this.m_Status = new SIL.FieldWorks.TE.TeEditorialChecks.CheckGridStatusColumn();
			this.m_toolTip = new System.Windows.Forms.ToolTip(this.components);
			((System.ComponentModel.ISupportInitialize)(this.m_dataGridView)).BeginInit();
			this.SuspendLayout();
			//
			// m_dataGridView
			//
			this.m_dataGridView.AllowUserToAddRows = false;
			this.m_dataGridView.AllowUserToDeleteRows = false;
			this.m_dataGridView.AllowUserToOrderColumns = true;
			this.m_dataGridView.AllowUserToResizeRows = false;
			dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.m_dataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
			this.m_dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
			this.m_Reference,
			this.m_TypeOfCheck,
			this.m_Message,
			this.m_Details,
			this.m_Status});
			dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle2.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.World);
			dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.m_dataGridView.DefaultCellStyle = dataGridViewCellStyle2;
			resources.ApplyResources(this.m_dataGridView, "m_dataGridView");
			this.m_dataGridView.IsStale = true;
			this.m_dataGridView.MultiSelect = false;
			this.m_dataGridView.Name = "m_dataGridView";
			this.m_dataGridView.RowHeadersVisible = false;
			this.m_dataGridView.RowHeight = 20;
			this.m_dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_dataGridView.SettingsKey = null;
			this.m_dataGridView.ShowCellToolTips = false;
			this.m_dataGridView.VirtualMode = true;
			this.m_dataGridView.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.m_dataGridView_ColumnHeaderMouseClick);
			this.m_dataGridView.CellClick += m_dataGridView_CellClick;
			this.m_dataGridView.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.m_dataGridView_RowsAdded);
			this.m_dataGridView.RowsRemoved += new System.Windows.Forms.DataGridViewRowsRemovedEventHandler(this.m_dataGridView_RowsRemoved);
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
			// m_TypeOfCheck
			//
			this.m_TypeOfCheck.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.m_TypeOfCheck.DataPropertyName = "TypeOfCheck";
			this.m_TypeOfCheck.FillWeight = 200F;
			resources.ApplyResources(this.m_TypeOfCheck, "m_TypeOfCheck");
			this.m_TypeOfCheck.Name = "m_TypeOfCheck";
			this.m_TypeOfCheck.ReadOnly = true;
			this.m_TypeOfCheck.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
			//
			// m_Message
			//
			this.m_Message.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.m_Message.DataPropertyName = "Message";
			this.m_Message.FillWeight = 500F;
			resources.ApplyResources(this.m_Message, "m_Message");
			this.m_Message.Name = "m_Message";
			this.m_Message.ReadOnly = true;
			this.m_Message.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
			//
			// m_Details
			//
			this.m_Details.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.m_Details.DataPropertyName = "Details";
			this.m_Details.FillWeight = 300F;
			resources.ApplyResources(this.m_Details, "m_Details");
			this.m_Details.Name = "m_Details";
			this.m_Details.ReadOnly = true;
			this.m_Details.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
			//
			// m_Status
			//
			this.m_Status.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.m_Status.DataPropertyName = "Status";
			resources.ApplyResources(this.m_Status, "m_Status");
			this.m_Status.Name = "m_Status";
			this.m_Status.ReadOnly = true;
			this.m_Status.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this.m_Status.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
			//
			// m_toolTip
			//
			this.m_toolTip.AutomaticDelay = 0;
			this.m_toolTip.OwnerDraw = true;
			this.m_toolTip.ShowAlways = true;
			//
			// EditorialChecksRenderingsControl
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_dataGridView);
			this.Name = "EditorialChecksRenderingsControl";
			((System.ComponentModel.ISupportInitialize)(this.m_dataGridView)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private EditorialChecksGrid m_dataGridView;
		private System.Windows.Forms.ToolTip m_toolTip;
		private System.Windows.Forms.DataGridViewTextBoxColumn m_Reference;
		private System.Windows.Forms.DataGridViewTextBoxColumn m_TypeOfCheck;
		private System.Windows.Forms.DataGridViewTextBoxColumn m_Message;
		private SIL.FieldWorks.Common.Widgets.FwTextBoxColumn m_Details;
		private CheckGridStatusColumn m_Status;
	}
}
