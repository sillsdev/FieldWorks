// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2003' to='2004' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TryAWordDlg.cs
// Responsibility: Andy Black
// Last reviewed:
//
// <remarks>
// Implementation of:
//		TryAWordDlg - Dialog for parsing a single wordform
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Text;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.FieldWorks.XWorks;
using XCore;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.LexText.Controls
{

	/// <summary>
	/// Summary description for TryAWordDlg.
	/// </summary>
	public class TryAWordDlg : Form, IFWDisposable, IxWindow
	{
		#region Data members

		/// <summary>
		/// xCore Mediator.
		/// </summary>
		protected Mediator m_mediator;
		/// <summary>
		/// Optional configuration parameters.
		/// </summary>
		//protected XmlNode m_configurationParameters;

		private Label m_lblWordToTry;
		private System.ComponentModel.IContainer components;

		private FwTextBox m_tbWordForm;
		private Button m_btnTryIt;
		private Button m_btnClose;
		private Panel m_pnlResults;
		private Panel m_pnlClose;
		private Panel m_pnlWord;
		private Label m_lblStatus;
		private Timer m_timer;
		private TryAWordRootSite m_rootsite;
		public FormWindowState m_windowState;

		/// <summary>
		/// The control that shows the HTML data.
		/// </summary>
		protected HtmlControl m_htmlControl;

		/// <summary>
		/// The parser trace objects
		/// </summary>
		private ParserTrace m_parserTrace;
		private readonly XAmpleTrace m_xampleTrace;
		private readonly HCTrace m_hermitCrabTrace;

		protected FdoCache m_cache;
		protected ParserListener m_parserListener;

		protected string m_sLastWordUsedPropertyName;
		private Button buttonHelp;
		protected string m_sWhileTracingFile;
		// private string m_sXAmpleSelectFile; // CS0414

		private const string s_helpTopic = "khtpTryAWord";
		private readonly HelpProvider helpProvider;

		// private string m_sOneWordMessage; // CS0414
		// private string m_sOneWordCaption; // CS0414
		private string m_sNoLexInfoForMorphsMessage;
		private string m_sNoLexInfoForMorphsCaption;
		private string m_sParserStatusPrefix;
		private string m_sParserStatusSuffix;
		private string m_sParserStatusRunning;
		private string m_sParserStatusStopped;
		private CheckBox m_cbDoTrace;

		private Timer m_connectionTimer;
		private Label m_lblResults;
		private Panel m_pnlSandbox;
		private CheckBox m_cbDoSelectMorphs;  // timer needed to wait for connection to be established
		private readonly bool m_fParserCanDoSelectMorphs = true;

		private bool m_fProcesingTextChange;
		private bool m_fJustMadeConnectionToParser;

		private readonly PersistenceProvider m_persistProvider;
		const string m_ksTryAWord = "TryAWord";
		private static TryAWordDlg m_dialog;

		private IAsyncResult m_tryAWordResult;

		private WebPageInteractor m_webPageInteractor;

		#endregion Data members
		// Using the Singleton pattern since we want/need only one instance of this dialog
		public static TryAWordDlg Instance(Mediator mediator, PersistenceProvider persistenceProvider)
		{
			if (m_dialog == null)
			{
				m_dialog = new TryAWordDlg(mediator, persistenceProvider);
				var form = (FwXWindow) mediator.PropertyTable.GetValue("window");
				if (persistenceProvider != null)
					persistenceProvider.RestoreWindowSettings(m_ksTryAWord, m_dialog);
				m_dialog.Show(form);
				m_dialog.InitStatusMaterial();
				// This allows Keyman to work correctly on initial typing.
				// Marc Durdin suggested switching to a different window and back.
				// PostMessage gets into the queue after the dialog settles down, so it works.
				Win32.PostMessage(form.Handle, Win32.WinMsgs.WM_SETFOCUS, 0, 0);
				Win32.PostMessage(m_dialog.Handle, Win32.WinMsgs.WM_SETFOCUS, 0, 0);
			}
			else
			{
				if (m_dialog.WindowState == FormWindowState.Minimized)
				{
					m_dialog.WindowState = m_dialog.m_windowState == FormWindowState.Maximized ? FormWindowState.Maximized : FormWindowState.Normal;
				}
				else
				{
					m_dialog.WindowState = m_dialog.m_windowState;
					m_dialog.Activate();
				}
			}
			return m_dialog;
		}


		/// <summary>
		/// For testing
		/// </summary>
		private TryAWordDlg()
		{
		}
		/// <summary>
		/// The real deal
		/// </summary>
		/// <param name="mediator">The mediator.</param>
		/// <param name="persistenceProvider">The persistence provider.</param>
		private TryAWordDlg(Mediator mediator, PersistenceProvider persistenceProvider)
		{
			m_mediator = mediator;
			m_persistProvider = persistenceProvider;
			m_xampleTrace = new XAmpleTrace(mediator);
			m_hermitCrabTrace = new HCTrace(mediator);
			m_parserTrace = m_xampleTrace; // we'll start with the default one; it can get changed by the user
			m_cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			m_parserListener = (ParserListener)m_mediator.PropertyTable.GetValue("ParserListener");

			m_sLastWordUsedPropertyName = m_cache.ProjectId.Name + "TryAWordDlg-lastWordToTry";
			m_sWhileTracingFile = Path.Combine(TransformPath, "WhileTracing.htm");
			// m_sXAmpleSelectFile = Path.Combine(Path.GetTempPath(), m_cache.DatabaseName + "XAmpleSelectFile.txt"); // CS0414

			m_connectionTimer = new Timer {Interval = 250};
			m_connectionTimer.Tick += m_connectionTimer_Tick;

			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = GetType().Name;
			// Ensure that <Enter> triggers a parse instead of splitting the word.  See FWR-3539.
			m_tbWordForm.SuppressEnter = true;

			Text = m_cache.ProjectId.UiName + " - " + Text;
			// order is important between SetInitialWord and SetRootSite
			SetRootSite();
			SetInitialWord();

			InitHtmlControl();

			SetStrings();

			// HermitCrab does not currently support selected tracing
			if (m_cache.LangProject.MorphologicalDataOA.ActiveParser == "HC")
			{
				m_fParserCanDoSelectMorphs = false;
				m_cbDoSelectMorphs.Enabled = false;
			}

			// No such thing as FwApp.App now: if(FwApp.App != null) // Could be null during testing
			if (m_mediator.HelpTopicProvider != null) // trying this
			{
				helpProvider = new HelpProvider();
				helpProvider.HelpNamespace = m_mediator.HelpTopicProvider.HelpFile;
				helpProvider.SetHelpKeyword(this, m_mediator.HelpTopicProvider.GetHelpString(s_helpTopic));
				helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			}
		}

		public void InitStatusMaterial()
		{
			SetInitialStatusMessage();
			if (Connection != null)
			{
				Connection.TryAWordDialogIsRunning = true;
			}
		}

		private void SetInitialStatusMessage()
		{
			// NB: cannot be called until SetStrings() is called
			if (Connection != null)
				m_lblStatus.Text = m_sParserStatusPrefix + m_sParserStatusRunning + m_sParserStatusSuffix;
			else
				m_lblStatus.Text = ParserStoppedMessage();
		}

		private void SetRootSite()
		{
			m_rootsite = new TryAWordRootSite(m_cache, m_mediator);
			//m_rootsite.Location = new Point(m_cbDoTrace.Location.X + 15, m_cbDoTrace.Location.Y + 20);
			//m_rootsite.Size = new Size(Width - 25, (m_lblResults.Location.Y - m_rootsite.Location.Y));
			m_rootsite.Dock = DockStyle.Top;
			//m_rootsite.AutoSize = true;
			//m_rootsite.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowOnly;
			m_pnlSandbox.Controls.Add(m_rootsite);
			m_rootsite.SizeChanged += m_rootsite_SizeChanged;
			if (m_pnlSandbox.Height != m_rootsite.Height)
				m_pnlSandbox.Height = m_rootsite.Height;
			//m_lblResults.Location = new Point(m_lblResults.Location.X, m_rootsite.Location.Y + m_rootsite.Height + 15);
		}

		void m_rootsite_SizeChanged(object sender, EventArgs e)
		{
			if (m_pnlSandbox.Height != m_rootsite.Height)
				m_pnlSandbox.Height = m_rootsite.Height;
		}

		void m_connectionTimer_Tick(object sender, EventArgs e)
		{
			if (Connection != null)
			{
				m_connectionTimer.Stop();  // now have a connection, so stop the timer
				TryTheWord();
			}
		}

		private void SetStrings()
		{
			const string ksPath = "Linguistics/Morphology/TryAWord";
			// m_sOneWordMessage = m_mediator.StringTbl.GetString("OnlyOneWordMessage", ksPath); // CS0414
			// m_sOneWordCaption = m_mediator.StringTbl.GetString("OnlyOneWordCaption", ksPath); // CS0414
			m_sNoLexInfoForMorphsMessage = m_mediator.StringTbl.GetString("NoLexInfoForMorphsMessage", ksPath);
			m_sNoLexInfoForMorphsCaption = m_mediator.StringTbl.GetString("NoLexInfoForMorphsCaption", ksPath);
			m_sParserStatusPrefix = m_mediator.StringTbl.GetString("ParserStatusPrefix", ksPath);
			m_sParserStatusSuffix = m_mediator.StringTbl.GetString("ParserStatusSuffix", ksPath);
			m_sParserStatusRunning = ParserUIStrings.ksIdle_;
			m_sParserStatusStopped = ParserUIStrings.ksNoParserLoaded;

		}
		private void InitHtmlControl()
		{
			m_htmlControl = new HtmlControl();
			// Setting the Dock to fill doesn't work, as we lose the top of the HtmlControl to the
			// label control at the top of the panel.  See LT-7446 for the worst case scenario (120dpi).
			// So, set the location and size of the HTML control, and anchor it to all four sides of the
			// panel.
			m_htmlControl.Location = new Point(0, m_lblResults.Bottom + 1);
			m_htmlControl.Size = new Size(m_pnlResults.Width, m_pnlResults.Height - (m_lblResults.Height + 1));
			m_htmlControl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right;
			m_pnlResults.Controls.Add(m_htmlControl);
			m_htmlControl.URL = Path.Combine(TransformPath, "InitialDocument.htm");

			m_webPageInteractor = new WebPageInteractor(m_htmlControl, m_parserTrace, m_mediator, m_tbWordForm);
#if !__MonoCS__
			m_htmlControl.Browser.ObjectForScripting = m_webPageInteractor;
#endif
		}

		protected void	SetInitialWord()
		{
			SetFontInfo();

			string sCurrentControl = m_mediator.PropertyTable.GetStringProperty("currentContentControl", null);
			if (sCurrentControl != null)
			{
				if (sCurrentControl != "Analyses" && sCurrentControl != "wordListConcordance")
				{
					// use the last wordform used in Try A Word if we're not in a control that lists out wordforms
					GetLastWordUsed();
					return;
				}
			}
			// we are in a control that lists out wordforms; try to get that wordform
			Object x = m_mediator.PropertyTable.GetValue("concordanceWords-selected");
			if (x == null)
			{
				// nothing set or no wordforms to use yet
				GetLastWordUsed();
				return;
			}

			IWfiWordform wordform = null;
			var info = x as RecordNavigationInfo;
			if (info != null)
			{
				wordform = info.Clerk.CurrentObject as IWfiWordform;
			}
			if (wordform == null)
			{
				// can't find the selected wordform
				GetLastWordUsed();
				return;
			}
			SetWordToUse(wordform.Form.VernacularDefaultWritingSystem.Text);
		}

		private void SetFontInfo()
		{
			// Set writing system factory and code for the two edit boxes.
			m_tbWordForm.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;
			m_tbWordForm.WritingSystemCode = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;
			m_tbWordForm.Text = "";
			m_tbWordForm.AdjustForStyleSheet(this, m_pnlWord, m_mediator);
		}

		protected void GetLastWordUsed()
		{
			Object x = m_mediator.PropertyTable.GetValue(m_sLastWordUsedPropertyName);
			if (x == null)
			{
				return;
			}

			var sWord = x as string;
			if (sWord == null)
				return;
			SetWordToUse(sWord.Trim());
		}

		private void SetWordToUse(string sWord)
		{
			m_tbWordForm.Text = sWord;
			m_btnTryIt.Enabled = !String.IsNullOrEmpty(sWord);
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if (m_connectionTimer != null)
				{
					m_connectionTimer.Stop();
					m_connectionTimer.Tick -= m_connectionTimer_Tick;
					m_connectionTimer.Dispose();
				}

				if(components != null)
				{
					components.Dispose();
				}
				m_connectionTimer = null;

			}
			base.Dispose( disposing );
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
			this.m_lblWordToTry = new System.Windows.Forms.Label();
			this.m_btnClose = new System.Windows.Forms.Button();
			this.m_btnTryIt = new System.Windows.Forms.Button();
			this.m_pnlResults = new System.Windows.Forms.Panel();
			this.m_lblResults = new System.Windows.Forms.Label();
			this.m_pnlClose = new System.Windows.Forms.Panel();
			this.m_lblStatus = new System.Windows.Forms.Label();
			this.buttonHelp = new System.Windows.Forms.Button();
			this.m_pnlWord = new System.Windows.Forms.Panel();
			this.m_cbDoSelectMorphs = new System.Windows.Forms.CheckBox();
			this.m_cbDoTrace = new System.Windows.Forms.CheckBox();
			this.m_tbWordForm = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.m_timer = new System.Windows.Forms.Timer(this.components);
			this.m_pnlSandbox = new System.Windows.Forms.Panel();
			this.m_pnlResults.SuspendLayout();
			this.m_pnlClose.SuspendLayout();
			this.m_pnlWord.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_tbWordForm)).BeginInit();
			this.SuspendLayout();
			//
			// m_lblWordToTry
			//
			resources.ApplyResources(this.m_lblWordToTry, "m_lblWordToTry");
			this.m_lblWordToTry.Name = "m_lblWordToTry";
			//
			// m_btnClose
			//
			resources.ApplyResources(this.m_btnClose, "m_btnClose");
			this.m_btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btnClose.Name = "m_btnClose";
			this.m_btnClose.Click += new System.EventHandler(this.m_btnClose_Click);
			//
			// m_btnTryIt
			//
			resources.ApplyResources(this.m_btnTryIt, "m_btnTryIt");
			this.m_btnTryIt.Name = "m_btnTryIt";
			this.m_btnTryIt.Click += new System.EventHandler(this.m_btnTryIt_Click);
			//
			// m_pnlResults
			//
			this.m_pnlResults.Controls.Add(this.m_lblResults);
			resources.ApplyResources(this.m_pnlResults, "m_pnlResults");
			this.m_pnlResults.Name = "m_pnlResults";
			//
			// m_lblResults
			//
			resources.ApplyResources(this.m_lblResults, "m_lblResults");
			this.m_lblResults.Name = "m_lblResults";
			//
			// m_pnlClose
			//
			this.m_pnlClose.Controls.Add(this.m_lblStatus);
			this.m_pnlClose.Controls.Add(this.buttonHelp);
			this.m_pnlClose.Controls.Add(this.m_btnClose);
			resources.ApplyResources(this.m_pnlClose, "m_pnlClose");
			this.m_pnlClose.Name = "m_pnlClose";
			//
			// m_lblStatus
			//
			resources.ApplyResources(this.m_lblStatus, "m_lblStatus");
			this.m_lblStatus.Name = "m_lblStatus";
			//
			// buttonHelp
			//
			resources.ApplyResources(this.buttonHelp, "buttonHelp");
			this.buttonHelp.Name = "buttonHelp";
			this.buttonHelp.Click += new System.EventHandler(this.buttonHelp_Click);
			//
			// m_pnlWord
			//
			this.m_pnlWord.Controls.Add(this.m_cbDoSelectMorphs);
			this.m_pnlWord.Controls.Add(this.m_cbDoTrace);
			this.m_pnlWord.Controls.Add(this.m_btnTryIt);
			this.m_pnlWord.Controls.Add(this.m_lblWordToTry);
			this.m_pnlWord.Controls.Add(this.m_tbWordForm);
			resources.ApplyResources(this.m_pnlWord, "m_pnlWord");
			this.m_pnlWord.Name = "m_pnlWord";
			//
			// m_cbDoSelectMorphs
			//
			resources.ApplyResources(this.m_cbDoSelectMorphs, "m_cbDoSelectMorphs");
			this.m_cbDoSelectMorphs.Name = "m_cbDoSelectMorphs";
			this.m_cbDoSelectMorphs.UseVisualStyleBackColor = true;
			this.m_cbDoSelectMorphs.CheckedChanged += new System.EventHandler(this.m_cbDoSelectMorphs_CheckedChanged);
			//
			// m_cbDoTrace
			//
			resources.ApplyResources(this.m_cbDoTrace, "m_cbDoTrace");
			this.m_cbDoTrace.Name = "m_cbDoTrace";
			this.m_cbDoTrace.UseVisualStyleBackColor = true;
			this.m_cbDoTrace.CheckedChanged += new System.EventHandler(this.m_cbDoTrace_CheckedChanged);
			//
			// m_tbWordForm
			//
			this.m_tbWordForm.AdjustStringHeight = true;
			this.m_tbWordForm.BackColor = System.Drawing.SystemColors.Window;
			this.m_tbWordForm.controlID = null;
			resources.ApplyResources(this.m_tbWordForm, "m_tbWordForm");
			this.m_tbWordForm.HasBorder = true;
			this.m_tbWordForm.Name = "m_tbWordForm";
			this.m_tbWordForm.SelectionLength = 0;
			this.m_tbWordForm.SelectionStart = 0;
			this.m_tbWordForm.TextChanged += new System.EventHandler(this.m_tbWordForm_TextChanged);
			this.m_tbWordForm.KeyDown += new System.Windows.Forms.KeyEventHandler(this.m_tbWordForm_KeyDown);
			//
			// m_timer
			//
			this.m_timer.Enabled = true;
			this.m_timer.Interval = 10;
			this.m_timer.Tick += new System.EventHandler(this.m_timer_Tick);
			//
			// m_pnlSandbox
			//
			resources.ApplyResources(this.m_pnlSandbox, "m_pnlSandbox");
			this.m_pnlSandbox.Name = "m_pnlSandbox";
			//
			// TryAWordDlg
			//
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.m_btnClose;
			this.Controls.Add(this.m_pnlResults);
			this.Controls.Add(this.m_pnlSandbox);
			this.Controls.Add(this.m_pnlWord);
			this.Controls.Add(this.m_pnlClose);
			this.Name = "TryAWordDlg";
			this.m_pnlResults.ResumeLayout(false);
			this.m_pnlClose.ResumeLayout(false);
			this.m_pnlWord.ResumeLayout(false);
			this.m_pnlWord.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_tbWordForm)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		protected override void OnClosed(EventArgs ea)
		{
			base.OnClosed(ea);
			// remember last word used, if possible
			m_mediator.PropertyTable.SetProperty(m_sLastWordUsedPropertyName, m_tbWordForm.Text.Trim());
			m_persistProvider.PersistWindowSettings(m_ksTryAWord, m_dialog);
			m_dialog = null;
			if (Connection != null)
				Connection.TryAWordDialogIsRunning = false;
		}

		private void m_tbWordForm_TextChanged(object sender, EventArgs e)
		{
			if (m_fProcesingTextChange)
				return;
			m_fProcesingTextChange = true;
			try
			{
				EnableDiableSelectMorphControls();
				m_btnTryIt.Enabled = m_tbWordForm.Text.Length > 0;
				UpdateSandboxWordform();
			}
			finally
			{
				m_fProcesingTextChange = false;
			}
		}

		private void UpdateSandboxWordform()
		{
			if (m_rootsite != null)
				m_rootsite.WordForm = m_tbWordForm.Tss;
		}

		private void m_btnTryIt_Click(object sender, EventArgs e)
		{
			// get a connection, if one does not exist
			if (Connection == null)
			{
				m_mediator.BroadcastMessageUntilHandled("ReInitParser", null);
				// Now we need to wait for the message to be handled.
				// We'll know it's done when there's a connection
				m_connectionTimer.Start();
				m_fJustMadeConnectionToParser = true;
				return;
			}
			TryTheWord();
		}

		private void TryTheWord()
		{
			string sWord = CleanUpWord();
			// check to see if limiting trace and, if so, if all morphs have msas
			string sSelectTraceMorphs;
			if (!GetSelectTraceMorphs(out sSelectTraceMorphs))
				return;
			// Display a "processing" message (and include info on how to improve the results)
			m_htmlControl.URL = m_sWhileTracingFile;
			sWord = sWord.Replace(' ', '.'); // LT-7334 to allow for phrases; do this at the last minute
			Connection.TryAWordDialogIsRunning = true; // make sure this is set properly

			SetParserTrace();

			if (m_webPageInteractor != null)
				m_webPageInteractor.ParserTrace = m_parserTrace;

			m_tryAWordResult = Connection.BeginTryAWord(sWord, DoTrace, sSelectTraceMorphs);
			// waiting for result, so disable Try It button
			m_btnTryIt.Enabled = false;
		}

		private void SetParserTrace()
		{
			if (DoTrace)
			{
				switch (m_cache.LanguageProject.MorphologicalDataOA.ActiveParser)
				{
					case "XAmple":
						m_parserTrace = m_xampleTrace;
						break;

					case "HC":
						m_parserTrace = m_hermitCrabTrace;
						break;
				}
			}
		}

		private string CleanUpWord()
		{
			TrimWord();
			RemoveExtraDashes();
			return m_tbWordForm.Text.Trim();
		}

		private void TrimWord()
		{
			string sTemp = m_tbWordForm.Text.Trim();
			if (sTemp.Length != m_tbWordForm.Text.Length)
			{
				m_tbWordForm.Text = sTemp;
			}
		}

		private bool GetSelectTraceMorphs(out string sSelectTraceMorphs)
		{
			sSelectTraceMorphs = null;
			if (DoTrace && DoManualParse)
			{
				sSelectTraceMorphs = CollectTraceMorphs(sSelectTraceMorphs);
				if (sSelectTraceMorphs != null && (sSelectTraceMorphs.StartsWith("0 ") || sSelectTraceMorphs.Contains(" 0 ")))
				{
					MessageBox.Show(m_sNoLexInfoForMorphsMessage, m_sNoLexInfoForMorphsCaption,
						MessageBoxButtons.OK, MessageBoxIcon.Information);
					return false;
				}
			}
			return true;
		}

		private string CollectTraceMorphs(string sSelectTraceMorphs)
		{
			System.Collections.Generic.List<int> msas = m_rootsite.MsaList;
			var sb = new StringBuilder();
			foreach (int msa in msas)
			{
				sb.Append(msa.ToString());
				sb.Append(" ");
			}
			if (sb.Length > 0)
			{
				sSelectTraceMorphs = sb.ToString();
			}
			return sSelectTraceMorphs;
		}

		/// <summary>
		/// Convert double dashes to single dash
		/// XML considers -- to be part of a comment or some such and thus causes a crash
		/// This is a hack to merely convert -- to - (it's exceedingly unlikely a user will really want two dashes in a row)
		/// </summary>
		private void RemoveExtraDashes()
		{
			string s = m_tbWordForm.Text;
			int i = s.IndexOf("--");
			while (i > -1)
			{
				m_tbWordForm.Text = s.Replace("--", "-");
				m_tbWordForm.Refresh();
				s = m_tbWordForm.Text;
				i = s.IndexOf("--");
			}
		}

		private void m_timer_Tick(object sender, EventArgs e)
		{
			if (m_parserListener != null)
			{
				m_lblStatus.Text = m_parserListener.GetParserActivityString();
			}

			ParserConnection conn = Connection;
			if (conn == null)
			{
				m_lblStatus.Text = ParserStoppedMessage();
				return;
			}
			Exception ex = conn.UnhandledException;
			if (ex != null)
			{
				conn.Dispose();
				Connection = null;
				m_lblStatus.Text = ParserStoppedMessage();
				m_btnTryIt.Enabled = true;
				var app = (IApp) m_mediator.PropertyTable.GetValue("App");
				ErrorReporter.ReportException(ex, app.SettingsKey, app.SupportEmailAddress, this, false);
				return;
			}

			if (m_fJustMadeConnectionToParser)
			{
				conn.TryAWordDialogIsRunning = true;
				m_fJustMadeConnectionToParser = false;
			}

			if (m_tryAWordResult != null && m_tryAWordResult.IsCompleted)
			{
				string sOutput = m_parserTrace.CreateResultPage((string) m_tryAWordResult.AsyncState);
				m_htmlControl.URL = sOutput;
				m_tryAWordResult = null;
				// got result so enable Try It button
				m_btnTryIt.Enabled = true;
			}
		}

		private string ParserStoppedMessage()
		{
			return m_sParserStatusPrefix + m_sParserStatusStopped + m_sParserStatusSuffix;
		}

		void m_tbWordForm_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter && m_btnTryIt.Enabled)
			{
				if (m_tbWordForm.SelectionLength > 0)
				{ // otherwise, the Enter stroke removes the word
					m_tbWordForm.SelectionStart = m_tbWordForm.Text.Length;
					m_tbWordForm.SelectionLength = 0;
				}
				m_btnTryIt_Click(null, null);
			}
			else
				base.OnKeyDown(e);
		}
		private bool DoTrace
		{
			get { return m_cbDoTrace.Checked; }
		}
		private bool DoManualParse
		{
			get { return m_cbDoSelectMorphs.Checked; }
		}
		/// <summary>
		/// Path to transforms
		/// </summary>
		private static string TransformPath
		{
			get { return DirectoryFinder.GetFWCodeSubDirectory(@"Language Explorer/Configuration/Words/Analyses/TraceParse"); }
		}

		private ParserConnection Connection
		{
			get
			{
				return (ParserConnection)m_mediator.PropertyTable.GetValue("ParserConnection");
			}

			set
			{
				m_mediator.PropertyTable.SetProperty("ParserConnection", value);
				m_mediator.PropertyTable.SetPropertyPersistence("ParserConnection", false);
			}
		}

		private void buttonHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_mediator.HelpTopicProvider, s_helpTopic);
		}

		private void m_btnClose_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void m_cbDoTrace_CheckedChanged(object sender, EventArgs e)
		{
			EnableDiableSelectMorphControls();
			UpdateSandboxWordform();
		}
		private void EnableDiableSelectMorphControls()
		{
			if (m_fParserCanDoSelectMorphs && m_cbDoTrace.Checked && m_tbWordForm.Text.Length > 0)
			{
				m_cbDoSelectMorphs.Enabled = true;
				m_pnlSandbox.Visible = m_cbDoSelectMorphs.Checked;
			}
			else
			{
				m_cbDoSelectMorphs.Enabled = false;
				m_pnlSandbox.Visible = false;
			}
		}

		private void m_cbDoSelectMorphs_CheckedChanged(object sender, EventArgs e)
		{
			if (m_cbDoSelectMorphs.Checked)
			{
				m_pnlSandbox.Visible = true;
				UpdateSandboxWordform();
			}
			else
			{
				m_pnlSandbox.Visible = false;
			}
		}
		protected override void OnResize(EventArgs e)
		{
			if (WindowState != FormWindowState.Minimized)
				m_windowState = WindowState; // remember the state before it might be minimized
			base.OnResize(e);
			m_lblResults.Width = m_pnlResults.Width;
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
