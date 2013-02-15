using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Reflection;
using System.Collections;
using System.ComponentModel;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;

using SIL.Utils;

namespace SIL.Utils
{
	/// <summary>
	/// Summary description for UsageEmailDialog.
	/// </summary>
	public class UsageEmailDialog : Form, IFWDisposable
	{
		private TabControl tabControl1;
		private TabPage tabPage1;
		private PictureBox pictureBox1;
		private RichTextBox richTextBox2;
		private Button btnSend;
		private LinkLabel btnNope;

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
			AccessibleName = GetType().Name;
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
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
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
		public string EmailAddress
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
		public string EmailSubject
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
		public string Body
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
		public string TopLineText
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
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "TODO-Linux: LinkLabel.TabStop is missing from Mono")]
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

		private void btnSend_Click(object sender, System.EventArgs e)
		{
			try
			{
				string body = m_emailBody.Replace(System.Environment.NewLine, "%0A").Replace("\"", "%22").Replace("&", "%26");

				using (Process p = new Process())
				{
					p.StartInfo.FileName = String.Format("mailto:{0}?subject={1}&body={2}", m_emailAddress, m_emailSubject, body);
					p.Start();
				}
			}
			catch(Exception)
			{
				//swallow it
			}
			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		private void btnNope_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			this.DialogResult = DialogResult.No;
			this.Close();
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// call this each time the application is launched if you have launch count-based reporting
		/// </summary>
		/// <param name="applicationKey">The application registry key.</param>
		/// ------------------------------------------------------------------------------------
		public static void IncrementLaunchCount(RegistryKey applicationKey)
		{
			int launchCount = int.Parse((string)applicationKey.GetValue("launches", "0")) + 1;
			applicationKey.SetValue("launches", launchCount.ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// used for testing purposes
		/// </summary>
		/// <param name="applicationKey">The application registry key.</param>
		/// ------------------------------------------------------------------------------------
		public static void ClearLaunchCount(RegistryKey applicationKey)
		{
			applicationKey.SetValue("launches", "0");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// if you call this every time the application starts, it will send reports on the
		/// specified launch number.  It will get version number and name out of the application.
		/// </summary>
		/// <param name="applicationName">Name of the application.</param>
		/// <param name="applicationKey">The application registry key.</param>
		/// <param name="emailAddress">The e-mail address.</param>
		/// <param name="topMessage">The message at the top of the e-mail.</param>
		/// <param name="addStats">True to add crash and application runtime statistics to the
		/// report.</param>
		/// <param name="launchNumber">The needed launch count to show the dialog and ask for
		/// an e-mail.</param>
		/// ------------------------------------------------------------------------------------
		public static void DoTrivialUsageReport(string applicationName, RegistryKey applicationKey,
			string emailAddress, string topMessage, bool addStats, int launchNumber)
		{
			DoTrivialUsageReport(applicationName, applicationKey, emailAddress, topMessage,
				addStats, launchNumber, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// if you call this every time the application starts, it will send reports on the
		/// specified launch number.  It will get version number and name out of the application.
		/// </summary>
		/// <param name="applicationName">Name of the application.</param>
		/// <param name="applicationKey">The application registry key.</param>
		/// <param name="emailAddress">The e-mail address.</param>
		/// <param name="topMessage">The message at the top of the e-mail.</param>
		/// <param name="addStats">True to add crash and application runtime statistics to the
		/// report.</param>
		/// <param name="launchNumber">The needed launch count to show the dialog and ask for
		/// an e-mail.</param>
		/// <param name="assembly">The assembly to use for getting version information (can be
		/// <c>null</c>).</param>
		/// ------------------------------------------------------------------------------------
		public static void DoTrivialUsageReport(string applicationName, RegistryKey applicationKey,
			string emailAddress, string topMessage, bool addStats, int launchNumber, Assembly assembly)
		{
			int launchCount = int.Parse((string)applicationKey.GetValue("launches", "0"));
			if (launchNumber == launchCount)
			{
				// Set the Application label to the name of the app
				if (assembly == null)
					assembly = Assembly.GetEntryAssembly();
				string version = Application.ProductVersion;
				if (assembly != null)
				{
					object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
					version = (attributes != null && attributes.Length > 0) ?
						((AssemblyFileVersionAttribute)attributes[0]).Version : Application.ProductVersion;
				}

				using (UsageEmailDialog d = new UsageEmailDialog())
				{
					d.TopLineText = topMessage;
					d.EmailAddress = emailAddress;
					d.EmailSubject = string.Format("{0} {1} Report {2} Launches", applicationName, version, launchCount);
					StringBuilder bldr = new StringBuilder();
					bldr.AppendFormat("<report app='{0}' version='{1}' linux='{2}'>", applicationName,
						version, MiscUtils.IsUnix);
					bldr.AppendFormat("<stat type='launches' value='{0}'/>", launchCount);
					if (launchCount > 1)
					{
						int val = (int)applicationKey.GetValue("NumberOfSeriousCrashes", 0);
						bldr.AppendFormat("<stat type='NumberOfSeriousCrashes' value='{0}'/>", val);
						val = (int)applicationKey.GetValue("NumberOfAnnoyingCrashes", 0);
						bldr.AppendFormat("<stat type='NumberOfAnnoyingCrashes' value='{0}'/>", val);
						int csec = (int)applicationKey.GetValue("TotalAppRuntime", 0);
						int cmin = csec / 60;
						string sRuntime = String.Format("{0}:{1:d2}:{2:d2}",
							cmin / 60, cmin % 60, csec % 60);
						bldr.AppendFormat("<stat type='TotalAppRuntime' value='{0}'/>", sRuntime);
					}
					bldr.AppendFormat("</report>");
					d.Body = bldr.ToString();
					d.ShowDialog();
				}
			}
		}
	}
}
