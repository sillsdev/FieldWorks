namespace SIL.FieldWorks.FwCoreDlgs
{
	partial class FwNewProjectProjectNameControl
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
			m_txtName.TextChanged -= ProjectNameTextChanged;
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
			System.Windows.Forms.Label m_lblExplainName;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwNewProjectProjectNameControl));
			this.m_txtName = new System.Windows.Forms.TextBox();
			this.m_lblProjectName = new System.Windows.Forms.Label();
			this._errorImage = new System.Windows.Forms.PictureBox();
			this._projectNameErrorLabel = new System.Windows.Forms.TextBox();
			m_lblExplainName = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this._errorImage)).BeginInit();
			this.SuspendLayout();
			// 
			// m_lblExplainName
			// 
			resources.ApplyResources(m_lblExplainName, "m_lblExplainName");
			m_lblExplainName.Name = "m_lblExplainName";
			// 
			// m_txtName
			// 
			resources.ApplyResources(this.m_txtName, "m_txtName");
			this.m_txtName.Name = "m_txtName";
			// 
			// m_lblProjectName
			// 
			resources.ApplyResources(this.m_lblProjectName, "m_lblProjectName");
			this.m_lblProjectName.Name = "m_lblProjectName";
			// 
			// _errorImage
			// 
			resources.ApplyResources(this._errorImage, "_errorImage");
			this._errorImage.Name = "_errorImage";
			this._errorImage.TabStop = false;
			// 
			// _projectNameErrorLabel
			// 
			this._projectNameErrorLabel.BorderStyle = System.Windows.Forms.BorderStyle.None;
			resources.ApplyResources(this._projectNameErrorLabel, "_projectNameErrorLabel");
			this._projectNameErrorLabel.Name = "_projectNameErrorLabel";
			this._projectNameErrorLabel.ReadOnly = true;
			// 
			// FwNewProjectProjectNameControl
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._projectNameErrorLabel);
			this.Controls.Add(this._errorImage);
			this.Controls.Add(this.m_txtName);
			this.Controls.Add(this.m_lblProjectName);
			this.Controls.Add(m_lblExplainName);
			this.Name = "FwNewProjectProjectNameControl";
			((System.ComponentModel.ISupportInitialize)(this._errorImage)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox m_txtName;
		private System.Windows.Forms.Label m_lblProjectName;
		private System.Windows.Forms.PictureBox _errorImage;
		private System.Windows.Forms.TextBox _projectNameErrorLabel;
	}
}
