// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ApplicationBusyDialog.cs

using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class that displays a dialog box on a separate thread to tell the user that the
	/// requested FW application can't be started just yet.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ApplicationBusyDialog : Form
	{
		/// <summary>Things this dialog can wait for</summary>
		public enum WaitFor
		{
			/// <summary>Application currently has no windows open because it's doing something.
			/// This dialog will wait until one is created.</summary>
			WindowToActivate,
			/// <summary>Other application has a data update in progress.
			/// This dialog will wait until it is finished.</summary>
			OtherBusyApp,
			/// <summary>Other application has a modal dialog or message box open.
			/// This dialog will wait until all open modal dialogs are closed.</summary>
			ModalDialogsToClose,
		}

		private WaitFor m_whatToWaitFor;
		private FwAppArgs m_args;
		private FwApp m_appToStart, m_appToWaitFor;
		private bool m_fCancelPressed = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ApplicationBusyDialog"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ApplicationBusyDialog()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows an instance of this dialog on a separate thread.
		/// </summary>
		/// <param name="args">The application arguments</param>
		/// <param name="whatToWaitFor">The condition we're waiting for.</param>
		/// <param name="appToStart">The application to start.</param>
		/// <param name="appToWaitFor">The application to wait for (null if waiting for
		/// WindowToActivate).</param>
		/// ------------------------------------------------------------------------------------
		internal static void ShowOnSeparateThread(FwAppArgs args, WaitFor whatToWaitFor,
			FwApp appToStart, FwApp appToWaitFor)
		{
			if (whatToWaitFor != WaitFor.WindowToActivate && appToWaitFor == null)
				throw new ArgumentNullException("appToWaitFor");
			if (appToStart == null)
				throw new ArgumentNullException("appToStart");

			ApplicationBusyDialog dlg = new ApplicationBusyDialog();
			dlg.m_whatToWaitFor = whatToWaitFor;
			dlg.m_args = args;
			dlg.m_appToStart = appToStart;
			dlg.m_appToWaitFor = appToWaitFor;
			Thread thread = new Thread(dlg.WaitForOtherApp);
			thread.IsBackground = true;
			thread.SetApartmentState(ApartmentState.STA);
			thread.Name = "WaitForOtherBusyApp";
			thread.CurrentUICulture = Thread.CurrentThread.CurrentUICulture;
			thread.Start();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Waits for other app.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void WaitForOtherApp()
		{
			Text = FwUtils.ksSuiteName;
			switch (m_whatToWaitFor)
			{
				case WaitFor.WindowToActivate:
					m_lblMessage.Text = string.Format(Properties.Resources.kstidThisApplicationIsBusy,
						m_appToStart.ApplicationName, m_appToStart.Cache.ProjectId.Name);
					break;
				case WaitFor.OtherBusyApp:
					m_lblMessage.Text = string.Format(Properties.Resources.kstidOtherApplicationBusy,
						m_appToStart.ApplicationName, m_appToStart.Cache.ProjectId.Name, m_appToWaitFor.ApplicationName);
					break;
				case WaitFor.ModalDialogsToClose:
					m_lblMessage.Text = string.Format(Properties.Resources.kstidOtherApplicationHasDialog,
						m_appToStart.ApplicationName, m_appToStart.Cache.ProjectId.Name, m_appToWaitFor.ApplicationName);
					break;
			}

			Show();
			Activate();

			bool readyToRoll = false;
			do
			{
				Application.DoEvents();

				Thread.Sleep(333);

				if (m_fCancelPressed)
					break;
				switch (m_whatToWaitFor)
				{
					case WaitFor.WindowToActivate:
						readyToRoll = (m_appToStart.MainWindows.Count > 0);
						break;
					case WaitFor.OtherBusyApp:
						readyToRoll = !DataUpdateMonitor.IsUpdateInProgress();
						break;
					case WaitFor.ModalDialogsToClose:
						readyToRoll = !m_appToWaitFor.IsModalDialogOpen;
						break;
				}
			}
			while (!readyToRoll);

			Close();

			if (readyToRoll)
				FieldWorks.KickOffAppFromOtherProcess(m_args);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnCancel control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_btnCancel_Click(object sender, System.EventArgs e)
		{
			m_fCancelPressed = true;
		}
	}
}