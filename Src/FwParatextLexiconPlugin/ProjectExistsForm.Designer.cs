namespace SIL.FieldWorks.ParatextLexiconPlugin
{
    partial class ProjectExistsForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProjectExistsForm));
			this.btnOverwrite = new System.Windows.Forms.Button();
			this.btnRename = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// btnOverwrite
			// 
			resources.ApplyResources(this.btnOverwrite, "btnOverwrite");
			this.btnOverwrite.Name = "btnOverwrite";
			this.btnOverwrite.UseVisualStyleBackColor = true;
			this.btnOverwrite.Click += new System.EventHandler(this.btnOverwrite_Click);
			// 
			// btnRename
			// 
			resources.ApplyResources(this.btnRename, "btnRename");
			this.btnRename.Name = "btnRename";
			this.btnRename.UseVisualStyleBackColor = true;
			this.btnRename.Click += new System.EventHandler(this.btnRename_Click);
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// ProjectExistsForm
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.label1);
			this.Controls.Add(this.btnRename);
			this.Controls.Add(this.btnOverwrite);
			this.Name = "ProjectExistsForm";
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnOverwrite;
        private System.Windows.Forms.Button btnRename;
        private System.Windows.Forms.Label label1;
    }
}