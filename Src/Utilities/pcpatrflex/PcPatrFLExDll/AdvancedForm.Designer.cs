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
            this.btnCancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCancel.Location = new System.Drawing.Point(672, 392);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(112, 46);
            this.btnCancel.TabIndex = 7;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnOK.Location = new System.Drawing.Point(524, 392);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(112, 46);
            this.btnOK.TabIndex = 6;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // lblMaxAmbiguities
            // 
            this.lblMaxAmbiguities.AutoSize = true;
            this.lblMaxAmbiguities.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMaxAmbiguities.Location = new System.Drawing.Point(40, 78);
            this.lblMaxAmbiguities.Name = "lblMaxAmbiguities";
            this.lblMaxAmbiguities.Size = new System.Drawing.Size(378, 25);
            this.lblMaxAmbiguities.TabIndex = 1;
            this.lblMaxAmbiguities.Text = "Maximum number of ambiguities to output:";
            // 
            // lblTimeLimit
            // 
            this.lblTimeLimit.AutoSize = true;
            this.lblTimeLimit.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTimeLimit.Location = new System.Drawing.Point(40, 139);
            this.lblTimeLimit.Name = "lblTimeLimit";
            this.lblTimeLimit.Size = new System.Drawing.Size(332, 25);
            this.lblTimeLimit.TabIndex = 3;
            this.lblTimeLimit.Text = "Parsing time limit (in whole seconds):";
            // 
            // cbRunIndividually
            // 
            this.cbRunIndividually.AutoSize = true;
            this.cbRunIndividually.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbRunIndividually.Location = new System.Drawing.Point(43, 222);
            this.cbRunIndividually.Name = "cbRunIndividually";
            this.cbRunIndividually.Size = new System.Drawing.Size(426, 29);
            this.cbRunIndividually.TabIndex = 5;
            this.cbRunIndividually.Text = "Run PC-PATR on each sentence individually";
            this.cbRunIndividually.UseVisualStyleBackColor = true;
            this.cbRunIndividually.CheckedChanged += new System.EventHandler(this.cbRunIndividually_CheckedChanged);
            // 
            // tbMaxAmbiguities
            // 
            this.tbMaxAmbiguities.Location = new System.Drawing.Point(434, 79);
            this.tbMaxAmbiguities.Name = "tbMaxAmbiguities";
            this.tbMaxAmbiguities.Size = new System.Drawing.Size(100, 26);
            this.tbMaxAmbiguities.TabIndex = 2;
            this.tbMaxAmbiguities.WordWrap = false;
            this.tbMaxAmbiguities.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbMaxAmbiguities_KeyPress);
            // 
            // tbTimeLimit
            // 
            this.tbTimeLimit.Location = new System.Drawing.Point(434, 141);
            this.tbTimeLimit.Name = "tbTimeLimit";
            this.tbTimeLimit.Size = new System.Drawing.Size(100, 26);
            this.tbTimeLimit.TabIndex = 4;
            this.tbTimeLimit.WordWrap = false;
            this.tbTimeLimit.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbTimeLimit_KeyPress);
            // 
            // AdvancedForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.tbTimeLimit);
            this.Controls.Add(this.tbMaxAmbiguities);
            this.Controls.Add(this.cbRunIndividually);
            this.Controls.Add(this.lblTimeLimit);
            this.Controls.Add(this.lblMaxAmbiguities);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "AdvancedForm";
            this.Text = "Advanced";
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