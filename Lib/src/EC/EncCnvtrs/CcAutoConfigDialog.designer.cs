namespace SilEncConverters40
{
	partial class CcAutoConfigDialog
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CcAutoConfigDialog));
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.labelCCTable = new System.Windows.Forms.Label();
			this.textBoxFileSpec = new System.Windows.Forms.TextBox();
			this.buttonBrowse = new System.Windows.Forms.Button();
			this.groupBoxExpects = new System.Windows.Forms.GroupBox();
			this.radioButtonExpectsLegacy = new System.Windows.Forms.RadioButton();
			this.radioButtonExpectsUnicode = new System.Windows.Forms.RadioButton();
			this.groupBoxReturns = new System.Windows.Forms.GroupBox();
			this.radioButtonReturnsLegacy = new System.Windows.Forms.RadioButton();
			this.radioButtonReturnsUnicode = new System.Windows.Forms.RadioButton();
			this.buttonAddSpellFixer = new System.Windows.Forms.Button();
			this.labelSpellFixerInstructions = new System.Windows.Forms.Label();
			this.openFileDialogBrowse = new System.Windows.Forms.OpenFileDialog();
			this.tabControl.SuspendLayout();
			this.tabPageSetup.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			this.groupBoxExpects.SuspendLayout();
			this.groupBoxReturns.SuspendLayout();
			this.SuspendLayout();
			//
			// tabPageSetup
			//
			this.tabPageSetup.Controls.Add(this.tableLayoutPanel1);
			//
			// buttonApply
			//
			this.helpProvider.SetHelpString(this.buttonApply, "Click this button to apply the configured values for this converter");
			this.helpProvider.SetShowHelp(this.buttonApply, true);
			//
			// buttonCancel
			//
			this.helpProvider.SetHelpString(this.buttonCancel, "Click this button to cancel this dialog");
			this.helpProvider.SetShowHelp(this.buttonCancel, true);
			//
			// buttonOK
			//
			this.helpProvider.SetHelpString(this.buttonOK, "Click this button to accept the configured values for this converter");
			this.helpProvider.SetShowHelp(this.buttonOK, true);
			//
			// buttonSaveInRepository
			//
			this.helpProvider.SetHelpString(this.buttonSaveInRepository, "\r\nClick to add this converter to the system repository permanently.\r\n    ");
			this.helpProvider.SetShowHelp(this.buttonSaveInRepository, true);
			//
			// tableLayoutPanel1
			//
			this.tableLayoutPanel1.ColumnCount = 3;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.Controls.Add(this.labelCCTable, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.textBoxFileSpec, 1, 0);
			this.tableLayoutPanel1.Controls.Add(this.buttonBrowse, 2, 0);
			this.tableLayoutPanel1.Controls.Add(this.groupBoxExpects, 1, 2);
			this.tableLayoutPanel1.Controls.Add(this.groupBoxReturns, 1, 3);
			this.tableLayoutPanel1.Controls.Add(this.buttonAddSpellFixer, 1, 5);
			this.tableLayoutPanel1.Controls.Add(this.labelSpellFixerInstructions, 1, 4);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 3);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 6;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(596, 394);
			this.tableLayoutPanel1.TabIndex = 2;
			//
			// labelCCTable
			//
			this.labelCCTable.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.labelCCTable.AutoSize = true;
			this.labelCCTable.Location = new System.Drawing.Point(3, 8);
			this.labelCCTable.Name = "labelCCTable";
			this.labelCCTable.Size = new System.Drawing.Size(54, 13);
			this.labelCCTable.TabIndex = 0;
			this.labelCCTable.Text = "CC Table:";
			//
			// textBoxFileSpec
			//
			this.textBoxFileSpec.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textBoxFileSpec.Location = new System.Drawing.Point(63, 3);
			this.textBoxFileSpec.Name = "textBoxFileSpec";
			this.textBoxFileSpec.Size = new System.Drawing.Size(500, 20);
			this.textBoxFileSpec.TabIndex = 1;
			this.textBoxFileSpec.TextChanged += new System.EventHandler(this.textBoxFileSpec_TextChanged);
			//
			// buttonBrowse
			//
			this.buttonBrowse.Dock = System.Windows.Forms.DockStyle.Fill;
			this.buttonBrowse.Location = new System.Drawing.Point(569, 3);
			this.buttonBrowse.Name = "buttonBrowse";
			this.buttonBrowse.Size = new System.Drawing.Size(24, 23);
			this.buttonBrowse.TabIndex = 2;
			this.buttonBrowse.Text = "...";
			this.buttonBrowse.UseVisualStyleBackColor = true;
			this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
			//
			// groupBoxExpects
			//
			this.groupBoxExpects.Controls.Add(this.radioButtonExpectsLegacy);
			this.groupBoxExpects.Controls.Add(this.radioButtonExpectsUnicode);
			this.groupBoxExpects.Dock = System.Windows.Forms.DockStyle.Fill;
			this.groupBoxExpects.Location = new System.Drawing.Point(63, 82);
			this.groupBoxExpects.Name = "groupBoxExpects";
			this.groupBoxExpects.Size = new System.Drawing.Size(500, 77);
			this.groupBoxExpects.TabIndex = 3;
			this.groupBoxExpects.TabStop = false;
			this.groupBoxExpects.Text = "CC table expects";
			this.groupBoxExpects.Visible = false;
			//
			// radioButtonExpectsLegacy
			//
			this.radioButtonExpectsLegacy.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.radioButtonExpectsLegacy.AutoSize = true;
			this.radioButtonExpectsLegacy.Location = new System.Drawing.Point(263, 30);
			this.radioButtonExpectsLegacy.Name = "radioButtonExpectsLegacy";
			this.radioButtonExpectsLegacy.Size = new System.Drawing.Size(152, 17);
			this.radioButtonExpectsLegacy.TabIndex = 0;
			this.radioButtonExpectsLegacy.TabStop = true;
			this.radioButtonExpectsLegacy.Text = "Non-Unicode String (bytes)";
			this.radioButtonExpectsLegacy.UseVisualStyleBackColor = true;
			this.radioButtonExpectsLegacy.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
			//
			// radioButtonExpectsUnicode
			//
			this.radioButtonExpectsUnicode.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.radioButtonExpectsUnicode.AutoSize = true;
			this.radioButtonExpectsUnicode.Location = new System.Drawing.Point(85, 30);
			this.radioButtonExpectsUnicode.Name = "radioButtonExpectsUnicode";
			this.radioButtonExpectsUnicode.Size = new System.Drawing.Size(134, 17);
			this.radioButtonExpectsUnicode.TabIndex = 0;
			this.radioButtonExpectsUnicode.TabStop = true;
			this.radioButtonExpectsUnicode.Text = "Unicode String (UTF-8)";
			this.radioButtonExpectsUnicode.UseVisualStyleBackColor = true;
			this.radioButtonExpectsUnicode.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
			//
			// groupBoxReturns
			//
			this.groupBoxReturns.Controls.Add(this.radioButtonReturnsLegacy);
			this.groupBoxReturns.Controls.Add(this.radioButtonReturnsUnicode);
			this.groupBoxReturns.Dock = System.Windows.Forms.DockStyle.Fill;
			this.groupBoxReturns.Location = new System.Drawing.Point(63, 165);
			this.groupBoxReturns.Name = "groupBoxReturns";
			this.groupBoxReturns.Size = new System.Drawing.Size(500, 77);
			this.groupBoxReturns.TabIndex = 4;
			this.groupBoxReturns.TabStop = false;
			this.groupBoxReturns.Text = "CC table returns";
			this.groupBoxReturns.Visible = false;
			//
			// radioButtonReturnsLegacy
			//
			this.radioButtonReturnsLegacy.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.radioButtonReturnsLegacy.AutoSize = true;
			this.radioButtonReturnsLegacy.Location = new System.Drawing.Point(263, 30);
			this.radioButtonReturnsLegacy.Name = "radioButtonReturnsLegacy";
			this.radioButtonReturnsLegacy.Size = new System.Drawing.Size(152, 17);
			this.radioButtonReturnsLegacy.TabIndex = 0;
			this.radioButtonReturnsLegacy.TabStop = true;
			this.radioButtonReturnsLegacy.Text = "Non-Unicode String (bytes)";
			this.radioButtonReturnsLegacy.UseVisualStyleBackColor = true;
			this.radioButtonReturnsLegacy.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
			//
			// radioButtonReturnsUnicode
			//
			this.radioButtonReturnsUnicode.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.radioButtonReturnsUnicode.AutoSize = true;
			this.radioButtonReturnsUnicode.Location = new System.Drawing.Point(85, 30);
			this.radioButtonReturnsUnicode.Name = "radioButtonReturnsUnicode";
			this.radioButtonReturnsUnicode.Size = new System.Drawing.Size(134, 17);
			this.radioButtonReturnsUnicode.TabIndex = 0;
			this.radioButtonReturnsUnicode.TabStop = true;
			this.radioButtonReturnsUnicode.Text = "Unicode String (UTF-8)";
			this.radioButtonReturnsUnicode.UseVisualStyleBackColor = true;
			this.radioButtonReturnsUnicode.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
			//
			// buttonAddSpellFixer
			//
			this.buttonAddSpellFixer.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.buttonAddSpellFixer.Location = new System.Drawing.Point(221, 368);
			this.buttonAddSpellFixer.Name = "buttonAddSpellFixer";
			this.buttonAddSpellFixer.Size = new System.Drawing.Size(183, 23);
			this.buttonAddSpellFixer.TabIndex = 5;
			this.buttonAddSpellFixer.Text = "Add or Edit a SpellFixer CC Table";
			this.buttonAddSpellFixer.UseVisualStyleBackColor = true;
			this.buttonAddSpellFixer.Click += new System.EventHandler(this.buttonAddSpellFixer_Click);
			//
			// labelSpellFixerInstructions
			//
			this.labelSpellFixerInstructions.AutoSize = true;
			this.labelSpellFixerInstructions.Location = new System.Drawing.Point(63, 245);
			this.labelSpellFixerInstructions.Name = "labelSpellFixerInstructions";
			this.labelSpellFixerInstructions.Size = new System.Drawing.Size(498, 117);
			this.labelSpellFixerInstructions.TabIndex = 6;
			this.labelSpellFixerInstructions.Text = resources.GetString("labelSpellFixerInstructions.Text");
			this.labelSpellFixerInstructions.Visible = false;
			//
			// openFileDialogBrowse
			//
			this.openFileDialogBrowse.DefaultExt = "cct";
			this.openFileDialogBrowse.Filter = "CC Tables Files (*.cct)|*.cct";
			this.openFileDialogBrowse.Title = "Browse for CC Table";
			//
			// CcAutoConfigDialog
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.ClientSize = new System.Drawing.Size(634, 479);
			this.IsModified = false;
			this.Name = "CcAutoConfigDialog";
			this.Controls.SetChildIndex(this.tabControl, 0);
			this.Controls.SetChildIndex(this.buttonApply, 0);
			this.Controls.SetChildIndex(this.buttonCancel, 0);
			this.Controls.SetChildIndex(this.buttonOK, 0);
			this.Controls.SetChildIndex(this.buttonSaveInRepository, 0);
			this.tabControl.ResumeLayout(false);
			this.tabPageSetup.ResumeLayout(false);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.groupBoxExpects.ResumeLayout(false);
			this.groupBoxExpects.PerformLayout();
			this.groupBoxReturns.ResumeLayout(false);
			this.groupBoxReturns.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Label labelCCTable;
		private System.Windows.Forms.TextBox textBoxFileSpec;
		private System.Windows.Forms.Button buttonBrowse;
		private System.Windows.Forms.OpenFileDialog openFileDialogBrowse;
		private System.Windows.Forms.GroupBox groupBoxExpects;
		private System.Windows.Forms.RadioButton radioButtonExpectsUnicode;
		private System.Windows.Forms.RadioButton radioButtonExpectsLegacy;
		private System.Windows.Forms.GroupBox groupBoxReturns;
		private System.Windows.Forms.RadioButton radioButtonReturnsLegacy;
		private System.Windows.Forms.RadioButton radioButtonReturnsUnicode;
		private System.Windows.Forms.Button buttonAddSpellFixer;
		private System.Windows.Forms.Label labelSpellFixerInstructions;
	}
}
