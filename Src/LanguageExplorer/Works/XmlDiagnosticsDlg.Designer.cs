namespace LanguageExplorer.Works
{
	partial class XmlDiagnosticsDlg
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ****** ");
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(XmlDiagnosticsDlg));
			this.m_tb_guid = new System.Windows.Forms.TextBox();
			this.m_lbl_guid = new System.Windows.Forms.Label();
			this.m_lbl_xml = new System.Windows.Forms.Label();
			this.m_tb_xml = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// m_tb_guid
			// 
			resources.ApplyResources(this.m_tb_guid, "m_tb_guid");
			this.m_tb_guid.Name = "m_tb_guid";
			// 
			// m_lbl_guid
			// 
			resources.ApplyResources(this.m_lbl_guid, "m_lbl_guid");
			this.m_lbl_guid.Name = "m_lbl_guid";
			// 
			// m_lbl_xml
			// 
			resources.ApplyResources(this.m_lbl_xml, "m_lbl_xml");
			this.m_lbl_xml.Name = "m_lbl_xml";
			// 
			// m_tb_xml
			// 
			resources.ApplyResources(this.m_tb_xml, "m_tb_xml");
			this.m_tb_xml.Name = "m_tb_xml";
			// 
			// XmlDiagnosticsDlg
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_tb_guid);
			this.Controls.Add(this.m_tb_xml);
			this.Controls.Add(this.m_lbl_xml);
			this.Controls.Add(this.m_lbl_guid);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "XmlDiagnosticsDlg";
			this.ShowIcon = false;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox m_tb_guid;
		private System.Windows.Forms.Label m_lbl_guid;
		private System.Windows.Forms.Label m_lbl_xml;
		private System.Windows.Forms.TextBox m_tb_xml;
	}
}