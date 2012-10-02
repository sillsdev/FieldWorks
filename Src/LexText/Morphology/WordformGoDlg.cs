using System;
using System.Drawing;
using System.Collections;
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
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.Common.Controls;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// Summary description for WordformGoDlg.
	/// </summary>
	public class WordformGoDlg : BaseGoDlg
	{
		#region	Data members

		protected List<ExtantWfiWordformInfo> m_filteredEntries = new List<ExtantWfiWordformInfo>();
		private int m_flidFake;
		private XmlNode m_configNode;
		private IVwStylesheet m_stylesheet;
		private RecordClerk m_clerk;

		#region	Designer data members

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		#endregion	// Designer data members
		#endregion	// Data members

		#region Properties

		protected RecordClerk Clerk
		{
			get
			{
				if (m_clerk == null)
				{
					m_clerk = RecordClerkFactory.CreateClerk(m_mediator, m_configNode);
					m_clerk.Editable = XmlUtils.GetOptionalBooleanAttributeValue(m_configNode, "allowInsertDeleteRecord", true);
				}
				return m_clerk;
			}
		}

		#endregion Properties

		#region Construction, Initialization, and Disposal

		public WordformGoDlg() : base()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			InitializeComponentsFromBaseGoDlg();

			SetHelpTopic("khtpFindWordform");
		}

		protected override void InitializeComponentsFromBaseGoDlg()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BaseGoDlg));
			this.matchingEntries = new BrowseViewer();
			this.SuspendLayout();
			this.matchingEntries.SuspendLayout();
			//
			// matchingEntries
			//
			resources.ApplyResources(this.matchingEntries, "matchingEntries");
			this.matchingEntries.Name = "matchingEntries";
			this.matchingEntries.TabStop = false;
			this.Controls.Add(this.matchingEntries);
			this.Name = "WordformGoDlg";
			this.ResumeLayout(false);
			this.PerformLayout();
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
				if (matchingEntries != null && matchingEntries is BrowseViewer)
				{
					(this.matchingEntries as BrowseViewer).SelectionChanged -= new FwSelectionChangedEventHandler(matchingEntries_SelectionChanged);
					(this.matchingEntries as BrowseViewer).SelectionMade -= new FwSelectionChangedEventHandler(m_bvMatches_SelectionMade);
					this.matchingEntries.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		public void SetDlgInfo(Mediator mediator, WindowParams wp, List<IWfiWordform> filteredEntries)
		{
			CheckDisposed();

			Debug.Assert(filteredEntries != null && filteredEntries.Count > 0);

			foreach (IWfiWordform ww in filteredEntries)
			{
				if (ww != null)	// assignment of ww.Hvo will crash: LT-4951
				{
					ExtantWfiWordformInfo ewi = new ExtantWfiWordformInfo();
					ewi.ID = ww.Hvo;
					m_filteredEntries.Add(ewi);
				}
			}
			base.SetDlgInfo((FdoCache)mediator.PropertyTable.GetValue("cache"), wp, mediator);
		}

		/// <summary>
		/// Just load current vernacular
		/// </summary>
		protected override void LoadWritingSystemCombo()
		{
			foreach (ILgWritingSystem ws in m_cache.LangProject.CurVernWssRS)
			{
				m_cbWritingSystems.Items.Add(ws);
			}
		}

		protected override void ReplaceMatchingItemsControl()
		{
			if (m_mediator == null)
				return;
			XmlNode xnWindow = (XmlNode)m_mediator.PropertyTable.GetValue("WindowConfiguration");
			if (xnWindow == null)
				return;
			m_configNode = xnWindow.SelectSingleNode("controls/parameters/guicontrol[@id=\"WordformsBrowseView\"]/parameters");
			if (m_configNode == null)
				return;
			m_stylesheet = SIL.FieldWorks.Common.Widgets.FontHeightAdjuster.StyleSheetFromMediator(m_mediator);
			this.SuspendLayout();
			panel1.SuspendLayout();

			Clerk.ActivateUI(false);
			m_flidFake = Clerk.VirtualFlid;
			BrowseViewer newMe = new SIL.FieldWorks.Common.Controls.BrowseViewer(m_configNode,
				m_cache.LangProject.WordformInventoryOA.Hvo, m_flidFake, m_cache, m_mediator, Clerk.SortItemProvider);

			if (matchingEntries != null && Controls.Contains(matchingEntries))
			{
				CopyBasicControlInfo(matchingEntries, newMe);
				Controls.Remove(matchingEntries);
				if (matchingEntries is BrowseViewer)
				{
					(this.matchingEntries as BrowseViewer).SelectionChanged -= new FwSelectionChangedEventHandler(matchingEntries_SelectionChanged);
					(this.matchingEntries as BrowseViewer).SelectionMade -= new FwSelectionChangedEventHandler(m_bvMatches_SelectionMade);
				}
				matchingEntries.Dispose();
			}

			this.matchingEntries = newMe;
			this.matchingEntries.SuspendLayout();
			//this.matchingEntries.Location = new Point(0, 0);
			//this.matchingEntries.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom |
			//	AnchorStyles.Right;
			this.matchingEntries.Name = "m_bv";
			this.matchingEntries.TabStop = true;
			//this.matchingEntries.Dock = DockStyle.Fill;
			(this.matchingEntries as BrowseViewer).Sorter = null;
			(this.matchingEntries as BrowseViewer).StyleSheet = m_stylesheet;
			(this.matchingEntries as BrowseViewer).SelectionChanged += new FwSelectionChangedEventHandler(matchingEntries_SelectionChanged);
			(this.matchingEntries as BrowseViewer).SelectionMade += new FwSelectionChangedEventHandler(m_bvMatches_SelectionMade);
			ResetTabOrder();
			this.Controls.Add(this.matchingEntries);
			this.matchingEntries.ResumeLayout();
			panel1.ResumeLayout(false);
			this.ResumeLayout();
		}

		#endregion Construction, Initialization, and Disposal

		#region Other methods

		/// <summary>
		/// Reset the list of matching items.
		/// </summary>
		/// <param name="searchKey"></param>
		protected override void ResetMatches(string searchKey)
		{
			Cursor = Cursors.WaitCursor;
			try
			{
				if (m_oldSearchKey == searchKey)
					return; // Nothing new to do, so skip it.
				else
					btnOK.Enabled = false; // disable Go button until we rebuild our match list.

				m_oldSearchKey = searchKey;
				List<ExtantWfiWordformInfo> matches = ExtantWfiWordformInfo.ExtantWordformInfo(m_cache, searchKey,
					StringUtils.GetWsAtOffset(m_tbForm.Tss, 0));
				this.matchingEntries.SuspendLayout();
				List<int> rghvo = new List<int>(matches.Count);
				foreach (ExtantWfiWordformInfo match in matches)
				{
					bool isFiltered = false;
					foreach (ExtantWfiWordformInfo ewi in m_filteredEntries)
					{
						if (match.ID == ewi.ID)
						{
							isFiltered = true;
							break;
						}
					}
					if (!isFiltered)
						rghvo.Add(match.ID);
				}
				(Clerk as MatchingItemsRecordClerk).UpdateList(rghvo.ToArray());
				this.matchingEntries.ResumeLayout(true);
				if (rghvo.Count == 0)
				{
					(this.matchingEntries as BrowseViewer).SelectedIndex = -1;
					m_selEntryID = 0;
				}
				else
				{
					(this.matchingEntries as BrowseViewer).SelectedIndex = Clerk.CurrentIndex;
					m_selEntryID = Clerk.CurrentObject.Hvo;
					//RaiseSelectionChanged();
					//RaiseRestoreFocus();
				}
				this.matchingEntries.TabStop = rghvo.Count > 0;
				btnOK.Enabled = (m_selEntryID > 0);
			}
			finally
			{
				matchingEntries_RestoreFocus(null, null);
				Cursor = Cursors.Default;
			}
		}

		/// <summary>
		/// Ensure the focus is in the text box when the dialog first comes up.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			m_tbForm.FocusAndSelectAll();
		}
		#endregion Other methods

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WordformGoDlg));
			this.panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).BeginInit();
			this.SuspendLayout();
			//
			// btnOK
			//
			resources.ApplyResources(this.btnOK, "btnOK");
			//
			// btnInsert
			//
			resources.ApplyResources(this.btnInsert, "btnInsert");
			//
			// m_formLabel
			//
			resources.ApplyResources(this.m_formLabel, "m_formLabel");
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			//
			// WordformGoDlg
			//
			resources.ApplyResources(this, "$this");
			this.helpProvider.SetHelpNavigator(this, ((System.Windows.Forms.HelpNavigator)(resources.GetObject("$this.HelpNavigator"))));
			this.Name = "WordformGoDlg";
			this.helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region	Event handlers

		/// <summary>
		/// translate up and down arrow keys in the Find textbox into moving the selection in
		/// the matching entries list view.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected override void m_tbForm_KeyDown(object sender, KeyEventArgs e)
		{
			int i;
			switch (e.KeyCode)
			{
				case Keys.Up:
					i = (this.matchingEntries as BrowseViewer).SelectedIndex;
					if (i > 0)
						(this.matchingEntries as BrowseViewer).SelectedIndex = i - 1;
					break;
				case Keys.Down:
					i = (this.matchingEntries as BrowseViewer).SelectedIndex;
					if (i != -1 && i + 1 < Clerk.ListSize)
						(this.matchingEntries as BrowseViewer).SelectedIndex = i + 1;
					break;
			}
			base.m_tbForm_KeyDown(sender, e);
		}

		protected override void HandleMatchingSelectionChanged(FwObjectSelectionEventArgs e)
		{
			base.HandleMatchingSelectionChanged(e);
			if (e != null)
				(Clerk as MatchingItemsRecordClerk).SetListIndex(e.Index);	// keep the list index in sync.
		}

		/// <summary>
		/// This comes from a double click on a row in the browse view.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void m_bvMatches_SelectionMade(object sender, FwObjectSelectionEventArgs e)
		{
			m_selEntryID = e.Hvo;
			btnOK.Enabled = m_selEntryID > 0;
			if (btnOK.Enabled)
			{
				this.DialogResult = DialogResult.OK;
				this.Close();
			}
		}

		#endregion	Event handlers
	}
}
