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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwChooseAnthroListCtrl));
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
			resources.ApplyResources(this.m_lblAnthroHeader, "m_lblAnthroHeader");
			this.m_lblAnthroHeader.Name = "m_lblAnthroHeader";
			// 
			// m_tbDescription
			// 
			this.m_tbDescription.BorderStyle = System.Windows.Forms.BorderStyle.None;
			resources.ApplyResources(this.m_tbDescription, "m_tbDescription");
			this.m_tbDescription.Name = "m_tbDescription";
			this.m_tbDescription.ReadOnly = true;
			this.m_tbDescription.TabStop = false;
			// 
			// _choicesLayout
			// 
			resources.ApplyResources(this._choicesLayout, "_choicesLayout");
			this._choicesLayout.Controls.Add(this.m_radioFRAME, 0, 0);
			this._choicesLayout.Controls.Add(this.customDescription, 1, 2);
			this._choicesLayout.Controls.Add(this.ocmDescription, 1, 1);
			this._choicesLayout.Controls.Add(this.m_radioOCM, 0, 1);
			this._choicesLayout.Controls.Add(this.m_radioCustom, 0, 2);
			this._choicesLayout.Controls.Add(this.frameDescription, 1, 0);
			this._choicesLayout.Name = "_choicesLayout";
			// 
			// m_radioFRAME
			// 
			resources.ApplyResources(this.m_radioFRAME, "m_radioFRAME");
			this.m_radioFRAME.Checked = true;
			this.m_radioFRAME.Name = "m_radioFRAME";
			this.m_radioFRAME.TabStop = true;
			this.m_radioFRAME.UseVisualStyleBackColor = true;
			this.m_radioFRAME.CheckedChanged += new System.EventHandler(this.Frame_CheckedChanged);
			// 
			// customDescription
			// 
			this.customDescription.BorderStyle = System.Windows.Forms.BorderStyle.None;
			resources.ApplyResources(this.customDescription, "customDescription");
			this.customDescription.Name = "customDescription";
			this.customDescription.ReadOnly = true;
			this.customDescription.TabStop = false;
			this.customDescription.MouseClick += new System.Windows.Forms.MouseEventHandler(this.CustomDescriptionClick);
			// 
			// ocmDescription
			// 
			this.ocmDescription.BorderStyle = System.Windows.Forms.BorderStyle.None;
			resources.ApplyResources(this.ocmDescription, "ocmDescription");
			this.ocmDescription.Name = "ocmDescription";
			this.ocmDescription.ReadOnly = true;
			this.ocmDescription.TabStop = false;
			this.ocmDescription.MouseClick += new System.Windows.Forms.MouseEventHandler(this.OcmDescriptionClick);
			// 
			// m_radioOCM
			// 
			resources.ApplyResources(this.m_radioOCM, "m_radioOCM");
			this.m_radioOCM.Name = "m_radioOCM";
			this.m_radioOCM.TabStop = true;
			this.m_radioOCM.UseVisualStyleBackColor = true;
			this.m_radioOCM.CheckedChanged += new System.EventHandler(this.OCM_CheckedChanged);
			// 
			// m_radioCustom
			// 
			resources.ApplyResources(this.m_radioCustom, "m_radioCustom");
			this.m_radioCustom.Name = "m_radioCustom";
			this.m_radioCustom.TabStop = true;
			this.m_radioCustom.UseVisualStyleBackColor = true;
			this.m_radioCustom.CheckedChanged += new System.EventHandler(this.Custom_CheckedChanged);
			// 
			// frameDescription
			// 
			this.frameDescription.BorderStyle = System.Windows.Forms.BorderStyle.None;
			resources.ApplyResources(this.frameDescription, "frameDescription");
			this.frameDescription.Name = "frameDescription";
			this.frameDescription.ReadOnly = true;
			this.frameDescription.TabStop = false;
			this.frameDescription.MouseClick += new System.Windows.Forms.MouseEventHandler(this.FrameDescriptionClick);
			// 
			// FwChooseAnthroListCtrl
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_lblAnthroHeader);
			this.Controls.Add(this.m_tbDescription);
			this.Controls.Add(this._choicesLayout);
			this.Name = "FwChooseAnthroListCtrl";
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
