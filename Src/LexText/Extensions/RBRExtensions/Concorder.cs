#define FULLLISTING
// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Concorder.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.Utils; // For IFWDisposable & StringTable
using XCore; // For Mediator
using SIL.FieldWorks.Common.FwUtils;

namespace RBRExtensions
{
	/// <summary>
	/// A Concorder is used to identify the users of any number of objects.
	/// </summary>
	internal class Concorder : Form, IFWDisposable
	{
		#region Data members

		private XWindow m_xwindow;
		private StatusBar m_statusBar1;
		private StatusBarPanel m_statusBarPanel1;
		private ConcorderControl m_concorderControl;
		private Button m_btnHelp;
		private Button m_btnClose;
		private Mediator m_myOwnPrivateMediator;
		private FdoCache m_cache;

		#endregion Data members

		#region Properties

		#endregion Properties

		#region Construction, Initialization, and disposal

		/// <summary>
		/// Constructor.
		/// </summary>
		internal Concorder()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			m_btnHelp.Enabled = true;  //have to set this here because the designer delete
											// ConcorderControl m_concorderControl;
											// if we try to enable this Help button it.
		}

		/// <summary>This is the message value that is used to communicate the need to process the defered mediator queue</summary>
		public const int WM_BROADCAST_ITEM_INQUEUE = 0x8000 + 0x77;	// wm_app + 0x77
		public const int WM_BROADCAST_CLOSE_CONCORDER = 0x8000 + 0x78;
		protected override void WndProc(ref Message m)
		{
			if (m.Msg == WM_BROADCAST_ITEM_INQUEUE)	// mediator queue message
			{
				m_myOwnPrivateMediator.ProcessItem();	// let the mediator service an item from the queue

				return;	// no need to pass on to base wndproc
			}

			if (m.Msg == WM_BROADCAST_CLOSE_CONCORDER)
			{
				Close();
			}

			base.WndProc(ref m);
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			m_myOwnPrivateMediator.MainWindow = this;
		}

		/// <summary>
		/// Initialize an Concorder dlg.
		/// </summary>
		/// <param name="xwindow">The main window for this modeless dlg.</param>
		/// <param name="configurationNode">The XML node to control the ConcorderControl.</param>
		internal void SetDlgInfo(XWindow xwindow, XmlNode configurationNode)
		{
			CheckDisposed();

			if (xwindow == null)
				throw new ArgumentNullException("configurationNode", "Main window is missing.");

			if (m_myOwnPrivateMediator != null)
				throw new InvalidOperationException("It is not legal to call this method more than once.");

			m_xwindow = xwindow;
			m_cache = m_xwindow.Mediator.PropertyTable.GetValue("cache") as FdoCache;
			m_myOwnPrivateMediator = new Mediator
							{
								FeedbackInfoProvider = m_xwindow.Mediator.FeedbackInfoProvider
							};
			// The extension XML files should be stored in the data area, not in the code area.
			// This reduces the need for users to have administrative privileges.
			var dir = DirectoryFinder.GetFWDataSubDirectory("Language Explorer/Configuration/Extensions/Concorder");
			m_myOwnPrivateMediator.StringTbl = new StringTable(dir);
			m_myOwnPrivateMediator.PropertyTable.SetProperty("cache", m_cache, false);
			m_myOwnPrivateMediator.PropertyTable.SetPropertyPersistence("cache", false);
			m_myOwnPrivateMediator.PropertyTable.SetPropertyDispose("cache", false);
			m_myOwnPrivateMediator.SpecificToOneMainWindow = true;
			//FwStyleSheet styleSheet = m_xwindow.Mediator.PropertyTable.GetValue("FwStyleSheet") as FwStyleSheet;
			//m_myOwnPrivateMediator.PropertyTable.SetProperty("FwStyleSheet", styleSheet);
			//m_myOwnPrivateMediator.PropertyTable.SetPropertyPersistence("FwStyleSheet", false);
			//m_myOwnPrivateMediator.PropertyTable.SetPropertyPersistence("FwStyleSheet", false);
			var fcfList = new List<ConcorderControl.FindComboFillerBase>();

			// Find: main POS:
			// UsedBy:  MSAs, senses, entries, wordforms, compound rules
			ConcorderControl.FindComboFillerBase fcf = new ConcorderControl.FindPossibilityComboFiller(m_cache.LangProject.PartsOfSpeechOA);
			fcf.Init(m_myOwnPrivateMediator, configurationNode.SelectSingleNode("sourcecontrols/control[@id='srcGrammaticalCategory']"));
			fcfList.Add(fcf);

			// Find: Sense
			// UsedBy: wordforms
			fcf = new ConcorderControl.FindComboFiller();
			fcf.Init(m_myOwnPrivateMediator, configurationNode.SelectSingleNode("sourcecontrols/control[@id='srcSense']"));
			fcfList.Add(fcf);

			// Find: Allomorph (main and alternates)
			// UsedBy: wordforms, ad hoc rules [MoAlloAdhocProhib x3 props]
			fcf = new ConcorderControl.FindComboFiller();
			fcf.Init(m_myOwnPrivateMediator, configurationNode.SelectSingleNode("sourcecontrols/control[@id='srcAllomorph']"));
			fcfList.Add(fcf);

			// Find: entry
			// UsedBy: wordforms
			fcf = new ConcorderControl.FindComboFiller();
			fcf.Init(m_myOwnPrivateMediator, configurationNode.SelectSingleNode("sourcecontrols/control[@id='srcEntry']"));
			fcfList.Add(fcf);
/*
			//// Find: MSA
			//// UsedBy: senses, wordforms
			//fcf = new ConcorderControl.FindComboFiller();
			//fcf.Init(m_myOwnPrivateMediator, configurationNode.SelectSingleNode("sourcecontrols/control[@id='srcGrammaticalFunction']"));
			//fcfList.Add(fcf);
*/

			// Find: Environment
			// UsedBy: Allomorphs
			fcf = new ConcorderControl.FindComboFiller();
			fcf.Init(m_myOwnPrivateMediator, configurationNode.SelectSingleNode("sourcecontrols/control[@id='srcEnvironment']"));
			fcfList.Add(fcf);

			/*
									//// Find: Word-level gloss
									//// UsedBy: wordforms
									//fcf = new ConcorderControl.FindComboFiller();
									//fcf.Init(m_myOwnPrivateMediator, configurationNode.SelectSingleNode("sourcecontrols/control[@id='srcWordLevelGloss']"));
									//fcfList.Add(fcf);

									// Find: Phonemes
									// UsedBy:

									// Find: Natural Classes
									// UsedBy:

									// Find: Features
									// UsedBy:

									// Find: Exception Features
									// UsedBy:

						//			// Find: eng rev POS
						//			// UsedBy: rev entries
						//			ubfList = new List<ConcorderControl.UsedByFiller>();
						//			fcfConfigNode = configurationNode.SelectSingleNode("sourcecontrols/control[@id='srcXX']");
						//			fcf = new ConcorderControl.FindComboFiller("Grammatical Category: English Reversal", ubfList);
						//			fcfList.Add(fcf);
						//			ubfList.Add(new ConcorderControl.UsedByFiller("Reversal Entries" ));

						//			// Find: spn rev POS
						//			// UsedBy: rev entries
						//			ubfList = new List<ConcorderControl.UsedByFiller>();
						//			fcfConfigNode = configurationNode.SelectSingleNode("sourcecontrols/control[@id='srcXX']");
						//			fcf = new ConcorderControl.FindComboFiller("Grammatical Category: Spanish Reversal", ubfList);
						//			fcfList.Add(fcf);
						//			ubfList.Add(new ConcorderControl.UsedByFiller("Reversal Entries" ));

									//// Find: Academic Domain
									//// UsedBy: Senses
									//ubfList = new List<ConcorderControl.UsedByFiller>();
									//fcf = new ConcorderControl.FindPossibilityComboFiller(
									//    configurationNode.SelectSingleNode("sourcecontrols/control[@id='srcAcademicDomain']"),
									//    ubfList,
									//    m_cache.LangProject.LexDbOA.DomainTypesOA);
									//fcfList.Add(fcf);
									//ubfList.Add(new ConcorderControl.UsedByFiller(
									//    configurationNode.SelectSingleNode("targetcontrols/control[@id='Senses']")));

									//// Find: Anthropology Category
									//// UsedBy: Senses
									//ubfList = new List<ConcorderControl.UsedByFiller>();
									//fcf = new ConcorderControl.FindPossibilityComboFiller(
									//    configurationNode.SelectSingleNode("sourcecontrols/control[@id='srcAnthropologyCategory']"),
									//    ubfList,
									//    m_cache.LangProject.AnthroListOA);
									//fcfList.Add(fcf);
									//ubfList.Add(new ConcorderControl.UsedByFiller(
									//    configurationNode.SelectSingleNode("targetcontrols/control[@id='Senses']")));

									// Find: Confidence Level
									// UsedBy:

									// Find: Education Level
									// UsedBy:

									//// Find: Entry Type
									//// UsedBy: Entries
									//ubfList = new List<ConcorderControl.UsedByFiller>();
									//fcf = new ConcorderControl.FindPossibilityComboFiller(
									//    configurationNode.SelectSingleNode("sourcecontrols/control[@id='srcEntryType']"),
									//    ubfList,
									//    m_cache.LangProject.LexDbOA.EntryTypesOA);
									//fcfList.Add(fcf);
									//ubfList.Add(new ConcorderControl.UsedByFiller(
									//    configurationNode.SelectSingleNode("targetcontrols/control[@id='Entries']")));

									// Find: Feature Type
									// UsedBy:

									// Find: Lexical Reference Type
									// UsedBy:

									// Find: Location
									// UsedBy:

									// Find: Minor Entry Condition
									// UsedBy: Entries
									//ubfList = new List<ConcorderControl.UsedByFiller>();
									//// TODO: Where is its pos list?
									//fcf = new ConcorderControl.FindComboFiller(
									//    configurationNode.SelectSingleNode("sourcecontrols/control[@id='srcMinorEntryCondition']"),
									//    ubfList);
									//fcfList.Add(fcf);
									//ubfList.Add(new ConcorderControl.UsedByFiller("Entries" ));

									//// Find: Morpheme Type
									//// UsedBy: Allomorphs
									//ubfList = new List<ConcorderControl.UsedByFiller>();
									//fcf = new ConcorderControl.FindPossibilityComboFiller(
									//    configurationNode.SelectSingleNode("sourcecontrols/control[@id='srcMorphemeType']"),
									//    ubfList,
									//    m_cache.LangProject.LexDbOA.MorphTypesOA);
									//fcfList.Add(fcf);
									//ubfList.Add(new ConcorderControl.UsedByFiller(
									//    configurationNode.SelectSingleNode("targetcontrols/control[@id='Allomorphs']")));

									// Find: Translation Type
									// UsedBy:

									// Find: People
									// UsedBy:

									// Find: Positions
									// UsedBy:

									// Find: Restrictions
									// UsedBy:

									//// Find: Semantic Domain
									//// UsedBy: Senses
									//ubfList = new List<ConcorderControl.UsedByFiller>();
									//fcf = new ConcorderControl.FindPossibilityComboFiller(
									//    configurationNode.SelectSingleNode("sourcecontrols/control[@id='srcSemanticDomain']"),
									//    ubfList,
									//    m_cache.LangProject.SemanticDomainListOA);
									//fcfList.Add(fcf);
									//ubfList.Add(new ConcorderControl.UsedByFiller(
									//    configurationNode.SelectSingleNode("targetcontrols/control[@id='Senses']")));
			*/

			// Find: Sense Type
			// UsedBy: Senses
			fcf = new ConcorderControl.FindPossibilityComboFiller(m_cache.LangProject.LexDbOA.SenseTypesOA);
			fcf.Init(m_myOwnPrivateMediator, configurationNode.SelectSingleNode("sourcecontrols/control[@id='srcSenseType']"));
			fcfList.Add(fcf);

			// Find: Status
			// UsedBy: Senses
			fcf = new ConcorderControl.FindPossibilityComboFiller(m_cache.LangProject.StatusOA);
			fcf.Init(m_myOwnPrivateMediator, configurationNode.SelectSingleNode("sourcecontrols/control[@id='srcStatus']"));
			fcfList.Add(fcf);

			// Find: Usages
			// UsedBy: Senses
			fcf = new ConcorderControl.FindPossibilityComboFiller(m_cache.LangProject.LexDbOA.UsageTypesOA);
			fcf.Init(m_myOwnPrivateMediator, configurationNode.SelectSingleNode("sourcecontrols/control[@id='srcUsages']"));
			fcfList.Add(fcf);

			m_concorderControl.SetupDlg(m_myOwnPrivateMediator, fcfList, fcfList[0]);
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
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			if (disposing)
			{
				// Do this after base class call,
				// as it will dispose the browse view,
				// which wants the mediator still kicking.
				// when it tires to get a RecordClerk out of it.
				if (m_myOwnPrivateMediator != null)
					m_myOwnPrivateMediator.Dispose();
			}
			m_xwindow = null;
			m_cache = null;
			m_myOwnPrivateMediator = null;
		}

		#endregion Construction, Initialization, and disposal

		#region Other methods

		#endregion Other methods

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Concorder));
			this.m_statusBar1 = new System.Windows.Forms.StatusBar();
			this.m_statusBarPanel1 = new System.Windows.Forms.StatusBarPanel();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_btnClose = new System.Windows.Forms.Button();
			this.m_concorderControl = new RBRExtensions.ConcorderControl();
			((System.ComponentModel.ISupportInitialize)(this.m_statusBarPanel1)).BeginInit();
			this.SuspendLayout();
			//
			// m_statusBar1
			//
			resources.ApplyResources(this.m_statusBar1, "m_statusBar1");
			this.m_statusBar1.Name = "m_statusBar1";
			this.m_statusBar1.Panels.AddRange(new System.Windows.Forms.StatusBarPanel[] {
			this.m_statusBarPanel1});
			//
			// m_statusBarPanel1
			//
			resources.ApplyResources(this.m_statusBarPanel1, "m_statusBarPanel1");
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_btnClose
			//
			resources.ApplyResources(this.m_btnClose, "m_btnClose");
			this.m_btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btnClose.Name = "m_btnClose";
			this.m_btnClose.UseVisualStyleBackColor = true;
			this.m_btnClose.Click += new System.EventHandler(this.m_btnClose_Click);
			//
			// m_concorderControl
			//
			resources.ApplyResources(this.m_concorderControl, "m_concorderControl");
			this.m_concorderControl.MinimumSize = new System.Drawing.Size(0, 0);
			this.m_concorderControl.Name = "m_concorderControl";
			//
			// Concorder
			//
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.m_btnClose);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_concorderControl);
			this.Controls.Add(this.m_statusBar1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "Concorder";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			((System.ComponentModel.ISupportInitialize)(this.m_statusBarPanel1)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		#region Event Handlers

		private void m_btnClose_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_xwindow.Mediator.HelpTopicProvider, "khtpConcorderTool");
		}

		#endregion Event Handlers
	}
}
