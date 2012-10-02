namespace SIL.FieldWorks.IText
{
	partial class StatisticsView
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.statisticsTable = new System.Windows.Forms.TableLayoutPanel();
			this.statisticsDescription = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// statisticsTable
			//
			this.statisticsTable.AutoSize = true;
			this.statisticsTable.Location = new System.Drawing.Point(3, 28);
			this.statisticsTable.Name = "statisticsTable";
			this.statisticsTable.Size = new System.Drawing.Size(50, 29);
			this.statisticsTable.TabIndex = 5;
			//
			// statisticsDescription
			//
			this.statisticsDescription.AutoSize = true;
			this.statisticsDescription.Location = new System.Drawing.Point(3, 0);
			this.statisticsDescription.Name = "statisticsDescription";
			this.statisticsDescription.Size = new System.Drawing.Size(357, 13);
			this.statisticsDescription.TabIndex = 6;
			this.statisticsDescription.Text = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX";
			//
			// StatisticsView
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.Controls.Add(this.statisticsDescription);
			this.Controls.Add(this.statisticsTable);
			this.Name = "StatisticsView";
			this.Size = new System.Drawing.Size(522, 478);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel statisticsTable;
		private System.Windows.Forms.Label statisticsDescription;
	}
}