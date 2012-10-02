namespace SilEncConverters31
{
	partial class FallbackAutoConfigDialog
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
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.comboBoxPrimary = new System.Windows.Forms.ComboBox();
			this.label3 = new System.Windows.Forms.Label();
			this.checkBoxReversePrimary = new System.Windows.Forms.CheckBox();
			this.label4 = new System.Windows.Forms.Label();
			this.comboBoxFallback = new System.Windows.Forms.ComboBox();
			this.label5 = new System.Windows.Forms.Label();
			this.checkBoxReverseFallback = new System.Windows.Forms.CheckBox();
			this.labelCompoundConverterName = new System.Windows.Forms.Label();
			this.tabControl.SuspendLayout();
			this.tabPageSetup.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			//
			// tabPageSetup
			//
			this.tabPageSetup.Controls.Add(this.tableLayoutPanel1);
			//
			// buttonSaveInRepository
			//
			this.helpProvider.SetHelpString(this.buttonSaveInRepository, "\r\nClick to add this converter to the system repository permanently.\r\n    ");
			this.helpProvider.SetShowHelp(this.buttonSaveInRepository, true);
			//
			// tableLayoutPanel1
			//
			this.tableLayoutPanel1.ColumnCount = 2;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.label1, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.label2, 0, 3);
			this.tableLayoutPanel1.Controls.Add(this.comboBoxPrimary, 0, 4);
			this.tableLayoutPanel1.Controls.Add(this.label3, 0, 5);
			this.tableLayoutPanel1.Controls.Add(this.checkBoxReversePrimary, 1, 5);
			this.tableLayoutPanel1.Controls.Add(this.label4, 0, 7);
			this.tableLayoutPanel1.Controls.Add(this.comboBoxFallback, 0, 8);
			this.tableLayoutPanel1.Controls.Add(this.label5, 0, 9);
			this.tableLayoutPanel1.Controls.Add(this.checkBoxReverseFallback, 1, 9);
			this.tableLayoutPanel1.Controls.Add(this.labelCompoundConverterName, 1, 1);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 3);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 11;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(596, 394);
			this.tableLayoutPanel1.TabIndex = 0;
			//
			// label1
			//
			this.label1.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 51);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(87, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Converter Name:";
			//
			// label2
			//
			this.label2.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.label2.AutoSize = true;
			this.tableLayoutPanel1.SetColumnSpan(this.label2, 2);
			this.label2.Location = new System.Drawing.Point(3, 115);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(233, 13);
			this.label2.TabIndex = 1;
			this.label2.Text = "Choose the Primary converter and the Direction:";
			//
			// comboBoxPrimary
			//
			this.tableLayoutPanel1.SetColumnSpan(this.comboBoxPrimary, 2);
			this.comboBoxPrimary.Dock = System.Windows.Forms.DockStyle.Fill;
			this.comboBoxPrimary.FormattingEnabled = true;
			this.comboBoxPrimary.Location = new System.Drawing.Point(3, 131);
			this.comboBoxPrimary.Name = "comboBoxPrimary";
			this.comboBoxPrimary.Size = new System.Drawing.Size(590, 21);
			this.comboBoxPrimary.TabIndex = 2;
			this.comboBoxPrimary.SelectedIndexChanged += new System.EventHandler(this.comboBoxPrimary_SelectedIndexChanged);
			//
			// label3
			//
			this.label3.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(3, 160);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(52, 13);
			this.label3.TabIndex = 3;
			this.label3.Text = "Direction:";
			//
			// checkBoxReversePrimary
			//
			this.checkBoxReversePrimary.AutoSize = true;
			this.checkBoxReversePrimary.Location = new System.Drawing.Point(96, 158);
			this.checkBoxReversePrimary.Name = "checkBoxReversePrimary";
			this.checkBoxReversePrimary.Size = new System.Drawing.Size(66, 17);
			this.checkBoxReversePrimary.TabIndex = 4;
			this.checkBoxReversePrimary.Text = "&Reverse";
			this.checkBoxReversePrimary.UseVisualStyleBackColor = true;
			this.checkBoxReversePrimary.CheckedChanged += new System.EventHandler(this.checkBoxReverse_CheckedChanged);
			//
			// label4
			//
			this.label4.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.label4.AutoSize = true;
			this.tableLayoutPanel1.SetColumnSpan(this.label4, 2);
			this.label4.Location = new System.Drawing.Point(3, 228);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(239, 13);
			this.label4.TabIndex = 5;
			this.label4.Text = "Choose the Fallback converter and the Direction:";
			//
			// comboBoxFallback
			//
			this.tableLayoutPanel1.SetColumnSpan(this.comboBoxFallback, 2);
			this.comboBoxFallback.Dock = System.Windows.Forms.DockStyle.Fill;
			this.comboBoxFallback.FormattingEnabled = true;
			this.comboBoxFallback.Location = new System.Drawing.Point(3, 244);
			this.comboBoxFallback.Name = "comboBoxFallback";
			this.comboBoxFallback.Size = new System.Drawing.Size(590, 21);
			this.comboBoxFallback.TabIndex = 6;
			this.comboBoxFallback.SelectedIndexChanged += new System.EventHandler(this.comboBoxFallback_SelectedIndexChanged);
			//
			// label5
			//
			this.label5.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(3, 273);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(52, 13);
			this.label5.TabIndex = 3;
			this.label5.Text = "Direction:";
			//
			// checkBoxReverseFallback
			//
			this.checkBoxReverseFallback.AutoSize = true;
			this.checkBoxReverseFallback.Location = new System.Drawing.Point(96, 271);
			this.checkBoxReverseFallback.Name = "checkBoxReverseFallback";
			this.checkBoxReverseFallback.Size = new System.Drawing.Size(66, 17);
			this.checkBoxReverseFallback.TabIndex = 4;
			this.checkBoxReverseFallback.Text = "&Reverse";
			this.checkBoxReverseFallback.UseVisualStyleBackColor = true;
			this.checkBoxReverseFallback.CheckedChanged += new System.EventHandler(this.checkBoxReverse_CheckedChanged);
			//
			// labelCompoundConverterName
			//
			this.labelCompoundConverterName.AutoSize = true;
			this.labelCompoundConverterName.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.labelCompoundConverterName.Dock = System.Windows.Forms.DockStyle.Fill;
			this.helpProvider.SetHelpString(this.labelCompoundConverterName, "This box shows the name that the primary-fallback converter goes by");
			this.labelCompoundConverterName.Location = new System.Drawing.Point(96, 50);
			this.labelCompoundConverterName.Name = "labelCompoundConverterName";
			this.helpProvider.SetShowHelp(this.labelCompoundConverterName, true);
			this.labelCompoundConverterName.Size = new System.Drawing.Size(497, 15);
			this.labelCompoundConverterName.TabIndex = 7;
			//
			// FallbackAutoConfigDialog
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.ClientSize = new System.Drawing.Size(634, 479);
			this.Name = "FallbackAutoConfigDialog";
			this.Controls.SetChildIndex(this.tabControl, 0);
			this.Controls.SetChildIndex(this.buttonApply, 0);
			this.Controls.SetChildIndex(this.buttonCancel, 0);
			this.Controls.SetChildIndex(this.buttonOK, 0);
			this.Controls.SetChildIndex(this.buttonSaveInRepository, 0);
			this.tabControl.ResumeLayout(false);
			this.tabPageSetup.ResumeLayout(false);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.ComboBox comboBoxPrimary;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.CheckBox checkBoxReversePrimary;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.ComboBox comboBoxFallback;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.CheckBox checkBoxReverseFallback;
		private System.Windows.Forms.Label labelCompoundConverterName;
	}
}
