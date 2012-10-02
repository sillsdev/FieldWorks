using System;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Data.SqlClient;
using System.Diagnostics;
using Microsoft.Win32;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Cellar;
using XCore;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.Common.Widgets;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Summary description for ReversalEntryGoDlg.
	/// </summary>
	public class ReversalEntryGoDlg : Form, IFWDisposable
	{
		#region	Data members

		protected bool m_skipCheck = false;
		protected string m_oldSearchKey;
		protected ITsStrFactory m_tsf;
		protected Mediator m_mediator;
		protected bool m_hasBeenActivated = false;
		protected int m_selEntryID;
		protected int m_ws;
		protected FdoCache m_cache;
		protected List<ExtantReversalIndexEntryInfo> m_filteredEntries = new List<ExtantReversalIndexEntryInfo>();
		private string m_helpTopic;
		private System.Windows.Forms.HelpProvider helpProvider;
		ICmObject m_owningIndex;

		#region	Designer data members

		private System.Windows.Forms.Label label1;
		private SIL.FieldWorks.Common.Widgets.FwTextBox m_tbForm;
		protected System.Windows.Forms.Button btnHelp;
		protected System.Windows.Forms.Button btnInsert;
		protected System.Windows.Forms.Button btnOK;
		protected System.Windows.Forms.Button btnClose;
		protected System.Windows.Forms.Label label2;
		private MatchingEntries matchingEntries;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		#endregion	// Designer data members
		#endregion	// Data members

		#region Properties

		/// <summary>
		/// Gets the database id of the selected object.
		/// </summary>
		public virtual int SelectedID
		{
			get
			{
				CheckDisposed();
				return m_selEntryID;
			}
		}

		protected string UnselectedText
		{
			get
			{
				string unSelText = null;
				if (m_tbForm.Text != null || m_tbForm.Text.Length > 0 )
				{
					unSelText = m_tbForm.Text;
					if (m_tbForm.SelectionLength > 0)
						unSelText = m_tbForm.Text.Substring(0, m_tbForm.SelectionStart);
				}
				return unSelText;
			}
		}

		#endregion Properties

		#region Construction, Initialization, and Disposal

		public ReversalEntryGoDlg()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
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
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		public void SetDlgInfo(Mediator mediator, WindowParams wp, List<IReversalIndexEntry> filteredEntries)
		{
			CheckDisposed();

			Debug.Assert(filteredEntries != null && filteredEntries.Count > 0);

			m_mediator = mediator;
			m_cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			m_ws = (filteredEntries[0] as ReversalIndexEntry).ReversalIndex.WritingSystemRAHvo;
			m_owningIndex = (filteredEntries[0] as ReversalIndexEntry).Owner;
			// Don't bother filtering out the current entry -- we don't do it for lex entries, why
			// do it here? (SFM 4/23/2009 Why? because it causes a crash, so uncommented)
			foreach (IReversalIndexEntry rie in filteredEntries)
			{
				ExtantReversalIndexEntryInfo eriei = new ExtantReversalIndexEntryInfo();
				eriei.ID = rie.Hvo;
				m_filteredEntries.Add(eriei);
			}
			// End SFM edit

			btnOK.Text = wp.m_btnText;
			Text = wp.m_title;
			label1.Text = wp.m_label;

			m_tbForm.Font = new Font(
					m_cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(m_ws).DefaultSerif,
					10);
			m_tbForm.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;
			m_tbForm.StyleSheet = FontHeightAdjuster.StyleSheetFromMediator(mediator);
			m_tbForm.WritingSystemCode = m_ws;
			m_tbForm.AdjustStringHeight = false;
			m_tsf = TsStrFactoryClass.Create();
			m_tbForm.Tss = m_tsf.MakeString("", m_ws);
			m_tbForm.TextChanged += new EventHandler(m_tbForm_TextChanged);

			btnInsert.Visible = false;
			btnHelp.Visible = true;

			switch(Text)
			{
				case "Find Reversal Entry":
					m_helpTopic = "khtpFindReversalEntry";
					break;
				case "Move Reversal Entry":
					m_helpTopic = "khtpMoveReversalEntry";
					break;
			}
			if(m_helpTopic != null && FwApp.App != null) // FwApp.App could be null during tests
			{
				this.helpProvider = new System.Windows.Forms.HelpProvider();
				this.helpProvider.HelpNamespace = FwApp.App.HelpFile;
				this.helpProvider.SetHelpKeyword(this, FwApp.App.GetHelpString(m_helpTopic, 0));
				this.helpProvider.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);
			}
			Debug.Assert(m_mediator != null);
			ReplaceMatchingItemsControl();

			// Adjust things if the form box needs to grow to accommodate its style.
			int oldHeight = m_tbForm.Height;
			int newHeight = Math.Max(oldHeight, m_tbForm.PreferredHeight);
			int delta = newHeight - oldHeight;
			if (delta != 0)
			{
				m_tbForm.Height = newHeight;
				FontHeightAdjuster.GrowDialogAndAdjustControls(this, delta, m_tbForm);
			}
		}

		#endregion Construction, Initialization, and Disposal

		#region Other methods

		/// <summary>
		/// Reset the list of matching items.
		/// </summary>
		/// <param name="searchKey"></param>
		protected void ResetMatches(string searchKey)
		{
			try
			{
				Cursor = Cursors.WaitCursor;
				if (m_oldSearchKey == searchKey)
				{
					Cursor = Cursors.Default;
					return; // Nothing new to do, so skip it.
				}
				else
				{
					// disable Go button until we rebuild our match list.
					btnOK.Enabled = false;
				}
				m_oldSearchKey = searchKey;
				List<ExtantReversalIndexEntryInfo> matches =
					ExtantReversalIndexEntryInfo.ExtantEntries(m_cache, searchKey, m_ws);
				(matchingEntries as MatchingReversalEntriesBrowser).ResetContents(m_cache,
					matches, m_filteredEntries);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		protected void ResetForm()
		{
			m_tbForm.Tss = m_tsf.MakeString("", m_tbForm.WritingSystemCode);
			m_tbForm.Focus();
		}

		#endregion Other methods

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ReversalEntryGoDlg));
			this.m_tbForm = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.btnHelp = new System.Windows.Forms.Button();
			this.btnInsert = new System.Windows.Forms.Button();
			this.btnOK = new System.Windows.Forms.Button();
			this.btnClose = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.matchingEntries = new SIL.FieldWorks.LexText.Controls.MatchingEntries();
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).BeginInit();
			this.SuspendLayout();
			//
			// m_tbForm
			//
			this.m_tbForm.AdjustStringHeight = true;
			this.m_tbForm.AllowMultipleLines = false;
			this.m_tbForm.BackColor = System.Drawing.SystemColors.Window;
			this.m_tbForm.controlID = null;
			resources.ApplyResources(this.m_tbForm, "m_tbForm");
			this.m_tbForm.Name = "m_tbForm";
			this.m_tbForm.SelectionLength = 0;
			this.m_tbForm.SelectionStart = 0;
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// btnHelp
			//
			resources.ApplyResources(this.btnHelp, "btnHelp");
			this.btnHelp.Name = "btnHelp";
			this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// btnInsert
			//
			resources.ApplyResources(this.btnInsert, "btnInsert");
			this.btnInsert.Name = "btnInsert";
			//
			// btnOK
			//
			resources.ApplyResources(this.btnOK, "btnOK");
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOK.Name = "btnOK";
			//
			// btnClose
			//
			resources.ApplyResources(this.btnClose, "btnClose");
			this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnClose.Name = "btnClose";
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// matchingEntries
			//
			resources.ApplyResources(this.matchingEntries, "matchingEntries");
			this.matchingEntries.Name = "matchingEntries";
			this.matchingEntries.TabStop = false;
			//
			// ReversalEntryGoDlgNew
			//
			this.AcceptButton = this.btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btnClose;
			this.Controls.Add(this.matchingEntries);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.btnHelp);
			this.Controls.Add(this.btnInsert);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.btnClose);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.m_tbForm);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ReversalEntryGoDlgNew";
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		#region	Event handlers

		protected void m_tbForm_TextChanged(object sender, System.EventArgs e)
		{
			if (m_skipCheck)
				return;

			bool fWantSelect = true;
			string unSelText = AdjustUnselectedText(out fWantSelect);
			int selLocation = unSelText.Length;
			ResetMatches(unSelText);
			// Unnecessary focus changes can cause loss of characters with Yi and Indic languages.
			if (fWantSelect && (m_tbForm.SelectionStart != selLocation || m_tbForm.SelectionLength != 0))
				m_tbForm.Select(selLocation, 0);
		}

		protected virtual string AdjustUnselectedText(out bool fWantSelect)
		{
			// TODO: For each keystroke:
			//		1. If it is a reserved character, then...
			//			(e.g., '-' for prefixes or suffixes).
			//		2. If it is not a wordforming character, then...?
			//		3. If it is a wordforming character, then modify the 'matching entries'
			//			list box, and select the first item in the list.
			string unSelText = UnselectedText.Trim(); ;
			fWantSelect = true;
			if (unSelText != UnselectedText)
			{
				// Note: Yi and Chinese use \x3000 for this.
				if (unSelText + " " == UnselectedText || unSelText + "\x3000" == UnselectedText)
				{
					// It's important (see LT-3770) to allow the user to type a space.
					// So if the only difference is a trailing space, don't adjust the string,
					// and also don't adjust the selection...that produces a stack overflow!
					fWantSelect = false;
				}
				else
				{
					m_skipCheck = true;
					m_tbForm.Text = unSelText;
					m_skipCheck = false;
				}
			}
			return unSelText;
		}

		private void btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(FwApp.App, m_helpTopic);
		}

		#endregion	Event handlers

		protected void ReplaceMatchingItemsControl()
		{
			if (m_mediator == null)
				return;
			XmlNode xnWindow = (XmlNode)m_mediator.PropertyTable.GetValue("WindowConfiguration");
			if (xnWindow == null)
				return;
			XmlNode xnControl = xnWindow.SelectSingleNode("controls/parameters/guicontrol[@id=\"matchingReversalEntries\"]");
			if (xnControl == null)
				return;
			// Replace the current matchingEntries object with the one specified in the XML.
			MatchingEntries newME = DynamicLoader.CreateObject(xnControl) as MatchingEntries;
			if (newME != null)
			{
				CopyBasicControlInfo(matchingEntries, newME);
				this.Controls.Remove(matchingEntries);
				matchingEntries.Dispose();
				matchingEntries = newME;
				this.Controls.Add(matchingEntries);
				(matchingEntries as MatchingEntries).SelectionChanged += new SIL.FieldWorks.Common.Utils.FwSelectionChangedEventHandler(this.matchingEntries_SelectionChanged);
				(matchingEntries as MatchingEntries).RestoreFocus += new EventHandler(matchingEntries_RestoreFocus);
				// Reset Tab indices of direct child controls of the form.
				ResetTabOrder();
				(newME as MatchingReversalEntriesBrowser).Initialize(m_cache,
					FontHeightAdjuster.StyleSheetFromMediator(m_mediator), m_mediator, m_owningIndex);
			}
		}

		protected void matchingEntries_SelectionChanged(object sender,
			SIL.FieldWorks.Common.Utils.FwObjectSelectionEventArgs e)
		{
			if (m_skipCheck)
				return;

			m_selEntryID = e.Hvo;

			HandleMatchingSelectionChanged(e);
		}

		protected void matchingEntries_RestoreFocus(object sender, EventArgs e)
		{
			// Set the focus on m_tbForm.
			// Note: due to Keyman/TSF interactions in Indic scripts, do not set focus
			// if it is already set, or we can lose typed characters (e.g., typing poM in
			// Kannada Keyman script causes everything to disappear on M)
			if (!m_tbForm.Focused)
				m_tbForm.Focus();
		}

		protected static void CopyBasicControlInfo(UserControl src, UserControl target)
		{
			target.Location = src.Location;
			target.Size = src.Size;
			target.Name = src.Name;
			target.AccessibleName = src.AccessibleName;
			target.TabStop = src.TabStop;
			target.TabIndex = src.TabIndex;
			target.Anchor = src.Anchor;
		}

		protected void InitializeMatchingEntries(FdoCache cache, Mediator mediator)
		{
			(matchingEntries as MatchingEntries).Initialize(cache,
				FontHeightAdjuster.StyleSheetFromMediator(mediator), mediator);
		}

		protected virtual void ResetTabOrder()
		{
			label1.TabIndex = 0;
			matchingEntries.TabIndex = 1;
			btnOK.TabIndex = 2;
			btnInsert.TabIndex = 3;
			btnClose.TabIndex = 4;
			btnHelp.TabIndex = 5;
		}

		protected virtual void HandleMatchingSelectionChanged(FwObjectSelectionEventArgs e)
		{
			HandleMatchingSelectionChanged();
		}

		protected virtual void HandleMatchingSelectionChanged()
		{
			btnOK.Enabled = (m_selEntryID > 0);
		}

	}
}
