// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Threading;
using System.Windows.Forms;
using LanguageExplorer;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks
{
	/// <summary>
	/// Class that displays a dialog box on a separate thread to tell the user that the
	/// requested FW application can't be started just yet.
	/// </summary>
	public partial class ApplicationBusyDialog : Form
	{
		private WaitFor m_whatToWaitFor;
		private FwAppArgs m_args;
		private IFlexApp m_appToStart;
		private IFlexApp m_appToWaitFor;
		private bool m_fCancelPressed;

		/// <summary />
		private ApplicationBusyDialog()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Shows an instance of this dialog on a separate thread.
		/// </summary>
		/// <param name="args">The application arguments</param>
		/// <param name="whatToWaitFor">The condition we're waiting for.</param>
		/// <param name="appToStart">The application to start.</param>
		/// <param name="appToWaitFor">The application to wait for (null if waiting for
		/// WindowToActivate).</param>
		internal static void ShowOnSeparateThread(FwAppArgs args, WaitFor whatToWaitFor, IFlexApp appToStart, IFlexApp appToWaitFor)
		{
			if (whatToWaitFor != WaitFor.WindowToActivate && appToWaitFor == null)
			{
				throw new ArgumentNullException(nameof(appToWaitFor));
			}
			if (appToStart == null)
			{
				throw new ArgumentNullException(nameof(appToStart));
			}

			var dlg = new ApplicationBusyDialog
			{
				m_whatToWaitFor = whatToWaitFor,
				m_args = args,
				m_appToStart = appToStart,
				m_appToWaitFor = appToWaitFor
			};
			var thread = new Thread(dlg.WaitForOtherApp)
			{
				IsBackground = true,
				Name = "WaitForOtherBusyApp",
				CurrentUICulture = Thread.CurrentThread.CurrentUICulture
			};
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
		}

		/// <summary>
		/// Waits for other app.
		/// </summary>
		public void WaitForOtherApp()
		{
			Text = FwUtils.ksSuiteName;
			switch (m_whatToWaitFor)
			{
				case WaitFor.WindowToActivate:
					m_lblMessage.Text = string.Format(Properties.Resources.kstidThisApplicationIsBusy, m_appToStart.ApplicationName, m_appToStart.Cache.ProjectId.Name);
					break;
				case WaitFor.OtherBusyApp:
					m_lblMessage.Text = string.Format(Properties.Resources.kstidOtherApplicationBusy, m_appToStart.ApplicationName, m_appToStart.Cache.ProjectId.Name, m_appToWaitFor.ApplicationName);
					break;
				case WaitFor.ModalDialogsToClose:
					m_lblMessage.Text = string.Format(Properties.Resources.kstidOtherApplicationHasDialog, m_appToStart.ApplicationName, m_appToStart.Cache.ProjectId.Name, m_appToWaitFor.ApplicationName);
					break;
			}

			Show();
			Activate();

			var readyToRoll = false;
			do
			{
				Application.DoEvents();

				Thread.Sleep(333);

				if (m_fCancelPressed)
				{
					break;
				}
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
			{
				FieldWorks.KickOffAppFromOtherProcess(m_args);
			}
		}

		/// <summary>
		/// Handles the Click event of the m_btnCancel control.
		/// </summary>
		private void m_btnCancel_Click(object sender, System.EventArgs e)
		{
			m_fCancelPressed = true;
		}
	}
}