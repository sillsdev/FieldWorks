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
// File: MatchingReversalEntriesBrowser.cs
// Last reviewed:
//
// <remarks>
// Implementation of:
//		MatchingEntries - User control to display reversal index entries that match
//		the given form.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;
using System.Data.SqlClient;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Utils;
using System.Xml;
using SIL.Utils;
using SIL.FieldWorks.XWorks;
using SIL.FieldWorks.LexText.Controls;
using System.Collections;
using SIL.FieldWorks.Common.Framework;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// List any lexical entries that match any of four possible input strings.
	/// The match is done starting at the beginning of the word.
	/// </summary>
	public class MatchingReversalEntriesBrowser : SIL.FieldWorks.LexText.Controls.MatchingEntries
	{
		#region Data members
		private FdoCache m_cache;
		private IVwStylesheet m_stylesheet; // used to figure font heights.
		private XCore.Mediator m_mediator;
		private XmlNode m_configNode;
		private string m_vectorName;
		private int m_fakeFlid;
		private RecordClerk m_clerk = null;

		private System.Windows.Forms.ImageList m_ilEntries;
		private System.ComponentModel.IContainer components;
		//private ExtantEntryInfo m_currentQueryInfo;

		// true when attempting to update contents to the query indicated by the following variables.
		//private bool m_fResetSearchInProgress;
		//private bool m_fResetSearchAborted;
		// This block of variables matches the arguments to ResetMatches, and contains
		// the information about the search we are trying to implement.
		//private FdoCache m_cacheSearch; // probably always the same as m_cache, but too difficult to figure out.
		//private int m_currentID;
		//private bool m_wantExactMatch;
		//private int m_vernWs;
		//private string m_cf;
		//private string m_uf;
		//private string m_af;
		//private int m_analWs;
		//private string m_gl;
		//private List<ExtantEntryInfo> m_filters;
		private BrowseViewer m_bvMatches;
		//private System.Windows.Forms.Timer m_resetTimer; // used to restart a search that has to be paused.

		#endregion Data members

		#region Properties

		/// <summary>
		/// Get the entry ID for the selected entry, or 0, if none selected.
		/// </summary>
		public override int SelectedEntryID()
		{
			CheckDisposed();

			return m_selEntryID;
		}

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

		#region Construction, Initialization, and disposal

		/// <summary>
		/// Constructor.
		/// </summary>
		public MatchingReversalEntriesBrowser()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			// everything of interest is in Initialize().
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
				//ClearResetTimer();
			}
			m_cache = null;
			//m_cacheSearch = null;
			m_clerk = null;

			base.Dispose( disposing );
		}

		#endregion Construction, Initialization, and disposal

		#region Other methods

		/// <summary>
		/// Initialize the control, creating the BrowseViewer among other things.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="stylesheet"></param>
		/// <param name="mediator"></param>
		public void Initialize(FdoCache cache, IVwStylesheet stylesheet, XCore.Mediator mediator, ICmObject owner)
		{
			CheckDisposed();

			m_cache = cache;
			m_stylesheet = stylesheet;
			m_mediator = mediator;
			XmlNode xnWindow = (XmlNode)m_mediator.PropertyTable.GetValue("WindowConfiguration");
			m_configNode = xnWindow.SelectSingleNode("controls/parameters/guicontrol[@id=\"matchingReversalEntries\"]/parameters");
			m_vectorName = ToolConfiguration.GetIdOfTool(m_configNode);
			Clerk.ActivateUI(false);
			this.SuspendLayout();
			CreateBrowseViewer(owner.Hvo);
			(Clerk as MatchingItemsRecordClerk).UpdateList(new int[0] { });
			Clerk.OwningObject = owner;
			this.ResumeLayout(false);
		}

		private void CreateBrowseViewer(int hvoIndex)
		{
			m_fakeFlid = Clerk.VirtualFlid;
			m_bvMatches = new SIL.FieldWorks.Common.Controls.BrowseViewer(m_configNode,
				hvoIndex, m_fakeFlid, m_cache, m_mediator, Clerk.SortItemProvider);
			m_bvMatches.SuspendLayout();
			m_bvMatches.Location = new Point(0, 0);
			m_bvMatches.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right;
			m_bvMatches.Name = "m_bv";
			m_bvMatches.Sorter = null;
			m_bvMatches.TabStop = false;
			m_bvMatches.StyleSheet = m_stylesheet;
			m_bvMatches.Dock = DockStyle.Fill;
			m_bvMatches.SelectionChanged += new FwSelectionChangedEventHandler(m_bvMatches_SelectionChanged);
			m_bvMatches.SelectionMade += new FwSelectionChangedEventHandler(m_bvMatches_SelectionMade);
			this.Controls.Add(m_bvMatches);
			m_bvMatches.ResumeLayout();
		}

		/// <summary>
		/// This method is not used by this subclass.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="currentID"></param>
		/// <param name="wantExactMatch"></param>
		/// <param name="vernWs"></param>
		/// <param name="cf"></param>
		/// <param name="uf"></param>
		/// <param name="af"></param>
		/// <param name="analWs"></param>
		/// <param name="gl"></param>
		/// <param name="filters"></param>
		public override void ResetSearch(FdoCache cache, int currentID,
			bool wantExactMatch,
			int vernWs, string cf, string uf, string af,
			int analWs, string gl, List<ExtantEntryInfo> filters)
		{
			CheckDisposed();
		}

		/// <summary>
		/// respond to an up arrow key in the find select box
		/// </summary>
		public override void SelectNext()
		{
			int i = m_bvMatches.SelectedIndex;
			if (i != -1 && i + 1 < Clerk.ListSize)
				m_bvMatches.SelectedIndex = i + 1;
		}

		/// <summary>
		/// respond to a down arrow key in the find select box
		/// </summary>
		public override void SelectPrevious()
		{
			int i = m_bvMatches.SelectedIndex;
			if (i > 0)
				m_bvMatches.SelectedIndex = i - 1;
		}
		#endregion Other methods

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MatchingReversalEntriesBrowser));
			this.m_ilEntries = new System.Windows.Forms.ImageList(this.components);
			this.SuspendLayout();
			//
			// m_ilEntries
			//
			this.m_ilEntries.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_ilEntries.ImageStream")));
			this.m_ilEntries.TransparentColor = System.Drawing.Color.Fuchsia;
			this.m_ilEntries.Images.SetKeyName(0, "");
			this.m_ilEntries.Images.SetKeyName(1, "");
			this.m_ilEntries.Images.SetKeyName(2, "");
			//
			// MatchingReversalEntriesBrowser
			//
			this.Name = "MatchingReversalEntriesBrowser";
			this.Load += new System.EventHandler(this.MatchingEntriesBrowser_Load);
			this.ResumeLayout(false);

		}

		void MatchingEntriesBrowser_Load(object sender, EventArgs e)
		{
			if (m_bvMatches != null)
				m_bvMatches.MaximizeColumnWidths();
		}

		#endregion

		#region Event Handlers

		/// <summary>
		/// This comes from a single click on a row in the browse view.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void m_bvMatches_SelectionChanged(object sender, FwObjectSelectionEventArgs e)
		{
			m_selEntryID = e.Hvo;
			(Clerk as MatchingItemsRecordClerk).SetListIndex(e.Index);	// keep the list index in sync.
			RaiseSelectionChanged();
		}

		/// <summary>
		/// This comes from a double click on a row in the browse view.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void m_bvMatches_SelectionMade(object sender, FwObjectSelectionEventArgs e)
		{
			m_selEntryID = e.Hvo;
			RaiseSelectionChanged();
			Form frm = FindForm();
			if (frm is BaseGoDlg && (frm as BaseGoDlg).IsOkEnabled)
				frm.DialogResult = DialogResult.OK;
			else if (frm is InsertEntryDlg)
				frm.DialogResult = DialogResult.Yes;
			frm.Close();
		}

		#endregion Event Handlers

		public bool OnRecordNavigation(object argument)
		{
			CheckDisposed();
			return true;
		}

		protected override void OnEnter(EventArgs e)
		{
			m_bvMatches.SelectedRowHighlighting = XmlBrowseViewBase.SelectionHighlighting.border;
			base.OnEnter(e);
			m_bvMatches.Select();
		}

		protected override void OnLeave(EventArgs e)
		{
			base.OnLeave(e);
			m_bvMatches.SelectedRowHighlighting = XmlBrowseViewBase.SelectionHighlighting.all;
		}

		/// <summary>
		/// This is the method used instead of ResetSearch().
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="entries"></param>
		/// <param name="filteredEntries"></param>
		internal void ResetContents(FdoCache cache, List<ExtantReversalIndexEntryInfo> entries,
			List<ExtantReversalIndexEntryInfo> filteredEntries)
		{
			if (entries == null)
				return;
			// Update the custom list inside the custom clerk.
			List<int> rghvo = new List<int>(entries.Count);
			for (int i = 0; i < entries.Count; ++i)
			{
				// Don't display entries that we don't want to see.  See LT-6304.
				bool skipEntry = false;
				foreach (ExtantReversalIndexEntryInfo filtered in filteredEntries)
				{
					if (entries[i].ID == filtered.ID)
					{
						skipEntry = true;
						break;
					}
				}
				if (skipEntry)
					continue;
				rghvo.Add(entries[i].ID);
			}
			(Clerk as MatchingItemsRecordClerk).UpdateList(rghvo.ToArray());
			this.TabStop = rghvo.Count > 0;
			if (rghvo.Count == 0)
			{
				m_bvMatches.SelectedIndex = -1;
				m_selEntryID = 0;
			}
			else
			{
				m_bvMatches.SelectedIndex = Clerk.CurrentIndex;
				// LT-6366
				if (Clerk.CurrentObject != null)
				{
					m_selEntryID = Clerk.CurrentObject.Hvo;
					RaiseSelectionChanged();
				}
				else
				{
					m_bvMatches.SelectedIndex = -1;
					m_selEntryID = 0;
				}
			}
			RaiseRestoreFocus();
		}
	}
}
