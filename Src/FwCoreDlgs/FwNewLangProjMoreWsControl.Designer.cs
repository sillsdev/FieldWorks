namespace SIL.FieldWorks.FwCoreDlgs
{
	partial class FwNewLangProjMoreWsControl
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwNewLangProjMoreWsControl));
			this.m_lblWsTypeHeader = new System.Windows.Forms.Label();
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.m_tbDescription = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// m_lblWsTypeHeader
			// 
			resources.ApplyResources(this.m_lblWsTypeHeader, "m_lblWsTypeHeader");
			this.m_lblWsTypeHeader.Name = "m_lblWsTypeHeader";
			// 
			// button1
			// 
			resources.ApplyResources(this.button1, "button1");
			this.button1.Name = "button1";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.ConfigureVernacularClick);
			// 
			// button2
			// 
			resources.ApplyResources(this.button2, "button2");
			this.button2.Name = "button2";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler(this.ConfigureAnalysisClick);
			// 
			// m_tbDescription
			// 
			this.m_tbDescription.BorderStyle = System.Windows.Forms.BorderStyle.None;
			resources.ApplyResources(this.m_tbDescription, "m_tbDescription");
			this.m_tbDescription.Name = "m_tbDescription";
			this.m_tbDescription.ReadOnly = true;
			this.m_tbDescription.TabStop = false;
			// 
			// FwNewLangProjMoreWsControl
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_tbDescription);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.m_lblWsTypeHeader);
			this.Name = "FwNewLangProjMoreWsControl";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.Label m_lblWsTypeHeader;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.TextBox m_tbDescription;
	}
}
