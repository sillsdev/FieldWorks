// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace FDOBrowser
{
	partial class ClassPropertySelector
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
			this.gridProperties = new System.Windows.Forms.DataGridView();
			this.colChecked = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this.colProperty = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.cboClass = new System.Windows.Forms.ComboBox();
			this.lblClass = new System.Windows.Forms.Label();
			this.lblMsg = new System.Windows.Forms.Label();
			this.btnOK = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.colUnderlyingData = new System.Windows.Forms.DataGridViewTextBoxColumn();
			((System.ComponentModel.ISupportInitialize)(this.gridProperties)).BeginInit();
			this.SuspendLayout();
			//
			// gridProperties
			//
			this.gridProperties.AllowUserToAddRows = false;
			this.gridProperties.AllowUserToDeleteRows = false;
			this.gridProperties.AllowUserToResizeColumns = false;
			this.gridProperties.AllowUserToResizeRows = false;
			this.gridProperties.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.gridProperties.BackgroundColor = System.Drawing.SystemColors.Window;
			this.gridProperties.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.gridProperties.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
			dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.gridProperties.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
			this.gridProperties.ColumnHeadersHeight = 24;
			this.gridProperties.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			this.gridProperties.ColumnHeadersVisible = false;
			this.gridProperties.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
			this.colChecked,
			this.colProperty,
			this.colUnderlyingData});
			dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.gridProperties.DefaultCellStyle = dataGridViewCellStyle2;
			this.gridProperties.Location = new System.Drawing.Point(15, 84);
			this.gridProperties.Name = "gridProperties";
			dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.gridProperties.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
			this.gridProperties.RowHeadersVisible = false;
			this.gridProperties.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.gridProperties.Size = new System.Drawing.Size(410, 359);
			this.gridProperties.TabIndex = 3;
			this.gridProperties.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.gridProperties_CellFormatting);
			this.gridProperties.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.gridProperties_CellPainting);
			this.gridProperties.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.gridProperties_KeyPress);
			//
			// colChecked
			//
			this.colChecked.Frozen = true;
			this.colChecked.HeaderText = "";
			this.colChecked.Name = "colChecked";
			this.colChecked.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this.colChecked.Width = 40;
			//
			// colProperty
			//
			this.colProperty.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.colProperty.HeaderText = "Property";
			this.colProperty.Name = "colProperty";
			this.colProperty.ReadOnly = true;
			this.colProperty.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this.colProperty.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			//
			// cboClass
			//
			this.cboClass.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.cboClass.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cboClass.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.cboClass.FormattingEnabled = true;
			this.cboClass.Location = new System.Drawing.Point(53, 16);
			this.cboClass.MaxDropDownItems = 20;
			this.cboClass.Name = "cboClass";
			this.cboClass.Size = new System.Drawing.Size(372, 23);
			this.cboClass.Sorted = true;
			this.cboClass.TabIndex = 1;
			this.cboClass.SelectionChangeCommitted += new System.EventHandler(this.cboClass_SelectionChangeCommitted);
			//
			// lblClass
			//
			this.lblClass.AutoSize = true;
			this.lblClass.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblClass.Location = new System.Drawing.Point(12, 19);
			this.lblClass.Name = "lblClass";
			this.lblClass.Size = new System.Drawing.Size(37, 15);
			this.lblClass.TabIndex = 0;
			this.lblClass.Text = "&Class:";
			//
			// lblMsg
			//
			this.lblMsg.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.lblMsg.AutoEllipsis = true;
			this.lblMsg.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblMsg.Location = new System.Drawing.Point(15, 57);
			this.lblMsg.Name = "lblMsg";
			this.lblMsg.Size = new System.Drawing.Size(410, 24);
			this.lblMsg.TabIndex = 2;
			this.lblMsg.Text = "Select the properties to display in the browser for {0} objects.";
			this.lblMsg.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// btnOK
			//
			this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOK.Location = new System.Drawing.Point(269, 449);
			this.btnOK.Name = "btnOK";
			this.btnOK.Size = new System.Drawing.Size(75, 26);
			this.btnOK.TabIndex = 4;
			this.btnOK.Text = "OK";
			this.btnOK.UseVisualStyleBackColor = true;
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			//
			// btnCancel
			//
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(350, 449);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(75, 26);
			this.btnCancel.TabIndex = 5;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			//
			// colUnderlyingData
			//
			this.colUnderlyingData.HeaderText = "#";
			this.colUnderlyingData.Name = "colUnderlyingData";
			this.colUnderlyingData.ReadOnly = true;
			this.colUnderlyingData.Visible = false;
			//
			// ClassPropertySelector
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(437, 484);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.lblMsg);
			this.Controls.Add(this.lblClass);
			this.Controls.Add(this.cboClass);
			this.Controls.Add(this.gridProperties);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(445, 290);
			this.Name = "ClassPropertySelector";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Class Property Selector";
			((System.ComponentModel.ISupportInitialize)(this.gridProperties)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.DataGridView gridProperties;
		private System.Windows.Forms.ComboBox cboClass;
		private System.Windows.Forms.Label lblClass;
		private System.Windows.Forms.Label lblMsg;
		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.DataGridViewCheckBoxColumn colChecked;
		private System.Windows.Forms.DataGridViewTextBoxColumn colProperty;
		private System.Windows.Forms.DataGridViewTextBoxColumn colUnderlyingData;
	}
}