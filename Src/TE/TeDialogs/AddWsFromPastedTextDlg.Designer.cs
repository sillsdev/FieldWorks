namespace SIL.FieldWorks.TE
{
	partial class AddWsFromPastedTextDlg
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
			System.Windows.Forms.Panel panel2;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddWsFromPastedTextDlg));
			this.rdoNeverAdd = new System.Windows.Forms.RadioButton();
			this.rdoAlwaysAsk = new System.Windows.Forms.RadioButton();
			this.lblDescription = new System.Windows.Forms.Label();
			this.lblWritingSystems = new System.Windows.Forms.Label();
			this.rdoUseDest = new System.Windows.Forms.RadioButton();
			this.rdoAddWs = new System.Windows.Forms.RadioButton();
			this.btnOk = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnHelp = new System.Windows.Forms.Button();
			this.panelOptions = new System.Windows.Forms.Panel();
			panel2 = new System.Windows.Forms.Panel();
			panel2.SuspendLayout();
			this.panelOptions.SuspendLayout();
			this.SuspendLayout();
			//
			// panel2
			//
			panel2.Controls.Add(this.rdoNeverAdd);
			panel2.Controls.Add(this.rdoAlwaysAsk);
			resources.ApplyResources(panel2, "panel2");
			panel2.Name = "panel2";
			//
			// rdoNeverAdd
			//
			resources.ApplyResources(this.rdoNeverAdd, "rdoNeverAdd");
			this.rdoNeverAdd.Name = "rdoNeverAdd";
			this.rdoNeverAdd.UseVisualStyleBackColor = true;
			//
			// rdoAlwaysAsk
			//
			resources.ApplyResources(this.rdoAlwaysAsk, "rdoAlwaysAsk");
			this.rdoAlwaysAsk.Checked = true;
			this.rdoAlwaysAsk.Name = "rdoAlwaysAsk";
			this.rdoAlwaysAsk.TabStop = true;
			this.rdoAlwaysAsk.UseVisualStyleBackColor = true;
			//
			// lblDescription
			//
			resources.ApplyResources(this.lblDescription, "lblDescription");
			this.lblDescription.Name = "lblDescription";
			//
			// lblWritingSystems
			//
			resources.ApplyResources(this.lblWritingSystems, "lblWritingSystems");
			this.lblWritingSystems.Name = "lblWritingSystems";
			//
			// rdoUseDest
			//
			resources.ApplyResources(this.rdoUseDest, "rdoUseDest");
			this.rdoUseDest.Checked = true;
			this.rdoUseDest.Name = "rdoUseDest";
			this.rdoUseDest.TabStop = true;
			this.rdoUseDest.UseVisualStyleBackColor = true;
			this.rdoUseDest.CheckedChanged += new System.EventHandler(this.rdoUseDestOrAddWs_CheckedChanged);
			//
			// rdoAddWs
			//
			resources.ApplyResources(this.rdoAddWs, "rdoAddWs");
			this.rdoAddWs.Name = "rdoAddWs";
			this.rdoAddWs.UseVisualStyleBackColor = true;
			this.rdoAddWs.CheckedChanged += new System.EventHandler(this.rdoUseDestOrAddWs_CheckedChanged);
			//
			// btnOk
			//
			resources.ApplyResources(this.btnOk, "btnOk");
			this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOk.Name = "btnOk";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
			//
			// btnCancel
			//
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			//
			// btnHelp
			//
			resources.ApplyResources(this.btnHelp, "btnHelp");
			this.btnHelp.Name = "btnHelp";
			this.btnHelp.UseVisualStyleBackColor = true;
			this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// panelOptions
			//
			resources.ApplyResources(this.panelOptions, "panelOptions");
			this.panelOptions.Controls.Add(panel2);
			this.panelOptions.Controls.Add(this.rdoUseDest);
			this.panelOptions.Controls.Add(this.btnHelp);
			this.panelOptions.Controls.Add(this.btnCancel);
			this.panelOptions.Controls.Add(this.rdoAddWs);
			this.panelOptions.Controls.Add(this.btnOk);
			this.panelOptions.Name = "panelOptions";
			//
			// AddWsFromPastedTextDlg
			//
			this.AcceptButton = this.btnOk;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ControlBox = false;
			this.Controls.Add(this.panelOptions);
			this.Controls.Add(this.lblWritingSystems);
			this.Controls.Add(this.lblDescription);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "AddWsFromPastedTextDlg";
			this.ShowIcon = false;
			panel2.ResumeLayout(false);
			panel2.PerformLayout();
			this.panelOptions.ResumeLayout(false);
			this.panelOptions.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label lblDescription;
		private System.Windows.Forms.Label lblWritingSystems;
		private System.Windows.Forms.RadioButton rdoUseDest;
		private System.Windows.Forms.RadioButton rdoNeverAdd;
		private System.Windows.Forms.RadioButton rdoAlwaysAsk;
		private System.Windows.Forms.RadioButton rdoAddWs;
		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnHelp;
		private System.Windows.Forms.Panel panelOptions;
	}
}