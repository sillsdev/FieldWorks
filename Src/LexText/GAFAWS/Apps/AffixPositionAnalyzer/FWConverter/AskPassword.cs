using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

	/// <summary>
	/// Summary description for AskPassword.
	/// </summary>
public class AskPassword : System.Windows.Forms.Form
{
	public string	userName		= "";
	public string	userPassword	= "";
	public bool		canceled		= false;

	private System.Windows.Forms.Button btnCancel;
	private System.Windows.Forms.Button btnOK;
	private System.Windows.Forms.TextBox txtPassword;
	private System.Windows.Forms.Label label1;
	private System.Windows.Forms.Label label2;
	private System.Windows.Forms.TextBox txtUserName;
	/// <summary>
	/// Required designer variable.
	/// </summary>
	private System.ComponentModel.Container components = null;

	public AskPassword()
	{
		//
		// Required for Windows Form Designer support
		//
		InitializeComponent();
	}

	/// <summary>
	/// Clean up any resources being used.
	/// </summary>
	protected override void Dispose( bool disposing )
	{
		if( disposing )
		{
			if(components != null)
			{
				components.Dispose();
			}
		}
		base.Dispose( disposing );
	}

	#region Windows Form Designer generated code
	/// <summary>
	/// Required method for Designer support - do not modify
	/// the contents of this method with the code editor.
	/// </summary>
	private void InitializeComponent()
	{
		System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(AskPassword));
		this.btnCancel = new System.Windows.Forms.Button();
		this.btnOK = new System.Windows.Forms.Button();
		this.txtPassword = new System.Windows.Forms.TextBox();
		this.label1 = new System.Windows.Forms.Label();
		this.label2 = new System.Windows.Forms.Label();
		this.txtUserName = new System.Windows.Forms.TextBox();
		this.SuspendLayout();
		//
		// btnCancel
		//
		this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		this.btnCancel.Location = new System.Drawing.Point(128, 64);
		this.btnCancel.Name = "btnCancel";
		this.btnCancel.Size = new System.Drawing.Size(96, 24);
		this.btnCancel.TabIndex = 4;
		this.btnCancel.Text = "Cancel";
		this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
		//
		// btnOK
		//
		this.btnOK.Location = new System.Drawing.Point(8, 64);
		this.btnOK.Name = "btnOK";
		this.btnOK.Size = new System.Drawing.Size(96, 24);
		this.btnOK.TabIndex = 3;
		this.btnOK.Text = "OK";
		this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
		//
		// txtPassword
		//
		this.txtPassword.Location = new System.Drawing.Point(88, 32);
		this.txtPassword.Name = "txtPassword";
		this.txtPassword.PasswordChar = '•';
		this.txtPassword.Size = new System.Drawing.Size(136, 20);
		this.txtPassword.TabIndex = 2;
		this.txtPassword.Text = "";
		this.txtPassword.TextChanged += new System.EventHandler(this.txtPassword_TextChanged);
		//
		// label1
		//
		this.label1.Location = new System.Drawing.Point(8, 8);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(80, 16);
		this.label1.TabIndex = 13;
		this.label1.Text = "User name:";
		this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
		//
		// label2
		//
		this.label2.Location = new System.Drawing.Point(8, 32);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(64, 18);
		this.label2.TabIndex = 14;
		this.label2.Text = "Password:";
		this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
		//
		// txtUserName
		//
		this.txtUserName.Location = new System.Drawing.Point(88, 8);
		this.txtUserName.Name = "txtUserName";
		this.txtUserName.Size = new System.Drawing.Size(136, 20);
		this.txtUserName.TabIndex = 1;
		this.txtUserName.Text = "";
		this.txtUserName.TextChanged += new System.EventHandler(this.txtUserName_TextChanged);
		//
		// AskPassword
		//
		this.AcceptButton = this.btnOK;
		this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
		this.CancelButton = this.btnCancel;
		this.ClientSize = new System.Drawing.Size(232, 93);
		this.Controls.Add(this.txtUserName);
		this.Controls.Add(this.label2);
		this.Controls.Add(this.label1);
		this.Controls.Add(this.btnCancel);
		this.Controls.Add(this.btnOK);
		this.Controls.Add(this.txtPassword);
		this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
		this.MaximizeBox = false;
		this.MaximumSize = new System.Drawing.Size(240, 120);
		this.MinimizeBox = false;
		this.MinimumSize = new System.Drawing.Size(240, 120);
		this.Name = "AskPassword";
		this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		this.Text = "Please provide user name and password";
		this.ResumeLayout(false);

	}
	#endregion

	private void btnOK_Click(object sender, System.EventArgs e)
	{
		canceled = false;
		this.Close();
	}

	private void txtUserName_TextChanged(object sender, System.EventArgs e)
	{
		userName = txtUserName.Text.Trim();
	}

	private void txtPassword_TextChanged(object sender, System.EventArgs e)
	{
		userPassword = txtPassword.Text.Trim();
	}

	private void btnCancel_Click(object sender, System.EventArgs e)
	{
		canceled = true;
		this.Close();
	}
}
