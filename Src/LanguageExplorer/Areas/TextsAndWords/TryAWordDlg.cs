// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs.Controls;
using SIL.LCModel;
using SIL.Utils;

namespace LanguageExplorer.Areas.TextsAndWords
{
	/// <summary />
	internal sealed class TryAWordDlg : Form, IFlexComponent
	{
		private const string PersistProviderID = "TryAWord";
		private const string HelpTopicID = "khtpTryAWord";

		#region Data members
		private LcmCache m_cache;
		private ParserMenuManager m_parserMenuManager;
		private IPersistenceProvider m_persistProvider;
		private HelpProvider m_helpProvider;
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
		private IParserTrace m_trace;
		private ISharedEventHandlers _sharedEventHandlers;
		#endregion Data members

		/// <summary />
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
		}

		#endregion

		internal void SetDlgInfo(ISharedEventHandlers sharedEventHandlers, IWfiWordform wordform, ParserMenuManager parserMenuManager)
		{
			_sharedEventHandlers = sharedEventHandlers;
			m_persistProvider = PersistenceProviderFactory.CreatePersistenceProvider(PropertyTable);
			m_cache = PropertyTable.GetValue<LcmCache>(LanguageExplorerConstants.cache);
			m_parserMenuManager = parserMenuManager;

			Text = $@"{m_cache.ProjectId.UiName} - {Text}";
			SetRootSite();
			SetFontInfo();
			// restore window location and size after setting up the form textbox, because it might adjust size of
			// window causing the window to grow every time it is opened
			m_persistProvider.RestoreWindowSettings(PersistProviderID, this);
			if (wordform == null)
			{
				GetLastWordUsed();
			}
			else
			{
				SetWordToUse(wordform.Form.VernacularDefaultWritingSystem.Text);
			}
			m_webPageInteractor = new WebPageInteractor(m_htmlControl, Publisher, m_cache, m_wordformTextBox);
			var helpTopicProvider = PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider);
			if (helpTopicProvider != null) // trying this
			{
				m_helpProvider.HelpNamespace = helpTopicProvider.HelpFile;
				m_helpProvider.SetHelpKeyword(this, helpTopicProvider.GetHelpString(HelpTopicID));
				m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			}
			if (m_parserMenuManager.Connection != null)
			{
				m_parserMenuManager.Connection.TryAWordDialogIsRunning = true;
				m_statusLabel.Text = GetString("ParserStatusPrefix") + ParserUIStrings.ksIdle_ + GetString("ParserStatusSuffix");
			}
			else
			{
				m_statusLabel.Text = ParserStoppedMessage();
			}
		}

		private void SetRootSite()
		{
			m_rootsite = new TryAWordRootSite(_sharedEventHandlers)
			{
				Dock = DockStyle.Top
			};
			m_rootsite.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
			m_sandboxPanel.Controls.Add(m_rootsite);
			m_rootsite.SizeChanged += m_rootsite_SizeChanged;
			if (m_sandboxPanel.Height != m_rootsite.Height)
			{
				m_sandboxPanel.Height = m_rootsite.Height;
			}
		}

		private void m_rootsite_SizeChanged(object sender, EventArgs e)
		{
			if (m_sandboxPanel.Height != m_rootsite.Height)
			{
				m_sandboxPanel.Height = m_rootsite.Height;
			}
		}

		private string GetString(string id)
		{
			return StringTable.Table.GetString(id, "Linguistics/Morphology/TryAWord");
		}

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
			var uri = new Uri(Path.Combine(TransformPath, "InitialDocument.htm"));
			m_htmlControl.URL = uri.AbsoluteUri;
		}

		private void SetFontInfo()
		{
			// Set writing system factory and code for the two edit boxes.
			m_wordformTextBox.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;
			m_wordformTextBox.WritingSystemCode = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;
			m_wordformTextBox.Text = string.Empty;
			m_wordformTextBox.AdjustForStyleSheet(this, m_wordPanel, PropertyTable);
		}

		private void GetLastWordUsed()
		{
			var word = PropertyTable.GetValue<string>("TryAWordDlg-lastWordToTry");
			if (word != null)
			{
				SetWordToUse(word.Trim());
			}
		}

		private void SetWordToUse(string word)
		{
			m_wordformTextBox.Text = word;
			m_tryItButton.Enabled = !string.IsNullOrEmpty(word);
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
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
				m_helpProvider?.Dispose();
			}
			base.Dispose(disposing);

			m_webPageInteractor = null;
			m_cache = null;
			m_parserMenuManager = null;
			m_persistProvider = null;
			m_helpProvider = null;
			m_trace = null;
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
			this.m_wordformTextBox = new SIL.FieldWorks.FwCoreDlgs.Controls.FwTextBox();
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
			PropertyTable.SetProperty("TryAWordDlg-lastWordToTry", m_wordformTextBox.Text.Trim(), true, settingsGroup: SettingsGroup.LocalSettings);
			m_persistProvider.PersistWindowSettings(PersistProviderID, this);
			if (m_parserMenuManager.Connection == null)
			{
				return;
			}
			m_parserMenuManager.Connection.TryAWordDialogIsRunning = false;
			m_parserMenuManager.DisconnectFromParser();
		}

		private void m_wordformTextBox_TextChanged(object sender, EventArgs e)
		{
			if (m_procesingTextChange)
			{
				return;
			}
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
			{
				m_rootsite.WordForm = m_wordformTextBox.Tss;
			}
		}

		private void m_tryItButton_Click(object sender, EventArgs e)
		{
			// get a connection, if one does not exist
			if (!m_parserMenuManager.ConnectToParser())
			{
				return;
			}
			var sWord = CleanUpWord();
			// check to see if limiting trace and, if so, if all morphs have msas
			int[] selectedTraceMorphs;
			if (!GetSelectedTraceMorphs(out selectedTraceMorphs))
			{
				return;
			}
			// Display a "processing" message (and include info on how to improve the results)
			var uri = new Uri(Path.Combine(TransformPath, "WhileTracing.htm"));
			m_htmlControl.URL = uri.AbsoluteUri;
			sWord = sWord.Replace(' ', '.'); // LT-7334 to allow for phrases; do this at the last minute
			m_parserMenuManager.Connection.TryAWordDialogIsRunning = true; // make sure this is set properly
			m_tryAWordResult = m_parserMenuManager.Connection.BeginTryAWord(sWord, DoTrace, selectedTraceMorphs);
			// waiting for result, so disable Try It button
			m_tryItButton.Enabled = false;
		}

		private void CreateResultPage(XDocument result)
		{
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
				IParserTrace trace = null;
				switch (m_cache.LanguageProject.MorphologicalDataOA.ActiveParser)
				{
					case "XAmple":
						trace = new XAmpleTrace();
						m_webPageInteractor.WordGrammarDebugger = new XAmpleWordGrammarDebugger(PropertyTable, result);
						break;
					case "HC":
						trace = new HCTrace();
						m_webPageInteractor.WordGrammarDebugger = null;
						break;
				}

				Debug.Assert(trace != null);

				sOutput = trace.CreateResultPage(PropertyTable, result, DoTrace);
			}
			var uri = new Uri(sOutput);
			m_htmlControl.URL = uri.AbsoluteUri;
		}

		private string CleanUpWord()
		{
			TrimWord();
			RemoveExtraDashes();
			return m_wordformTextBox.Text.Trim();
		}

		private void TrimWord()
		{
			var sTemp = m_wordformTextBox.Text.Trim();
			if (sTemp.Length != m_wordformTextBox.Text.Length)
			{
				m_wordformTextBox.Text = sTemp;
			}
		}

		private bool GetSelectedTraceMorphs(out int[] selectedTraceMorphs)
		{
			selectedTraceMorphs = null;
			if (!DoTrace || !DoManualParse)
			{
				return true;
			}
			selectedTraceMorphs = m_rootsite.MsaList.ToArray();
			if (selectedTraceMorphs.All(hvo => hvo != 0))
			{
				return true;
			}
			MessageBox.Show(GetString("NoLexInfoForMorphsMessage"), GetString("NoLexInfoForMorphsCaption"), MessageBoxButtons.OK, MessageBoxIcon.Information);
			return false;
		}

		/// <summary>
		/// Convert double dashes to single dash
		/// XML considers -- to be part of a comment or some such and thus causes a crash
		/// This is a hack to merely convert -- to - (it's exceedingly unlikely a user will really want two dashes in a row)
		/// </summary>
		private void RemoveExtraDashes()
		{
			var s = m_wordformTextBox.Text;
			var i = s.IndexOf("--");
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
			if (m_parserMenuManager == null)
			{
				return;
			}

			m_statusLabel.Text = m_parserMenuManager.ParserActivityString;

			if (m_parserMenuManager.Connection == null)
			{
				m_statusLabel.Text = ParserStoppedMessage();
				return;
			}
			var ex = m_parserMenuManager.Connection.UnhandledException;
			if (ex != null)
			{
				m_parserMenuManager.DisconnectFromParser();
				m_statusLabel.Text = ParserStoppedMessage();
				m_tryItButton.Enabled = true;
				var app = PropertyTable.GetValue<IApp>(LanguageExplorerConstants.App);
				ErrorReporter.ReportException(ex, app.SettingsKey, app.SupportEmailAddress, this, false);
				return;
			}
			if (m_tryAWordResult != null && m_tryAWordResult.IsCompleted)
			{
				var result = (XDocument)m_tryAWordResult.AsyncState;
				CreateResultPage(result);
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
			{
				OnKeyDown(e);
			}
		}

		private bool DoTrace => m_doTraceCheckBox.Checked;

		private bool DoManualParse => m_doSelectMorphsCheckBox.Checked;

		/// <summary>
		/// Path to transforms
		/// </summary>
		private static string TransformPath => FwDirectoryFinder.GetCodeSubDirectory(@"Language Explorer/Configuration/Words/Analyses/TraceParse");

		private void m_buttonHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider), HelpTopicID);
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
		protected override void OnLoad(EventArgs e)
		{
			var szOld = Size;
			base.OnLoad(e);
			if (Size != szOld)
			{
				Size = szOld;
			}
		}
	}
}