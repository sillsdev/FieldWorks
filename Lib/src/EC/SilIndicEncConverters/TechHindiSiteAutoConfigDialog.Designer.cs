namespace SilEncConverters40
{
	partial class TechHindiSiteAutoConfigDialog
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TechHindiSiteAutoConfigDialog));
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.labelCCTable = new System.Windows.Forms.Label();
			this.textBoxFileSpec = new System.Windows.Forms.TextBox();
			this.buttonBrowse = new System.Windows.Forms.Button();
			this.groupBoxConversionType = new System.Windows.Forms.GroupBox();
			this.checkBoxBidirectional = new System.Windows.Forms.CheckBox();
			this.radioButtonLegacyToLegacy = new System.Windows.Forms.RadioButton();
			this.radioButtonUnicodeToUnicode = new System.Windows.Forms.RadioButton();
			this.radioButtonLegacyToUnicode = new System.Windows.Forms.RadioButton();
			this.labelInputId = new System.Windows.Forms.Label();
			this.textBoxInputId = new System.Windows.Forms.TextBox();
			this.buttonBrowseInputId = new System.Windows.Forms.Button();
			this.labelOutputId = new System.Windows.Forms.Label();
			this.textBoxOutputId = new System.Windows.Forms.TextBox();
			this.buttonOutputId = new System.Windows.Forms.Button();
			this.labelConvertFunctionForward = new System.Windows.Forms.Label();
			this.textBoxConvertFunctionForward = new System.Windows.Forms.TextBox();
			this.buttonConvertFunctionForward = new System.Windows.Forms.Button();
			this.labelConvertFunctionReverse = new System.Windows.Forms.Label();
			this.textBoxConvertFunctionReverse = new System.Windows.Forms.TextBox();
			this.buttonConvertFunctionReverse = new System.Windows.Forms.Button();
			this.openFileDialogBrowse = new System.Windows.Forms.OpenFileDialog();
			this.tabControl.SuspendLayout();
			this.tabPageSetup.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			this.groupBoxConversionType.SuspendLayout();
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
			this.tableLayoutPanel1.Controls.Add(this.groupBoxConversionType, 1, 1);
			this.tableLayoutPanel1.Controls.Add(this.labelInputId, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this.textBoxInputId, 1, 2);
			this.tableLayoutPanel1.Controls.Add(this.buttonBrowseInputId, 2, 2);
			this.tableLayoutPanel1.Controls.Add(this.labelOutputId, 0, 3);
			this.tableLayoutPanel1.Controls.Add(this.textBoxOutputId, 1, 3);
			this.tableLayoutPanel1.Controls.Add(this.buttonOutputId, 2, 3);
			this.tableLayoutPanel1.Controls.Add(this.labelConvertFunctionForward, 0, 4);
			this.tableLayoutPanel1.Controls.Add(this.textBoxConvertFunctionForward, 1, 4);
			this.tableLayoutPanel1.Controls.Add(this.buttonConvertFunctionForward, 2, 4);
			this.tableLayoutPanel1.Controls.Add(this.labelConvertFunctionReverse, 0, 5);
			this.tableLayoutPanel1.Controls.Add(this.textBoxConvertFunctionReverse, 1, 5);
			this.tableLayoutPanel1.Controls.Add(this.buttonConvertFunctionReverse, 2, 5);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 3);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 7;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(596, 394);
			this.tableLayoutPanel1.TabIndex = 3;
			//
			// labelCCTable
			//
			this.labelCCTable.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.labelCCTable.AutoSize = true;
			this.labelCCTable.Location = new System.Drawing.Point(33, 8);
			this.labelCCTable.Name = "labelCCTable";
			this.labelCCTable.Size = new System.Drawing.Size(84, 13);
			this.labelCCTable.TabIndex = 0;
			this.labelCCTable.Text = "Converter Page:";
			//
			// textBoxFileSpec
			//
			this.textBoxFileSpec.Dock = System.Windows.Forms.DockStyle.Fill;
			this.helpProvider.SetHelpString(this.textBoxFileSpec, resources.GetString("textBoxFileSpec.HelpString"));
			this.textBoxFileSpec.Location = new System.Drawing.Point(123, 3);
			this.textBoxFileSpec.Name = "textBoxFileSpec";
			this.helpProvider.SetShowHelp(this.textBoxFileSpec, true);
			this.textBoxFileSpec.Size = new System.Drawing.Size(440, 20);
			this.textBoxFileSpec.TabIndex = 1;
			this.textBoxFileSpec.TextChanged += new System.EventHandler(this.textBoxFileSpec_TextChanged);
			//
			// buttonBrowse
			//
			this.buttonBrowse.Dock = System.Windows.Forms.DockStyle.Fill;
			this.helpProvider.SetHelpString(this.buttonBrowse, "Click to browse the local computer for the web-page converter (*.htm;*.html) file" +
					"");
			this.buttonBrowse.Location = new System.Drawing.Point(569, 3);
			this.buttonBrowse.Name = "buttonBrowse";
			this.helpProvider.SetShowHelp(this.buttonBrowse, true);
			this.buttonBrowse.Size = new System.Drawing.Size(24, 23);
			this.buttonBrowse.TabIndex = 2;
			this.buttonBrowse.Text = "...";
			this.buttonBrowse.UseVisualStyleBackColor = true;
			this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
			//
			// groupBoxConversionType
			//
			this.groupBoxConversionType.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.groupBoxConversionType.Controls.Add(this.checkBoxBidirectional);
			this.groupBoxConversionType.Controls.Add(this.radioButtonLegacyToLegacy);
			this.groupBoxConversionType.Controls.Add(this.radioButtonUnicodeToUnicode);
			this.groupBoxConversionType.Controls.Add(this.radioButtonLegacyToUnicode);
			this.groupBoxConversionType.Dock = System.Windows.Forms.DockStyle.Fill;
			this.helpProvider.SetHelpString(this.groupBoxConversionType, "Indicate which kind of conversion this converter will perform");
			this.groupBoxConversionType.Location = new System.Drawing.Point(123, 32);
			this.groupBoxConversionType.Name = "groupBoxConversionType";
			this.helpProvider.SetShowHelp(this.groupBoxConversionType, true);
			this.groupBoxConversionType.Size = new System.Drawing.Size(440, 133);
			this.groupBoxConversionType.TabIndex = 3;
			this.groupBoxConversionType.TabStop = false;
			this.groupBoxConversionType.Text = "Conversion Type";
			//
			// checkBoxBidirectional
			//
			this.checkBoxBidirectional.AutoSize = true;
			this.helpProvider.SetHelpString(this.checkBoxBidirectional, "Indicates whether the web-page converter has two functions: one for a conversion " +
					"in one (forward) direction and one for a conversion in the opposite (reverse) di" +
					"rection.");
			this.checkBoxBidirectional.Location = new System.Drawing.Point(21, 99);
			this.checkBoxBidirectional.Name = "checkBoxBidirectional";
			this.helpProvider.SetShowHelp(this.checkBoxBidirectional, true);
			this.checkBoxBidirectional.Size = new System.Drawing.Size(89, 17);
			this.checkBoxBidirectional.TabIndex = 2;
			this.checkBoxBidirectional.Text = "Bidirectional?";
			this.checkBoxBidirectional.UseVisualStyleBackColor = true;
			this.checkBoxBidirectional.CheckedChanged += new System.EventHandler(this.checkBoxBidirectional_CheckedChanged);
			//
			// radioButtonLegacyToLegacy
			//
			this.radioButtonLegacyToLegacy.AutoSize = true;
			this.helpProvider.SetHelpString(this.radioButtonLegacyToLegacy, "e.g. KrutiDev to Shusha font");
			this.radioButtonLegacyToLegacy.Location = new System.Drawing.Point(21, 75);
			this.radioButtonLegacyToLegacy.Name = "radioButtonLegacyToLegacy";
			this.helpProvider.SetShowHelp(this.radioButtonLegacyToLegacy, true);
			this.radioButtonLegacyToLegacy.Size = new System.Drawing.Size(110, 17);
			this.radioButtonLegacyToLegacy.TabIndex = 1;
			this.radioButtonLegacyToLegacy.Text = "Legacy to Legacy";
			this.radioButtonLegacyToLegacy.UseVisualStyleBackColor = true;
			this.radioButtonLegacyToLegacy.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
			//
			// radioButtonUnicodeToUnicode
			//
			this.radioButtonUnicodeToUnicode.AutoSize = true;
			this.helpProvider.SetHelpString(this.radioButtonUnicodeToUnicode, "e.g. Devanagari Unicode to IPA Unicode transliteration");
			this.radioButtonUnicodeToUnicode.Location = new System.Drawing.Point(21, 52);
			this.radioButtonUnicodeToUnicode.Name = "radioButtonUnicodeToUnicode";
			this.helpProvider.SetShowHelp(this.radioButtonUnicodeToUnicode, true);
			this.radioButtonUnicodeToUnicode.Size = new System.Drawing.Size(120, 17);
			this.radioButtonUnicodeToUnicode.TabIndex = 0;
			this.radioButtonUnicodeToUnicode.Text = "Unicode to Unicode";
			this.radioButtonUnicodeToUnicode.UseVisualStyleBackColor = true;
			this.radioButtonUnicodeToUnicode.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
			//
			// radioButtonLegacyToUnicode
			//
			this.radioButtonLegacyToUnicode.AutoSize = true;
			this.radioButtonLegacyToUnicode.Checked = true;
			this.helpProvider.SetHelpString(this.radioButtonLegacyToUnicode, "e.g. KrutiDev to Unicode");
			this.radioButtonLegacyToUnicode.Location = new System.Drawing.Point(21, 29);
			this.radioButtonLegacyToUnicode.Name = "radioButtonLegacyToUnicode";
			this.helpProvider.SetShowHelp(this.radioButtonLegacyToUnicode, true);
			this.radioButtonLegacyToUnicode.Size = new System.Drawing.Size(115, 17);
			this.radioButtonLegacyToUnicode.TabIndex = 0;
			this.radioButtonLegacyToUnicode.TabStop = true;
			this.radioButtonLegacyToUnicode.Text = "Legacy to Unicode";
			this.radioButtonLegacyToUnicode.UseVisualStyleBackColor = true;
			this.radioButtonLegacyToUnicode.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
			//
			// labelInputId
			//
			this.labelInputId.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.labelInputId.AutoSize = true;
			this.labelInputId.Location = new System.Drawing.Point(71, 176);
			this.labelInputId.Name = "labelInputId";
			this.labelInputId.Size = new System.Drawing.Size(46, 13);
			this.labelInputId.TabIndex = 4;
			this.labelInputId.Text = "Input Id:";
			//
			// textBoxInputId
			//
			this.textBoxInputId.Dock = System.Windows.Forms.DockStyle.Fill;
			this.helpProvider.SetHelpString(this.textBoxInputId, resources.GetString("textBoxInputId.HelpString"));
			this.textBoxInputId.Location = new System.Drawing.Point(123, 171);
			this.textBoxInputId.Name = "textBoxInputId";
			this.helpProvider.SetShowHelp(this.textBoxInputId, true);
			this.textBoxInputId.Size = new System.Drawing.Size(440, 20);
			this.textBoxInputId.TabIndex = 5;
			//
			// buttonBrowseInputId
			//
			this.helpProvider.SetHelpString(this.buttonBrowseInputId, "Click to list the textarea elements embedded in the html source.");
			this.buttonBrowseInputId.Location = new System.Drawing.Point(569, 171);
			this.buttonBrowseInputId.Name = "buttonBrowseInputId";
			this.helpProvider.SetShowHelp(this.buttonBrowseInputId, true);
			this.buttonBrowseInputId.Size = new System.Drawing.Size(24, 23);
			this.buttonBrowseInputId.TabIndex = 6;
			this.buttonBrowseInputId.Text = "...";
			this.buttonBrowseInputId.UseVisualStyleBackColor = true;
			this.buttonBrowseInputId.Click += new System.EventHandler(this.buttonBrowseInputId_Click);
			//
			// labelOutputId
			//
			this.labelOutputId.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.labelOutputId.AutoSize = true;
			this.labelOutputId.Location = new System.Drawing.Point(63, 205);
			this.labelOutputId.Name = "labelOutputId";
			this.labelOutputId.Size = new System.Drawing.Size(54, 13);
			this.labelOutputId.TabIndex = 7;
			this.labelOutputId.Text = "Output Id:";
			//
			// textBoxOutputId
			//
			this.textBoxOutputId.Dock = System.Windows.Forms.DockStyle.Fill;
			this.helpProvider.SetHelpString(this.textBoxOutputId, resources.GetString("textBoxOutputId.HelpString"));
			this.textBoxOutputId.Location = new System.Drawing.Point(123, 200);
			this.textBoxOutputId.Name = "textBoxOutputId";
			this.helpProvider.SetShowHelp(this.textBoxOutputId, true);
			this.textBoxOutputId.Size = new System.Drawing.Size(440, 20);
			this.textBoxOutputId.TabIndex = 8;
			//
			// buttonOutputId
			//
			this.helpProvider.SetHelpString(this.buttonOutputId, "Click to list the textarea elements embedded in the html source.");
			this.buttonOutputId.Location = new System.Drawing.Point(569, 200);
			this.buttonOutputId.Name = "buttonOutputId";
			this.helpProvider.SetShowHelp(this.buttonOutputId, true);
			this.buttonOutputId.Size = new System.Drawing.Size(24, 23);
			this.buttonOutputId.TabIndex = 9;
			this.buttonOutputId.Text = "...";
			this.buttonOutputId.UseVisualStyleBackColor = true;
			this.buttonOutputId.Click += new System.EventHandler(this.buttonOutputId_Click);
			//
			// labelConvertFunctionForward
			//
			this.labelConvertFunctionForward.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.labelConvertFunctionForward.AutoSize = true;
			this.labelConvertFunctionForward.Location = new System.Drawing.Point(3, 234);
			this.labelConvertFunctionForward.Name = "labelConvertFunctionForward";
			this.labelConvertFunctionForward.Size = new System.Drawing.Size(114, 13);
			this.labelConvertFunctionForward.TabIndex = 10;
			this.labelConvertFunctionForward.Text = "Convert function (fwd):";
			//
			// textBoxConvertFunctionForward
			//
			this.textBoxConvertFunctionForward.Dock = System.Windows.Forms.DockStyle.Fill;
			this.helpProvider.SetHelpString(this.textBoxConvertFunctionForward, resources.GetString("textBoxConvertFunctionForward.HelpString"));
			this.textBoxConvertFunctionForward.Location = new System.Drawing.Point(123, 229);
			this.textBoxConvertFunctionForward.Name = "textBoxConvertFunctionForward";
			this.helpProvider.SetShowHelp(this.textBoxConvertFunctionForward, true);
			this.textBoxConvertFunctionForward.Size = new System.Drawing.Size(440, 20);
			this.textBoxConvertFunctionForward.TabIndex = 11;
			//
			// buttonConvertFunctionForward
			//
			this.helpProvider.SetHelpString(this.buttonConvertFunctionForward, "Click to list the script functions embedded in the html source.");
			this.buttonConvertFunctionForward.Location = new System.Drawing.Point(569, 229);
			this.buttonConvertFunctionForward.Name = "buttonConvertFunctionForward";
			this.helpProvider.SetShowHelp(this.buttonConvertFunctionForward, true);
			this.buttonConvertFunctionForward.Size = new System.Drawing.Size(24, 23);
			this.buttonConvertFunctionForward.TabIndex = 12;
			this.buttonConvertFunctionForward.Text = "...";
			this.buttonConvertFunctionForward.UseVisualStyleBackColor = true;
			this.buttonConvertFunctionForward.Click += new System.EventHandler(this.buttonConvertFunctionForward_Click);
			//
			// labelConvertFunctionReverse
			//
			this.labelConvertFunctionReverse.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.labelConvertFunctionReverse.AutoSize = true;
			this.labelConvertFunctionReverse.Location = new System.Drawing.Point(5, 263);
			this.labelConvertFunctionReverse.Name = "labelConvertFunctionReverse";
			this.labelConvertFunctionReverse.Size = new System.Drawing.Size(112, 13);
			this.labelConvertFunctionReverse.TabIndex = 13;
			this.labelConvertFunctionReverse.Text = "Convert function (rev):";
			this.labelConvertFunctionReverse.Visible = false;
			//
			// textBoxConvertFunctionReverse
			//
			this.textBoxConvertFunctionReverse.Dock = System.Windows.Forms.DockStyle.Fill;
			this.helpProvider.SetHelpNavigator(this.textBoxConvertFunctionReverse, System.Windows.Forms.HelpNavigator.KeywordIndex);
			this.helpProvider.SetHelpString(this.textBoxConvertFunctionReverse, resources.GetString("textBoxConvertFunctionReverse.HelpString"));
			this.textBoxConvertFunctionReverse.Location = new System.Drawing.Point(123, 258);
			this.textBoxConvertFunctionReverse.Name = "textBoxConvertFunctionReverse";
			this.helpProvider.SetShowHelp(this.textBoxConvertFunctionReverse, true);
			this.textBoxConvertFunctionReverse.Size = new System.Drawing.Size(440, 20);
			this.textBoxConvertFunctionReverse.TabIndex = 14;
			this.textBoxConvertFunctionReverse.Visible = false;
			//
			// buttonConvertFunctionReverse
			//
			this.helpProvider.SetHelpString(this.buttonConvertFunctionReverse, "Click to list the script functions embedded in the html source.");
			this.buttonConvertFunctionReverse.Location = new System.Drawing.Point(569, 258);
			this.buttonConvertFunctionReverse.Name = "buttonConvertFunctionReverse";
			this.helpProvider.SetShowHelp(this.buttonConvertFunctionReverse, true);
			this.buttonConvertFunctionReverse.Size = new System.Drawing.Size(24, 23);
			this.buttonConvertFunctionReverse.TabIndex = 15;
			this.buttonConvertFunctionReverse.Text = "...";
			this.buttonConvertFunctionReverse.UseVisualStyleBackColor = true;
			this.buttonConvertFunctionReverse.Visible = false;
			this.buttonConvertFunctionReverse.Click += new System.EventHandler(this.buttonConvertFunctionReverse_Click);
			//
			// openFileDialogBrowse
			//
			this.openFileDialogBrowse.DefaultExt = "html";
			this.openFileDialogBrowse.Filter = "Web page converter Files (*.htm; *.html)|*.htm; *.html";
			this.openFileDialogBrowse.Title = "Browse for web page converter file";
			//
			// TechHindiSiteAutoConfigDialog
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.ClientSize = new System.Drawing.Size(634, 479);
			this.Name = "TechHindiSiteAutoConfigDialog";
			this.Controls.SetChildIndex(this.tabControl, 0);
			this.Controls.SetChildIndex(this.buttonApply, 0);
			this.Controls.SetChildIndex(this.buttonCancel, 0);
			this.Controls.SetChildIndex(this.buttonOK, 0);
			this.Controls.SetChildIndex(this.buttonSaveInRepository, 0);
			this.tabControl.ResumeLayout(false);
			this.tabPageSetup.ResumeLayout(false);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.groupBoxConversionType.ResumeLayout(false);
			this.groupBoxConversionType.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Label labelCCTable;
		private System.Windows.Forms.TextBox textBoxFileSpec;
		private System.Windows.Forms.Button buttonBrowse;
		private System.Windows.Forms.GroupBox groupBoxConversionType;
		private System.Windows.Forms.RadioButton radioButtonUnicodeToUnicode;
		private System.Windows.Forms.RadioButton radioButtonLegacyToUnicode;
		private System.Windows.Forms.RadioButton radioButtonLegacyToLegacy;
		private System.Windows.Forms.CheckBox checkBoxBidirectional;
		private System.Windows.Forms.Label labelInputId;
		private System.Windows.Forms.TextBox textBoxInputId;
		private System.Windows.Forms.Button buttonBrowseInputId;
		private System.Windows.Forms.Label labelOutputId;
		private System.Windows.Forms.TextBox textBoxOutputId;
		private System.Windows.Forms.Button buttonOutputId;
		private System.Windows.Forms.Label labelConvertFunctionForward;
		private System.Windows.Forms.TextBox textBoxConvertFunctionForward;
		private System.Windows.Forms.Button buttonConvertFunctionForward;
		private System.Windows.Forms.Label labelConvertFunctionReverse;
		private System.Windows.Forms.TextBox textBoxConvertFunctionReverse;
		private System.Windows.Forms.Button buttonConvertFunctionReverse;
		private System.Windows.Forms.OpenFileDialog openFileDialogBrowse;
	}
}
