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
// File: MatchingEntries.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// Implementation of:
//		MatchingEntries - User control to display entries that match
//		the given forms or gloss.
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
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// List any lexical entries that match any of four possible input strings.
	/// The match is done starting at the beginning of the word.
	/// </summary>
	public class MatchingEntries : UserControl, IFWDisposable
	{
		/// <summary>
		/// Declare an event to deal with selection changed.
		/// </summary>
		public event FwSelectionChangedEventHandler SelectionChanged;
		/// <summary>
		/// An event that fires when the searching icon should be displayed or hidden.
		/// </summary>
		public event EventHandler SearchingChanged;
		/// <summary>
		/// This is used to restore focus to the parent dialog's control when we grab it
		/// merely by setting an index value on the browse view.
		/// </summary>
		public event EventHandler RestoreFocus;

		#region Data members

		protected int m_selEntryID = 0;
		// map from wsId (Hvo) to Font object (default serif font).
		// Loaded on demand by FontForWs method, which should therefore always be used to get a font.
		private Dictionary<int, Font> m_fonts;
		FdoCache m_cache;

		// Using an FwListView gives us the advantage of having double buffering turned on
		private FwListView m_lvMatches;
		private System.Windows.Forms.ColumnHeader m_chLF;
		private System.Windows.Forms.ColumnHeader m_chCF;
		private System.Windows.Forms.ColumnHeader m_chAltFms;
		private System.Windows.Forms.ColumnHeader m_chGlosses;
		private System.Windows.Forms.ImageList m_ilEntries;
		private System.ComponentModel.IContainer components;
		private ExtantEntryInfo m_currentQueryInfo;

		// true when attempting to update contents to the query indicated by the following variables.
		bool m_fResetSearchInProgress;
		bool m_fResetSearchAborted;
		// This block of variables matches the arguments to ResetMatches, an contains
		// the information about the search we are trying to implement.
		FdoCache m_cacheSearch; // probably always the same as m_cache, but too difficult to figure out.
		int m_currentID;
		bool m_wantExactMatch;
		int m_vernWs;
		string m_cf;
		string m_uf;
		string m_af;
		int m_analWs;
		string m_gl;
		List<ExtantEntryInfo> m_filters;
		System.Windows.Forms.Timer m_resetTimer; // used to restart a search that has to be paused.

		IVwStylesheet m_stylesheet; // used to figure font heights.

		#endregion Data members

		#region Properties

		/// <summary>
		/// Get the entry ID for the selected entry, or 0, if none selected.
		/// </summary>
		public virtual int SelectedEntryID()
		{
			CheckDisposed();

			return m_selEntryID;
		}

		public bool HasSearchingChanged
		{
			get { return SearchingChanged != null; }
		}
		#endregion Properties

		#region Construction, Initialization, and disposal

		/// <summary>
		/// Constructor.
		/// </summary>
		public MatchingEntries()
		{
			if (this.GetType().FullName != "SIL.FieldWorks.LexText.Controls.MatchingEntries")
				return;	// subclasses don't want any of this stuff!

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// This used to be generated in InitializeComponent(), but not any longer.
			this.m_lvMatches.SmallImageList = new ImageList();

			m_fonts = new Dictionary<int, Font>();
			// TabStop should only be set when we have list items.
			this.TabStop = m_lvMatches.Items.Count > 0;

			// The standard localization codes doesn't seem to work, so set these explicitly.
			m_chLF.Text = SIL.FieldWorks.LexText.Controls.LexTextControls.ksLexemeForm;
			m_chCF.Text = SIL.FieldWorks.LexText.Controls.LexTextControls.ksCitationForm;
			m_chAltFms.Text = SIL.FieldWorks.LexText.Controls.LexTextControls.ksAlternateForms;
			m_chGlosses.Text = SIL.FieldWorks.LexText.Controls.LexTextControls.ksGlosses;
		}

		/// <summary>
		/// Encapsulates on-demand loading of fonts for writing systems in use.
		/// Returns the default serif font for the argument writing system.
		/// Caches the result for future use.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		Font FontForWs(int ws)
		{
			if (!m_fonts.ContainsKey(ws))
			{
				int fontHeight = SIL.FieldWorks.Common.Widgets.FontHeightAdjuster.GetFontHeightForStyle(
					"Normal", m_stylesheet, ws, m_cache.LanguageWritingSystemFactoryAccessor);
				Font result = new Font(m_cache.LanguageWritingSystemFactoryAccessor.
					get_EngineOrNull(ws).DefaultSerif, (float)fontHeight/1000);
				m_fonts.Add(ws, result);
			}
			return m_fonts[ws];
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
				if(components != null)
				{
					components.Dispose();
				}
				ZapFonts();
				if (m_resetTimer != null)
				{
					m_resetTimer.Dispose();
					m_resetTimer = null;
				}
			}
			m_fonts = null;
			m_cache = null;
			m_cacheSearch = null;

			base.Dispose( disposing );
		}

		private void ZapFonts()
		{
			if (m_fonts != null)
			{
				foreach (Font font in m_fonts.Values)
					font.Dispose();
				m_fonts.Clear();
			}
		}

		#endregion Construction, Initialization, and disposal

		#region Other methods

		public virtual void Initialize(FdoCache cache, IVwStylesheet stylesheet, XCore.Mediator mediator)
		{
			CheckDisposed();

			m_cache = cache;
			m_stylesheet = stylesheet;
//			ILgWritingSystemFactory wsFact = cache.LanguageWritingSystemFactoryAccessor;
//			foreach (int ws in cache.LanguageEncodingIds)
//				m_fonts.Add(ws, new Font(wsFact.get_EngineOrNull(ws).DefaultSerif, 8.25F)); // TODO: Use the right size.
//			int UIWs = cache.DefaultUserWs;
//			if (m_fonts[UIWs] == null)
//				m_fonts.Add(UIWs, new Font(wsFact.get_EngineOrNull(UIWs).DefaultSerif, 8.25F)); // TODO: Use the right size.
		}

		/// <summary>
		/// Subclasses may need to be reinitialized at some point.
		/// </summary>
		public virtual void ReInitialize()
		{
		}

		public virtual void ResetSearch(FdoCache cache, int currentID,
			bool wantExactMatch,
			int vernWs, string cf, string uf, string af,
			int analWs, string gl, List<ExtantEntryInfo> filters)
		{
			CheckDisposed();

			m_cacheSearch = cache;
			m_currentID = currentID;
			m_wantExactMatch = wantExactMatch;
			m_vernWs = vernWs;
			m_cf = cf;
			m_uf = uf;
			m_af = af;
			m_analWs = analWs;
			m_gl = gl;
			m_filters = filters;
			m_fResetSearchInProgress = true;
			DoResetSearch();
		}

		public virtual bool Searching
		{
			get
			{
				CheckDisposed();
				return m_currentQueryInfo != null;
			}
		}

		public void RaiseSearchingChanged()
		{
			CheckDisposed();

			if (SearchingChanged != null)
				SearchingChanged(this, new EventArgs());
		}

		private void DoResetSearch()
		{
			if (m_currentQueryInfo != null)
			{
				m_currentQueryInfo.Cancel();
				m_currentQueryInfo = null;
			}
			if (!m_fResetSearchInProgress)
				return; // last reset succeeded, nothing to do.
			m_fResetSearchAborted = false;
			ClearResetTimer();

			if (ShouldAbort())
				return;

			m_currentQueryInfo = new ExtantEntryInfo();
			m_currentQueryInfo.ExtantEntriesCompleted += new EventHandler(m_currentQueryInfo_ExtantEntriesCompleted);
			// This should come before we call StartGetting...because the Completed call
			// can happen DURING the StartGettting call.
			RaiseSearchingChanged();
			m_currentQueryInfo.StartGettingExtantEntries(m_cacheSearch, m_currentID,
				m_wantExactMatch,
				m_vernWs, m_cf, m_uf, m_af,
				m_analWs, m_gl);
		}

		void m_currentQueryInfo_ExtantEntriesCompleted(object sender, EventArgs e)
		{
			if (sender != m_currentQueryInfo)
			{
				// We ended some obsolete query. We do NOT want to cancel the
				// search-in-progress thing!
				(sender as ExtantEntryInfo).Cleanup();
				return;
			}
				if (m_fonts == null)
				{
					// disposed...dialog has closed...don't try to reset!
					// This can easily happen if the search thread is still running
					// when the dialog is closed.
					return;
				}
			try
			{
				ResetSearch();
				if (m_fResetSearchAborted)
					return; // we did not complete the reset, will try again.

				ClearResetTimer();
				m_fResetSearchInProgress = false;
			}
			finally
			{
				// Any exit path from here means we're done searching.
				m_currentQueryInfo.Cleanup();
				m_currentQueryInfo = null;
				RaiseSearchingChanged();
			}
		}

		private void ClearResetTimer()
		{
			if (m_resetTimer != null)
			{
				m_resetTimer.Stop();
				m_resetTimer.Dispose();
				m_resetTimer = null;
			}
		}

		/// <summary>
		/// Abort resetting if the user types anything, anywhere.
		/// Also sets the flag (if it returns true) to indicate the search WAS aborted.
		/// </summary>
		/// <returns></returns>
		private bool ShouldAbort()
		{
			Win32.MSG msg = new Win32.MSG();
			if (Win32.PeekMessage(ref msg, IntPtr.Zero, (uint)Win32.WinMsgs.WM_KEYDOWN, (uint)Win32.WinMsgs.WM_KEYDOWN,
				(uint)Win32.PeekFlags.PM_NOREMOVE))
			{
				m_fResetSearchAborted = true;
				if (m_resetTimer == null)
				{
					m_resetTimer = new System.Windows.Forms.Timer();
					m_resetTimer.Interval = 100; // try again in 1/10 second.
					m_resetTimer.Tick += new EventHandler(m_resetTimer_Tick);
					m_resetTimer.Start();
				}
				return true;
			}
			return false;
		}

		void m_resetTimer_Tick(object sender, EventArgs e)
		{
			DoResetSearch();
		}

		/// <summary>
		/// Reset the search strings, and refresh the matching display.
		/// </summary>
		/// <param name="entries">Set of entries to show in list view.</param>
		/// <param name="filters">
		/// Filter out classes (class ID) and specific entries (entry ID).
		/// The array list must contain ExtantEntryInfo objects.
		/// </param>
		private void ResetSearch()
		{
			List<ExtantEntryInfo> entries = m_currentQueryInfo.Results();
			m_lvMatches.Items.Clear();
			if (entries == null)
				return;

			List<ListViewItem> items = new List<ListViewItem>(entries.Count);
			Font largestFont = null;
			int control = 0;
			StringUtils.InitIcuDataDir();	// used for normalizing strings to NFC
			foreach (ExtantEntryInfo eei in entries)
			{
				// Every so often see whether the user has typed something that makes our search irrelevant.
				if (control++ % 50 == 0 && ShouldAbort())
					return;

				bool skipEntry = false;
				foreach (ExtantEntryInfo filter in m_filters)
				{
					if (eei.ID == filter.ID)
					{
						skipEntry = true;
						break;
					}
				}
				if (skipEntry)
					continue;
/* Handled by ExtantEntryInfo now
				if (eei.LexemeForm == "***")
					eei.LexemeForm = String.Empty;
				if (eei.CitationForm == "***")
					eei.CitationForm = String.Empty;
				if (eei.Glosses == "***")
					eei.Glosses = String.Empty;
				if (eei.AlternateForms == "***")
					eei.AlternateForms = String.Empty;
*/

				ListViewItem lvi = new ListViewItem( StringUtils.NormalizeToNFC(eei.LexemeForm) );
				lvi.UseItemStyleForSubItems = false;
				Font currentFont = FontForWs(eei.LexemeFormWs);
				if (largestFont == null || largestFont.Height < currentFont.Height)
					largestFont = currentFont;
				if (eei.LexemeForm.ToLower().StartsWith(ExtantEntryInfo.SrcLexemeForm.ToLower()))
					lvi.Font = new Font(currentFont, currentFont.Style | FontStyle.Bold);
				else
					lvi.Font = new Font(currentFont, currentFont.Style | FontStyle.Regular);

				currentFont = FontForWs(eei.CitationFormWs);
				if (largestFont == null || largestFont.Height < currentFont.Height)
					largestFont = currentFont;
				ListViewItem.ListViewSubItem lvsi = lvi.SubItems.Add( StringUtils.NormalizeToNFC(eei.CitationForm) );
				if (eei.CitationForm.ToLower().StartsWith(ExtantEntryInfo.SrcCitationForm.ToLower()))
					lvsi.Font = new Font(currentFont, currentFont.Style | FontStyle.Bold);
				else
					lvsi.Font = new Font(currentFont, currentFont.Style | FontStyle.Regular);

				currentFont = FontForWs(eei.AlternateFormsWs);
				if (largestFont == null || largestFont.Height < currentFont.Height)
					largestFont = currentFont;
				lvsi = lvi.SubItems.Add( StringUtils.NormalizeToNFC(eei.AlternateForms) );
				if (eei.AlternateForms.ToLower().StartsWith(ExtantEntryInfo.SrcAlternateForms.ToLower()))
					lvsi.Font = new Font(currentFont, currentFont.Style | FontStyle.Bold);
				else
					lvsi.Font = new Font(currentFont, currentFont.Style | FontStyle.Regular);

				currentFont = FontForWs(eei.GlossesWs);
				if (largestFont == null || largestFont.Height < currentFont.Height)
					largestFont = currentFont;
				lvsi = lvi.SubItems.Add( StringUtils.NormalizeToNFC(eei.Glosses) );
				if (eei.Glosses.ToLower().StartsWith(ExtantEntryInfo.SrcGloss.ToLower()))
					lvsi.Font = new Font(currentFont, currentFont.Style | FontStyle.Bold);
				else
					lvsi.Font = new Font(currentFont, currentFont.Style | FontStyle.Regular);

				lvi.Tag = eei.ID;
				items.Add(lvi);
			}
			if (ShouldAbort())
				return;
			m_lvMatches.SuspendLayout();
			m_lvMatches.Items.AddRange(items.ToArray());

			// This is a nasty hack to change the height of the rows of the listview without changing the size of the header.
			// Until some improvements are made to the .NET framework, this is the only way to accomplish it.  What happens
			// is that the ListView is created with a SmallImageList, then everytime we populate the list, we assign a new
			// size to that image list to match the height of the largest font.  Unfortunately, this throws off the vertical
			// alignment, so we have to turn on OwnerDraw and render the text ourselves.
			if (largestFont != null)
				m_lvMatches.SmallImageList.ImageSize = new Size(1, largestFont.Height);

			m_lvMatches.ResumeLayout(true);
			if (m_lvMatches.Items.Count > 0)
				m_lvMatches.Items[0].Selected = true;
			else
				HandleSelectionChanged();	// Handle cleared list
		}

		private void HandleSelectionChanged()
		{
			ListView.SelectedListViewItemCollection col = m_lvMatches.SelectedItems;
			m_selEntryID = (col.Count > 0) ? (int)(col[0].Tag) : 0;
			this.TabStop = m_lvMatches.Items.Count > 0;
			RaiseSelectionChanged();
		}

		public void RaiseSelectionChanged()
		{
			if (SelectionChanged != null)
				SelectionChanged(this, new SIL.FieldWorks.Common.Utils.FwObjectSelectionEventArgs(m_selEntryID));
		}

		public virtual void SelectNext()
		{
			ListView.SelectedListViewItemCollection col = m_lvMatches.SelectedItems;
			if (col.Count > 0)
			{
				int i = col[0].Index;
				if (i + 1 < m_lvMatches.Items.Count)
					m_lvMatches.Items[i + 1].Selected = true;
			}
		}

		public virtual void SelectPrevious()
		{
			ListView.SelectedListViewItemCollection col = m_lvMatches.SelectedItems;
			if (col.Count > 0)
			{
				int i = col[0].Index;
				if (i > 0)
					m_lvMatches.Items[i - 1].Selected = true;
			}
		}

		public void RaiseRestoreFocus()
		{
			if (RestoreFocus != null)
				RestoreFocus(this, new EventArgs());
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MatchingEntries));
			this.m_lvMatches = new SIL.FieldWorks.Common.Controls.FwListView();
			this.m_chLF = new System.Windows.Forms.ColumnHeader();
			this.m_chCF = new System.Windows.Forms.ColumnHeader();
			this.m_chAltFms = new System.Windows.Forms.ColumnHeader();
			this.m_chGlosses = new System.Windows.Forms.ColumnHeader();
			this.m_ilEntries = new System.Windows.Forms.ImageList(this.components);
			this.SuspendLayout();
			//
			// m_lvMatches
			//
			resources.ApplyResources(this.m_lvMatches, "m_lvMatches");
			this.m_lvMatches.CausesValidation = false;
			this.m_lvMatches.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.m_chLF,
			this.m_chCF,
			this.m_chAltFms,
			this.m_chGlosses});
			this.m_lvMatches.FullRowSelect = true;
			this.m_lvMatches.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.m_lvMatches.HideSelection = false;
			this.m_lvMatches.MultiSelect = false;
			this.m_lvMatches.Name = "m_lvMatches";
			this.m_lvMatches.OwnerDraw = true;
			this.m_lvMatches.UseCompatibleStateImageBehavior = false;
			this.m_lvMatches.View = System.Windows.Forms.View.Details;
			this.m_lvMatches.DrawItem += new System.Windows.Forms.DrawListViewItemEventHandler(this.m_lvMatches_DrawItem);
			this.m_lvMatches.DoubleClick += new System.EventHandler(this.m_lvMatches_DoubleClick);
			this.m_lvMatches.SelectedIndexChanged += new System.EventHandler(this.m_lvMatches_SelectedIndexChanged);
			this.m_lvMatches.DrawSubItem += new System.Windows.Forms.DrawListViewSubItemEventHandler(this.m_lvMatches_DrawSubItem);
			this.m_lvMatches.DrawColumnHeader += new System.Windows.Forms.DrawListViewColumnHeaderEventHandler(this.m_lvMatches_DrawColumnHeader);
			//
			// m_chLF
			//
			resources.ApplyResources(this.m_chLF, "m_chLF");
			//
			// m_chCF
			//
			resources.ApplyResources(this.m_chCF, "m_chCF");
			//
			// m_chAltFms
			//
			resources.ApplyResources(this.m_chAltFms, "m_chAltFms");
			//
			// m_chGlosses
			//
			resources.ApplyResources(this.m_chGlosses, "m_chGlosses");
			//
			// m_ilEntries
			//
			this.m_ilEntries.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_ilEntries.ImageStream")));
			this.m_ilEntries.TransparentColor = System.Drawing.Color.Fuchsia;
			this.m_ilEntries.Images.SetKeyName(0, "");
			this.m_ilEntries.Images.SetKeyName(1, "");
			this.m_ilEntries.Images.SetKeyName(2, "");
			//
			// MatchingEntries
			//
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.m_lvMatches);
			this.Name = "MatchingEntries";
			this.Load += new System.EventHandler(this.MatchingEntries_Load);
			this.ResumeLayout(false);

		}

		// We have to draw this ourselves to ensure the alignment works correctly when we change font sizes
		void m_lvMatches_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
		{
			FwListView lv = (FwListView)sender;

			// In the future, different image indexes may be used for affixes or other kinds of results
			//int imageIndex = 0;

			e.SubItem.BackColor = e.Item.Selected ?
				(lv.Focused ? SystemColors.Highlight : SystemColors.GrayText) : lv.BackColor;

			e.DrawBackground();

			// Per LT-4815 comments, we don't want the icon. If we reinstate it, it should be the
			// new find entry icon.
			Image image = null; //  m_ilEntries.Images[imageIndex];
			Rectangle rect = e.Bounds;
			if (image != null && e.ColumnIndex == 0)
			{
				// Create a bounds rectangle that isn't larger than the area we have to draw in
				Rectangle imageRect = new Rectangle(rect.Location, new Size(Math.Min(rect.Width, image.Width), Math.Min(rect.Height, image.Height)));
				e.Graphics.DrawImage(image, imageRect);
				rect.X += image.Width;
			}

			TextRenderer.DrawText(e.Graphics, e.SubItem.Text, e.SubItem.Font, rect, lv.GetTextColor(e),
				TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);
		}

		// We just want the default behaivor here
		void m_lvMatches_DrawItem(object sender, DrawListViewItemEventArgs e)
		{
			e.DrawDefault = true;
		}

		// We just want the default behaivor here
		void m_lvMatches_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
		{
			e.DrawDefault = true;
		}

		#endregion

		#region Event Handlers

		private void m_lvMatches_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			HandleSelectionChanged();
		}

		private void m_lvMatches_DoubleClick(object sender, System.EventArgs e)
		{
			HandleSelectionChanged();
			Form frm = FindForm();
			if (frm is BaseGoDlg && (frm as BaseGoDlg).IsOkEnabled)
				frm.DialogResult = DialogResult.OK;
			else if (frm is InsertEntryDlg)
				frm.DialogResult = DialogResult.Yes;
			frm.Close();
		}

		private void MatchingEntries_Load(object sender, System.EventArgs e)
		{
			int width = (Width - 24)/4;
			foreach (ColumnHeader ch in m_lvMatches.Columns)
				ch.Width = width;
		}

		#endregion Event Handlers
	}
}
