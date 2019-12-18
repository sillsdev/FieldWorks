// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.PlatformUtilities;

namespace LanguageExplorer.UtilityTools
{
	/// <summary>
	/// This dialog presents the users with the list of utilities defined in 'Language Explorer\Configuration\UtilityCatalogInclude.xml'
	/// These utilities must implement the IUtility class and can set several labels in the dialog to explain the conditions where they
	/// are needed and their behavior.
	/// </summary>
	public class UtilityDlg : Form, IFlexComponent
	{
		private RichTextBox m_rtbDescription;
		private Label m_lSteps;
		private IContainer components = null;
		private const string s_helpTopic = "khtpProjectUtilities";
		private HelpProvider m_helpProvider;
		private Button m_btnRunUtils;
		private Label label1;
		private Label label2;
		private Button m_btnHelp;
		private Button m_btnClose;
		private IHelpTopicProvider m_helpTopicProvider;

		/// <summary />
		public UtilityDlg(IHelpTopicProvider helpTopicProvider)
		{
			InitializeComponent();
			AccessibleName = GetType().Name;
			m_btnRunUtils.Enabled = false;
			m_helpTopicProvider = helpTopicProvider;
			m_helpProvider = new HelpProvider
			{
				HelpNamespace = FwDirectoryFinder.CodeDirectory + m_helpTopicProvider.GetHelpString("UserHelpFile")
			};
			m_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(s_helpTopic));
			m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			// The standard localization doesn't seem to be working, so do it explicitly here.
			label1.Text = LanguageExplorerResources.ksUtilities;
			label2.Text = LanguageExplorerResources.ksDescription;
			m_btnHelp.Text = LanguageExplorerResources.ksHelpForUtiltiesDlg;
			m_btnClose.Text = LanguageExplorerResources.ks_Close;
			m_btnRunUtils.Text = LanguageExplorerResources.ksRunUtilities;
		}

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		#endregion

		#region Implementation of IFlexComponent

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentParameters.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;

			Text = StringTable.Table.LocalizeAttributeValue("FieldWorks Project Utilities");
			SuspendLayout();
			Utilities.Items.Clear();
			Utilities.Sorted = false;
			var utilities = new Dictionary<string, IUtility>(13);
			var interfaceType = typeof(IUtility);
			var leAssembly = Assembly.GetExecutingAssembly();
			foreach (var type in leAssembly.GetTypes().Where(t => interfaceType.IsAssignableFrom(t) && t.IsClass))
			{
				utilities.Add(type.Name, (IUtility)leAssembly.CreateInstance(type.FullName, true, BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { this }, null, null));
			}
			var knownUtilities = new List<string>
			{
				"HomographResetter",
				"ParserAnalysisRemover",
				"ErrorFixer",
				"WriteAllObjectsUtility",
				"DuplicateWordformFixer",
				"DuplicateAnalysisFixer",
				"ParseIsCurrentFixer",
				"DeleteEntriesSensesWithoutInterlinearization",
				"LexEntryInflTypeConverter",
				"LexEntryTypeConverter",
				"GoldEticGuidFixer",
				"SortReversalSubEntries",
				"CircularRefBreaker"

			};
			foreach (var utilityTypeName in knownUtilities)
			{
				Utilities.Items.Add(utilities[utilityTypeName]);
				utilities.Remove(utilityTypeName);
			}
			foreach (var userCreatedUtility in utilities.Values)
			{
				Utilities.Items.Add(userCreatedUtility);
			}
			ResumeLayout();
			if (Utilities.Items.Count > 0)
			{
				Utilities.SelectedIndex = 0;
			}
		}

		#endregion

		/// <summary>
		/// Get the Utilites list box.
		/// </summary>
		public CheckedListBox Utilities { get; private set; }

		/// <summary>
		/// Get the progress bar.
		/// </summary>
		public ProgressBar ProgressBar { get; private set; }

		/// <summary>
		/// Set the When Description substring.
		/// </summary>
		public string WhenDescription { get; set; }

		/// <summary>
		/// Set the What Description substring.
		/// </summary>
		public string WhatDescription { get; set; }

		/// <summary>
		/// Set the "Cautions" substring. (Keeping the legacy API)
		/// </summary>
		public string RedoDescription { get; set; }

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run more than once.
				return;
			}

			if (disposing)
			{
				components?.Dispose();
				m_helpProvider.ResetShowHelp(this);
				m_helpProvider.Dispose();
			}

			PropertyTable = null;
			Publisher = null;
			Subscriber = null;
			m_helpProvider = null;
			m_helpTopicProvider = null;

			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UtilityDlg));
			this.Utilities = new System.Windows.Forms.CheckedListBox();
			this.m_rtbDescription = new System.Windows.Forms.RichTextBox();
			this.m_btnRunUtils = new System.Windows.Forms.Button();
			this.ProgressBar = new System.Windows.Forms.ProgressBar();
			this.m_lSteps = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_btnClose = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// m_clbUtilities
			//
			resources.ApplyResources(this.Utilities, "m_clbUtilities");
			this.Utilities.Name = "m_clbUtilities";
			this.Utilities.SelectedIndexChanged += new System.EventHandler(this.m_clbUtilities_SelectedIndexChanged);
			this.Utilities.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.m_clbUtilities_ItemCheck);
			//
			// m_rtbDescription
			//
			resources.ApplyResources(this.m_rtbDescription, "m_rtbDescription");
			this.m_rtbDescription.Name = "m_rtbDescription";
			this.m_rtbDescription.ReadOnly = true;
			//
			// m_btnRunUtils
			//
			resources.ApplyResources(this.m_btnRunUtils, "m_btnRunUtils");
			this.m_btnRunUtils.Name = "m_btnRunUtils";
			this.m_btnRunUtils.Click += new System.EventHandler(this.m_btnRunUtils_Click);
			//
			// m_progressBar
			//
			resources.ApplyResources(this.ProgressBar, "m_progressBar");
			this.ProgressBar.Name = "m_progressBar";
			//
			// m_lSteps
			//
			resources.ApplyResources(this.m_lSteps, "m_lSteps");
			this.m_lSteps.Name = "m_lSteps";
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_btnClose
			//
			this.m_btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.m_btnClose, "m_btnClose");
			this.m_btnClose.Name = "m_btnClose";
			this.m_btnClose.Click += new System.EventHandler(this.m_btnClose_Click);
			//
			// UtilityDlg
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btnClose;
			this.ControlBox = false;
			this.Controls.Add(this.m_lSteps);
			this.Controls.Add(this.ProgressBar);
			this.Controls.Add(this.m_btnClose);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnRunUtils);
			this.Controls.Add(this.m_rtbDescription);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.Utilities);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "UtilityDlg";
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);

		}
		#endregion

		#region Events

		private void m_clbUtilities_SelectedIndexChanged(object sender, EventArgs e)
		{
			var currentFont = m_rtbDescription.SelectionFont;
			Font boldFont = null;
			try
			{
				var boldFontStyle = FontStyle.Bold;
				// Creating the bold equivalent of the current font doesn't seem to work in Mono,
				// as we crash shortly due to failures in GDIPlus.GdipMeasureString() using that
				// font.
				boldFont = Platform.IsMono ? currentFont : new Font(currentFont.FontFamily, currentFont.Size, boldFontStyle);
				WhatDescription = WhenDescription = RedoDescription = null;
				((IUtility)Utilities.SelectedItem).OnSelection();
				m_rtbDescription.Clear();
				// What
				m_rtbDescription.SelectionFont = boldFont;
				m_rtbDescription.AppendText(LanguageExplorerResources.ksWhatItDoes);
				m_rtbDescription.AppendText(Environment.NewLine);
				m_rtbDescription.SelectionFont = currentFont;
				m_rtbDescription.AppendText(string.IsNullOrEmpty(WhatDescription) ? LanguageExplorerResources.ksQuestions : WhatDescription);
				m_rtbDescription.AppendText(string.Format("{0}{0}", Environment.NewLine));
				// When
				m_rtbDescription.SelectionFont = boldFont;
				m_rtbDescription.AppendText(LanguageExplorerResources.ksWhenToUse);
				m_rtbDescription.AppendText(Environment.NewLine);
				m_rtbDescription.SelectionFont = currentFont;
				m_rtbDescription.AppendText(string.IsNullOrEmpty(WhenDescription) ? LanguageExplorerResources.ksQuestions : WhenDescription);
				m_rtbDescription.AppendText(string.Format("{0}{0}", Environment.NewLine));
				// Cautions
				m_rtbDescription.SelectionFont = boldFont;
				m_rtbDescription.AppendText(LanguageExplorerResources.ksCautions);
				m_rtbDescription.AppendText(Environment.NewLine);
				m_rtbDescription.SelectionFont = currentFont;
				m_rtbDescription.AppendText(string.IsNullOrEmpty(RedoDescription) ? LanguageExplorerResources.ksQuestions : RedoDescription);
				if (Platform.IsMono)
				{
					// If we don't have a selection explicitly set, we will crash deep in the Mono
					// code (RichTextBox.cs:618, property SelectionFont:get) shortly.
					m_rtbDescription.Focus();
					m_rtbDescription.SelectionStart = 0;
					m_rtbDescription.SelectionLength = 0;
					Utilities.Focus();
				}
			}
			finally
			{
				if (!Platform.IsMono)
				{
					boldFont?.Dispose();
				}
			}
		}

		private void m_btnRunUtils_Click(object sender, System.EventArgs e)
		{
			using (new WaitCursor(this))
			{
				// Note: Resetting the steps text doesn't work.
				//m_lSteps.Text = string.Empty;
				//int totalSteps = m_clbUtilities.CheckedItems.Count;
				//int currentStep = 0;
				var checkedItems = new HashSet<IUtility>();
				foreach (IUtility util in Utilities.CheckedItems)
				{
					util.Process();
					ProgressBar.Value = 0;
					checkedItems.Add(util);
				}
				// Uncheck each one that was done.
				foreach (var checkedUtil in checkedItems)
				{
					Utilities.SetItemChecked(Utilities.Items.IndexOf(checkedUtil), false);
				}
			}
		}

		private void m_btnClose_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void m_clbUtilities_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			var cnt = Utilities.CheckedItems.Count;
			cnt = (e.NewValue == CheckState.Checked) ? cnt + 1 : cnt - 1;
			m_btnRunUtils.Enabled = cnt > 0;
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, s_helpTopic);
		}

		#endregion Events
	}
}