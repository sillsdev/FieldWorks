namespace SIL.AllomorphGenerator
{
    partial class EditReplaceOpForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditReplaceOpForm));
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOK = new System.Windows.Forms.Button();
			this.lbReplace = new System.Windows.Forms.Label();
			this.cbRegEx = new System.Windows.Forms.CheckBox();
			this.lbTo = new System.Windows.Forms.Label();
			this.lbVarieties = new System.Windows.Forms.Label();
			this.lbName = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.tbDescription = new System.Windows.Forms.TextBox();
			this.tbName = new System.Windows.Forms.TextBox();
			this.clbWritingSystems = new System.Windows.Forms.CheckedListBox();
			this.SuspendLayout();
			// 
			// btnCancel
			// 
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			// 
			// btnOK
			// 
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(this.btnOK, "btnOK");
			this.btnOK.Name = "btnOK";
			this.btnOK.UseVisualStyleBackColor = true;
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			// 
			// lbReplace
			// 
			resources.ApplyResources(this.lbReplace, "lbReplace");
			this.lbReplace.Name = "lbReplace";
			// 
			// cbRegEx
			// 
			resources.ApplyResources(this.cbRegEx, "cbRegEx");
			this.cbRegEx.Name = "cbRegEx";
			this.cbRegEx.UseVisualStyleBackColor = true;
			this.cbRegEx.CheckedChanged += new System.EventHandler(this.cbRegEx_CheckedChanged);
			// 
			// lbTo
			// 
			resources.ApplyResources(this.lbTo, "lbTo");
			this.lbTo.Name = "lbTo";
			// 
			// lbVarieties
			// 
			resources.ApplyResources(this.lbVarieties, "lbVarieties");
			this.lbVarieties.Name = "lbVarieties";
			// 
			// lbName
			// 
			resources.ApplyResources(this.lbName, "lbName");
			this.lbName.Name = "lbName";
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// tbDescription
			// 
			resources.ApplyResources(this.tbDescription, "tbDescription");
			this.tbDescription.Name = "tbDescription";
			// 
			// tbName
			// 
			resources.ApplyResources(this.tbName, "tbName");
			this.tbName.Name = "tbName";
			// 
			// clbWritingSystems
			// 
			this.clbWritingSystems.FormattingEnabled = true;
			resources.ApplyResources(this.clbWritingSystems, "clbWritingSystems");
			this.clbWritingSystems.Name = "clbWritingSystems";
			// 
			// EditReplaceOpForm
			// 
			this.AcceptButton = this.btnOK;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.clbWritingSystems);
			this.Controls.Add(this.tbName);
			this.Controls.Add(this.tbDescription);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.lbName);
			this.Controls.Add(this.lbVarieties);
			this.Controls.Add(this.lbTo);
			this.Controls.Add(this.cbRegEx);
			this.Controls.Add(this.lbReplace);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.btnCancel);
			this.Name = "EditReplaceOpForm";
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Label lbReplace;
        private System.Windows.Forms.CheckBox cbRegEx;
        private System.Windows.Forms.Label lbTo;
        private System.Windows.Forms.Label lbVarieties;
        private System.Windows.Forms.Label lbName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbDescription;
        private System.Windows.Forms.TextBox tbName;
        private System.Windows.Forms.CheckedListBox clbWritingSystems;
    }
}