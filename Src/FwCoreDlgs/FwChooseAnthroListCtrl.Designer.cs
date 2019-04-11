namespace SIL.FieldWorks.FwCoreDlgs
{
	partial class FwChooseAnthroListCtrl
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.m_lblAnthroHeader = new System.Windows.Forms.Label();
			this.m_tbDescription = new System.Windows.Forms.TextBox();
			this._choicesLayout = new System.Windows.Forms.TableLayoutPanel();
			this.m_radioFRAME = new System.Windows.Forms.RadioButton();
			this.customDescription = new System.Windows.Forms.TextBox();
			this.ocmDescription = new System.Windows.Forms.TextBox();
			this.m_radioOCM = new System.Windows.Forms.RadioButton();
			this.m_radioCustom = new System.Windows.Forms.RadioButton();
			this.frameDescription = new System.Windows.Forms.TextBox();
			this._choicesLayout.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_lblAnthroHeader
			// 
			this.m_lblAnthroHeader.AutoSize = true;
			this.m_lblAnthroHeader.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
			this.m_lblAnthroHeader.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.m_lblAnthroHeader.Location = new System.Drawing.Point(13, 14);
			this.m_lblAnthroHeader.Name = "m_lblAnthroHeader";
			this.m_lblAnthroHeader.Size = new System.Drawing.Size(288, 17);
			this.m_lblAnthroHeader.TabIndex = 12;
			this.m_lblAnthroHeader.Text = "Choose list of anthropology categories";
			// 
			// m_tbDescription
			// 
			this.m_tbDescription.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_tbDescription.Location = new System.Drawing.Point(16, 39);
			this.m_tbDescription.Multiline = true;
			this.m_tbDescription.Name = "m_tbDescription";
			this.m_tbDescription.ReadOnly = true;
			this.m_tbDescription.Size = new System.Drawing.Size(458, 28);
			this.m_tbDescription.TabIndex = 10;
			this.m_tbDescription.TabStop = false;
			this.m_tbDescription.Text = "In Notebook, you will use a list of anthropology categories to catalog your obser" +
    "vations.";
			// 
			// _choicesLayout
			// 
			this._choicesLayout.ColumnCount = 2;
			this._choicesLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 34F));
			this._choicesLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this._choicesLayout.Controls.Add(this.m_radioFRAME, 0, 0);
			this._choicesLayout.Controls.Add(this.customDescription, 1, 2);
			this._choicesLayout.Controls.Add(this.ocmDescription, 1, 1);
			this._choicesLayout.Controls.Add(this.m_radioOCM, 0, 1);
			this._choicesLayout.Controls.Add(this.m_radioCustom, 0, 2);
			this._choicesLayout.Controls.Add(this.frameDescription, 1, 0);
			this._choicesLayout.Location = new System.Drawing.Point(16, 73);
			this._choicesLayout.Name = "_choicesLayout";
			this._choicesLayout.RowCount = 3;
			this._choicesLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
			this._choicesLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			this._choicesLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			this._choicesLayout.Size = new System.Drawing.Size(536, 100);
			this._choicesLayout.TabIndex = 11;
			// 
			// m_radioFRAME
			// 
			this.m_radioFRAME.AutoSize = true;
			this.m_radioFRAME.Checked = true;
			this.m_radioFRAME.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.m_radioFRAME.Location = new System.Drawing.Point(3, 3);
			this.m_radioFRAME.Name = "m_radioFRAME";
			this.m_radioFRAME.Size = new System.Drawing.Size(14, 13);
			this.m_radioFRAME.TabIndex = 2;
			this.m_radioFRAME.TabStop = true;
			this.m_radioFRAME.UseVisualStyleBackColor = true;
			this.m_radioFRAME.CheckedChanged += new System.EventHandler(this.Frame_CheckedChanged);
			// 
			// customDescription
			// 
			this.customDescription.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.customDescription.Dock = System.Windows.Forms.DockStyle.Fill;
			this.customDescription.Location = new System.Drawing.Point(37, 66);
			this.customDescription.Multiline = true;
			this.customDescription.Name = "customDescription";
			this.customDescription.ReadOnly = true;
			this.customDescription.Size = new System.Drawing.Size(496, 31);
			this.customDescription.TabIndex = 9;
			this.customDescription.TabStop = false;
			this.customDescription.Text = "Create my own set of anthropology categories (start with an empty list)";
			this.customDescription.MouseClick += new System.Windows.Forms.MouseEventHandler(this.CustomDescriptionClick);
			// 
			// ocmDescription
			// 
			this.ocmDescription.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.ocmDescription.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ocmDescription.Location = new System.Drawing.Point(37, 41);
			this.ocmDescription.Multiline = true;
			this.ocmDescription.Name = "ocmDescription";
			this.ocmDescription.ReadOnly = true;
			this.ocmDescription.Size = new System.Drawing.Size(496, 19);
			this.ocmDescription.TabIndex = 10;
			this.ocmDescription.TabStop = false;
			this.ocmDescription.Text = "Standard OCM anthropology categories";
			this.ocmDescription.MouseClick += new System.Windows.Forms.MouseEventHandler(this.OcmDescriptionClick);
			// 
			// m_radioOCM
			// 
			this.m_radioOCM.AutoSize = true;
			this.m_radioOCM.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.m_radioOCM.Location = new System.Drawing.Point(3, 41);
			this.m_radioOCM.Name = "m_radioOCM";
			this.m_radioOCM.Size = new System.Drawing.Size(14, 13);
			this.m_radioOCM.TabIndex = 3;
			this.m_radioOCM.TabStop = true;
			this.m_radioOCM.UseVisualStyleBackColor = true;
			this.m_radioOCM.CheckedChanged += new System.EventHandler(this.OCM_CheckedChanged);
			// 
			// m_radioCustom
			// 
			this.m_radioCustom.AutoSize = true;
			this.m_radioCustom.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.m_radioCustom.Location = new System.Drawing.Point(3, 66);
			this.m_radioCustom.Name = "m_radioCustom";
			this.m_radioCustom.Size = new System.Drawing.Size(14, 13);
			this.m_radioCustom.TabIndex = 4;
			this.m_radioCustom.TabStop = true;
			this.m_radioCustom.UseVisualStyleBackColor = true;
			this.m_radioCustom.CheckedChanged += new System.EventHandler(this.Custom_CheckedChanged);
			// 
			// frameDescription
			// 
			this.frameDescription.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.frameDescription.Dock = System.Windows.Forms.DockStyle.Fill;
			this.frameDescription.Location = new System.Drawing.Point(37, 3);
			this.frameDescription.Multiline = true;
			this.frameDescription.Name = "frameDescription";
			this.frameDescription.ReadOnly = true;
			this.frameDescription.Size = new System.Drawing.Size(496, 32);
			this.frameDescription.TabIndex = 11;
			this.frameDescription.TabStop = false;
			this.frameDescription.Text = "Enhanced Outline of Cultural Materials (OCM), which includes additional codes to " +
    "better differentiate social, religious, and ethnomusicology topics (recommended)" +
    "";
			this.frameDescription.MouseClick += new System.Windows.Forms.MouseEventHandler(this.FrameDescriptionClick);
			// 
			// FwChooseAnthroListCtrl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_lblAnthroHeader);
			this.Controls.Add(this.m_tbDescription);
			this.Controls.Add(this._choicesLayout);
			this.Name = "FwChooseAnthroListCtrl";
			this.Size = new System.Drawing.Size(566, 259);
			this._choicesLayout.ResumeLayout(false);
			this._choicesLayout.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label m_lblAnthroHeader;
		private System.Windows.Forms.TextBox m_tbDescription;
		private System.Windows.Forms.TableLayoutPanel _choicesLayout;
		private System.Windows.Forms.RadioButton m_radioFRAME;
		private System.Windows.Forms.TextBox customDescription;
		private System.Windows.Forms.TextBox ocmDescription;
		private System.Windows.Forms.RadioButton m_radioOCM;
		private System.Windows.Forms.RadioButton m_radioCustom;
		private System.Windows.Forms.TextBox frameDescription;
	}
}
