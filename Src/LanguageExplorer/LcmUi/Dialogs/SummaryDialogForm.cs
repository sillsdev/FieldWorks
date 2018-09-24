// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Areas;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.LcmUi.Dialogs
{
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
	internal class SummaryDialogForm : Form
	{
		#region Member variables
		private List<int> m_rghvo;
		private int m_hvoSelected;		// object selected in the view.
		private XmlView m_xv;
		private LcmCache m_cache;
		private IPropertyTable m_propertyTable;
		private IHelpTopicProvider m_helpProvider;
		private string m_helpFileKey;
		private Button btnOther;
		private Button btnLexicon;
		private Button btnClose;
		private Panel panel1;
		private Button btnHelp;

		private System.ComponentModel.Container components = null;
		private const string s_helpTopicKey = "khtpFindInDictionary";
		private HelpProvider helpProvider;
		#endregion

		#region Constructor/destructor
		/// <summary>
		/// Constructor for a single LexEntry object.
		/// </summary>
		internal SummaryDialogForm(LexEntryUi leui, IHelpTopicProvider helpProvider, string helpFileKey, IVwStylesheet styleSheet)
			: this(new List<int>(leui.MyCmObject.Hvo), helpProvider, helpFileKey, styleSheet, leui.MyCmObject.Cache, leui.PropertyTable)
		{
		}

		/// <summary>
		/// Constructor for multiple matching LexEntry objects.
		/// </summary>
		internal SummaryDialogForm(List<int> rghvo, IHelpTopicProvider helpProvider, string helpFileKey, IVwStylesheet styleSheet, LcmCache cache, IPropertyTable propertyTable)
		{
			InitializeComponent();
			AccessibleName = GetType().Name;

			Debug.Assert(rghvo != null && rghvo.Count > 0);
			m_rghvo = rghvo;
			m_cache = cache;
			m_propertyTable = propertyTable;
			Initialize(helpProvider, helpFileKey, styleSheet);
		}

		/// <summary>
		/// Common initialization shared by the constructors.
		/// </summary>
		private void Initialize(IHelpTopicProvider helpProvider, string helpFileKey, IVwStylesheet styleSheet)
		{
			m_helpProvider = helpProvider;
			if (m_helpProvider == null)
			{
				btnHelp.Enabled = false;
			}
			else
			{
				m_helpFileKey = helpFileKey;
				this.helpProvider = new HelpProvider
				{
					HelpNamespace = FwDirectoryFinder.CodeDirectory + m_helpProvider.GetHelpString("UserHelpFile")
				};
				this.helpProvider.SetHelpKeyword(this, m_helpProvider.GetHelpString(s_helpTopicKey));
				this.helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			}
			m_xv = CreateSummaryView(m_rghvo, m_cache, styleSheet);
			m_xv.Dock = DockStyle.Top;	// panel1 is docked to the bottom.
			m_xv.TabStop = true;
			m_xv.TabIndex = 0;
			Controls.Add(m_xv);
			m_xv.Height = panel1.Location.Y - m_xv.Location.Y;
			m_xv.Width = Width - 15; // Changed from magic to more magic on 8/8/2014
			m_xv.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			m_xv.EditingHelper.DefaultCursor = Cursors.Arrow;
			m_xv.EditingHelper.VwSelectionChanged += m_xv_VwSelectionChanged;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if( disposing )
			{
				components?.Dispose();
				m_rghvo?.Clear();
				m_xv?.Dispose();
			}
			m_rghvo = null;
			m_xv = null;
			m_cache = null;
			m_propertyTable = null;
			m_helpProvider = null;

			base.Dispose( disposing );
		}
		#endregion

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
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


		/// <summary>
		/// Create a view with multiple LexEntry objects.
		/// </summary>
		private XmlView CreateSummaryView(List<int> rghvoEntries, LcmCache cache, IVwStylesheet styleSheet)
		{
			// Make a decorator to publish the list of entries as a fake property of the LexDb.
			const int kflidEntriesFound = 8999950; // some arbitrary number not conflicting with real flids.
			var sda = new ObjectListPublisher(cache.DomainDataByFlid as ISilDataAccessManaged, kflidEntriesFound);
			var hvoRoot = RootHvo;
			sda.CacheVecProp(hvoRoot, rghvoEntries.ToArray());
			//TODO: Make this method return a GeckoBrowser control, and generate the content here.
			// The name of this property must match the property used by the publishFound layout.
			sda.SetOwningPropInfo(CmObjectTags.kflidClass, "LexDb", "EntriesFound");

			// Make an XmlView which displays that object using the specified layout.
			var xv = new XmlView(hvoRoot, "publishFound", false, sda)
			{
				Cache = cache,
				StyleSheet = styleSheet
			};
			return xv;
		}

		/// <summary>
		/// True if client should call LinkToLexicon after dialog closes.
		/// NOTE: after calling ShowDialog, clients should test the ShouldLink property, and if it
		/// is true, call LinkToLexicon().
		/// This unusual pattern is used, instead of having the button do the linking directly,
		/// because it is necessary to fully close the dialog before invoking the link. Otherwise,
		/// TE will jump back in front of Flex even before Flex has finished jumping to the entry.
		/// See LT-3461.
		/// </summary>
		internal bool ShouldLink { get; private set; }

		/// <summary>
		/// Adjust the height of the embedded XmlView to display as much as possible, up to a
		/// maximum dialog height of 400px.  (See LT-8392.)
		/// </summary>
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			if (m_xv.Height >= m_xv.ScrollMinSize.Height || Height >= 400)
			{
				return;
			}
			var maxViewHeight = 400 - (Height - m_xv.Height);
			var delta = Math.Min(m_xv.ScrollMinSize.Height, maxViewHeight) - m_xv.Height;
			Height += delta;
		}

		/// <summary>
		/// Protect against recursing into the selection changed handler, as it changes the selection itself.
		/// </summary>
		private bool m_fInSelChange;
		/// <summary>
		/// Event handler to grow selection if it's not a range when the selection changes.
		/// </summary>
		private void m_xv_VwSelectionChanged(object sender, VwSelectionArgs e)
		{
			Debug.Assert(e.RootBox == m_xv.RootBox);
			if (m_fInSelChange)
			{
				return;
			}
			m_fInSelChange = true;
			try
			{
				// Expand the selection to cover the entire LexEntry object.
				var cvsli = e.Selection.CLevels(false) - 1;
				int ihvoRoot;
				int tagTextProp;
				int cpropPrev;
				int ichAnchor;
				int ichEnd;
				int ws;
				bool fAssocPrev;
				int ihvoEnd;
				ITsTextProps ttp;
				var rgvsli = SelLevInfo.AllTextSelInfo(e.Selection, cvsli,
					out ihvoRoot, out tagTextProp, out cpropPrev, out ichAnchor, out ichEnd,
					out ws, out fAssocPrev, out ihvoEnd, out ttp);
				// The selection should cover the outermost object (which should be a LexEntry).
				var rgvsliOuter = new SelLevInfo[1];
				rgvsliOuter[0] = rgvsli[cvsli - 1];
				e.RootBox.MakeTextSelInObj(ihvoRoot, 1, rgvsliOuter, 0, null, false, false, false, true, true);
				// Save the selected object for possible use later.
				m_hvoSelected = rgvsliOuter[0].hvo;
				Debug.Assert(m_rghvo.Contains(m_hvoSelected));
				// Make the "Open Lexicon" button the default action after making a selection.
				AcceptButton = btnLexicon;
			}
			finally
			{
				m_fInSelChange = false;
			}
		}

		private int RootHvo => m_cache.LangProject.LexDbOA.Hvo;

		#region Event handlers

		/// <summary>
		/// NOTE: after calling ShowDialog, clients should test the OtherButtonClicked property,
		/// and if it is true, invoke a "find entry" dialog and loop back to display another
		/// SummaryDialogForm.
		/// </summary>
		private void btnOther_Click(object sender, EventArgs e)
		{
			OtherButtonClicked = true;
			Close();
		}

		/// <summary>
		/// Gets or sets a value indicating whether the <see cref="SummaryDialogForm"/> Other button was clicked.
		/// </summary>
		internal bool OtherButtonClicked { get; private set; }

		/// <summary>
		/// NOTE: after calling ShowDialog, clients should test the ShouldLink property, and if it
		/// is true, call LinkToLexicon().
		/// This unusual pattern is used, instead of having the button do the linking directly,
		/// because it is necessary to fully close the dialog before invoking the link. Otherwise,
		/// TE will jump back in front of Flex even before Flex has finished jumping to the entry.
		/// See LT-3461.
		/// </summary>
		private void btnLexicon_Click(object sender, EventArgs e)
		{
			ShouldLink = true;
			Close();
		}

		/// <summary>
		/// Links to lexicon.
		/// </summary>
		internal void LinkToLexicon()
		{
			var hvo = m_hvoSelected;
			if (hvo == 0 && m_rghvo != null && m_rghvo.Count > 0)
			{
				hvo = m_rghvo[0];
			}
			// REVIEW: THIS SHOULD NEVER HAPPEN, BUT IF IT DOES, SHOULD WE TELL THE USER ANYTHING?
			if (hvo == 0)
			{
				return;
			}
			var cmo = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
			var link = new FwAppArgs(m_cache.ProjectId.Handle, AreaServices.LexiconEditMachineName, cmo.Guid);
			var app = m_propertyTable.GetValue<IApp>(LanguageExplorerConstants.App);
			app.HandleOutgoingLink(link);
		}

		/// <summary />
		private void btnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpProvider, m_helpFileKey, s_helpTopicKey);
		}
		#endregion
	}
}
