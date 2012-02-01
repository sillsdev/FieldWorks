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
using System.Drawing;  // GDI+ stuff
using System.Drawing.Imaging;  // ImageFormat

using Microsoft.Win32;
using Palaso.Email;
using SIL.Utils;

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
		private Label lbl_EmailReport;

		/// <summary>The subject for error report emails</summary>
		protected static readonly string s_emailSubject = "Automated Error Report";

		private readonly bool m_isLethal;
		private readonly string m_emailAddress;
		private bool m_userChoseToExit;
		private bool m_showChips;

		/// <summary>
		/// a list of name, string value pairs that will be included in the details of the error report.
		/// For example, xWorks would could the name of the database in here.
		/// </summary>
		protected static Dictionary<string, string> s_properties = new Dictionary<string, string>();

		/// <summary></summary>
		protected static bool s_isOkToInteractWithUser = true;
		private LinkLabel viewDetailsLink;
		private Button cancelButton;
		private Label m_stepsLabel;
		private Label lbl_AdditionInfo;
		private static bool s_fIgnoreReport;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// this is protected so that we can have a Singleton
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected ErrorReporter(bool isLethal, string emailAddress)
		{
			m_isLethal = isLethal;
			m_emailAddress = emailAddress;
			AccessibleName = GetType().Name;
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ErrorReporter));
			this.m_reproduce = new System.Windows.Forms.TextBox();
			this.radEmail = new System.Windows.Forms.RadioButton();
			this.m_details = new System.Windows.Forms.TextBox();
			this.lbl_EmailReport = new System.Windows.Forms.Label();
			this.radSelf = new System.Windows.Forms.RadioButton();
			this.btnClose = new System.Windows.Forms.Button();
			this.m_notification = new System.Windows.Forms.TextBox();
			this.labelAttemptToContinue = new System.Windows.Forms.Label();
			this.viewDetailsLink = new System.Windows.Forms.LinkLabel();
			this.m_stepsLabel = new System.Windows.Forms.Label();
			this.cancelButton = new System.Windows.Forms.Button();
			this.lbl_AdditionInfo = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// m_reproduce
			//
			this.m_reproduce.AcceptsReturn = true;
			this.m_reproduce.AcceptsTab = true;
			resources.ApplyResources(this.m_reproduce, "m_reproduce");
			this.m_reproduce.Name = "m_reproduce";
			this.m_reproduce.TextChanged += new System.EventHandler(this.m_reproduce_TextChanged);
			//
			// radEmail
			//
			resources.ApplyResources(this.radEmail, "radEmail");
			this.radEmail.BackColor = System.Drawing.Color.Transparent;
			this.radEmail.Checked = true;
			this.radEmail.Name = "radEmail";
			this.radEmail.TabStop = true;
			this.radEmail.UseVisualStyleBackColor = false;
			//
			// m_details
			//
			resources.ApplyResources(this.m_details, "m_details");
			this.m_details.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this.m_details.Name = "m_details";
			this.m_details.ReadOnly = true;
			//
			// lbl_EmailReport
			//
			resources.ApplyResources(this.lbl_EmailReport, "lbl_EmailReport");
			this.lbl_EmailReport.BackColor = System.Drawing.Color.Transparent;
			this.lbl_EmailReport.Name = "lbl_EmailReport";
			//
			// radSelf
			//
			resources.ApplyResources(this.radSelf, "radSelf");
			this.radSelf.BackColor = System.Drawing.Color.Transparent;
			this.radSelf.Name = "radSelf";
			this.radSelf.UseVisualStyleBackColor = false;
			//
			// btnClose
			//
			resources.ApplyResources(this.btnClose, "btnClose");
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
			this.labelAttemptToContinue.BackColor = System.Drawing.Color.Transparent;
			this.labelAttemptToContinue.ForeColor = System.Drawing.Color.Firebrick;
			this.labelAttemptToContinue.Name = "labelAttemptToContinue";
			//
			// viewDetailsLink
			//
			resources.ApplyResources(this.viewDetailsLink, "viewDetailsLink");
			this.viewDetailsLink.Name = "viewDetailsLink";
			this.viewDetailsLink.TabStop = true;
			this.viewDetailsLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.viewDetailsLink_LinkClicked);
			//
			// m_stepsLabel
			//
			resources.ApplyResources(this.m_stepsLabel, "m_stepsLabel");
			this.m_stepsLabel.BackColor = System.Drawing.Color.Transparent;
			this.m_stepsLabel.Name = "m_stepsLabel";
			//
			// cancelButton
			//
			resources.ApplyResources(this.cancelButton, "cancelButton");
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.UseVisualStyleBackColor = true;
			this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
			//
			// lbl_AdditionInfo
			//
			resources.ApplyResources(this.lbl_AdditionInfo, "lbl_AdditionInfo");
			this.lbl_AdditionInfo.BackColor = System.Drawing.Color.Transparent;
			this.lbl_AdditionInfo.Name = "lbl_AdditionInfo";
			//
			// ErrorReporter
			//
			this.AcceptButton = this.btnClose;
			resources.ApplyResources(this, "$this");
			this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
			this.CancelButton = this.cancelButton;
			this.ControlBox = false;
			this.Controls.Add(this.lbl_AdditionInfo);
			this.Controls.Add(this.cancelButton);
			this.Controls.Add(this.viewDetailsLink);
			this.Controls.Add(this.m_reproduce);
			this.Controls.Add(this.m_notification);
			this.Controls.Add(this.m_details);
			this.Controls.Add(this.labelAttemptToContinue);
			this.Controls.Add(this.m_stepsLabel);
			this.Controls.Add(this.lbl_EmailReport);
			this.Controls.Add(this.radEmail);
			this.Controls.Add(this.radSelf);
			this.Controls.Add(this.btnClose);
			this.KeyPreview = true;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ErrorReporter";
			this.ShowIcon = false;
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
		/// <param name="applicationKey">The application registry key.</param>
		/// <param name="emailAddress">The e-mail address for reporting errors.</param>
		/// <returns>True if the error was lethal and the user chose to exit the application,
		/// false otherwise.</returns>
		/// ------------------------------------------------------------------------------------
		public static bool ReportException(Exception error, RegistryKey applicationKey,
			string emailAddress)
		{
			return ReportException(error, applicationKey, emailAddress, null, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// show a dialog or output to the error log, as appropriate.
		/// </summary>
		/// <param name="error">the exception you want to report</param>
		/// <param name="applicationKey">The application registry key.</param>
		/// <param name="emailAddress">The e-mail address for reporting errors.</param>
		/// <param name="parent">the parent form that this error belongs to (i.e. the form
		/// show modally on)</param>
		/// <param name="isLethal">set to <c>true</c> if the error is lethal, otherwise
		/// <c>false</c>.</param>
		/// <returns>True if the error was lethal and the user chose to exit the application,
		/// false otherwise.</returns>
		/// ------------------------------------------------------------------------------------
		public static bool ReportException(Exception error, RegistryKey applicationKey,
			string emailAddress, Form parent, bool isLethal)
		{
			// I (TomH) think that unit tests should never show this Exception dialog.
			if (MiscUtils.RunningTests)
				throw error;

			// ignore message if we are showing from a previous error
			if (s_fIgnoreReport)
				return false;

			if (String.IsNullOrEmpty(emailAddress))
				emailAddress = "fieldworks_support@sil.org";

			// If the error has a message and a help link, then show that error
			if (!string.IsNullOrEmpty(error.HelpLink) && error.HelpLink.IndexOf("::/") > 0 &&
				!string.IsNullOrEmpty(error.Message))
			{
				s_fIgnoreReport = true; // This is presumably a hopelessly fatal error, so we
				// don't want to report any subsequent errors at all.
				// Look for the end of the basic message which will be terminated by two new lines or
				// two CRLF sequences.
				int lengthOfBasicMessage = error.Message.IndexOf("\r\n\r\n");
				if (lengthOfBasicMessage <= 0)
					lengthOfBasicMessage = error.Message.IndexOf("\n\n");
				if (lengthOfBasicMessage <= 0)
					lengthOfBasicMessage = error.Message.Length;

				int iSeparatorBetweenFileNameAndTopic = error.HelpLink.IndexOf("::/");
				string sHelpFile = error.HelpLink.Substring(0, iSeparatorBetweenFileNameAndTopic);
				string sHelpTopic = error.HelpLink.Substring(iSeparatorBetweenFileNameAndTopic + 3);

				string caption = ReportingStrings.kstidFieldWorksErrorCaption;
				string appExit = ReportingStrings.kstidFieldWorksErrorExitInfo;
				MessageBox.Show(parent, error.Message.Substring(0, lengthOfBasicMessage) + Environment.NewLine + appExit,
					caption, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, 0,
					sHelpFile, HelpNavigator.Topic, sHelpTopic);
				if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
					ClipboardUtils.SetDataObject(error.Message, true);
				else
					Logger.WriteError(error);
				Application.Exit();
				return true;
			}

			using (ErrorReporter e = new ErrorReporter(isLethal, emailAddress))
			{
				e.ShowDialog(applicationKey, error, parent);
				return e.m_userChoseToExit;
			}
		}

		/// <summary>
		/// Invoked by a menu command, allows the user to report a problem that didn't crash the program,
		/// complete with all the context information we attach to crashes (except a stack dump).
		/// </summary>
		public static void ReportProblem(RegistryKey applicationKey, string emailAddress, Form parent)
		{
			ErrorReporter e = new ErrorReporter(false, emailAddress);
			e.m_fUserReport = true;
			e.ShowDialog(applicationKey, null, parent);
			e.Closed += ModelessClosed;
		}

		static void ModelessClosed(object sender, EventArgs e)
		{
			((ErrorReporter)sender).Dispose();
		}

		/// <summary>
		/// Invoked by a menu command, allows the user to make a suggestion,
		/// complete with all the context information we attach to crashes (except a stack dump).
		/// </summary>
		public static void MakeSuggestion(RegistryKey applicationKey, string emailAddress, Form parent)
		{
			ErrorReporter e = new ErrorReporter(false, emailAddress);
			e.m_fUserReport = true;
			e.m_fSuggestion = true;
			e.ShowDialog(applicationKey, null, parent);
			e.Closed += ModelessClosed;
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
		#endregion

		#region Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected string GatherData()
		{
			//m_details.Text += "\r\nTo Reproduce: " + m_reproduce.Text + "\r\n";
			StringBuilder sb = new StringBuilder(m_details.Text.Length + m_reproduce.Text.Length + 50);
			sb.AppendLine(m_fUserReport ? (m_fSuggestion ? "Suggestion:" : "Problem Description:") : "To Reproduce:");
			sb.AppendLine(m_reproduce.Text);
			sb.AppendLine("");
			sb.Append(m_details.Text);
			return sb.ToString();
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

		private string m_viewDetailsOriginalText;
		private int m_originalHeight;
		private int m_originalHeightWithoutDetails;
		private Size m_originalMinSize;
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the dialog (handles the error, if it is one).
		/// </summary>
		/// <param name="applicationKey">The application registry key.</param>
		/// <param name="error">the exception you want to report</param>
		/// <param name="parent">the parent form that this error belongs to (i.e. the form
		/// show modally on)</param>
		/// ------------------------------------------------------------------------------------
		private void ShowDialog(RegistryKey applicationKey, Exception error, Form parent)
		{
			CheckDisposed();
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			m_viewDetailsOriginalText = viewDetailsLink.Text;
			m_originalHeightWithoutDetails = Height - m_details.Height - 10;
			m_originalHeight = Height;
			m_originalMinSize = MinimumSize;

			SetDialogStringsSoLocatilizationWorks();

			if (m_fUserReport)
			{
				ControlBox = true;
				cancelButton.Visible = true;
				btnClose.Size = cancelButton.Size;
				btnClose.Left = cancelButton.Left - btnClose.Width - 15;
				if (m_fSuggestion)
				{
					Text = ReportingStrings.kstidMakeSuggestionCaption;
					m_notification.Text = ReportingStrings.kstidMakeSuggestionNotification;
					m_stepsLabel.Text = ReportingStrings.kstidGoalAndSuggestion;
					m_reproduce.Text = ReportingStrings.kstidSampleSuggestion;
				}
				else
				{
					Text = ReportingStrings.kstidReportProblemCaption;
					m_notification.Text = ReportingStrings.kstidReportProblemNotification;
					m_stepsLabel.Text = ReportingStrings.kstidProblemAndSteps;
					m_reproduce.Text = ReportingStrings.ksSampleProblemReport;
				}
				// Do this AFTER filling in the sample...it is disabled until they change something.
				btnClose.Enabled = false;
			}

			var show = s_showDetails; // should it be showing?
			s_showDetails = true; // the resource-file state of the dialog is showing.
			ShowDetails(show); // put everything in the desired state.

			if (m_emailAddress == null)
			{
				radEmail.Enabled = false;
				radSelf.Checked = true;
			}
			else
			{
				// Add the e-mail address to the dialog.
				lbl_EmailReport.Text = String.Format(lbl_EmailReport.Text, m_emailAddress);
			}

			if (!m_isLethal)
			{
				btnClose.Text = ReportingStrings.ks_Ok;
				BackColor = m_fSuggestion
					? Color.FromKnownColor(KnownColor.Control) // standard dialog background
					: Color.FromArgb(255, 255, 192);//yellow
				m_notification.BackColor = BackColor;
				UpdateCrashCount(applicationKey, "NumberOfAnnoyingCrashes");
			}
			else
			{
				UpdateCrashCount(applicationKey, "NumberOfSeriousCrashes");
			}
			UpdateAppRuntime(applicationKey);

			StringBuilder detailsText = new StringBuilder();
			Exception innerMostException = null;
			if (error != null)
			{
				detailsText.AppendLine(ExceptionHelper.GetHiearchicalExceptionInfo(error, out innerMostException));

				// if the exception had inner exceptions, show the inner-most exception first, since
				// that is usually the one we want the developer to read.
				if (innerMostException != null)
				{
					StringBuilder innerException = new StringBuilder();
					innerException.AppendLine("Inner most exception:");
					innerException.AppendLine(ExceptionHelper.GetExceptionText(innerMostException));
					innerException.AppendLine();
					innerException.AppendLine("Full, hierarchical exception contents:");
					detailsText.Insert(0, innerException.ToString());
				}
			}

			detailsText.AppendLine("Additional information about the computer and project:");
			foreach(string label in s_properties.Keys )
				detailsText.AppendLine(label + ": " + s_properties[label]);

			if (innerMostException != null)
				error = innerMostException;
			if (error != null)
				Logger.WriteEvent("Got exception " + error.GetType().Name);

			detailsText.AppendLine(Logger.LogText);
			Debug.WriteLine(detailsText.ToString());
			m_details.Text = detailsText.ToString();

			if (m_fUserReport)
			{
				// show modeless, so they can use the program while filling in details.
				// Don't set the Ignore flag, a real crash might happen while trying to report a lesser problem.
				Show(parent);
				return;
			}
			if (s_isOkToInteractWithUser)
			{
				s_fIgnoreReport = true;
				ShowDialog((parent != null && !parent.IsDisposed) ? parent : null);
				s_fIgnoreReport = false;
			}
			else	//the test environment already prohibits dialogs but will save the contents of assertions in some log.
				Debug.Fail(m_details.Text);
		}

		private void SetDialogStringsSoLocatilizationWorks()
		{
			Text = ReportingStrings.kstidERTexts;
			m_notification.Text = ReportingStrings.kstidERReportProblemNotification;
			m_stepsLabel.Text = ReportingStrings.kstidERProblemAndSteps;

			lbl_AdditionInfo.Text = ReportingStrings.kstidAdditionalInfoSent;
			viewDetailsLink.Text = ReportingStrings.kstidViewDetailsLink;
			lbl_EmailReport.Text = ReportingStrings.kstidEmailReport;
			radEmail.Text = ReportingStrings.kstidradEmail;
			radSelf.Text = ReportingStrings.kstidradSelf;
		}

		private static void UpdateCrashCount(RegistryKey applicationKey, string sPropName)
		{
			if (applicationKey == null)
				return;

			int count = (int)applicationKey.GetValue(sPropName, 0) + 1;
			applicationKey.SetValue(sPropName, count);
			s_properties[sPropName] = count.ToString();
		}

		private static void UpdateAppRuntime(RegistryKey applicationKey)
		{
			if (applicationKey == null)
				return;

			string sStartup = (string)applicationKey.GetValue("LatestAppStartupTime", string.Empty);
			int csec = (int)applicationKey.GetValue("TotalAppRuntime", 0);
			int secBeforeCrash = 0;
			long start;
			if (!String.IsNullOrEmpty(sStartup) && long.TryParse(sStartup, out start))
			{
				DateTime started = new DateTime(start);
				DateTime finished = DateTime.Now.ToUniversalTime();
				TimeSpan delta = finished - started;
				secBeforeCrash = (int)delta.TotalSeconds;
				csec += secBeforeCrash;
				applicationKey.SetValue("TotalAppRuntime", csec);
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
		#endregion

		private bool m_fUserReport;
		private bool m_fSuggestion;

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
			var body = GatherData();

			if(radEmail.Checked)
			{
				var emailProvider = EmailProviderFactory.PreferredEmailProvider();
				var emailMessage = emailProvider.CreateMessage();
				emailMessage.To.Add(m_emailAddress);
				var emailSubject = s_emailSubject;
				if (m_fSuggestion)
					emailSubject = "Suggested Improvement to FLEx:";
				else if (m_fUserReport)
					emailSubject = "Manual Error Report:";
				emailMessage.Subject = emailSubject;
				emailMessage.Body = body;
				if (!emailMessage.Send(emailProvider))
				{
					MessageBox.Show(this, ReportingStrings.kstidSendFailed, ReportingStrings.kstidSendFailedCaption,
						MessageBoxButtons.OK, MessageBoxIcon.Warning);
					radSelf.Checked = true;
					return;
				}

				//try
				//{
				//    // WARNING! This currently does not work. The main issue seems to be the length of the error report. mailto
				//    // apparently has some limit on the length of the message, and we are exceeding that.
				//    //make it safe, but does too much (like replacing spaces with +'s)
				//    //string s = System.Web.HttpUtility.UrlPathEncode( m_details.Text);
				//    string body = m_details.Text.Replace(Environment.NewLine, "%0A").Replace("\"", "%22").Replace("&", "%26");

				//    Process p = new Process();
				//    p.StartInfo.FileName = String.Format("mailto:{0}?subject={1}&body={2}", m_emailAddress, s_emailSubject, body);
				//    p.Start();
				//}
				//catch(Exception)
				//{
				//    //swallow it
				//}
//				catch(Exception ex)
//				{
//					System.Diagnostics.Debug.WriteLine(ex.Message);
//					System.Diagnostics.Debug.WriteLine(ex.StackTrace);
//				}
			}
			else if(radSelf.Checked)
			{
				if(m_emailAddress != null)
				{
					if (m_fUserReport)
					{
						body = string.Format(ReportingStrings.kstidPleaseEmailThisTo0WithASuitableSubject,
							m_emailAddress, body);
					}
					else
					{
						body = string.Format(ReportingStrings.ksPleaseEMailThisTo0WithThisExactSubject12,
							m_emailAddress, s_emailSubject, body);
					}
				}
				// Copying to the clipboard works only if thread is STA which is not the case if
				// called from the Finalizer thread
				if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
					ClipboardUtils.SetDataObject(body, true);
				else
					Logger.WriteEvent(body);
			}

			if(!m_isLethal || ModifierKeys.Equals(Keys.Shift))
			{
				Logger.WriteEvent("Continuing...");
				Close();
				return;
			}

			m_userChoseToExit = true;
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
			if (e.KeyCode == Keys.ShiftKey && Visible && !labelAttemptToContinue.Visible)
			{
				labelAttemptToContinue.Visible = true;
				m_showChips = true;
				Refresh();
			}
			base.OnKeyDown(e);
		}

#if DEBUG
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Paint Chips
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			if (m_showChips)
			{
				base.OnPaint(e);
				Random Rnd = new Random();
				int Number = Rnd.Next(30);
				for (int i = 10; i < Number; i++)
					e.Graphics.DrawImage(ReportingStrings.cc, new Point(Rnd.Next(Width), Rnd.Next(Height)));
			}
		}
#endif

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Hides the attempt to continue label if the shift key is pressed
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnKeyUp(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.ShiftKey && Visible)
			{
				m_showChips = false;
				labelAttemptToContinue.Visible = false;
				Refresh();
			}
			base.OnKeyUp(e);
		}
		#endregion

		private static bool s_showDetails;
		private void viewDetailsLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			ShowDetails(!s_showDetails);
		}

		void ShowDetails(bool show)
		{
			viewDetailsLink.Text = show ? ReportingStrings.ksHideDetails : m_viewDetailsOriginalText;
			if (s_showDetails == show)
				return; // already in desired state!
			s_showDetails = show;
			m_details.Visible = show;
			// Not currently necessary, all the relevant controls are anchored bottom.
			//foreach (Control c in Controls)
			//{
			//    if (c.Top > m_details.Top)
			//    {
			//        if (show)
			//            c.Top += m_details.Height;
			//        else
			//            c.Top -= m_details.Height;
			//    }
			//}
			if (show)
			{
				Height = m_originalHeight;
				MinimumSize = m_originalMinSize;
			}
			else
			{
				MinimumSize = new Size(m_originalMinSize.Width, m_originalHeightWithoutDetails); // BEFORE height!
				Height = m_originalHeightWithoutDetails;
			}
		}

		private void m_reproduce_TextChanged(object sender, EventArgs e)
		{
			// We can send a message only if one has been written!
			btnClose.Enabled = !string.IsNullOrEmpty(m_reproduce.Text) || !m_fUserReport;
		}

		private void cancelButton_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}
