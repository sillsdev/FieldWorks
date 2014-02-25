namespace SIL.FieldWorks.XWorks
{
	partial class DictionaryConfigurationTreeControl
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.tree = new System.Windows.Forms.TreeView();
			this.moveUp = new System.Windows.Forms.Button();
			this.moveDown = new System.Windows.Forms.Button();
			this.duplicate = new System.Windows.Forms.Button();
			this.remove = new System.Windows.Forms.Button();
			this.rename = new System.Windows.Forms.Button();
			this.tableLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// tableLayoutPanel
			// 
			this.tableLayoutPanel.ColumnCount = 2;
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 38F));
			this.tableLayoutPanel.Controls.Add(this.tree, 0, 0);
			this.tableLayoutPanel.Controls.Add(this.moveUp, 1, 0);
			this.tableLayoutPanel.Controls.Add(this.moveDown, 1, 1);
			this.tableLayoutPanel.Controls.Add(this.duplicate, 1, 2);
			this.tableLayoutPanel.Controls.Add(this.remove, 1, 3);
			this.tableLayoutPanel.Controls.Add(this.rename, 1, 4);
			this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel.Name = "tableLayoutPanel";
			this.tableLayoutPanel.RowCount = 5;
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel.Size = new System.Drawing.Size(276, 394);
			this.tableLayoutPanel.TabIndex = 0;
			// 
			// tree
			// 
			this.tree.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tree.HideSelection = false;
			this.tree.Location = new System.Drawing.Point(3, 3);
			this.tree.Name = "tree";
			this.tableLayoutPanel.SetRowSpan(this.tree, 5);
			this.tree.Size = new System.Drawing.Size(232, 388);
			this.tree.TabIndex = 0;
			// 
			// moveUp
			// 
			this.moveUp.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.moveUp.Location = new System.Drawing.Point(241, 3);
			this.moveUp.Name = "moveUp";
			this.moveUp.Size = new System.Drawing.Size(32, 32);
			this.moveUp.TabIndex = 1;
			this.moveUp.Text = "▲";
			this.moveUp.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.moveUp.UseVisualStyleBackColor = true;
			// 
			// moveDown
			// 
			this.moveDown.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.moveDown.Location = new System.Drawing.Point(241, 41);
			this.moveDown.Name = "moveDown";
			this.moveDown.Size = new System.Drawing.Size(32, 32);
			this.moveDown.TabIndex = 2;
			this.moveDown.Text = "▼";
			this.moveDown.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.moveDown.UseVisualStyleBackColor = true;
			// 
			// duplicate
			// 
			this.duplicate.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.duplicate.Location = new System.Drawing.Point(241, 79);
			this.duplicate.Name = "duplicate";
			this.duplicate.Size = new System.Drawing.Size(32, 32);
			this.duplicate.TabIndex = 3;
			this.duplicate.Text = "cp";
			this.duplicate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.duplicate.UseVisualStyleBackColor = true;
			// 
			// remove
			// 
			this.remove.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.remove.Location = new System.Drawing.Point(241, 117);
			this.remove.Name = "remove";
			this.remove.Size = new System.Drawing.Size(32, 32);
			this.remove.TabIndex = 4;
			this.remove.Text = "✘";
			this.remove.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.remove.UseVisualStyleBackColor = true;
			// 
			// rename
			// 
			this.rename.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.rename.Location = new System.Drawing.Point(241, 155);
			this.rename.Name = "rename";
			this.rename.Size = new System.Drawing.Size(32, 32);
			this.rename.TabIndex = 5;
			this.rename.Text = "✍";
			this.rename.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.rename.UseVisualStyleBackColor = true;
			// 
			// DictionaryConfigurationTreeControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.tableLayoutPanel);
			this.Name = "DictionaryConfigurationTreeControl";
			this.Size = new System.Drawing.Size(276, 394);
			this.tableLayoutPanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
		private System.Windows.Forms.TreeView tree;
		private System.Windows.Forms.Button moveDown;
		private System.Windows.Forms.Button remove;
		private System.Windows.Forms.Button duplicate;
		private System.Windows.Forms.Button moveUp;
		private System.Windows.Forms.Button rename;
	}
}
