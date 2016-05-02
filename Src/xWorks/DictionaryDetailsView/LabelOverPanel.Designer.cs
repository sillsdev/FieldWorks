namespace SIL.FieldWorks.XWorks.DictionaryDetailsView
{
	partial class LabelOverPanel
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LabelOverPanel));
			this.panel = new System.Windows.Forms.Panel();
			this.label = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// panel
			// 
			resources.ApplyResources(this.panel, "panel");
			this.panel.Name = "panel";
			// 
			// label
			// 
			resources.ApplyResources(this.label, "label");
			this.label.Name = "label";
			// 
			// LabelOverPanel
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.label);
			this.Controls.Add(this.panel);
			this.Name = "LabelOverPanel";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Panel panel;
		private System.Windows.Forms.Label label;
	}
}
