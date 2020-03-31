// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LCMBrowser
{
	partial class InspectorWnd
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		// TODO-Linux: VirtualMode is not supported on Mono
		private void InitializeComponent()
		{
			this.InspectorGrid = new InspectorGrid();
			((System.ComponentModel.ISupportInitialize)(this.InspectorGrid)).BeginInit();
			this.SuspendLayout();
			//
			// gridInspector
			//
			this.InspectorGrid.AllowUserToAddRows = false;
			this.InspectorGrid.AllowUserToDeleteRows = false;
			this.InspectorGrid.AllowUserToResizeRows = false;
			this.InspectorGrid.BackgroundColor = System.Drawing.SystemColors.Window;
			this.InspectorGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.InspectorGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.InspectorGrid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.InspectorGrid.Font = new System.Drawing.Font("Segoe UI", 8.25F);
			this.InspectorGrid.GridColor = System.Drawing.SystemColors.Window;
			this.InspectorGrid.Location = new System.Drawing.Point(1, 0);
			this.InspectorGrid.MultiSelect = false;
			this.InspectorGrid.Name = "InspectorGrid";
			this.InspectorGrid.ReadOnly = false;
			this.InspectorGrid.RowHeadersVisible = false;
			this.InspectorGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.InspectorGrid.Size = new System.Drawing.Size(734, 538);
			this.InspectorGrid.TabIndex = 0;
			this.InspectorGrid.VirtualMode = true;
			this.InspectorGrid.CellMouseDown += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.gridInspector_CellMouseDown);
			//
			// InspectorWnd
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Window;
			this.ClientSize = new System.Drawing.Size(736, 539);
			this.Controls.Add(this.InspectorGrid);
			this.DockAreas = ((WeifenLuo.WinFormsUI.Docking.DockAreas)((WeifenLuo.WinFormsUI.Docking.DockAreas.Float | WeifenLuo.WinFormsUI.Docking.DockAreas.Document)));
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "InspectorWnd";
			this.Padding = new System.Windows.Forms.Padding(1, 0, 1, 1);
			this.Text = "InspectorDlg";
			((System.ComponentModel.ISupportInitialize)(this.InspectorGrid)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion
	}
}