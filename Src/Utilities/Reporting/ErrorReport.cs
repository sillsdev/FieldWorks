// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004-2007, SIL International. All Rights Reserved.
// <copyright from='2004' to='2007' company='SIL International'>
//		Copyright (c) 2004-2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ErrorReport.cs
// Responsibility:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using Microsoft.Win32;

using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Utils;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for ErrorReporter.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ErrorReporter : Form, IFWDisposable
	{
		#region Member variables

		private TextBox m_details;
		private RadioButton radSelf;
		private TextBox m_notification;
		private TextBox m_reproduce;
		private Label labelAttemptToContinue;
		private Button btnClose;
		private RadioButton radEmail;
		private Label emailLabel;

		/// <summary>The email address that receives error reports</summary>
		protected static string s_emailAddress= null;

		/// <summary>The subject for error report emails</summary>
		protected static string s_emailSubject= "Automated Error Report";

		private bool m_isLethal;

		/// <summary>
		/// a list of name, string value pairs that will be included in the details of the error report.
		/// For example, xWorks would could the name of the database in here.
		/// </summary>
		protected static Dictionary<string, string> s_properties = new Dictionary<string, string>();

		/// <summary></summary>
		protected static bool s_isOkToInteractWithUser = true;
		private static bool s_fIgnoreReport;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// this is protected so that we can have a Singleton
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected ErrorReporter(bool isLethal)
		{
			m_isLethal = isLethal;
		}

		#region IDisposable override

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
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Windows.Forms.Label label2;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ErrorReporter));
			System.Windows.Forms.Label label3;
			this.m_reproduce = new System.Windows.Forms.TextBox();
			this.radEmail = new System.Windows.Forms.RadioButton();
			this.m_details = new System.Windows.Forms.TextBox();
			this.emailLabel = new System.Windows.Forms.Label();
			this.radSelf = new System.Windows.Forms.RadioButton();
			this.btnClose = new System.Windows.Forms.Button();
			this.m_notification = new System.Windows.Forms.TextBox();
			this.labelAttemptToContinue = new System.Windows.Forms.Label();
			label2 = new System.Windows.Forms.Label();
			label3 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// label2
			//
			resources.ApplyResources(label2, "label2");
			label2.Name = "label2";
			//
			// m_reproduce
			//
			this.m_reproduce.AcceptsReturn = true;
			this.m_reproduce.AcceptsTab = true;
			resources.ApplyResources(this.m_reproduce, "m_reproduce");
			this.m_reproduce.Name = "m_reproduce";
			//
			// label3
			//
			resources.ApplyResources(label3, "label3");
			label3.Name = "label3";
			//
			// radEmail
			//
			resources.ApplyResources(this.radEmail, "radEmail");
			this.radEmail.Checked = true;
			this.radEmail.Name = "radEmail";
			this.radEmail.TabStop = true;
			//
			// m_details
			//
			resources.ApplyResources(this.m_details, "m_details");
			this.m_details.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this.m_details.Name = "m_details";
			this.m_details.ReadOnly = true;
			//
			// emailLabel
			//
			resources.ApplyResources(this.emailLabel, "emailLabel");
			this.emailLabel.Name = "emailLabel";
			//
			// radSelf
			//
			resources.ApplyResources(this.radSelf, "radSelf");
			this.radSelf.Name = "radSelf";
			//
			// btnClose
			//
			resources.ApplyResources(this.btnClose, "btnClose");
			this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnClose.Name = "btnClose";
			this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
			//
			// m_notification
			//
			resources.ApplyResources(this.m_notification, "m_notification");
			this.m_notification.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
			this.m_notification.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_notification.ForeColor = System.Drawing.Color.Black;
			this.m_notification.Name = "m_notification";
			this.m_notification.ReadOnly = true;
			//
			// labelAttemptToContinue
			//
			resources.ApplyResources(this.labelAttemptToContinue, "labelAttemptToContinue");
			this.labelAttemptToContinue.ForeColor = System.Drawing.Color.Firebrick;
			this.labelAttemptToContinue.Name = "labelAttemptToContinue";
			//
			// ErrorReporter
			//
			this.AcceptButton = this.btnClose;
			resources.ApplyResources(this, "$this");
			this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
			this.CancelButton = this.btnClose;
			this.ControlBox = false;
			this.Controls.Add(this.m_reproduce);
			this.Controls.Add(this.m_notification);
			this.Controls.Add(this.m_details);
			this.Controls.Add(this.labelAttemptToContinue);
			this.Controls.Add(label2);
			this.Controls.Add(label3);
			this.Controls.Add(this.emailLabel);
			this.Controls.Add(this.radEmail);
			this.Controls.Add(this.radSelf);
			this.Controls.Add(this.btnClose);
			this.KeyPreview = true;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ErrorReporter";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region Static methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// show a dialog or output to the error log, as appropriate.
		/// </summary>
		/// <param name="error">the exception you want to report</param>
		/// ------------------------------------------------------------------------------------
		public static void ReportException(Exception error)
		{
			ReportException(error, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reports the exception.
		/// </summary>
		/// <param name="error">The error.</param>
		/// <param name="parent">The parent.</param>
		/// ------------------------------------------------------------------------------------
		public static void ReportException(Exception error, Form parent)
		{
			ReportException(error, parent, true);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// show a dialog or output to the error log, as appropriate.
		/// </summary>
		/// <param name="error">the exception you want to report</param>
		/// <param name="parent">the parent form that this error belongs to (i.e. the form
		/// show modally on)</param>
		/// <param name="isLethal">set to <c>true</c> if the error is lethal, otherwise
		/// <c>false</c>.</param>
		/// ------------------------------------------------------------------------------------
		public static void ReportException(Exception error, Form parent, bool isLethal)
		{
			// ignore message if we are showing from a previous error
			if (s_fIgnoreReport)
				return;

			// If the error has a message and a help link, then show that error
			if (!string.IsNullOrEmpty(error.HelpLink) && error.HelpLink.IndexOf("::/") > 0 &&
				!string.IsNullOrEmpty(error.Message))
			{
				s_fIgnoreReport = true; // This is presumably a hopelessly fatal error, so we
				// don't want to report any subsequent errors at all.
				// Look for the end of the basic message which will be terminated by two new lines or
				// two CRLF sequences.
				int lengthOfBasicMessage = error.Message.IndexOf("\r\n");
				if (lengthOfBasicMessage <= 0)
					lengthOfBasicMessage = error.Message.IndexOf("\n\n");
				if (lengthOfBasicMessage <= 0)
					lengthOfBasicMessage = error.Message.Length;

				int iSeparatorBetweenFileNameAndTopic = error.HelpLink.IndexOf("::/");
				string sHelpFile = error.HelpLink.Substring(0, iSeparatorBetweenFileNameAndTopic);
				string sHelpTopic = error.HelpLink.Substring(iSeparatorBetweenFileNameAndTopic + 3);

				string caption = ReportingStrings.kstidFieldWorksErrorCaption;
				string appExit = ReportingStrings.kstidFieldWorksErrorExitInfo;
				MessageBox.Show(parent, error.Message.Substring(0, lengthOfBasicMessage) + "\n" + appExit,
					caption, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, 0,
					sHelpFile, HelpNavigator.Topic, sHelpTopic);
				if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
					Clipboard.SetDataObject(error.Message, true);
				else
					Logger.WriteError(error);
				Application.Exit();
			}

			using (ErrorReporter e = new ErrorReporter(isLethal))
			{
				e.HandleError(error, parent);
			}
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///make this false during automated testing
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool OkToInteractWithUser
		{
			set {s_isOkToInteractWithUser = value;}
			get {return s_isOkToInteractWithUser;}
		}

		/// <summary>
		/// set this property if you want the dialog to offer to create an e-mail message.
		/// </summary>
		public static string EmailAddress
		{
			set {s_emailAddress = value;}
			get {return s_emailAddress;}
		}
		/// <summary>
		/// set this property if you want something other than the default e-mail subject
		/// </summary>
		public static string EmailSubject
		{
			set {s_emailSubject = value;}
			get {return s_emailSubject;}
		}
		#endregion

		#region Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void GatherData()
		{
			//m_details.Text += "\r\nTo Reproduce: " + m_reproduce.Text + "\r\n";
			StringBuilder sb = new StringBuilder(m_details.Text.Length + m_reproduce.Text.Length + 50);
			sb.AppendLine("To Reproduce:");
			sb.AppendLine(m_reproduce.Text);
			sb.AppendLine("");
			sb.Append(m_details.Text);
			m_details.Text = sb.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///	add a property that he would like included in any bug reports created by this application.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void AddProperty(string label, string contents)
		{
			s_properties[label] = contents;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="error"></param>
		/// <param name="owner"></param>
		/// ------------------------------------------------------------------------------------
		private void HandleError(Exception error, Form owner)
		{
			CheckDisposed();
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			// These 2 lines can be deleted after the problems with mailto have been resolved.
			this.radEmail.Enabled = false;
			this.radSelf.Checked = true;

			if(s_emailAddress == null)
			{
				this.radEmail.Enabled = false;
				this.radSelf.Checked = true;
			}
			else
			{
				// Add the e-mail address to the dialog.
				emailLabel.Text += ": " + s_emailAddress;
			}

			if (!m_isLethal)
			{
				btnClose.Text = ReportingStrings.ks_Ok;
				this.BackColor = System.Drawing.Color.FromArgb(255, 255, 192);//yellow
				m_notification.BackColor = this.BackColor;
				UpdateCrashCount("NumberOfAnnoyingCrashes");
			}
			else
			{
				UpdateCrashCount("NumberOfSeriousCrashes");
			}
			UpdateAppRuntime();

			StringBuilder detailsText = new StringBuilder();
			Exception innerMostException;
			detailsText.AppendLine(GetHiearchicalExceptionInfo(error, out innerMostException));

			// if the exception had inner exceptions, show the inner-most exception first, since
			// that is usually the one we want the developer to read.
			if (innerMostException != null)
			{
				StringBuilder innerException = new StringBuilder();
				innerException.AppendLine("Inner most exception:");
				innerException.AppendLine(GetExceptionText(innerMostException));
				innerException.AppendLine();
				innerException.AppendLine("Full, hierarchical exception contents:");
				detailsText.Insert(0, innerException.ToString());
			}

			detailsText.AppendLine("Error Reporting Properties:");
			foreach(string label in s_properties.Keys )
				detailsText.AppendLine(label + ": " + s_properties[label]);

			if (innerMostException != null)
				error = innerMostException;
			Logger.WriteEvent("Got exception " + error.GetType().Name);

			detailsText.AppendLine(Logger.LogText);
			Debug.WriteLine(detailsText.ToString());
			m_details.Text = detailsText.ToString();

			if (s_isOkToInteractWithUser)
			{
				s_fIgnoreReport = true;
				ShowDialog(owner);
				s_fIgnoreReport = false;
			}
			else	//the test environment already prohibits dialogs but will save the contents of assertions in some log.
				System.Diagnostics.Debug.Fail(m_details.Text);
		}

		private void UpdateCrashCount(string sPropName)
		{
			int count = UsageEmailDialog.RegistryAccess.GetIntRegistryValue(sPropName, 0) + 1;
			UsageEmailDialog.RegistryAccess.SetIntRegistryValue(sPropName, count);
			s_properties[sPropName] = count.ToString();
		}

		private void UpdateAppRuntime()
		{
			string sStartup = UsageEmailDialog.RegistryAccess.GetStringRegistryValue("LatestAppStartupTime", "");
			int csec = UsageEmailDialog.RegistryAccess.GetIntRegistryValue("TotalAppRuntime", 0);
			int secBeforeCrash = 0;
			long start;
			if (!String.IsNullOrEmpty(sStartup) && long.TryParse(sStartup, out start))
			{
				DateTime started = new DateTime(start);
				DateTime finished = DateTime.Now.ToUniversalTime();
				TimeSpan delta = finished - started;
				secBeforeCrash = (int)delta.TotalSeconds;
				csec += secBeforeCrash;
				UsageEmailDialog.RegistryAccess.SetIntRegistryValue("TotalAppRuntime", csec);
			}
			int cmin = csec / 60;
			s_properties["TotalRuntime"] = String.Format("{0}:{1:d2}:{2:d2}",
				cmin / 60, cmin % 60, csec % 60);
			if (secBeforeCrash > 0)
			{
				int minBeforeCrash = secBeforeCrash / 60;
				s_properties["RuntimeBeforeCrash"] = String.Format("{0}:{1:d2}:{2:d2}",
					minBeforeCrash / 60, minBeforeCrash % 60, secBeforeCrash % 60);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the hiearchical exception info.
		/// </summary>
		/// <param name="error">The error.</param>
		/// <param name="innerMostException">The inner most exception or null if the error is
		/// the inner most exception</param>
		/// <returns>A string containing the text of the specified error</returns>
		/// ------------------------------------------------------------------------------------
		public static string GetHiearchicalExceptionInfo(Exception error,
			out Exception innerMostException)
		{
			innerMostException = error.InnerException;
			string x = GetExceptionText(error);

			if (error.InnerException != null)
			{
				x += "**Inner Exception:\r\n";
				x += GetHiearchicalExceptionInfo(error.InnerException, out innerMostException);
			}
			return x;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="error"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static string GetExceptionText(Exception error)
		{
			StringBuilder txt = new StringBuilder();

			txt.Append("Msg: ");
			txt.AppendLine(error.Message);

			try
			{
				if (error is COMException)
				{
					txt.Append("COM message: ");
					txt.AppendLine(new Win32Exception(((COMException)error).ErrorCode).Message);
				}
			}
			catch
			{
			}

			try
			{
				txt.Append("Source: ");
				txt.AppendLine(error.Source);
			}
			catch
			{
			}

			try
			{
				if(error.TargetSite != null)
				{
					txt.Append("Assembly: ");
					txt.AppendLine(error.TargetSite.DeclaringType.Assembly.FullName);
				}
			}
			catch
			{
			}

			try
			{
				txt.Append("Stack: ");
				txt.AppendLine(error.StackTrace);
			}
			catch
			{
			}
			txt.AppendFormat("Thread: {0}", Thread.CurrentThread.Name);
			txt.AppendLine();

			txt.AppendFormat("Thread UI culture: {0}", Thread.CurrentThread.CurrentUICulture);
			txt.AppendLine();

			txt.AppendFormat("Exception: {0}", error.GetType());
			txt.AppendLine();

			try
			{
				if (error.Data.Count > 0)
				{
					txt.AppendLine("Additional Exception Information:");
					foreach (DictionaryEntry de in error.Data)
					{
						txt.AppendFormat("{0}={1}", de.Key, de.Value);
						txt.AppendLine();
					}
				}
			}
			catch
			{
			}

			return txt.ToString();
		}
		#endregion

		#region Event Handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnClose_Click(object sender, System.EventArgs e)
		{
			GatherData();

			if(radEmail.Checked)
			{
				try
				{
					// WARNING! This currently does not work. The main issue seems to be the length of the error report. mailto
					// apparently has some limit on the length of the message, and we are exceeding that.
					//make it safe, but does too much (like replacing spaces with +'s)
					//string s = System.Web.HttpUtility.UrlPathEncode( m_details.Text);
					string body = m_details.Text.Replace(System.Environment.NewLine, "%0A").Replace("\"", "%22").Replace("&", "%26");

					System.Diagnostics.Process p = new Process();
					p.StartInfo.FileName =String.Format("mailto:{0}?subject={1}&body={2}", s_emailAddress, s_emailSubject, body);
					p.Start();
				}
				catch(Exception)
				{
					//swallow it
				}
//				catch(Exception ex)
//				{
//					System.Diagnostics.Debug.WriteLine(ex.Message);
//					System.Diagnostics.Debug.WriteLine(ex.StackTrace);
//				}
			}
			else if(radSelf.Checked)
			{
				if(s_emailAddress != null)
				{
					m_details.Text = string.Format(ReportingStrings.ksPleaseEMailThisTo0WithThisExactSubject12,
						s_emailAddress, s_emailSubject, m_details.Text);
				}
				// Copying to the clipboard works only if thread is STA which is not the case if
				// called from the Finalizer thread
				if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
					Clipboard.SetDataObject(m_details.Text, true);
				else
					Logger.WriteEvent(m_details.Text);
			}

			if(!m_isLethal || Control.ModifierKeys.Equals(System.Windows.Forms.Keys.Shift))
			{
				Logger.WriteEvent("Continuing...");
				return;
			}

			Logger.WriteEvent("Exiting...");
			Application.Exit();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the attempt to continue label if the shift key is pressed
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.ShiftKey && Visible)
				labelAttemptToContinue.Visible = true;
			base.OnKeyDown(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Hides the attempt to continue label if the shift key is pressed
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnKeyUp(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.ShiftKey && Visible)
				labelAttemptToContinue.Visible = false;
			base.OnKeyUp(e);
		}
		#endregion
	}
}
