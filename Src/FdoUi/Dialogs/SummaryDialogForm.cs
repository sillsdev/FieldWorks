// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: SummaryDialogForm.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.FdoUi.Dialogs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// SummaryDialogForm is the dialog that TE launches from its Find In Lexicon command,
	/// when there is a matching lexical entry. It displays a representation of the lex entry,
	/// with buttons to find other similar entries, or to open Flex showing the relevant entry.
	/// NOTE: after calling ShowDialog, clients should test the ShouldLink property, and if it
	/// is true, call LinkToLexicon().
	/// This unusual pattern is used, instead of having the button do the linking directly,
	/// because it is necessary to fully close the dialog before invoking the link. Otherwise,
	/// TE will jump back in front of Flex even before Flex has finished jumping to the entry.
	/// See LT-3461.
	/// </summary>
	/// <remarks>
	/// Clients should also test the OtherButtonClicked property to see if a "find entry" dialog
	/// should pop up to find a different entry to display with a new SummaryDialogForm.
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	internal class SummaryDialogForm : Form, IFWDisposable
	{
		#region Member variables
		private List<int> m_rghvo;
		private int m_hvoSelected;		// object selected in the view.
		private ITsString m_tssWf;
		private XmlView m_xv;
		private FdoCache m_cache;
		private XCore.Mediator m_mediator;
		private IHelpTopicProvider m_helpProvider;
		private string m_helpFileKey;
//		private IVwStylesheet m_vss;
		private System.Windows.Forms.Button btnOther;
		private System.Windows.Forms.Button btnLexicon;
		private System.Windows.Forms.Button btnClose;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Button btnHelp;

		private System.ComponentModel.Container components = null;
		private const string s_helpTopicKey = "khtpFindInDictionary";
		private System.Windows.Forms.HelpProvider helpProvider;
		private bool m_fShouldLink; // set true by btnLexicon_Click, caller should call LinkToLexicon after dialog closes.
		private bool m_fOtherClicked;	// set true by btnOther_Click, caller should call OtherButtonClicked after dialog closes.
		#endregion

		#region Constructor/destructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for a single LexEntry object.
		/// </summary>
		/// <param name="leui">The lex entry ui.</param>
		/// <param name="tssForm">The TSS form.</param>
		/// <param name="helpProvider">The help provider.</param>
		/// <param name="helpFileKey">string key to get the help file name</param>
		/// <param name="styleSheet">The stylesheet.</param>
		/// ------------------------------------------------------------------------------------
		internal SummaryDialogForm(LexEntryUi leui, ITsString tssForm, IHelpTopicProvider helpProvider,
			string helpFileKey, IVwStylesheet styleSheet)
		{
			InitializeComponent();
			AccessibleName = GetType().Name;

			m_rghvo = new List<int>(1);
			m_rghvo.Add(leui.Object.Hvo);
			m_cache = leui.Object.Cache;
			m_mediator = leui.Mediator;
			Initialize(tssForm, helpProvider, helpFileKey, styleSheet);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for multiple matching LexEntry objects.
		/// </summary>
		/// <param name="rghvo">The rghvo.</param>
		/// <param name="tssForm">The TSS form.</param>
		/// <param name="helpProvider">The help provider.</param>
		/// <param name="helpFileKey">The help file key.</param>
		/// <param name="styleSheet">The stylesheet.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="mediator">The mediator.</param>
		/// ------------------------------------------------------------------------------------
		internal SummaryDialogForm(List<int> rghvo, ITsString tssForm, IHelpTopicProvider helpProvider,
			string helpFileKey, IVwStylesheet styleSheet, FdoCache cache, Mediator mediator)
		{
			InitializeComponent();
			AccessibleName = GetType().Name;

			Debug.Assert(rghvo != null && rghvo.Count > 0);
			m_rghvo = rghvo;
			m_cache = cache;
			m_mediator = mediator;
			Initialize(tssForm, helpProvider, helpFileKey, styleSheet);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Common initialization shared by the constructors.
		/// </summary>
		/// <param name="tssForm">The TSS form.</param>
		/// <param name="helpProvider">The help provider.</param>
		/// <param name="helpFileKey">The help file key.</param>
		/// <param name="styleSheet">The stylesheet.</param>
		/// ------------------------------------------------------------------------------------
		private void Initialize(ITsString tssForm, IHelpTopicProvider helpProvider, string helpFileKey,
			IVwStylesheet styleSheet)
		{
			m_tssWf = tssForm;
			m_helpProvider = helpProvider;
//			m_vss = styleSheet;
			if (m_helpProvider == null)
			{
				btnHelp.Enabled = false;
			}
			else
			{
				m_helpFileKey = helpFileKey;
				this.helpProvider = new HelpProvider();
				this.helpProvider.HelpNamespace = FwDirectoryFinder.CodeDirectory + m_helpProvider.GetHelpString("UserHelpFile");
				this.helpProvider.SetHelpKeyword(this, m_helpProvider.GetHelpString(s_helpTopicKey));
				this.helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			}
			m_xv = CreateSummaryView(m_rghvo, m_cache, styleSheet);
			m_xv.Dock = DockStyle.Top;	// panel1 is docked to the bottom.
			m_xv.TabStop = true;
			m_xv.TabIndex = 0;
			Controls.Add(m_xv);
			m_xv.Height = panel1.Location.Y - m_xv.Location.Y;
			m_xv.Width = this.Width - 15; // Changed from magic to more magic on 8/8/2014
			m_xv.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			m_xv.EditingHelper.DefaultCursor = Cursors.Arrow;
			m_xv.EditingHelper.VwSelectionChanged += new EventHandler<VwSelectionArgs>(m_xv_VwSelectionChanged);
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

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}
		#endregion

		#region Windows Form Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SummaryDialogForm));
			this.btnOther = new System.Windows.Forms.Button();
			this.btnLexicon = new System.Windows.Forms.Button();
			this.btnClose = new System.Windows.Forms.Button();
			this.panel1 = new System.Windows.Forms.Panel();
			this.btnHelp = new System.Windows.Forms.Button();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			//
			// btnOther
			//
			resources.ApplyResources(this.btnOther, "btnOther");
			this.btnOther.Name = "btnOther";
			this.btnOther.Click += new System.EventHandler(this.btnOther_Click);
			//
			// btnLexicon
			//
			resources.ApplyResources(this.btnLexicon, "btnLexicon");
			this.btnLexicon.Name = "btnLexicon";
			this.btnLexicon.Click += new System.EventHandler(this.btnLexicon_Click);
			//
			// btnClose
			//
			this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.btnClose, "btnClose");
			this.btnClose.Name = "btnClose";
			//
			// panel1
			//
			resources.ApplyResources(this.panel1, "panel1");
			this.panel1.BackColor = System.Drawing.Color.Transparent;
			this.panel1.Controls.Add(this.btnHelp);
			this.panel1.Controls.Add(this.btnLexicon);
			this.panel1.Controls.Add(this.btnClose);
			this.panel1.Controls.Add(this.btnOther);
			this.panel1.Name = "panel1";
			//
			// btnHelp
			//
			resources.ApplyResources(this.btnHelp, "btnHelp");
			this.btnHelp.Name = "btnHelp";
			this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// SummaryDialogForm
			//
			this.AcceptButton = this.btnClose;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btnClose;
			this.Controls.Add(this.panel1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SummaryDialogForm";
			this.ShowInTaskbar = false;
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a view with multiple LexEntry objects.
		/// </summary>
		/// <param name="rghvoEntries"></param>
		/// <param name="cache"></param>
		/// <param name="styleSheet"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private XmlView CreateSummaryView(List<int> rghvoEntries, FdoCache cache, IVwStylesheet styleSheet)
		{
			// Make a decorator to publish the list of entries as a fake property of the LexDb.
			int kflidEntriesFound = 8999950; // some arbitrary number not conflicting with real flids.
			var sda = new ObjectListPublisher(cache.DomainDataByFlid as ISilDataAccessManaged, kflidEntriesFound);
			int hvoRoot = RootHvo;
			sda.CacheVecProp(hvoRoot, rghvoEntries.ToArray());
			//TODO: Make this method return a GeckoBrowser control, and generate the content here.
			// The name of this property must match the property used by the publishFound layout.
			sda.SetOwningPropInfo(LexDbTags.kflidClass, "LexDb", "EntriesFound");

			// Make an XmlView which displays that object using the specified layout.
			XmlView xv = new XmlView(hvoRoot, "publishFound", null, false, sda);
			xv.Cache = cache;
			xv.Mediator = m_mediator;
			xv.StyleSheet = styleSheet;
			return xv;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// True if client should call LinkToLexicon after dialog closes.
		/// NOTE: after calling ShowDialog, clients should test the ShouldLink property, and if it
		/// is true, call LinkToLexicon().
		/// This unusual pattern is used, instead of having the button do the linking directly,
		/// because it is necessary to fully close the dialog before invoking the link. Otherwise,
		/// TE will jump back in front of Flex even before Flex has finished jumping to the entry.
		/// See LT-3461.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool ShouldLink
		{
			get
			{
				CheckDisposed();
				return m_fShouldLink;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjust the height of the embedded XmlView to display as much as possible, up to a
		/// maximum dialog height of 400px.  (See LT-8392.)
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			if (m_xv.Height < m_xv.ScrollMinSize.Height && this.Height < 400)
			{
				int maxViewHeight = 400 - (this.Height - m_xv.Height);
				int delta = Math.Min(m_xv.ScrollMinSize.Height, maxViewHeight) - m_xv.Height;
				this.Height += delta;
			}
		}

		/// <summary>
		/// Protect against recursing into the selection changed handler, as it changes the selection itself.
		/// </summary>
		private bool m_fInSelChange = false;
		/// <summary>
		/// Event handler to grow selection if it's not a range when the selection changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void m_xv_VwSelectionChanged(object sender, VwSelectionArgs e)
		{
			Debug.Assert(e.RootBox == m_xv.RootBox);
			if (!m_fInSelChange)
			{
				m_fInSelChange = true;
				try
				{
					// Expand the selection to cover the entire LexEntry object.
					int cvsli = e.Selection.CLevels(false) - 1;
					int ihvoRoot;
					int tagTextProp;
					int cpropPrev;
					int ichAnchor;
					int ichEnd;
					int ws;
					bool fAssocPrev;
					int ihvoEnd;
					ITsTextProps ttp;
					SelLevInfo[] rgvsli = SelLevInfo.AllTextSelInfo(e.Selection, cvsli,
						out ihvoRoot, out tagTextProp, out cpropPrev, out ichAnchor, out ichEnd,
						out ws, out fAssocPrev, out ihvoEnd, out ttp);
					// The selection should cover the outermost object (which should be a LexEntry).
					SelLevInfo[] rgvsliOuter = new SelLevInfo[1];
					rgvsliOuter[0] = rgvsli[cvsli - 1];
					e.RootBox.MakeTextSelInObj(ihvoRoot, 1, rgvsliOuter, 0, null,
						false, false, false, true, true);
					// Save the selected object for possible use later.
					m_hvoSelected = rgvsliOuter[0].hvo;
					Debug.Assert(m_rghvo.Contains(m_hvoSelected));
					// Make the "Open Lexicon" button the default action after making a selection.
					this.AcceptButton = btnLexicon;
				}
				finally
				{
					m_fInSelChange = false;
				}
			}
		}

		int RootHvo
		{
			get
			{
				return m_cache.LangProject.LexDbOA.Hvo;
			}
		}

		#region Event handlers

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// NOTE: after calling ShowDialog, clients should test the OtherButtonClicked property,
		/// and if it is true, invoke a "find entry" dialog and loop back to display another
		/// SummaryDialogForm.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnOther_Click(object sender, System.EventArgs e)
		{
			m_fOtherClicked = true;
			Close();
		}

		/// <summary>
		/// Gets or sets a value indicating whether the <see cref="SummaryDialogForm"/> Other button was clicked.
		/// </summary>
		/// <value>
		/// <c>true</c> if Other button clicked; otherwise, <c>false</c>.
		/// </value>
		internal bool OtherButtonClicked
		{
			get
			{
				CheckDisposed();
				return m_fOtherClicked;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// NOTE: after calling ShowDialog, clients should test the ShouldLink property, and if it
		/// is true, call LinkToLexicon().
		/// This unusual pattern is used, instead of having the button do the linking directly,
		/// because it is necessary to fully close the dialog before invoking the link. Otherwise,
		/// TE will jump back in front of Flex even before Flex has finished jumping to the entry.
		/// See LT-3461.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnLexicon_Click(object sender, System.EventArgs e)
		{
			m_fShouldLink = true;
			Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Links to lexicon.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void LinkToLexicon()
		{
			CheckDisposed();
			int hvo = m_hvoSelected;
			if (hvo == 0 && m_rghvo != null && m_rghvo.Count > 0)
				hvo = m_rghvo[0];
			// REVIEW: THIS SHOULD NEVER HAPPEN, BUT IF IT DOES, SHOULD WE TELL THE USER ANYTHING?
			if (hvo == 0)
				return;
			ICmObject cmo = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
			FwAppArgs link = new FwAppArgs(FwUtils.ksFlexAppName, m_cache.ProjectId.Handle,
				m_cache.ProjectId.ServerName, "lexiconEdit", cmo.Guid);
			Debug.Assert(m_mediator != null, "The program must pass in a mediator to follow a link in the same application!");
			IApp app = (IApp)m_mediator.PropertyTable.GetValue("App");
			app.HandleOutgoingLink(link);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpProvider, m_helpFileKey, s_helpTopicKey);
		}
		#endregion
	}
}
