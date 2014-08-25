// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TryAWordDlg.cs
// Responsibility: Andy Black
// Last reviewed:
//
// <remarks>
// Implementation of:
//		TryAWordDlg - Dialog for parsing a single wordform
// </remarks>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Text;
using System.Xml.Linq;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.WordWorks.Parser;
using SIL.Utils;
using XCore;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.LexText.Controls
{

	/// <summary>
	/// Summary description for TryAWordDlg.
	/// </summary>
	public class TryAWordDlg : Form, IFWDisposable, IxWindow
	{
		private const string PersistProviderID = "TryAWord";
		private const string HelpTopicID = "khtpTryAWord";

		#region Data members

		/// <summary>
		/// xCore Mediator.
		/// </summary>
		private Mediator m_mediator;
		private FdoCache m_cache;
		private ParserListener m_parserListener;
		private PersistenceProvider m_persistProvider;
		private readonly HelpProvider m_helpProvider;

		private Label m_wordToTryLabel;
		private IContainer components;
		private FwTextBox m_wordformTextBox;
		private Button m_tryItButton;
		private Button m_closeButton;
		private Panel m_resultsPanel;
		private Panel m_closePanel;
		private Panel m_wordPanel;
		private Label m_statusLabel;
		private Timer m_timer;
		private TryAWordRootSite m_rootsite;
		private HtmlControl m_htmlControl;
		private Button m_helpButton;
		private CheckBox m_doTraceCheckBox;
		private Label m_resultsLabel;
		private Panel m_sandboxPanel;
		private CheckBox m_doSelectMorphsCheckBox;

		private bool m_parserCanDoSelectMorphs = true;

		private bool m_procesingTextChange;

		private IAsyncResult m_tryAWordResult;

		private WebPageInteractor m_webPageInteractor;

		#endregion Data members

		/// <summary>
		///
		/// </summary>
		public TryAWordDlg()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = GetType().Name;

			InitHtmlControl();

			m_helpProvider = new HelpProvider();
		}

		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "Code in question is only compiled on Windows")]
		public void SetDlgInfo(Mediator mediator, IWfiWordform wordform, ParserListener parserListener)
		{
			m_mediator = mediator;
			m_persistProvider = new PersistenceProvider(PersistProviderID, m_mediator.PropertyTable);
			m_cache = (FdoCache) m_mediator.PropertyTable.GetValue("cache");
			m_parserListener = parserListener;

			Text = m_cache.ProjectId.UiName + " - " + Text;
			SetRootSite();
			SetFontInfo();
			// restore window location and size after setting up the form textbox, because it might adjust size of
			// window causing the window to grow every time it is opened
			m_persistProvider.RestoreWindowSettings(PersistProviderID, this);
			if (wordform == null)
				GetLastWordUsed();
			else
				SetWordToUse(wordform.Form.VernacularDefaultWritingSystem.Text);

			m_webPageInteractor = new WebPageInteractor(m_htmlControl, m_mediator, m_wordformTextBox);
#if !__MonoCS__
			m_htmlControl.Browser.ObjectForScripting = m_webPageInteractor;
#endif

			// HermitCrab does not currently support selected tracing
			/*if (m_cache.LangProject.MorphologicalDataOA.ActiveParser == "HC")
			{
				m_parserCanDoSelectMorphs = false;
				m_doSelectMorphsCheckBox.Enabled = false;
			}
*/
			// No such thing as FwApp.App now: if(FwApp.App != null) // Could be null during testing
			if (m_mediator.HelpTopicProvider != null) // trying this
			{
				m_helpProvider.HelpNamespace = m_mediator.HelpTopicProvider.HelpFile;
				m_helpProvider.SetHelpKeyword(this, m_mediator.HelpTopicProvider.GetHelpString(HelpTopicID));
				m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			}

			if (m_parserListener.Connection != null)
			{
				m_parserListener.Connection.TryAWordDialogIsRunning = true;
				m_statusLabel.Text = GetString("ParserStatusPrefix") + ParserUIStrings.ksIdle_ + GetString("ParserStatusSuffix");
			}
			else
			{
				m_statusLabel.Text = ParserStoppedMessage();
			}
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="TryAWordRootSite gets added to control collection and disposed there")]
		private void SetRootSite()
		{
			m_rootsite = new TryAWordRootSite(m_cache, m_mediator) { Dock = DockStyle.Top };
			m_sandboxPanel.Controls.Add(m_rootsite);
			m_rootsite.SizeChanged += m_rootsite_SizeChanged;
			if (m_sandboxPanel.Height != m_rootsite.Height)
				m_sandboxPanel.Height = m_rootsite.Height;
		}

		private void m_rootsite_SizeChanged(object sender, EventArgs e)
		{
			if (m_sandboxPanel.Height != m_rootsite.Height)
				m_sandboxPanel.Height = m_rootsite.Height;
		}

		private string GetString(string id)
		{
			return m_mediator.StringTbl.GetString(id, "Linguistics/Morphology/TryAWord");
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="HtmlControl gets added to control collection and disposed there")]
		private void InitHtmlControl()
		{
			m_htmlControl = new HtmlControl
				{
					Location = new Point(0, m_resultsLabel.Bottom + 1),
					Size = new Size(m_resultsPanel.Width, m_resultsPanel.Height - (m_resultsLabel.Height + 1)),
					Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right
				};
			// Setting the Dock to fill doesn't work, as we lose the top of the HtmlControl to the
			// label control at the top of the panel.  See LT-7446 for the worst case scenario (120dpi).
			// So, set the location and size of the HTML control, and anchor it to all four sides of the
			// panel.
			m_resultsPanel.Controls.Add(m_htmlControl);
			m_htmlControl.URL = Path.Combine(TransformPath, "InitialDocument.htm");
		}

		private void SetFontInfo()
		{
			// Set writing system factory and code for the two edit boxes.
			m_wordformTextBox.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;
			m_wordformTextBox.WritingSystemCode = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;
			m_wordformTextBox.Text = "";
			m_wordformTextBox.AdjustForStyleSheet(this, m_wordPanel, m_mediator);
		}

		private void GetLastWordUsed()
		{
			var word = m_mediator.PropertyTable.GetValue("TryAWordDlg-lastWordToTry") as string;
			if (word != null)
				SetWordToUse(word.Trim());
		}

		private void SetWordToUse(string word)
		{
			m_wordformTextBox.Text = word;
			m_tryItButton.Enabled = !String.IsNullOrEmpty(word);
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
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (components != null)
					components.Dispose();
				if (m_helpProvider != null)
					m_helpProvider.Dispose();
				m_webPageInteractor = null;
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TryAWordDlg));
			this.m_wordToTryLabel = new System.Windows.Forms.Label();
			this.m_closeButton = new System.Windows.Forms.Button();
			this.m_tryItButton = new System.Windows.Forms.Button();
			this.m_resultsPanel = new System.Windows.Forms.Panel();
			this.m_resultsLabel = new System.Windows.Forms.Label();
			this.m_closePanel = new System.Windows.Forms.Panel();
			this.m_statusLabel = new System.Windows.Forms.Label();
			this.m_helpButton = new System.Windows.Forms.Button();
			this.m_wordPanel = new System.Windows.Forms.Panel();
			this.m_doSelectMorphsCheckBox = new System.Windows.Forms.CheckBox();
			this.m_doTraceCheckBox = new System.Windows.Forms.CheckBox();
			this.m_wordformTextBox = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.m_timer = new System.Windows.Forms.Timer(this.components);
			this.m_sandboxPanel = new System.Windows.Forms.Panel();
			this.m_resultsPanel.SuspendLayout();
			this.m_closePanel.SuspendLayout();
			this.m_wordPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_wordformTextBox)).BeginInit();
			this.SuspendLayout();
			//
			// m_wordToTryLabel
			//
			resources.ApplyResources(this.m_wordToTryLabel, "m_wordToTryLabel");
			this.m_wordToTryLabel.Name = "m_wordToTryLabel";
			//
			// m_closeButton
			//
			resources.ApplyResources(this.m_closeButton, "m_closeButton");
			this.m_closeButton.Name = "m_closeButton";
			this.m_closeButton.Click += new System.EventHandler(this.m_closeButton_Click);
			//
			// m_tryItButton
			//
			resources.ApplyResources(this.m_tryItButton, "m_tryItButton");
			this.m_tryItButton.Name = "m_tryItButton";
			this.m_tryItButton.Click += new System.EventHandler(this.m_tryItButton_Click);
			//
			// m_resultsPanel
			//
			this.m_resultsPanel.Controls.Add(this.m_resultsLabel);
			resources.ApplyResources(this.m_resultsPanel, "m_resultsPanel");
			this.m_resultsPanel.Name = "m_resultsPanel";
			//
			// m_resultsLabel
			//
			resources.ApplyResources(this.m_resultsLabel, "m_resultsLabel");
			this.m_resultsLabel.Name = "m_resultsLabel";
			//
			// m_closePanel
			//
			this.m_closePanel.Controls.Add(this.m_statusLabel);
			this.m_closePanel.Controls.Add(this.m_helpButton);
			this.m_closePanel.Controls.Add(this.m_closeButton);
			resources.ApplyResources(this.m_closePanel, "m_closePanel");
			this.m_closePanel.Name = "m_closePanel";
			//
			// m_statusLabel
			//
			resources.ApplyResources(this.m_statusLabel, "m_statusLabel");
			this.m_statusLabel.Name = "m_statusLabel";
			//
			// m_helpButton
			//
			resources.ApplyResources(this.m_helpButton, "m_helpButton");
			this.m_helpButton.Name = "m_helpButton";
			this.m_helpButton.Click += new System.EventHandler(this.m_buttonHelp_Click);
			//
			// m_wordPanel
			//
			this.m_wordPanel.Controls.Add(this.m_doSelectMorphsCheckBox);
			this.m_wordPanel.Controls.Add(this.m_doTraceCheckBox);
			this.m_wordPanel.Controls.Add(this.m_tryItButton);
			this.m_wordPanel.Controls.Add(this.m_wordToTryLabel);
			this.m_wordPanel.Controls.Add(this.m_wordformTextBox);
			resources.ApplyResources(this.m_wordPanel, "m_wordPanel");
			this.m_wordPanel.Name = "m_wordPanel";
			//
			// m_doSelectMorphsCheckBox
			//
			resources.ApplyResources(this.m_doSelectMorphsCheckBox, "m_doSelectMorphsCheckBox");
			this.m_doSelectMorphsCheckBox.Name = "m_doSelectMorphsCheckBox";
			this.m_doSelectMorphsCheckBox.UseVisualStyleBackColor = true;
			this.m_doSelectMorphsCheckBox.CheckedChanged += new System.EventHandler(this.m_doSelectMorphsCheckBox_CheckedChanged);
			//
			// m_doTraceCheckBox
			//
			resources.ApplyResources(this.m_doTraceCheckBox, "m_doTraceCheckBox");
			this.m_doTraceCheckBox.Name = "m_doTraceCheckBox";
			this.m_doTraceCheckBox.UseVisualStyleBackColor = true;
			this.m_doTraceCheckBox.CheckedChanged += new System.EventHandler(this.m_doTraceCheckBox_CheckedChanged);
			//
			// m_wordformTextBox
			//
			this.m_wordformTextBox.AcceptsReturn = false;
			this.m_wordformTextBox.AdjustStringHeight = true;
			this.m_wordformTextBox.BackColor = System.Drawing.SystemColors.Window;
			this.m_wordformTextBox.controlID = null;
			resources.ApplyResources(this.m_wordformTextBox, "m_wordformTextBox");
			this.m_wordformTextBox.HasBorder = true;
			this.m_wordformTextBox.Name = "m_wordformTextBox";
			this.m_wordformTextBox.SuppressEnter = true;
			this.m_wordformTextBox.WordWrap = false;
			this.m_wordformTextBox.TextChanged += new System.EventHandler(this.m_wordformTextBox_TextChanged);
			this.m_wordformTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.m_wordformTextBox_KeyDown);
			//
			// m_timer
			//
			this.m_timer.Enabled = true;
			this.m_timer.Interval = 10;
			this.m_timer.Tick += new System.EventHandler(this.m_timer_Tick);
			//
			// m_sandboxPanel
			//
			resources.ApplyResources(this.m_sandboxPanel, "m_sandboxPanel");
			this.m_sandboxPanel.Name = "m_sandboxPanel";
			//
			// TryAWordDlg
			//
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.m_resultsPanel);
			this.Controls.Add(this.m_sandboxPanel);
			this.Controls.Add(this.m_wordPanel);
			this.Controls.Add(this.m_closePanel);
			this.Name = "TryAWordDlg";
			this.m_resultsPanel.ResumeLayout(false);
			this.m_closePanel.ResumeLayout(false);
			this.m_wordPanel.ResumeLayout(false);
			this.m_wordPanel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_wordformTextBox)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		protected override void OnClosed(EventArgs ea)
		{
			base.OnClosed(ea);
			// remember last word used, if possible
			m_mediator.PropertyTable.SetProperty("TryAWordDlg-lastWordToTry", m_wordformTextBox.Text.Trim(), false, PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetPropertyPersistence("TryAWordDlg-lastWordToTry", true, PropertyTable.SettingsGroup.LocalSettings);
			m_persistProvider.PersistWindowSettings(PersistProviderID, this);
			if (m_parserListener.Connection != null)
				m_parserListener.Connection.TryAWordDialogIsRunning = false;
		}

		private void m_wordformTextBox_TextChanged(object sender, EventArgs e)
		{
			if (m_procesingTextChange)
				return;
			m_procesingTextChange = true;
			try
			{
				EnableDisableSelectMorphControls();
				m_tryItButton.Enabled = m_wordformTextBox.Text.Length > 0;
				UpdateSandboxWordform();
			}
			finally
			{
				m_procesingTextChange = false;
			}
		}

		private void UpdateSandboxWordform()
		{
			if (m_rootsite != null)
				m_rootsite.WordForm = m_wordformTextBox.Tss;
		}

		private void m_tryItButton_Click(object sender, EventArgs e)
		{
			// get a connection, if one does not exist
			if (m_parserListener.ConnectToParser())
			{
				string sWord = CleanUpWord();
				// check to see if limiting trace and, if so, if all morphs have msas
				string selectedTraceMorphs;
				if (GetSelectedTraceMorphs(out selectedTraceMorphs))
				{
					// Display a "processing" message (and include info on how to improve the results)
					m_htmlControl.URL = Path.Combine(TransformPath, "WhileTracing.htm");
					sWord = sWord.Replace(' ', '.'); // LT-7334 to allow for phrases; do this at the last minute
					m_parserListener.Connection.TryAWordDialogIsRunning = true; // make sure this is set properly
					if (m_webPageInteractor != null)
						m_webPageInteractor.ParserTrace = CreateParserTrace();
					m_tryAWordResult = m_parserListener.Connection.BeginTryAWord(sWord, DoTrace, selectedTraceMorphs);
					// waiting for result, so disable Try It button
					m_tryItButton.Enabled = false;
				}
			}
		}

		private ParserTrace CreateParserTrace()
		{
			switch (m_cache.LanguageProject.MorphologicalDataOA.ActiveParser)
			{
				case "XAmple":
					return new XAmpleTrace(m_mediator);
				case "HC":
					return new HCTrace(m_mediator);
			}
			return null;
		}

		private string CleanUpWord()
		{
			TrimWord();
			RemoveExtraDashes();
			return m_wordformTextBox.Text.Trim();
		}

		private void TrimWord()
		{
			string sTemp = m_wordformTextBox.Text.Trim();
			if (sTemp.Length != m_wordformTextBox.Text.Length)
			{
				m_wordformTextBox.Text = sTemp;
			}
		}

		private bool GetSelectedTraceMorphs(out string selectedTraceMorphs)
		{
			selectedTraceMorphs = null;
			if (DoTrace && DoManualParse)
			{
				List<int> msas = m_rootsite.MsaList;
				var sb = new StringBuilder();
				foreach (int msa in msas)
				{
					sb.Append(msa.ToString());
					sb.Append(" ");
				}
				if (sb.Length > 0)
				{
					selectedTraceMorphs = sb.ToString();
				}
				if (selectedTraceMorphs != null && (selectedTraceMorphs.StartsWith("0 ") || selectedTraceMorphs.Contains(" 0 ")))
				{
					MessageBox.Show(GetString("NoLexInfoForMorphsMessage"), GetString("NoLexInfoForMorphsCaption"),
						MessageBoxButtons.OK, MessageBoxIcon.Information);
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Convert double dashes to single dash
		/// XML considers -- to be part of a comment or some such and thus causes a crash
		/// This is a hack to merely convert -- to - (it's exceedingly unlikely a user will really want two dashes in a row)
		/// </summary>
		private void RemoveExtraDashes()
		{
			string s = m_wordformTextBox.Text;
			int i = s.IndexOf("--");
			while (i > -1)
			{
				m_wordformTextBox.Text = s.Replace("--", "-");
				m_wordformTextBox.Refresh();
				s = m_wordformTextBox.Text;
				i = s.IndexOf("--");
			}
		}

		private void m_timer_Tick(object sender, EventArgs e)
		{
			if (m_parserListener == null)
				return;

			m_statusLabel.Text = m_parserListener.ParserActivityString;

			if (m_parserListener.Connection == null)
			{
				m_statusLabel.Text = ParserStoppedMessage();
				return;
			}
			Exception ex = m_parserListener.Connection.UnhandledException;
			if (ex != null)
			{
				m_parserListener.DisconnectFromParser();
				m_statusLabel.Text = ParserStoppedMessage();
				m_tryItButton.Enabled = true;
				var iree = ex as InvalidReduplicationEnvironmentException;
				if (iree != null)
				{
					string msg = String.Format(ParserUIStrings.ksHermitCrabReduplicationProblem, iree.Morpheme,
						iree.Message);
					MessageBox.Show(this, msg, ParserUIStrings.ksBadAffixForm,
							MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				else
				{
					var app = (IApp) m_mediator.PropertyTable.GetValue("App");
					ErrorReporter.ReportException(ex, app.SettingsKey, app.SupportEmailAddress, this, false);
				}
				return;
			}

			if (m_tryAWordResult != null && m_tryAWordResult.IsCompleted)
			{
				var result = (XDocument) m_tryAWordResult.AsyncState;
				string sOutput;
				if (result == null)
				{
					// It's an error message.
					sOutput = Path.GetTempFileName();
					using (var writer = new StreamWriter(sOutput))
					{
						writer.WriteLine("<!DOCTYPE html>");
						writer.WriteLine("<body>");
						writer.WriteLine(ParserUIStrings.ksDidNotParse);
						writer.WriteLine("</body>");
						writer.WriteLine("</html>");
					}
				}
				else
				{
					sOutput = m_webPageInteractor.ParserTrace.CreateResultPage(result, DoTrace);
				}
				m_htmlControl.URL = sOutput;
				m_tryAWordResult = null;
				// got result so enable Try It button
				m_tryItButton.Enabled = true;
			}
		}

		private string ParserStoppedMessage()
		{
			return GetString("ParserStatusPrefix") + ParserUIStrings.ksNoParserLoaded + GetString("ParserStatusSuffix");
		}

		private void m_wordformTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter && m_tryItButton.Enabled)
			{
				if (m_wordformTextBox.SelectionLength > 0)
				{ // otherwise, the Enter stroke removes the word
					m_wordformTextBox.SelectionStart = m_wordformTextBox.Text.Length;
					m_wordformTextBox.SelectionLength = 0;
				}
				m_tryItButton_Click(null, null);
			}
			else
				base.OnKeyDown(e);
		}

		private bool DoTrace
		{
			get { return m_doTraceCheckBox.Checked; }
		}

		private bool DoManualParse
		{
			get { return m_doSelectMorphsCheckBox.Checked; }
		}

		/// <summary>
		/// Path to transforms
		/// </summary>
		private static string TransformPath
		{
			get { return FwDirectoryFinder.GetCodeSubDirectory(@"Language Explorer/Configuration/Words/Analyses/TraceParse"); }
		}

		private void m_buttonHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_mediator.HelpTopicProvider, HelpTopicID);
		}

		private void m_closeButton_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void m_doTraceCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			EnableDisableSelectMorphControls();
			UpdateSandboxWordform();
		}

		private void EnableDisableSelectMorphControls()
		{
			if (m_parserCanDoSelectMorphs && m_doTraceCheckBox.Checked && m_wordformTextBox.Text.Length > 0)
			{
				m_doSelectMorphsCheckBox.Enabled = true;
				m_sandboxPanel.Visible = m_doSelectMorphsCheckBox.Checked;
			}
			else
			{
				m_doSelectMorphsCheckBox.Enabled = false;
				m_sandboxPanel.Visible = false;
			}
		}

		private void m_doSelectMorphsCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (m_doSelectMorphsCheckBox.Checked)
			{
				m_sandboxPanel.Visible = true;
				UpdateSandboxWordform();
			}
			else
			{
				m_sandboxPanel.Visible = false;
			}
		}
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			m_resultsLabel.Width = m_resultsPanel.Width;
		}

		/// <summary>
		/// Prevent magic expanding dialog box for 120dpi fonts.  See LT-7446.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnLoad(EventArgs e)
		{
			Size szOld = Size;
			base.OnLoad(e);
			if (Size != szOld)
				Size = szOld;
		}


		#region IxWindow Members

		public Mediator Mediator
		{
			get { return m_mediator; }
		}

		public void SuspendWindowSizePersistence()
		{
			throw new NotImplementedException();
		}

		public void ResumeWindowSizePersistence()
		{
			throw new NotImplementedException();
		}

		public void ResumeIdleProcessing()
		{
			throw new NotImplementedException();
		}

		public void SuspendIdleProcessing()
		{
			throw new NotImplementedException();
		}

		#endregion
	}

}
