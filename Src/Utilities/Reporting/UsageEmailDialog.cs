using System;
using System.Drawing;
using System.Reflection;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;

using SIL.FieldWorks.Common.Utils;

namespace SIL.Utils
{
	/// <summary>
	/// Summary description for UsageEmailDialog.
	/// </summary>
	public class UsageEmailDialog : Form, IFWDisposable
	{
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.RichTextBox richTextBox2;
		private System.Windows.Forms.Button btnSend;
		private System.Windows.Forms.LinkLabel btnNope;

		/// <summary></summary>
		protected string m_emailAddress= "";
		/// <summary></summary>
		protected string m_emailBody= "";
		/// <summary></summary>
		protected string m_emailSubject= "Automated Usage Report";
		private System.Windows.Forms.RichTextBox m_topLineText;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		private UsageEmailDialog()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		/// <summary>
		///
		/// </summary>
		public  string EmailAddress
		{
			set
			{
				CheckDisposed();
				m_emailAddress= value;
			}
			get
			{
				CheckDisposed();
				return m_emailAddress;
			}
		}
		/// <summary>
		/// the  e-mail subject
		/// </summary>
		public  string EmailSubject
		{
			set
			{
				CheckDisposed();
				m_emailSubject = value;
			}
			get
			{
				CheckDisposed();
				return m_emailSubject;
			}
		}
		/// <summary>
		///
		/// </summary>
		public  string Body
		{
			set
			{
				CheckDisposed();
				m_emailBody = value;
			}
			get
			{
				CheckDisposed();
				return m_emailBody;
			}
		}
		/// <summary>
		///
		/// </summary>
		public  string TopLineText
		{
			set
			{
				CheckDisposed();
				 m_topLineText.Text = value;
			}
			get
			{
				CheckDisposed();
				return m_topLineText.Text;
			}
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UsageEmailDialog));
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.m_topLineText = new System.Windows.Forms.RichTextBox();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.richTextBox2 = new System.Windows.Forms.RichTextBox();
			this.btnSend = new System.Windows.Forms.Button();
			this.btnNope = new System.Windows.Forms.LinkLabel();
			this.tabControl1.SuspendLayout();
			this.tabPage1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.SuspendLayout();
			//
			// tabControl1
			//
			this.tabControl1.Controls.Add(this.tabPage1);
			resources.ApplyResources(this.tabControl1, "tabControl1");
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			//
			// tabPage1
			//
			this.tabPage1.BackColor = System.Drawing.SystemColors.Window;
			this.tabPage1.Controls.Add(this.m_topLineText);
			this.tabPage1.Controls.Add(this.pictureBox1);
			this.tabPage1.Controls.Add(this.richTextBox2);
			resources.ApplyResources(this.tabPage1, "tabPage1");
			this.tabPage1.Name = "tabPage1";
			//
			// m_topLineText
			//
			this.m_topLineText.BorderStyle = System.Windows.Forms.BorderStyle.None;
			resources.ApplyResources(this.m_topLineText, "m_topLineText");
			this.m_topLineText.Name = "m_topLineText";
			this.m_topLineText.TextChanged += new System.EventHandler(this.richTextBox1_TextChanged);
			//
			// pictureBox1
			//
			resources.ApplyResources(this.pictureBox1, "pictureBox1");
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.TabStop = false;
			//
			// richTextBox2
			//
			this.richTextBox2.BorderStyle = System.Windows.Forms.BorderStyle.None;
			resources.ApplyResources(this.richTextBox2, "richTextBox2");
			this.richTextBox2.Name = "richTextBox2";
			//
			// btnSend
			//
			resources.ApplyResources(this.btnSend, "btnSend");
			this.btnSend.Name = "btnSend";
			this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
			//
			// btnNope
			//
			resources.ApplyResources(this.btnNope, "btnNope");
			this.btnNope.Name = "btnNope";
			this.btnNope.TabStop = true;
			this.btnNope.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.btnNope_LinkClicked);
			//
			// UsageEmailDialog
			//
			this.AcceptButton = this.btnSend;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btnNope;
			this.ControlBox = false;
			this.Controls.Add(this.btnNope);
			this.Controls.Add(this.btnSend);
			this.Controls.Add(this.tabControl1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MinimizeBox = false;
			this.Name = "UsageEmailDialog";
			this.TopMost = true;
			this.tabControl1.ResumeLayout(false);
			this.tabPage1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		private void richTextBox1_TextChanged(object sender, System.EventArgs e)
		{
		}

		private void btnSend_Click(object sender, System.EventArgs e)
		{
			try
			{
				string body = m_emailBody.Replace(System.Environment.NewLine, "%0A").Replace("\"", "%22").Replace("&", "%26");

				System.Diagnostics.Process p = new Process();
				p.StartInfo.FileName =String.Format("mailto:{0}?subject={1}&body={2}", m_emailAddress, m_emailSubject, body);
				p.Start();
			}
			catch(Exception)
			{
				//swallow it
			}
			this.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.Close();
		}

		private void btnNope_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
		{
			this.DialogResult = System.Windows.Forms.DialogResult.No;
			this.Close();
		}


		/// <summary>
		/// call this each time the application is launched if you have launch count-based reporting
		/// </summary>
		public static void IncrementLaunchCount()
		{
			int launchCount = 1 + int.Parse(RegistryAccess.GetStringRegistryValue("launches","0"));
			RegistryAccess.SetStringRegistryValue("launches",launchCount.ToString());
		}

		/// <summary>
		/// used for testing purposes
		/// </summary>
		public static void ClearLaunchCount()
		{
			RegistryAccess.SetStringRegistryValue("launches","0");
		}


		/// <summary>
		/// if you call this every time the application starts, it will send reports on those launches
		/// (e.g. {1, 10}) that are listed in the launches parameter.  It will get version number and name out of the application.
		/// </summary>
		public static void DoTrivialUsageReport(string emailAddress, string topMessage, int[] launches)
		{
			int launchCount = int.Parse(RegistryAccess.GetStringRegistryValue("launches","0"));
			foreach(int launch in launches)
			{
				if (launch == launchCount)
				{
					// Set the Application label to the name of the app
					Assembly assembly = Assembly.GetEntryAssembly();
					string version = Application.ProductVersion;
					if (assembly != null)
					{
						object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
						version = (attributes != null && attributes.Length > 0) ?
							((AssemblyFileVersionAttribute)attributes[0]).Version : Application.ProductVersion;
					}

					using (UsageEmailDialog d = new SIL.Utils.UsageEmailDialog())
					{
						d.TopLineText = topMessage;
						d.EmailAddress = emailAddress;
						d.EmailSubject=string.Format("{0} {1} Report {2} Launches", Application.ProductName, version, launchCount);
						System.Text.StringBuilder bldr = new System.Text.StringBuilder();
						bldr.AppendFormat("<report app='{0}' version='{1}'>", Application.ProductName, version);
						bldr.AppendFormat("<stat type='launches' value='{0}'/>", launchCount);
						if (launchCount > 1)
						{
							int val = RegistryAccess.GetIntRegistryValue("NumberOfSeriousCrashes", 0);
							bldr.AppendFormat("<stat type='NumberOfSeriousCrashes' value='{0}'/>", val);
							val = RegistryAccess.GetIntRegistryValue("NumberOfAnnoyingCrashes", 0);
							bldr.AppendFormat("<stat type='NumberOfAnnoyingCrashes' value='{0}'/>", val);
							int csec = RegistryAccess.GetIntRegistryValue("TotalAppRuntime", 0);
							int cmin = csec / 60;
							string sRuntime = String.Format("{0}:{1:d2}:{2:d2}",
								cmin / 60, cmin % 60, csec % 60);
							bldr.AppendFormat("<stat type='TotalAppRuntime' value='{0}'/>", sRuntime);
						}
						bldr.AppendFormat("</report>");
						d.Body = bldr.ToString();
						d.ShowDialog();
					}
					break;
				}
			}
		}

		/// <summary>
		/// A class for managing registry access.
		/// </summary>
		public class RegistryAccess
		{
			private const string SOFTWARE_KEY = "Software";
//			private static string s_company;
//			private static string s_application;
//
//			static Application App
//			{
//				set
//				{
//					s_company = Application.CompanyName;
//
//				}
//			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Method for retrieving a Registry Value.
			/// </summary>
			/// <param name="key">The key.</param>
			/// <param name="defaultValue">The default value.</param>
			/// <returns></returns>
			/// --------------------------------------------------------------------------------
			static public string GetStringRegistryValue(string key, string defaultValue)
			{
				return (string)GetRegistryValue(key, defaultValue);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Method for retrieving a Registry Value.
			/// </summary>
			/// <param name="key">The key.</param>
			/// <param name="defaultValue">The default value.</param>
			/// <returns></returns>
			/// --------------------------------------------------------------------------------
			static public int GetIntRegistryValue(string key, int defaultValue)
			{
				return (int)GetRegistryValue(key, defaultValue);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Method for retrieving a Registry Value.
			/// </summary>
			/// <param name="key">The key.</param>
			/// <param name="defaultValue">The default value.</param>
			/// <returns></returns>
			/// --------------------------------------------------------------------------------
			static private object GetRegistryValue(string key, object defaultValue)
			{
				RegistryKey rkCompany;
				RegistryKey rkApplication;
				RegistryKey rkFieldWorks;

				// The generic Company Name is SIL International, but in the registry we want this
				// to use SIL. If we want to keep a generic approach, we probably need another member
				// variable
				// for ShortCompanyName, or something similar.
				//rkCompany = Registry.CurrentUser.OpenSubKey(SOFTWARE_KEY, false).OpenSubKey(Application.CompanyName, false);
				rkCompany = Registry.CurrentUser.OpenSubKey(SOFTWARE_KEY, false).OpenSubKey("SIL", false);
				if (rkCompany != null)
				{
					rkFieldWorks = rkCompany.OpenSubKey("FieldWorks", false);
					if (rkFieldWorks != null)
					{
						rkApplication = rkFieldWorks.OpenSubKey(Application.ProductName, false);
						if (rkApplication != null)
						{
							foreach (string sKey in rkApplication.GetValueNames())
							{
								if (sKey == key)
								{
									return rkApplication.GetValue(sKey);
								}
							}
						}
					}
				}
				return defaultValue;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Method for storing a Registry Value.
			/// </summary>
			/// <param name="key">The key.</param>
			/// <param name="stringValue">The string value.</param>
			/// --------------------------------------------------------------------------------
			static public void SetStringRegistryValue(string key, string stringValue)
			{
				SetRegistryValue(key, stringValue);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Method for storing a Registry Value.
			/// </summary>
			/// <param name="key">The key.</param>
			/// <param name="val">The value.</param>
			/// --------------------------------------------------------------------------------
			static public void SetIntRegistryValue(string key, int val)
			{
				SetRegistryValue(key, val);
			}

			private static void SetRegistryValue(string key, object val)
			{
				RegistryKey rkSoftware;
				RegistryKey rkCompany;
				RegistryKey rkFieldWorks;
				RegistryKey rkApplication;

				rkSoftware = Registry.CurrentUser.OpenSubKey(SOFTWARE_KEY, true);
				// The generic Company Name is SIL International, but in the registry we want this to use
				// SIL. If we want to keep a generic approach, we probably need another member variable
				// for ShortCompanyName, or something similar.
				//rkCompany = rkSoftware.CreateSubKey(Application.CompanyName);
				rkCompany = rkSoftware.CreateSubKey("SIL");
				if (rkCompany != null)
				{
					rkFieldWorks = rkCompany.CreateSubKey("FieldWorks");
					if (rkFieldWorks != null)
					{
						rkApplication = rkFieldWorks.CreateSubKey(Application.ProductName);
						if (rkApplication != null)
						{
							rkApplication.SetValue(key, val);
						}
					}
				}

			}
		}



	}
}
