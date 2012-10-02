namespace TECkit_Mapping_Editor
{
	partial class FindReplaceForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FindReplaceForm));
			this.labelFindWhat = new System.Windows.Forms.Label();
			this.comboBoxFindWhat = new System.Windows.Forms.ComboBox();
			this.labelReplaceWith = new System.Windows.Forms.Label();
			this.comboBoxReplaceWith = new System.Windows.Forms.ComboBox();
			this.groupBoxSearchOptions = new System.Windows.Forms.GroupBox();
			this.checkBoxSearchUp = new System.Windows.Forms.CheckBox();
			this.checkBoxWholeWords = new System.Windows.Forms.CheckBox();
			this.checkBoxMatchCase = new System.Windows.Forms.CheckBox();
			this.buttonFindNext = new System.Windows.Forms.Button();
			this.buttonReplace = new System.Windows.Forms.Button();
			this.buttonReplaceAll = new System.Windows.Forms.Button();
			this.toolStrip = new System.Windows.Forms.ToolStrip();
			this.toolStripDropDownButtonFindNext = new System.Windows.Forms.ToolStripDropDownButton();
			this.findNextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.groupBoxSearchOptions.SuspendLayout();
			this.toolStrip.SuspendLayout();
			this.SuspendLayout();
			//
			// labelFindWhat
			//
			this.labelFindWhat.AutoSize = true;
			this.labelFindWhat.Location = new System.Drawing.Point(12, 16);
			this.labelFindWhat.Name = "labelFindWhat";
			this.labelFindWhat.Size = new System.Drawing.Size(56, 13);
			this.labelFindWhat.TabIndex = 0;
			this.labelFindWhat.Text = "Fi&nd what:";
			//
			// comboBoxFindWhat
			//
			this.comboBoxFindWhat.FormattingEnabled = true;
			this.comboBoxFindWhat.Location = new System.Drawing.Point(93, 13);
			this.comboBoxFindWhat.Name = "comboBoxFindWhat";
			this.comboBoxFindWhat.Size = new System.Drawing.Size(390, 21);
			this.comboBoxFindWhat.TabIndex = 1;
			this.comboBoxFindWhat.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(FindReplaceForm_PreviewKeyDown);
			//
			// labelReplaceWith
			//
			this.labelReplaceWith.AutoSize = true;
			this.labelReplaceWith.Location = new System.Drawing.Point(12, 44);
			this.labelReplaceWith.Name = "labelReplaceWith";
			this.labelReplaceWith.Size = new System.Drawing.Size(72, 13);
			this.labelReplaceWith.TabIndex = 2;
			this.labelReplaceWith.Text = "Replace w&ith:";
			//
			// comboBoxReplaceWith
			//
			this.comboBoxReplaceWith.FormattingEnabled = true;
			this.comboBoxReplaceWith.Location = new System.Drawing.Point(93, 40);
			this.comboBoxReplaceWith.Name = "comboBoxReplaceWith";
			this.comboBoxReplaceWith.Size = new System.Drawing.Size(390, 21);
			this.comboBoxReplaceWith.TabIndex = 3;
			this.comboBoxReplaceWith.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(FindReplaceForm_PreviewKeyDown);
			//
			// groupBoxSearchOptions
			//
			this.groupBoxSearchOptions.Controls.Add(this.checkBoxSearchUp);
			this.groupBoxSearchOptions.Controls.Add(this.checkBoxWholeWords);
			this.groupBoxSearchOptions.Controls.Add(this.checkBoxMatchCase);
			this.groupBoxSearchOptions.Location = new System.Drawing.Point(336, 67);
			this.groupBoxSearchOptions.Name = "groupBoxSearchOptions";
			this.groupBoxSearchOptions.Size = new System.Drawing.Size(147, 98);
			this.groupBoxSearchOptions.TabIndex = 7;
			this.groupBoxSearchOptions.TabStop = false;
			this.groupBoxSearchOptions.Text = "Search Options";
			//
			// checkBoxSearchUp
			//
			this.checkBoxSearchUp.AutoSize = true;
			this.checkBoxSearchUp.Location = new System.Drawing.Point(7, 68);
			this.checkBoxSearchUp.Name = "checkBoxSearchUp";
			this.checkBoxSearchUp.Size = new System.Drawing.Size(75, 17);
			this.checkBoxSearchUp.TabIndex = 2;
			this.checkBoxSearchUp.Text = "Search &up";
			this.checkBoxSearchUp.UseVisualStyleBackColor = true;
			//
			// checkBoxWholeWords
			//
			this.checkBoxWholeWords.AutoSize = true;
			this.checkBoxWholeWords.Location = new System.Drawing.Point(7, 44);
			this.checkBoxWholeWords.Name = "checkBoxWholeWords";
			this.checkBoxWholeWords.Size = new System.Drawing.Size(130, 17);
			this.checkBoxWholeWords.TabIndex = 1;
			this.checkBoxWholeWords.Text = "Find whole words onl&y";
			this.checkBoxWholeWords.UseVisualStyleBackColor = true;
			//
			// checkBoxMatchCase
			//
			this.checkBoxMatchCase.AutoSize = true;
			this.checkBoxMatchCase.Location = new System.Drawing.Point(7, 20);
			this.checkBoxMatchCase.Name = "checkBoxMatchCase";
			this.checkBoxMatchCase.Size = new System.Drawing.Size(82, 17);
			this.checkBoxMatchCase.TabIndex = 0;
			this.checkBoxMatchCase.Text = "Matc&h case";
			this.checkBoxMatchCase.UseVisualStyleBackColor = true;
			//
			// buttonFindNext
			//
			this.buttonFindNext.Location = new System.Drawing.Point(93, 67);
			this.buttonFindNext.Name = "buttonFindNext";
			this.buttonFindNext.Size = new System.Drawing.Size(75, 23);
			this.buttonFindNext.TabIndex = 4;
			this.buttonFindNext.Text = "&Find Next";
			this.buttonFindNext.UseVisualStyleBackColor = true;
			this.buttonFindNext.Click += new System.EventHandler(this.buttonFindNext_Click);
			//
			// buttonReplace
			//
			this.buttonReplace.Location = new System.Drawing.Point(174, 67);
			this.buttonReplace.Name = "buttonReplace";
			this.buttonReplace.Size = new System.Drawing.Size(75, 23);
			this.buttonReplace.TabIndex = 5;
			this.buttonReplace.Text = "&Replace";
			this.buttonReplace.UseVisualStyleBackColor = true;
			this.buttonReplace.Click += new System.EventHandler(this.buttonReplace_Click);
			//
			// buttonReplaceAll
			//
			this.buttonReplaceAll.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.buttonReplaceAll.Location = new System.Drawing.Point(255, 67);
			this.buttonReplaceAll.Name = "buttonReplaceAll";
			this.buttonReplaceAll.Size = new System.Drawing.Size(75, 23);
			this.buttonReplaceAll.TabIndex = 6;
			this.buttonReplaceAll.Text = "Replace &All";
			this.buttonReplaceAll.UseVisualStyleBackColor = true;
			this.buttonReplaceAll.Click += new System.EventHandler(this.buttonReplaceAll_Click);
			//
			// toolStrip
			//
			this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.toolStripDropDownButtonFindNext});
			this.toolStrip.Location = new System.Drawing.Point(0, 0);
			this.toolStrip.Name = "toolStrip";
			this.toolStrip.Size = new System.Drawing.Size(495, 25);
			this.toolStrip.TabIndex = 8;
			this.toolStrip.Text = "toolStrip1";
			this.toolStrip.Visible = false;
			//
			// toolStripDropDownButtonFindNext
			//
			this.toolStripDropDownButtonFindNext.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripDropDownButtonFindNext.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.findNextToolStripMenuItem});
			this.toolStripDropDownButtonFindNext.Image = ((System.Drawing.Image)(resources.GetObject("toolStripDropDownButtonFindNext.Image")));
			this.toolStripDropDownButtonFindNext.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripDropDownButtonFindNext.Name = "toolStripDropDownButtonFindNext";
			this.toolStripDropDownButtonFindNext.Size = new System.Drawing.Size(29, 22);
			this.toolStripDropDownButtonFindNext.Text = "toolStripDropDownButton1";
			//
			// findNextToolStripMenuItem
			//
			this.findNextToolStripMenuItem.Name = "findNextToolStripMenuItem";
			this.findNextToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F3;
			this.findNextToolStripMenuItem.Size = new System.Drawing.Size(139, 22);
			this.findNextToolStripMenuItem.Text = "Find &Next";
			this.findNextToolStripMenuItem.Click += new System.EventHandler(this.findNextToolStripMenuItem_Click);
			//
			// buttonCancel
			//
			this.buttonCancel.Location = new System.Drawing.Point(15, 141);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 9;
			this.buttonCancel.Text = "&Close";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Visible = false;
			this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
			//
			// FindReplaceForm
			//
			this.AcceptButton = this.buttonFindNext;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(495, 177);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonReplaceAll);
			this.Controls.Add(this.buttonReplace);
			this.Controls.Add(this.buttonFindNext);
			this.Controls.Add(this.groupBoxSearchOptions);
			this.Controls.Add(this.comboBoxReplaceWith);
			this.Controls.Add(this.labelReplaceWith);
			this.Controls.Add(this.comboBoxFindWhat);
			this.Controls.Add(this.labelFindWhat);
			this.Controls.Add(this.toolStrip);
			this.Name = "FindReplaceForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = "Find/Replace";
			this.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.FindReplaceForm_PreviewKeyDown);
			this.Activated += new System.EventHandler(this.FindReplaceForm_Activated);
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FindReplaceForm_FormClosing);
			this.groupBoxSearchOptions.ResumeLayout(false);
			this.groupBoxSearchOptions.PerformLayout();
			this.toolStrip.ResumeLayout(false);
			this.toolStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label labelFindWhat;
		private System.Windows.Forms.ComboBox comboBoxFindWhat;
		private System.Windows.Forms.Label labelReplaceWith;
		private System.Windows.Forms.ComboBox comboBoxReplaceWith;
		private System.Windows.Forms.GroupBox groupBoxSearchOptions;
		private System.Windows.Forms.CheckBox checkBoxMatchCase;
		private System.Windows.Forms.CheckBox checkBoxSearchUp;
		private System.Windows.Forms.CheckBox checkBoxWholeWords;
		private System.Windows.Forms.Button buttonFindNext;
		private System.Windows.Forms.Button buttonReplace;
		private System.Windows.Forms.Button buttonReplaceAll;
		private System.Windows.Forms.ToolStrip toolStrip;
		private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButtonFindNext;
		private System.Windows.Forms.ToolStripMenuItem findNextToolStripMenuItem;
		private System.Windows.Forms.Button buttonCancel;
	}
}