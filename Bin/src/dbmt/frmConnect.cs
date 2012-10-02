using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Data;
using System.Data.SqlClient;


namespace dbmt
{
	/// <summary>
	/// Summary description for frmConnect.
	/// </summary>
	public class frmConnect : System.Windows.Forms.Form
	{
		string[,] m_sLastServers = null;
		int m_cServers;

		private enum ServerIndex
		{
			Server = 0,
			Username,

			Lim
		};

		private System.Windows.Forms.Button cmdOK;
		private System.Windows.Forms.Button cmdCancel;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.ComboBox cboServer;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox txtUsername;
		private System.Windows.Forms.TextBox txtPassword;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.RadioButton optWindows;
		private System.Windows.Forms.RadioButton optSqlServer;
		private System.Windows.Forms.Panel pnlSqlServer;
		private System.Windows.Forms.CheckBox chkAutoStart;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public frmConnect()
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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(frmConnect));
			this.cmdOK = new System.Windows.Forms.Button();
			this.cmdCancel = new System.Windows.Forms.Button();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.cboServer = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.label2 = new System.Windows.Forms.Label();
			this.optWindows = new System.Windows.Forms.RadioButton();
			this.optSqlServer = new System.Windows.Forms.RadioButton();
			this.pnlSqlServer = new System.Windows.Forms.Panel();
			this.txtPassword = new System.Windows.Forms.TextBox();
			this.txtUsername = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.chkAutoStart = new System.Windows.Forms.CheckBox();
			this.pnlSqlServer.SuspendLayout();
			this.SuspendLayout();
			//
			// cmdOK
			//
			this.cmdOK.Location = new System.Drawing.Point(144, 208);
			this.cmdOK.Name = "cmdOK";
			this.cmdOK.Size = new System.Drawing.Size(72, 24);
			this.cmdOK.TabIndex = 8;
			this.cmdOK.Text = "&OK";
			this.cmdOK.Click += new System.EventHandler(this.cmdOK_Click);
			//
			// cmdCancel
			//
			this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cmdCancel.Location = new System.Drawing.Point(224, 208);
			this.cmdCancel.Name = "cmdCancel";
			this.cmdCancel.Size = new System.Drawing.Size(72, 24);
			this.cmdCancel.TabIndex = 9;
			this.cmdCancel.Text = "&Cancel";
			this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
			//
			// pictureBox1
			//
			this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
			this.pictureBox1.Location = new System.Drawing.Point(16, 8);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(32, 32);
			this.pictureBox1.TabIndex = 2;
			this.pictureBox1.TabStop = false;
			//
			// cboServer
			//
			this.cboServer.Location = new System.Drawing.Point(128, 8);
			this.cboServer.Name = "cboServer";
			this.cboServer.Size = new System.Drawing.Size(168, 21);
			this.cboServer.TabIndex = 1;
			this.cboServer.SelectedIndexChanged += new System.EventHandler(this.cboServer_SelectedIndexChanged);
			//
			// label1
			//
			this.label1.Location = new System.Drawing.Point(56, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(72, 21);
			this.label1.TabIndex = 0;
			this.label1.Text = "SQL Server:";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// groupBox1
			//
			this.groupBox1.Location = new System.Drawing.Point(8, 56);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(288, 4);
			this.groupBox1.TabIndex = 3;
			this.groupBox1.TabStop = false;
			//
			// label2
			//
			this.label2.Location = new System.Drawing.Point(8, 72);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(80, 16);
			this.label2.TabIndex = 4;
			this.label2.Text = "Connect using:";
			//
			// optWindows
			//
			this.optWindows.Checked = true;
			this.optWindows.Location = new System.Drawing.Point(24, 88);
			this.optWindows.Name = "optWindows";
			this.optWindows.Size = new System.Drawing.Size(160, 16);
			this.optWindows.TabIndex = 5;
			this.optWindows.TabStop = true;
			this.optWindows.Text = "Windows authentication";
			//
			// optSqlServer
			//
			this.optSqlServer.Location = new System.Drawing.Point(24, 112);
			this.optSqlServer.Name = "optSqlServer";
			this.optSqlServer.Size = new System.Drawing.Size(160, 16);
			this.optSqlServer.TabIndex = 6;
			this.optSqlServer.Text = "SQL Server authentication";
			this.optSqlServer.CheckedChanged += new System.EventHandler(this.optSqlServer_CheckedChanged);
			//
			// pnlSqlServer
			//
			this.pnlSqlServer.Controls.Add(this.txtPassword);
			this.pnlSqlServer.Controls.Add(this.txtUsername);
			this.pnlSqlServer.Controls.Add(this.label4);
			this.pnlSqlServer.Controls.Add(this.label3);
			this.pnlSqlServer.Enabled = false;
			this.pnlSqlServer.Location = new System.Drawing.Point(40, 136);
			this.pnlSqlServer.Name = "pnlSqlServer";
			this.pnlSqlServer.Size = new System.Drawing.Size(256, 48);
			this.pnlSqlServer.TabIndex = 9;
			//
			// txtPassword
			//
			this.txtPassword.Location = new System.Drawing.Point(72, 24);
			this.txtPassword.Name = "txtPassword";
			this.txtPassword.Size = new System.Drawing.Size(176, 20);
			this.txtPassword.TabIndex = 3;
			this.txtPassword.Text = "";
			//
			// txtUsername
			//
			this.txtUsername.Location = new System.Drawing.Point(72, 0);
			this.txtUsername.Name = "txtUsername";
			this.txtUsername.Size = new System.Drawing.Size(176, 20);
			this.txtUsername.TabIndex = 1;
			this.txtUsername.Text = "";
			//
			// label4
			//
			this.label4.Location = new System.Drawing.Point(0, 24);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(72, 20);
			this.label4.TabIndex = 2;
			this.label4.Text = "Password:";
			this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// label3
			//
			this.label3.Location = new System.Drawing.Point(0, 0);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(72, 20);
			this.label3.TabIndex = 0;
			this.label3.Text = "Logon name:";
			this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// groupBox2
			//
			this.groupBox2.Location = new System.Drawing.Point(8, 192);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(288, 4);
			this.groupBox2.TabIndex = 7;
			this.groupBox2.TabStop = false;
			//
			// chkAutoStart
			//
			this.chkAutoStart.Location = new System.Drawing.Point(128, 36);
			this.chkAutoStart.Name = "chkAutoStart";
			this.chkAutoStart.Size = new System.Drawing.Size(184, 16);
			this.chkAutoStart.TabIndex = 2;
			this.chkAutoStart.Text = "Start SQL Server if it is stopped";
			//
			// frmConnect
			//
			this.AcceptButton = this.cmdOK;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.CancelButton = this.cmdCancel;
			this.ClientSize = new System.Drawing.Size(306, 239);
			this.Controls.Add(this.chkAutoStart);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.pnlSqlServer);
			this.Controls.Add(this.optSqlServer);
			this.Controls.Add(this.optWindows);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.cboServer);
			this.Controls.Add(this.pictureBox1);
			this.Controls.Add(this.cmdCancel);
			this.Controls.Add(this.cmdOK);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "frmConnect";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Connect to SQL Server";
			this.Load += new System.EventHandler(this.frmConnect_Load);
			this.pnlSqlServer.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		private void cmdCancel_Click(object sender, System.EventArgs e)
		{
			this.Close();
		}

		private void cmdOK_Click(object sender, System.EventArgs e)
		{
			string sConnectionString;
			string sUsername;
			string sServer = cboServer.Text;
			string sDatabase = "";

			RegistryKey oKey = null;
			try
			{
				oKey = Registry.CurrentUser.OpenSubKey(Globals.ksRegKeyRoot + @"\Databases");
				if (oKey != null)
				{
					string sLocalServer = SystemInformation.ComputerName.ToLower();
					sDatabase = oKey.GetValue(sServer.ToLower().Replace(sLocalServer, "."), "").ToString();
					if (sDatabase.Length == 0 && sServer.IndexOf(".") != -1)
					{
						// Try it with the actual name of the machine instead of the "."
						sDatabase = oKey.GetValue(sServer.Replace(".", sLocalServer), "").ToString();
					}
				}
			}
			finally
			{
				if (oKey != null)
					oKey.Close();
			}

			sServer = sServer.Replace(".", SystemInformation.ComputerName);

			if (optWindows.Checked)
			{
				sUsername = SystemInformation.UserDomainName + "\\" + SystemInformation.UserName;
				sConnectionString =
					"Data Source=" + sServer + ";" +
					"Integrated Security=SSPI";
			}
			else
			{
				sUsername = txtUsername.Text;
				sConnectionString =
					"Data Source=" + sServer + ";" +
					"User Id=" + sUsername + ";" +
					"Password=" + txtPassword.Text;
			}

			if (Globals.OpenConnection(this, sUsername, sServer,
				sConnectionString, chkAutoStart.Checked, sDatabase))
			{
				SaveLastServers();
				this.Close();
			}
		}

		private void frmConnect_Load(object sender, System.EventArgs e)
		{
			this.Icon = Globals.MainForm.Icon;

			LoadLastServers();
		}

		private void optSqlServer_CheckedChanged(object sender, System.EventArgs e)
		{
			pnlSqlServer.Enabled = optSqlServer.Checked;
			if (optSqlServer.Checked && this.ActiveControl == optSqlServer)
			{
				txtUsername.Focus();
				txtUsername.SelectAll();
			}
		}

		private void cboServer_SelectedIndexChanged(object sender, EventArgs e)
		{
			string sUser = m_sLastServers[cboServer.SelectedIndex, 1];
			if (sUser.Length == 0)
			{
				optWindows.Checked = true;
			}
			else
			{
				optSqlServer.Checked = true;
				txtUsername.Text = sUser;
			}
		}

		private void LoadLastServers()
		{
			RegistryKey oKey = null;
			m_cServers = 0;

			try
			{
				oKey = Registry.CurrentUser.OpenSubKey(Globals.ksRegKeyRoot + @"\LastServers");

				if (oKey == null)
				{
					// Try to load the settings from Query Analyzer.
					oKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Microsoft SQL Server\80\Tools\Client\PrefServers");
				}
				if (oKey == null)
				{
					cboServer.Text = ".";
					return;
				}

				string sServers = oKey.GetValue("Count", "").ToString();
				try
				{
					m_cServers = Int32.Parse(sServers);
				}
				catch
				{
					m_cServers = 10;
				}

				m_sLastServers = new string[m_cServers, 2];
				for (int iServer = 0; iServer < m_cServers; iServer++)
				{
					string sServer = oKey.GetValue("Server" + iServer, "").ToString();
					if (sServer.Length > 0)
					{
						string sUser = oKey.GetValue("User" + iServer, "").ToString();
						m_sLastServers[iServer, 0] = sServer;
						m_sLastServers[iServer, 1] = sUser;
						cboServer.Items.Add(sServer);
					}
				}

				if (cboServer.Items.Count == 0)
					cboServer.Text = ".";
				else
					cboServer.SelectedIndex = 0;
			}
			finally
			{
				if (oKey != null)
					oKey.Close();
			}
		}

		private void SaveLastServers()
		{
			string sCurServer = cboServer.Text;
			string sCurUser = optSqlServer.Checked ? txtUsername.Text : "";

			RegistryKey oKey = null;
			try
			{
				oKey = Registry.CurrentUser.CreateSubKey(Globals.ksRegKeyRoot + @"\LastServers");

				string sServersToSave = oKey.GetValue("Count", "").ToString();
				int cServersToSave;
				try
				{
					cServersToSave = Int32.Parse(sServersToSave);
				}
				catch
				{
					cServersToSave = 10;
					oKey.SetValue("Count", cServersToSave);
				}

				if (cServersToSave > 0)
				{
					// Save the current server in the first position.
					oKey.SetValue("Server0", sCurServer);
					oKey.SetValue("User0", sCurUser);
				}

				// Save the other servers, up to the maximum number of servers.
				if (cServersToSave > 1)
				{
					int iRegServer = 1;
					for (int iServer = 0; iServer < m_cServers; iServer++)
					{
						string sServer = m_sLastServers[iServer, 0];
						if (sServer != null && sServer.Length > 0 && sServer.ToLower() != sCurServer.ToLower())
						{
							oKey.SetValue("Server" + iRegServer, sServer);
							oKey.SetValue("User" + iRegServer, m_sLastServers[iServer, 1]);
							if (++iRegServer >= cServersToSave)
								break;
						}
					}
				}
			}
			finally
			{
				if (oKey != null)
					oKey.Close();
			}
		}
	}
}
