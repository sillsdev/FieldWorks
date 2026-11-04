namespace SIL.PcPatrFLEx
{
    partial class AdvancedForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AdvancedForm));
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOK = new System.Windows.Forms.Button();
			this.lblMaxAmbiguities = new System.Windows.Forms.Label();
			this.lblTimeLimit = new System.Windows.Forms.Label();
			this.cbRunIndividually = new System.Windows.Forms.CheckBox();
			this.tbMaxAmbiguities = new System.Windows.Forms.TextBox();
			this.tbTimeLimit = new System.Windows.Forms.TextBox();
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
			// lblMaxAmbiguities
			// 
			resources.ApplyResources(this.lblMaxAmbiguities, "lblMaxAmbiguities");
			this.lblMaxAmbiguities.Name = "lblMaxAmbiguities";
			// 
			// lblTimeLimit
			// 
			resources.ApplyResources(this.lblTimeLimit, "lblTimeLimit");
			this.lblTimeLimit.Name = "lblTimeLimit";
			// 
			// cbRunIndividually
			// 
			resources.ApplyResources(this.cbRunIndividually, "cbRunIndividually");
			this.cbRunIndividually.Name = "cbRunIndividually";
			this.cbRunIndividually.UseVisualStyleBackColor = true;
			this.cbRunIndividually.CheckedChanged += new System.EventHandler(this.cbRunIndividually_CheckedChanged);
			// 
			// tbMaxAmbiguities
			// 
			resources.ApplyResources(this.tbMaxAmbiguities, "tbMaxAmbiguities");
			this.tbMaxAmbiguities.Name = "tbMaxAmbiguities";
			this.tbMaxAmbiguities.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbMaxAmbiguities_KeyPress);
			// 
			// tbTimeLimit
			// 
			resources.ApplyResources(this.tbTimeLimit, "tbTimeLimit");
			this.tbTimeLimit.Name = "tbTimeLimit";
			this.tbTimeLimit.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbTimeLimit_KeyPress);
			// 
			// AdvancedForm
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.tbTimeLimit);
			this.Controls.Add(this.tbMaxAmbiguities);
			this.Controls.Add(this.cbRunIndividually);
			this.Controls.Add(this.lblTimeLimit);
			this.Controls.Add(this.lblMaxAmbiguities);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.btnCancel);
			this.Name = "AdvancedForm";
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Label lblMaxAmbiguities;
        private System.Windows.Forms.Label lblTimeLimit;
        private System.Windows.Forms.CheckBox cbRunIndividually;
        private System.Windows.Forms.TextBox tbMaxAmbiguities;
        private System.Windows.Forms.TextBox tbTimeLimit;
    }
}