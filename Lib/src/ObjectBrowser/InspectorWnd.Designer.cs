using System.Diagnostics.CodeAnalysis;

namespace SIL.ObjectBrowser
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
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		private void InitializeComponent()
		{
			this.gridInspector = new InspectorGrid();
			((System.ComponentModel.ISupportInitialize)(this.gridInspector)).BeginInit();
			this.SuspendLayout();
			//
			// gridInspector
			//
			this.gridInspector.AllowUserToAddRows = false;
			this.gridInspector.AllowUserToDeleteRows = false;
			this.gridInspector.AllowUserToResizeRows = false;
			this.gridInspector.BackgroundColor = System.Drawing.SystemColors.Window;
			this.gridInspector.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.gridInspector.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.gridInspector.Dock = System.Windows.Forms.DockStyle.Fill;
			this.gridInspector.Font = new System.Drawing.Font("Segoe UI", 8.25F);
			this.gridInspector.GridColor = System.Drawing.SystemColors.Window;
			this.gridInspector.Location = new System.Drawing.Point(1, 0);
			this.gridInspector.MultiSelect = false;
			this.gridInspector.Name = "gridInspector";
			this.gridInspector.ReadOnly = false;
			this.gridInspector.RowHeadersVisible = false;
			this.gridInspector.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.gridInspector.Size = new System.Drawing.Size(734, 538);
			this.gridInspector.TabIndex = 0;
			this.gridInspector.VirtualMode = true;
			this.gridInspector.CellMouseDown += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.gridInspector_CellMouseDown);
			//
			// InspectorWnd
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Window;
			this.ClientSize = new System.Drawing.Size(736, 539);
			this.Controls.Add(this.gridInspector);
			this.DockAreas = ((WeifenLuo.WinFormsUI.Docking.DockAreas)((WeifenLuo.WinFormsUI.Docking.DockAreas.Float | WeifenLuo.WinFormsUI.Docking.DockAreas.Document)));
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "InspectorWnd";
			this.Padding = new System.Windows.Forms.Padding(1, 0, 1, 1);
			this.Text = "InspectorDlg";
			((System.ComponentModel.ISupportInitialize)(this.gridInspector)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private InspectorGrid gridInspector;

	}
}