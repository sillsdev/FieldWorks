// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Security.Policy;
using System.Windows.Forms;
using LanguageExplorer;
using LanguageExplorer.Impls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Utils;
using SIL.PlatformUtilities;
using SIL.Reporting;
using SIL.Windows.Forms;

namespace SIL.FieldWorks
{
	/// <summary>
	/// Dialog presenting multiple options for how to begin a FLEx session
	/// </summary>
	internal sealed partial class WelcomeToFieldWorksDlg : Form
	{
		private readonly string _helpTopic = "khtpWelcomeToFieldworks";
		private HelpProvider _helpProvider;

		#region Construction, Initialization and Deconstruction

		/// <summary />
		/// <param name="helpTopicProvider">Help topic provider</param>
		/// <param name="exception">Exception that was thrown if the previously requested
		/// project could not be opened.</param>
		/// <param name="showReportingRow">True (usually only on the first run) when we want to show the first-time warning about
		/// sending google analytics information</param>
		internal WelcomeToFieldWorksDlg(IHelpTopicProvider helpTopicProvider, StartupException exception, bool showReportingRow)
		{
			InitializeComponent();
			AccessibleName = GetType().Name;
			var fullAppName = Properties.Resources.kstidFLEx;
			SetCheckboxText = fullAppName;  // Setter uses the app name in a format string.

			if (exception == null || !exception.ReportToUser)
			{
				Text = fullAppName;
				Logger.WriteEvent("Opening 'Welcome to FieldWorks' dialog");
			}
			else
			{
				_helpTopic = "khtpUnableToOpenProject";
				Text = Properties.Resources.kstidUnableToOpenProjectCaption;
				m_lblProjectLoadError.Text = exception.Message;
				Logger.WriteEvent("Opening 'Unable to Open Project' dialog");
			}

			if (!showReportingRow)
			{
				reportingInfoLayout.Visible = false;
			}

			m_helpTopicProvider = helpTopicProvider;
			_helpProvider = new HelpProvider
			{
				HelpNamespace = FwDirectoryFinder.CodeDirectory + m_helpTopicProvider.GetHelpString(FwUtilsConstants.UserHelpFile)
			};
			_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(_helpTopic));
			_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			receiveButton.Enabled = FLExBridgeHelper.IsFlexBridgeInstalled;
		}

		internal bool OpenLastProjectCheckboxIsChecked
		{
			get => alwaysOpenLastProjectCheckBox.Checked;
			set => alwaysOpenLastProjectCheckBox.Checked = value;
		}

		internal void SetFirstOrLastProjectText(bool firstTimeOpening)
		{
			m_sampleOrLastProjectLinkLabel.Text = firstTimeOpening ? Properties.Resources.ksOpenSampleProject : Properties.Resources.ksOpenLastEditedProject;
		}

		internal string ProjectLinkUiName
		{
			get => m_openSampleOrLastProjectLink.Text;
			set => m_openSampleOrLastProjectLink.Text = value;
		}

		private string SetCheckboxText
		{
			set => alwaysOpenLastProjectCheckBox.Text = string.Format(Properties.Resources.ksWelcomeDialogCheckboxText, value);
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}
			if (disposing)
			{
				components?.Dispose();
				_helpProvider?.Dispose();
			}
			_helpProvider = null;

			base.Dispose(disposing);
		}

		#endregion

		/// <summary>
		/// Gets the button that was pressed
		/// </summary>
		internal ButtonPress DlgResult { get; private set; } = ButtonPress.Exit;

		#region Overriden methods

		/// <summary>
		/// Log the dialog result
		/// </summary>
		protected override void OnClosing(CancelEventArgs e)
		{
			Logger.WriteEvent("Closing dialog: " + DlgResult);
			base.OnClosing(e);
		}

		/// <summary>
		/// When the dialog is loaded, make sure it gets focused.
		/// </summary>
		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			// This dialog may be created when no other forms are active. Calling Activate will
			// make sure that the dialog comes up visible and activated.
			Activate();

			if (Platform.IsUnix)
			{
				ReLayoutCorrectly();
			}
		}

		/// <summary>
		/// Adjust dialog so that the height of the main FlowLayoutPanel is the right value to
		/// contain the displayed controls.
		/// A better solution will be to fix Mono FlowLayoutPanel to not include
		/// non-Visible controls in the FlowLayoutPanel height calculation, if that is what
		/// it is doing when it AutoSizes.
		/// </summary>
		private void ReLayoutCorrectly()
		{
			var shrunkWidth = mainVerticalLayout.Width;
			mainVerticalLayout.AutoSize = false;
			mainVerticalLayout.Width = shrunkWidth;

			var heightOfVisibleControls = 0;
			foreach (Control control in mainVerticalLayout.Controls)
			{
				if (control.Visible == false)
				{
					continue;
				}
				heightOfVisibleControls += control.Height;
				heightOfVisibleControls += control.Margin.Top;
				heightOfVisibleControls += control.Margin.Bottom;
			}
			mainVerticalLayout.Height = heightOfVisibleControls;
		}
		#endregion

		#region Button click handlers

		/// <summary />
		private void m_btnOpen_Click(object sender, EventArgs e)
		{
			DlgResult = ButtonPress.Open;
			Hide();
		}

		/// <summary />
		private void m_btnNew_Click(object sender, EventArgs e)
		{
			DlgResult = ButtonPress.New;
			Hide();
		}

		/// <summary />
		private void m_btnRestore_Click(object sender, EventArgs e)
		{
			DlgResult = ButtonPress.Restore;
			Hide();
		}

		/// <summary />
		private void m_btnExit_Click(object sender, EventArgs e)
		{
			DlgResult = ButtonPress.Exit;
			Hide();
		}

		private void m_openProjectLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			DlgResult = ButtonPress.Link;
			Hide();
		}

		private void Receive_Click(object sender, EventArgs e)
		{
			DlgResult = ButtonPress.Receive;
			Hide();
		}

		private void Import_Click(object sender, EventArgs e)
		{
			DlgResult = ButtonPress.Import;
			Hide();
		}
		private void m_btnOptions_Click(object sender, EventArgs e)
		{
			var optionsDlg = new LexOptionsDlg();
			optionsDlg.InitHelpTopicOnly(m_helpTopicProvider);
			optionsDlg.Show(this);
		}

		/// <summary>
		/// Open the context-sensitive help for this dialog.
		/// </summary>
		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, _helpTopic);
		}

		#endregion

		internal void ShowErrorLabelHideLink()
		{
			m_sampleOrLastProjectLinkLabel.Visible = false;
			m_openSampleOrLastProjectLink.Visible = false;
			m_lblProjectLoadError.Visible = true;
			if (!string.IsNullOrEmpty(m_lblProjectLoadError.Text))
			{
				Icon = SystemIcons.Exclamation;
			}
		}

		internal void ShowLinkHideErrorLabel()
		{
			m_lblProjectLoadError.Visible = false;
			m_sampleOrLastProjectLinkLabel.Visible = true;
			m_openSampleOrLastProjectLink.Visible = true;
		}
	}
}