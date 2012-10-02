namespace SIL.ObjectBrowser
{
	partial class OptionsDlg
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
			this.chkShade = new System.Windows.Forms.CheckBox();
			this.grpShadeColor = new System.Windows.Forms.GroupBox();
			this.txtRGB = new System.Windows.Forms.TextBox();
			this.lblRGB = new System.Windows.Forms.Label();
			this.lblDescription = new System.Windows.Forms.Label();
			this.clrPicker = new ColorPicker();
			this.btnColor = new System.Windows.Forms.Button();
			this.lblSample = new System.Windows.Forms.Label();
			this.clrDlg = new System.Windows.Forms.ColorDialog();
			this.btnOK = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.grpShadeColor.SuspendLayout();
			this.SuspendLayout();
			//
			// chkShade
			//
			this.chkShade.AutoSize = true;
			this.chkShade.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.chkShade.Location = new System.Drawing.Point(21, 19);
			this.chkShade.Name = "chkShade";
			this.chkShade.Size = new System.Drawing.Size(198, 19);
			this.chkShade.TabIndex = 0;
			this.chkShade.Text = "Use &Shading on Selected Objects";
			this.chkShade.UseVisualStyleBackColor = true;
			//
			// grpShadeColor
			//
			this.grpShadeColor.Controls.Add(this.txtRGB);
			this.grpShadeColor.Controls.Add(this.lblRGB);
			this.grpShadeColor.Controls.Add(this.lblDescription);
			this.grpShadeColor.Controls.Add(this.clrPicker);
			this.grpShadeColor.Controls.Add(this.btnColor);
			this.grpShadeColor.Controls.Add(this.lblSample);
			this.grpShadeColor.Location = new System.Drawing.Point(12, 21);
			this.grpShadeColor.Name = "grpShadeColor";
			this.grpShadeColor.Size = new System.Drawing.Size(348, 295);
			this.grpShadeColor.TabIndex = 1;
			this.grpShadeColor.TabStop = false;
			//
			// txtRGB
			//
			this.txtRGB.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.txtRGB.Location = new System.Drawing.Point(249, 258);
			this.txtRGB.Name = "txtRGB";
			this.txtRGB.Size = new System.Drawing.Size(85, 23);
			this.txtRGB.TabIndex = 5;
			this.txtRGB.Validated += new System.EventHandler(this.txtRGB_Validated);
			this.txtRGB.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtRGB_KeyPress);
			//
			// lblRGB
			//
			this.lblRGB.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblRGB.Location = new System.Drawing.Point(249, 223);
			this.lblRGB.Name = "lblRGB";
			this.lblRGB.Size = new System.Drawing.Size(78, 32);
			this.lblRGB.TabIndex = 4;
			this.lblRGB.Text = "Hexadecimal &RGB Value:";
			//
			// lblDescription
			//
			this.lblDescription.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblDescription.Location = new System.Drawing.Point(16, 27);
			this.lblDescription.Name = "lblDescription";
			this.lblDescription.Size = new System.Drawing.Size(211, 32);
			this.lblDescription.TabIndex = 0;
			this.lblDescription.Text = "Choose a color used to shade selected objects and their children.";
			//
			// clrPicker
			//
			this.clrPicker.Location = new System.Drawing.Point(16, 101);
			this.clrPicker.Name = "clrPicker";
			this.clrPicker.SelectedColor = System.Drawing.Color.Empty;
			this.clrPicker.Size = new System.Drawing.Size(211, 151);
			this.clrPicker.TabIndex = 2;
			this.clrPicker.ColorPicked += new ColorPicker.ColorPickedHandler(this.clrPicker_ColorPicked);
			//
			// btnColor
			//
			this.btnColor.Location = new System.Drawing.Point(16, 256);
			this.btnColor.Name = "btnColor";
			this.btnColor.Size = new System.Drawing.Size(211, 26);
			this.btnColor.TabIndex = 3;
			this.btnColor.Text = "More &Colors...";
			this.btnColor.UseVisualStyleBackColor = true;
			this.btnColor.Click += new System.EventHandler(this.btnColor_Click);
			//
			// lblSample
			//
			this.lblSample.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblSample.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblSample.Location = new System.Drawing.Point(16, 63);
			this.lblSample.Name = "lblSample";
			this.lblSample.Size = new System.Drawing.Size(211, 35);
			this.lblSample.TabIndex = 1;
			this.lblSample.Text = "Sample Text";
			this.lblSample.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.lblSample.Paint += new System.Windows.Forms.PaintEventHandler(this.lblSample_Paint);
			//
			// clrDlg
			//
			this.clrDlg.AnyColor = true;
			this.clrDlg.FullOpen = true;
			//
			// btnOK
			//
			this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOK.Location = new System.Drawing.Point(204, 337);
			this.btnOK.Name = "btnOK";
			this.btnOK.Size = new System.Drawing.Size(75, 26);
			this.btnOK.TabIndex = 2;
			this.btnOK.Text = "OK";
			this.btnOK.UseVisualStyleBackColor = true;
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			//
			// btnCancel
			//
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(285, 337);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(75, 26);
			this.btnCancel.TabIndex = 3;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			//
			// OptionsDlg
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(372, 375);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.chkShade);
			this.Controls.Add(this.grpShadeColor);
			this.DoubleBuffered = true;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "OptionsDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Options";
			this.grpShadeColor.ResumeLayout(false);
			this.grpShadeColor.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox chkShade;
		private System.Windows.Forms.GroupBox grpShadeColor;
		private System.Windows.Forms.Label lblSample;
		private System.Windows.Forms.Button btnColor;
		private System.Windows.Forms.ColorDialog clrDlg;
		private ColorPicker clrPicker;
		private System.Windows.Forms.Label lblDescription;
		private System.Windows.Forms.TextBox txtRGB;
		private System.Windows.Forms.Label lblRGB;
		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.Button btnCancel;
	}
}