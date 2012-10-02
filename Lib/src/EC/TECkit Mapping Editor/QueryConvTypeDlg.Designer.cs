namespace TECkit_Mapping_Editor
{
	partial class QueryConvTypeDlg
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
			this.groupBoxConvType = new System.Windows.Forms.GroupBox();
			this.radioButtonLegacyLegacy = new System.Windows.Forms.RadioButton();
			this.radioButtonLegacyUnicode = new System.Windows.Forms.RadioButton();
			this.radioButtonUnicodeLegacy = new System.Windows.Forms.RadioButton();
			this.radioButtonUnicodeUnicode = new System.Windows.Forms.RadioButton();
			this.checkBoxBiDirectional = new System.Windows.Forms.CheckBox();
			this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.groupBoxConvType.SuspendLayout();
			this.tableLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			//
			// groupBoxConvType
			//
			this.groupBoxConvType.Controls.Add(this.checkBoxBiDirectional);
			this.groupBoxConvType.Controls.Add(this.radioButtonUnicodeUnicode);
			this.groupBoxConvType.Controls.Add(this.radioButtonUnicodeLegacy);
			this.groupBoxConvType.Controls.Add(this.radioButtonLegacyUnicode);
			this.groupBoxConvType.Controls.Add(this.radioButtonLegacyLegacy);
			this.groupBoxConvType.Location = new System.Drawing.Point(13, 13);
			this.groupBoxConvType.Name = "groupBoxConvType";
			this.groupBoxConvType.Size = new System.Drawing.Size(158, 138);
			this.groupBoxConvType.TabIndex = 0;
			this.groupBoxConvType.TabStop = false;
			this.groupBoxConvType.Text = "Conversion Type";
			//
			// radioButtonLegacyLegacy
			//
			this.radioButtonLegacyLegacy.AutoSize = true;
			this.radioButtonLegacyLegacy.Location = new System.Drawing.Point(7, 20);
			this.radioButtonLegacyLegacy.Name = "radioButtonLegacyLegacy";
			this.radioButtonLegacyLegacy.Size = new System.Drawing.Size(110, 17);
			this.radioButtonLegacyLegacy.TabIndex = 0;
			this.radioButtonLegacyLegacy.TabStop = true;
			this.radioButtonLegacyLegacy.Text = "Legacy to Legacy";
			this.radioButtonLegacyLegacy.UseVisualStyleBackColor = true;
			//
			// radioButtonLegacyUnicode
			//
			this.radioButtonLegacyUnicode.AutoSize = true;
			this.radioButtonLegacyUnicode.Checked = true;
			this.radioButtonLegacyUnicode.Location = new System.Drawing.Point(7, 43);
			this.radioButtonLegacyUnicode.Name = "radioButtonLegacyUnicode";
			this.radioButtonLegacyUnicode.Size = new System.Drawing.Size(115, 17);
			this.radioButtonLegacyUnicode.TabIndex = 1;
			this.radioButtonLegacyUnicode.TabStop = true;
			this.radioButtonLegacyUnicode.Text = "Legacy to Unicode";
			this.radioButtonLegacyUnicode.UseVisualStyleBackColor = true;
			//
			// radioButtonUnicodeLegacy
			//
			this.radioButtonUnicodeLegacy.AutoSize = true;
			this.radioButtonUnicodeLegacy.Location = new System.Drawing.Point(7, 66);
			this.radioButtonUnicodeLegacy.Name = "radioButtonUnicodeLegacy";
			this.radioButtonUnicodeLegacy.Size = new System.Drawing.Size(115, 17);
			this.radioButtonUnicodeLegacy.TabIndex = 2;
			this.radioButtonUnicodeLegacy.TabStop = true;
			this.radioButtonUnicodeLegacy.Text = "Unicode to Legacy";
			this.radioButtonUnicodeLegacy.UseVisualStyleBackColor = true;
			//
			// radioButtonUnicodeUnicode
			//
			this.radioButtonUnicodeUnicode.AutoSize = true;
			this.radioButtonUnicodeUnicode.Location = new System.Drawing.Point(7, 89);
			this.radioButtonUnicodeUnicode.Name = "radioButtonUnicodeUnicode";
			this.radioButtonUnicodeUnicode.Size = new System.Drawing.Size(120, 17);
			this.radioButtonUnicodeUnicode.TabIndex = 3;
			this.radioButtonUnicodeUnicode.TabStop = true;
			this.radioButtonUnicodeUnicode.Text = "Unicode to Unicode";
			this.radioButtonUnicodeUnicode.UseVisualStyleBackColor = true;
			//
			// checkBoxBiDirectional
			//
			this.checkBoxBiDirectional.AutoSize = true;
			this.checkBoxBiDirectional.Checked = true;
			this.checkBoxBiDirectional.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxBiDirectional.Location = new System.Drawing.Point(7, 113);
			this.checkBoxBiDirectional.Name = "checkBoxBiDirectional";
			this.checkBoxBiDirectional.Size = new System.Drawing.Size(83, 17);
			this.checkBoxBiDirectional.TabIndex = 4;
			this.checkBoxBiDirectional.Text = "Bidirectional";
			this.checkBoxBiDirectional.UseVisualStyleBackColor = true;
			//
			// tableLayoutPanel
			//
			this.tableLayoutPanel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.tableLayoutPanel.AutoSize = true;
			this.tableLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.tableLayoutPanel.ColumnCount = 2;
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel.Controls.Add(this.buttonOK, 0, 0);
			this.tableLayoutPanel.Controls.Add(this.buttonCancel, 1, 0);
			this.tableLayoutPanel.Location = new System.Drawing.Point(12, 160);
			this.tableLayoutPanel.Name = "tableLayoutPanel";
			this.tableLayoutPanel.RowCount = 1;
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel.Size = new System.Drawing.Size(162, 29);
			this.tableLayoutPanel.TabIndex = 1;
			//
			// buttonOK
			//
			this.buttonOK.Location = new System.Drawing.Point(3, 3);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(75, 23);
			this.buttonOK.TabIndex = 0;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			//
			// buttonCancel
			//
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(84, 3);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 1;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			//
			// QueryConvType
			//
			this.AcceptButton = this.buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(187, 201);
			this.ControlBox = false;
			this.Controls.Add(this.tableLayoutPanel);
			this.Controls.Add(this.groupBoxConvType);
			this.Name = "QueryConvType";
			this.Text = "Select Conversion Type";
			this.groupBoxConvType.ResumeLayout(false);
			this.groupBoxConvType.PerformLayout();
			this.tableLayoutPanel.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.GroupBox groupBoxConvType;
		private System.Windows.Forms.RadioButton radioButtonLegacyLegacy;
		private System.Windows.Forms.RadioButton radioButtonLegacyUnicode;
		private System.Windows.Forms.RadioButton radioButtonUnicodeLegacy;
		private System.Windows.Forms.RadioButton radioButtonUnicodeUnicode;
		private System.Windows.Forms.CheckBox checkBoxBiDirectional;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
	}
}