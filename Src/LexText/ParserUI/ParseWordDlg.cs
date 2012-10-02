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
// File: ParseWordDlg.cs
// Responsibility: Andy Black
// Last reviewed:
//
// <remarks>
// Implementation of:
//		ParseWordDlg - Dialog for parsing a single wordform
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Text;
using System.Xml;
using System.Diagnostics;
using System.Xml.Xsl;
using System.Xml.XPath;
using MsHtmHstInterop;
using System.Runtime.InteropServices;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.IText;
using SIL.Utils;
using SIL.FieldWorks.XWorks;
using SIL.FieldWorks.WordWorks.Parser;
using XCore;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for ParseWordDlg.
	/// </summary>
	public class ParseWordDlg : Form, IFWDisposable
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

		private System.Windows.Forms.Label m_lblWordToTry;
		private System.ComponentModel.IContainer components;

		private SIL.FieldWorks.Common.Widgets.FwTextBox m_tbWordForm;
		private System.Windows.Forms.Button m_btnTryIt;
		private System.Windows.Forms.Button m_btnClose;
		private System.Windows.Forms.Panel m_pnlResults;
		private System.Windows.Forms.Panel m_pnlClose;
		private System.Windows.Forms.Panel m_pnlWord;
		private Label m_lblStatus;
		private System.Windows.Forms.Timer m_timer;
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
		private XAmpleTrace m_xampleTrace;
		private HCTrace m_hermitCrabTrace;

		protected FdoCache m_cache;

		protected string m_sLastWordUsedPropertyName;
		private System.Windows.Forms.Button buttonHelp;
		protected string m_sWhileTracingFile;
		private string m_sXAmpleSelectFile;

		private const string s_helpTopic = "khtpTryAWord";
		private System.Windows.Forms.HelpProvider helpProvider;

		private string m_sOneWordMessage;
		private string m_sOneWordCaption;
		private string m_sNoLexInfoForMorphsMessage;
		private string m_sNoLexInfoForMorphsCaption;
		private string m_sParserStatusPrefix;
		private string m_sParserStatusSuffix;
		private string m_sParserStatusRunning;
		private string m_sParserStatusStopped;
		private CheckBox m_cbDoTrace;

		private System.Windows.Forms.Timer m_connectionTimer;
		private Label m_lblResults;
		private Panel m_pnlSandbox;
		private CheckBox m_cbDoSelectMorphs;  // timer needed to wait for connection to be established
		private bool m_fParserCanDoSelectMorphs = true;

		private bool m_fProcesingTextChange = false;
		private bool m_fJustMadeConnectionToParser = false;
		#endregion Data members

		/// <summary>
		/// For testing
		/// </summary>
		public ParseWordDlg()
		{
		}
		/// <summary>
		/// The real deal
		/// </summary>
		/// <param name="mediator"></param>
		public ParseWordDlg(Mediator mediator)
		{
			m_mediator = mediator;
			m_xampleTrace = new XAmpleTrace(mediator);
			m_hermitCrabTrace = new HCTrace(mediator);
			m_parserTrace = m_xampleTrace; // we'll start with the default one; it can get changed by the user
			m_cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			m_sLastWordUsedPropertyName = m_cache.DatabaseName + "ParseWordDlg-lastWordToTry";
			m_sWhileTracingFile = Path.Combine(TransformPath, "WhileTracing.htm");
			m_sXAmpleSelectFile = Path.Combine(Path.GetTempPath(), m_cache.DatabaseName + "XAmpleSelectFile.txt");

			m_connectionTimer = new System.Windows.Forms.Timer();
			m_connectionTimer.Interval = 250; // use a quarter of a second
			m_connectionTimer.Tick += new EventHandler(m_connectionTimer_Tick);

			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			this.Text = m_cache.DatabaseName + " - " + this.Text;
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

			if(FwApp.App != null) // Could be null during testing
			{
				this.helpProvider = new System.Windows.Forms.HelpProvider();
				this.helpProvider.HelpNamespace = FwApp.App.HelpFile;
				this.helpProvider.SetHelpKeyword(this, FwApp.App.GetHelpString(s_helpTopic, 0));
				this.helpProvider.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);
			}
		}

		public void InitStatusMaterial()
		{
			SetInitialStatusMessage();
			if (Connection != null)
			{
				Connection.TryAWordDialogIsRunning = true;
				SubscribeToParserEvents();
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
			m_rootsite.SizeChanged += new EventHandler(m_rootsite_SizeChanged);
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
			m_sOneWordMessage = m_mediator.StringTbl.GetString("OnlyOneWordMessage", ksPath);
			m_sOneWordCaption = m_mediator.StringTbl.GetString("OnlyOneWordCaption", ksPath);
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

			m_htmlControl.Browser.ObjectForScripting = new WebPageInteractor(m_htmlControl, m_parserTrace, m_mediator, m_tbWordForm);
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

			WfiWordform wordform = null;
			RecordNavigationInfo info = x as RecordNavigationInfo;
			if (info != null)
			{
				wordform = info.Clerk.CurrentObject as WfiWordform;
			}
			if (wordform == null)
			{
				// can't find the selected wordform
				GetLastWordUsed();
				return;
			}
			SetWordToUse(wordform.Form.VernacularDefaultWritingSystem);
		}

		private void SetFontInfo()
		{
			// Set writing system factory and code for the two edit boxes.
			m_tbWordForm.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;
			m_tbWordForm.WritingSystemCode = m_cache.LangProject.DefaultVernacularWritingSystem;
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

			string sWord = x as string;
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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if (m_connectionTimer != null)
				{
					m_connectionTimer.Stop();
					m_connectionTimer.Tick -= new EventHandler(m_connectionTimer_Tick);
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ParseWordDlg));
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
			this.m_tbWordForm.AllowMultipleLines = false;
			this.m_tbWordForm.BackColor = System.Drawing.SystemColors.Window;
			this.m_tbWordForm.controlID = null;
			this.m_tbWordForm.HasBorder = true;
			resources.ApplyResources(this.m_tbWordForm, "m_tbWordForm");
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
			// ParseWordDlg
			//
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.m_btnClose;
			this.Controls.Add(this.m_pnlResults);
			this.Controls.Add(this.m_pnlSandbox);
			this.Controls.Add(this.m_pnlWord);
			this.Controls.Add(this.m_pnlClose);
			this.Name = "ParseWordDlg";
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
			UnSubscribeToParserEvents();
			if (Connection != null)
				Connection.TryAWordDialogIsRunning = false;
		}

		private void m_tbWordForm_TextChanged(object sender, System.EventArgs e)
		{
			if (m_fProcesingTextChange)
				return;
			m_fProcesingTextChange = true;
			try
			{
				EnableDiableSelectMorphControls();
				if (m_tbWordForm.Text.Length > 0)
					m_btnTryIt.Enabled = true;
				else
					m_btnTryIt.Enabled = false;
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

		private void m_btnTryIt_Click(object sender, System.EventArgs e)
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
			Connection.ParseAllWordforms = false;

			SetParserTrace();
			WebPageInteractor webPageInteractor = (WebPageInteractor)m_htmlControl.Browser.ObjectForScripting;
			if (webPageInteractor != null)
				webPageInteractor.ParserTrace = m_parserTrace;

			Connection.TryAWordAsynchronously(sWord, DoTrace, sSelectTraceMorphs);
			// waiting for result, so disable Try It button
			m_btnTryIt.Enabled = false;
		}

		private void SetParserTrace()
		{
			if (DoTrace)
			{
				switch (Connection.Parser.Parser)
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
			StringBuilder sb = new StringBuilder();
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

		private void m_timer_Tick(object sender, System.EventArgs e)
		{
			if (this.Connection == null)
			{
				m_lblStatus.Text = ParserStoppedMessage();
				return;
			}
			if (m_fJustMadeConnectionToParser)
			{
				Connection.TryAWordDialogIsRunning = true;
				SubscribeToParserEvents();
				m_fJustMadeConnectionToParser = false;
			}

			string result = this.Connection.GetAndClearTraceResult();
			if (result != null)
			{
				string sOutput = m_parserTrace.CreateResultPage(result);
				m_htmlControl.URL = sOutput;
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
			if ((e.KeyCode == Keys.Enter)
				&& (m_btnTryIt.Enabled)
				)
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
		private string TransformPath
		{
			get { return DirectoryFinder.GetFWCodeSubDirectory(@"Language Explorer\Configuration\Words\Analyses\TraceParse"); }
		}

		public ParserConnection Connection
		{
			get
			{
				return (ParserConnection)m_mediator.PropertyTable.GetValue("ParserConnection");
			}
		}

		private void buttonHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(FwApp.App, s_helpTopic);
		}

		private void m_btnClose_Click(object sender, EventArgs e)
		{
			this.Close();
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
				if (m_cbDoSelectMorphs.Checked)
					m_pnlSandbox.Visible = true;
				else
					m_pnlSandbox.Visible = false;
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
			Size szOld = this.Size;
			base.OnLoad(e);
			if (this.Size != szOld)
				this.Size = szOld;
		}

		public void SubscribeToParserEvents()
		{
			if (Connection != null)
				Connection.SubscribeToParserEvents(true, ParserUpdateHandler);
		}

		public void UnSubscribeToParserEvents()
		{
			if (Connection != null)
				Connection.UnsubscribeToParserEvents();
		}
		//this is invoked by the parser connection, on our own event handling thread.
		protected void ParserUpdateHandler(ParserScheduler parser, TaskReport task)
		{
			Trace.WriteLine("   In ParserUpdateHandler");
			try
			{
				switch (task.Phase)
				{
					case TaskReport.TaskPhase.started:
						m_lblStatus.Text = m_sParserStatusPrefix + task.Description + m_sParserStatusSuffix;
						Trace.WriteLine("   started: " + task.Description);
						break;
					case TaskReport.TaskPhase.finished:
						m_lblStatus.Text = m_sParserStatusPrefix + task.Description + m_sParserStatusSuffix;
						Trace.WriteLine("   finished: " + task.Description);
						Trace.WriteLine("   finished: Duration: " + task.DurationSeconds.ToString() + " seconds");
						Trace.WriteLineIf(task.Details != null, "finished: Details: " + task.Details);

						break;
					default:
						m_lblStatus.Text = m_sParserStatusPrefix + task.Description + m_sParserStatusSuffix;
						Trace.WriteLine("   default: " + task.Description + "    " + task.PhaseDescription);
						break;
				}
			}
			catch (ObjectDisposedException ode)
			{
				// By the time we get any "finished" tasks, they have been disposed.  Ignore them.
				Trace.WriteLine("   " + ode.Message);
				Trace.WriteLine("   " + ode.StackTrace);
			}
		}

	}

	[System.Runtime.InteropServices.ComVisible(true)]
	public class WebPageInteractor
	{
		private HtmlControl m_htmlControl;
		private ParserTrace m_parserTrace;
		private Mediator m_mediator;
		private FdoCache m_cache;
		private SIL.FieldWorks.Common.Widgets.FwTextBox m_tbWordForm;

		/// <summary>
		/// Requires a language object
		/// </summary>
		/// <param name="lang"></param>
		public WebPageInteractor(HtmlControl htmlControl, ParserTrace parserTrace, Mediator mediator, SIL.FieldWorks.Common.Widgets.FwTextBox tbWordForm)
		{
			m_htmlControl = htmlControl;
			m_parserTrace = parserTrace;
			m_mediator = mediator;
			m_cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			m_tbWordForm = tbWordForm;
		}

		/// <summary>
		/// Set the current parser to use when tracing
		/// </summary>
		public ParserTrace ParserTrace
		{
			set
			{
				m_parserTrace = value;
			}
		}
		/// <summary>
		/// Have the main FLEx window jump to the appropriate item
		/// </summary>
		/// <param name="hvo">item whose parent will indcate where to jump to</param>
		public void JumpToToolBasedOnHvo(int hvo)
		{
			if (hvo == 0)
				return;
			string sTool = null;
			int parentClassId = 0;
			ICmObject obj = CmObject.CreateFromDBObject(m_cache, hvo);
			switch (obj.ClassID)
			{
				case MoForm.kclsidMoForm:								   // fall through
				case MoAffixAllomorph.kclsidMoAffixAllomorph:			   // fall through
				case MoStemAllomorph.kclsidMoStemAllomorph:				   // fall through
				case MoInflAffMsa.kclsidMoInflAffMsa:  // fall through
				case MoDerivAffMsa.kclsidMoDerivAffMsa:  // fall through
				case MoUnclassifiedAffixMsa.kclsidMoUnclassifiedAffixMsa:  // fall through
				case MoStemMsa.kclsidMoStemMsa:                            // fall through
				case MoMorphSynAnalysis.kclsidMoMorphSynAnalysis:
				case MoAffixProcess.kclsidMoAffixProcess:
					sTool = "lexiconEdit";
					parentClassId = LexEntry.kclsidLexEntry;
					break;
				case MoInflAffixSlot.kclsidMoInflAffixSlot:          // fall through
				case MoInflAffixTemplate.kclsidMoInflAffixTemplate:  // fall through
				case PartOfSpeech.kclsidPartOfSpeech:
					sTool = "posEdit";
					parentClassId = PartOfSpeech.kclsidPartOfSpeech;
					break;
					// still need to test compound rule ones
				case MoCoordinateCompound.kclsidMoCoordinateCompound:
				case MoEndoCompound.kclsidMoEndoCompound:
				case MoExoCompound.kclsidMoExoCompound:
					sTool = "compoundRuleAdvancedEdit";
					parentClassId = obj.ClassID;
					break;
				case PhRegularRule.kclsidPhRegularRule: // fall through
				case PhMetathesisRule.kclsidPhMetathesisRule:
					sTool = "PhonologicalRuleEdit";
					parentClassId = obj.ClassID;
					break;
			}
			if (parentClassId <= 0)
				return; // do nothing
			int parentHvo = CmObjectUi.GetParentOfClass(m_cache, hvo, parentClassId);
			if (parentHvo <= 0)
				return; // do nothing
			m_mediator.PostMessage("FollowLink",
				SIL.FieldWorks.FdoUi.FwLink.Create(sTool, m_cache.GetGuidFromId(parentHvo), m_cache.ServerName, m_cache.DatabaseName));
		}
		/// <summary>
		/// Change mouse cursor to a hand when the mouse is moved over an object
		/// </summary>
		public void MouseMove()
		{
			Cursor.Current = Cursors.Hand;
		}
		/// <summary>
		/// Show the first pass of the Word Grammar Debugger
		/// </summary>
		/// <param name="sNodeId">The node id in the XAmple trace to use</param>
		public void ShowWordGrammarDetail(string sNodeId)
		{
			string sForm = AdjustForm(m_tbWordForm.Text);
			m_htmlControl.URL = m_parserTrace.SetUpWordGrammarDebuggerPage(sNodeId, sForm, m_htmlControl.URL);
		}
		/// <summary>
		/// Try another pass in the Word Grammar Debugger
		/// </summary>
		/// <param name="sNodeId">the node id of the step to try</param>
		public void TryWordGrammarAgain(string sNodeId)
		{
			string sForm = AdjustForm(m_tbWordForm.Text);
			m_htmlControl.URL = m_parserTrace.PerformAnotherWordGrammarDebuggerStepPage(sNodeId, sForm, m_htmlControl.URL);
		}
		/// <summary>
		/// Back up a page in the Word Grammar Debugger
		/// </summary>
		/// <remarks>
		/// We cannot merely use the history mechanism of the html control
		/// because we need to keep track of the xml page source file as well as the html page.
		/// This info is kept in the WordGrammarStack.
		/// </remarks>
		public void GoToPreviousWordGrammarPage()
		{
			m_htmlControl.URL = m_parserTrace.PopWordGrammarStack();
		}
		/// <summary>
		/// Modify the content of the form to use entities when needed
		/// </summary>
		/// <param name="sForm">form to adjust</param>
		/// <returns>adjusted form</returns>
		protected string AdjustForm(string sForm)
		{
			string sResult1 = sForm.Replace("&", "&amp;");
			string sResult2 = sResult1.Replace("<", "&lt;");
			return sResult2;
		}
	}

	[XCore.MediatorDispose]
	public class TraceWordDialogLauncher : IxCoreColleague, IFWDisposable
	{
		protected Mediator m_mediator;
		protected ParseWordDlg m_dialog;
		protected XCore.PersistenceProvider m_persistProvider;
		const string m_ksTryAWord = "TryAWord";
		/// <summary>
		/// Constructor.
		/// </summary>
		public TraceWordDialogLauncher()
		{
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

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
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~TraceWordDialogLauncher()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
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
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_mediator != null)
					m_mediator.RemoveColleague(this);
				if (m_dialog != null)
					m_dialog = null;
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_mediator = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#region IxCoreColleague implementation

		/// <summary>
		/// Initialize the IxCoreColleague object.
		/// </summary>
		public void Init(Mediator mediator, XmlNode configurationParameters)
		{
			CheckDisposed();

			m_mediator = mediator;
			mediator.AddColleague(this);
			if (m_mediator.PropertyTable != null)
				m_persistProvider = new PersistenceProvider(m_ksTryAWord, m_mediator.PropertyTable);
		}

		/// <summary>
		/// return an array of all of the objects which should
		/// 1) be queried when looking for someone to deliver a message to
		/// 2) be potential recipients of a broadcast
		/// </summary>
		/// <returns></returns>
		public IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			return new IxCoreColleague[]{this};
		}

		#endregion IxCoreColleague implementation

		#region XCORE Message Handlers

		public bool OnDisplayTraceWord(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			// display.Enabled = (m_dialog == null);
			return true;	//we handled this.
		}

		/// <summary>
		/// Handles the xWorks message for Try A Word
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>false</returns>
		public bool OnTraceWord(object argument)
		{
			CheckDisposed();

			if (m_dialog == null)
			{
				m_dialog = new ParseWordDlg(m_mediator);
				m_dialog.FormClosed += new FormClosedEventHandler(m_dialog_FormClosed);
				XWorks.FwXWindow form = (XWorks.FwXWindow) m_mediator.PropertyTable.GetValue("window");
				if (m_persistProvider != null)
					m_persistProvider.RestoreWindowSettings(m_ksTryAWord, m_dialog);
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
					if (m_dialog.m_windowState == FormWindowState.Maximized)
						m_dialog.WindowState = FormWindowState.Maximized;
					else
						m_dialog.WindowState = FormWindowState.Normal;
				}
				else
				{
					m_dialog.WindowState = m_dialog.m_windowState;
					m_dialog.Activate();
				}
			}
			return true; // we handled this
		}

		void m_dialog_FormClosed(object sender, FormClosedEventArgs e)
		{
			if (m_dialog != null)
			{
				if (m_persistProvider != null)
					m_persistProvider.PersistWindowSettings(m_ksTryAWord, m_dialog);
				m_dialog = null;
			}
		}

		#endregion XCORE Message Handlers
	}
}
