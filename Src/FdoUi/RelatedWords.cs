using System;
using System.Diagnostics;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// Summary description for RelatedWords.
	/// </summary>
	public class RelatedWords : Form, IFWDisposable
	{
		private System.Windows.Forms.Button m_btnInsert;
		private System.Windows.Forms.Button m_btnClose;
		private System.Windows.Forms.Button m_btnLookup;
		private System.Windows.Forms.Button m_btnCopy;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		RelatedWordsView m_view;
		FdoCache m_cache;
		IVwSelection m_sel;
		IVwStylesheet m_styleSheet;
		int m_hvoEntry;
		IVwCacheDa m_cdaTemp;
		XmlView m_detailView;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the "not in dictionary" message.
		/// </summary>
		/// <param name="owner">The owner.</param>
		/// ------------------------------------------------------------------------------------
		static internal void ShowNotInDictMessage(IWin32Window owner)
		{
			MessageBox.Show(owner, FdoUiStrings.kstidFindRelWordsNotInDict,
				FdoUiStrings.kstidFindRelWordsTitle);
		}

		/// <summary>
		/// Load both the semantic domains and the lexical relation information for hvoEntry.
		/// Returns false (after displaying a dialog) if the entry has no associated domains or
		/// lexical relations, or none of them are linked to any other entries.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoEntry"></param>
		/// <param name="domainsOut"></param>
		/// <param name="lexrelsOut"></param>
		/// <param name="cdaTemp"></param>
		/// <param name="owner"></param>
		/// <returns></returns>
		static internal bool LoadDomainAndRelationInfo(FdoCache cache, int hvoEntry, out int[] domainsOut,
			out int[] lexrelsOut, out IVwCacheDa cdaTemp, IWin32Window owner)
		{
			bool fHaveSemDomains = LoadDomainInfo(cache, hvoEntry, out domainsOut, out cdaTemp, owner);
			bool fHaveLexRels = LoadLexicalRelationInfo(cache, hvoEntry, out lexrelsOut, cdaTemp, owner);
			if (!fHaveSemDomains && !fHaveLexRels)
			{
				MessageBox.Show(owner,
					FdoUiStrings.ksNoSemanticDomainsListedForEntry,
					FdoUiStrings.ksFindRelatedWords);
				return false;
			}
			if (domainsOut.Length == 0 && lexrelsOut.Length == 0)
			{
				MessageBox.Show(owner,
					FdoUiStrings.ksNoEntriesWithSameSemanticDomain,
					FdoUiStrings.ksFindRelatedWords);
				return false;
			}
			return true;
		}

		/// <summary>
		/// Load the information about the domains of hvoEntry. Returns false
		/// if the entry has no associated domains or none of them are linked to any other entries.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoEntry"></param>
		/// <param name="domains"></param>
		/// <param name="cdaTemp"></param>
		/// <param name="fMoreRows"></param>
		/// <param name="owner"></param>
		/// <returns></returns>
		static private bool LoadDomainInfo(FdoCache cache, int hvoEntry, out int[] domainsOut, out IVwCacheDa cdaTemp, IWin32Window owner)
		{
			// This produces first the Semantic domains of the senses of the entry,
			// then restricts to those that occur on some other entry,
			// then looks up the vernacular (or, if none, analysis) name of the domains. The are sorted by
			// domain name.
			// We do left outer joins for the last two so we can distinguish the failure
			// modes "no SDs on senses of initial entry" versus "no other entries in those SDs"
			string sql1 = string.Format("select lssd2.dst, cn.txt, cn.ws from LexEntry le"
				+ " join LexSense_ ls on ls.owner$ = le.id"
				+ " join LexSense_SemanticDomains lssd on lssd.src = ls.id "
				+ " left outer join LexSense_SemanticDomains lssd2 on lssd2.dst = lssd.dst"
				+ " and exists (select * from CmObject lsother"
				+ " join LexEntry leother on leother.id = lsother.owner$ and lsother.id = lssd2.src and leother.id != le.id)"
				+ " left outer join CmPossibility_Name cn on lssd2.dst = cn.obj and cn.ws"
				+ " in ({0}, {1}) where le.id = {2}"
				+ " group by lssd2.dst, cn.txt, cn.ws"
				+ " order by cn.txt", cache.DefaultVernWs, cache.DefaultAnalWs, hvoEntry);

			IOleDbCommand odc = DbOps.MakeRowSet(cache, sql1, null);
			bool fGotSrcDomain = false; // true if we found a semantic domain on some sense of the source entry
			try
			{
				bool fMoreRows;
				List<int> domains = new List<int>();
				cdaTemp = VwCacheDaClass.Create();
				for (odc.NextRow(out fMoreRows); fMoreRows; odc.NextRow(out fMoreRows))
				{
					fGotSrcDomain = true; // any row indicates success here.
					int hvoDomain = DbOps.ReadInt(odc, 0);
					if (hvoDomain == 0)
						continue; // null row, an SD that occurs on no other entry.
					if (!((ISilDataAccess)cdaTemp).get_IsPropInCache(hvoDomain, RelatedWordsVc.ktagName,
						(int)CellarModuleDefns.kcptString, 0))
					{
						ITsString tss = DbOps.ReadTss2(odc, 1);
						if (tss == null)
						{
							tss = FDO.Cellar.CmPossibility.BestAnalysisOrVernName(cache, hvoDomain);
						}
						cdaTemp.CacheStringProp(hvoDomain, RelatedWordsVc.ktagName,
							tss);
						domains.Add(hvoDomain);
					}
				}
				domainsOut = DbOps.ListToIntArray(domains);
			}
			finally
			{
				DbOps.ShutdownODC(ref odc);
			}
			return fGotSrcDomain;
		}

		/// <summary>
		/// Load the information about the lexical relations that link to hvoEntry.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoEntry"></param>
		/// <param name="relsOut"></param>
		/// <param name="cdaTemp"></param>
		/// <param name="owner"></param>
		/// <returns>false if the entry has no associated lexical relations, or none of them are linked to any other entries.</returns>
		static private bool LoadLexicalRelationInfo(FdoCache cache, int hvoEntry, out int[] relsOut,
			IVwCacheDa cdaTemp, IWin32Window owner)
		{
			string sql1 = string.Format("SELECT DISTINCT tar2.Src, lrt.MappingType, tar.Ord, cn.Txt, cn.Ws, rev.Txt, rev.Ws"
				+ " FROM LexReference_Targets tar"
				+ " LEFT OUTER JOIN LexReference_Targets tar2 ON tar2.Src=tar.Src AND EXISTS (SELECT * FROM CmObject other WHERE other.Id=tar2.Dst AND other.Id != tar.Dst)"
				+ " LEFT OUTER JOIN LexRefType_Members mem ON mem.Dst=tar2.Src"
				+ " LEFT OUTER JOIN LexRefType lrt ON lrt.Id=mem.Src"
				+ " LEFT OUTER JOIN CmPossibility_Name cn ON cn.Obj=mem.Src AND cn.Ws IN ({0}, {1})"
				+ " LEFT OUTER JOIN LexRefType_ReverseName rev ON rev.Obj=mem.Src AND rev.Ws IN ({0}, {1})"
				+ " WHERE tar.Dst = {2} OR tar.Dst IN (SELECT Id FROM fnGetOwnedIds({2}, {3}, {4}))",
				cache.DefaultVernWs, cache.DefaultAnalWs, hvoEntry,
				(int)LexEntry.LexEntryTags.kflidSenses, (int)LexSense.LexSenseTags.kflidSenses);

			IOleDbCommand odc = DbOps.MakeRowSet(cache, sql1, null);
			bool fGotLexRef = false; // true if we found a lexical relation for the entry or one of its senses
			try
			{
				bool fMoreRows;
				List<int> rels = new List<int>();
				for (odc.NextRow(out fMoreRows); fMoreRows; odc.NextRow(out fMoreRows))
				{
					fGotLexRef = true;
					int hvoLexRef = DbOps.ReadInt(odc, 0);
					if (hvoLexRef == 0)
						continue;	// null row.
					if (!((ISilDataAccess)cdaTemp).get_IsPropInCache(hvoLexRef, RelatedWordsVc.ktagName,
						(int)CellarModuleDefns.kcptString, 0))
					{

						int type = DbOps.ReadInt(odc, 1);
						int ord = DbOps.ReadInt(odc, 2);
						ITsString tssName = DbOps.ReadTss2(odc, 3);
						ITsString tssRevName = DbOps.ReadTss2(odc, 5);
						if (type == (int)LexRefType.MappingTypes.kmtEntryAsymmetricPair ||
							type == (int)LexRefType.MappingTypes.kmtEntryOrSenseAsymmetricPair ||
							type == (int)LexRefType.MappingTypes.kmtSenseAsymmetricPair)
						{
							if (ord != 0 && tssRevName != null && tssRevName.Length > 0)
								tssName = tssRevName;
						}
						else if (type == (int)LexRefType.MappingTypes.kmtEntryTree ||
							type == (int)LexRefType.MappingTypes.kmtEntryOrSenseTree ||
							type == (int)LexRefType.MappingTypes.kmtSenseTree)
						{
							if (ord != 0 && tssRevName != null && tssRevName.Length > 0)
								tssName = tssRevName;
						}
						cdaTemp.CacheStringProp(hvoLexRef, RelatedWordsVc.ktagName, tssName);
						rels.Add(hvoLexRef);
					}
				}
				relsOut = DbOps.ListToIntArray(rels);
			}
			finally
			{
				DbOps.ShutdownODC(ref odc);
			}
			return fGotLexRef;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a view with a single LexEntry object.
		/// </summary>
		/// <param name="hvoEntry"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static XmlView MakeSummaryView(int hvoEntry, FdoCache cache, IVwStylesheet styleSheet)
		{
			XmlView xv = new XmlView(hvoEntry, "publishStem", null, false);
			xv.LoadFlexLayouts = true;
			xv.Cache = cache;
			xv.StyleSheet = styleSheet;
			return xv;
		}


		public RelatedWords(FdoCache cache, IVwSelection sel, int hvoEntry, int[] domains, int[] lexrels,
			IVwCacheDa cdaTemp, IVwStylesheet styleSheet)
		{
			m_cache = cache;
			m_sel = sel;
			m_hvoEntry = hvoEntry;
			m_styleSheet = styleSheet;
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			m_cdaTemp = cdaTemp;
			ISilDataAccess sda = m_cdaTemp as ISilDataAccess;
			sda.WritingSystemFactory = cache.MainCacheAccessor.WritingSystemFactory;

			SetupForEntry(domains, lexrels);

			m_view = new RelatedWordsView(m_hvoEntry, m_cdaTemp as ISilDataAccess, cache.DefaultUserWs);
			m_view.Width = this.Width - 20;
			m_view.Height = m_btnClose.Top - 20;
			m_view.Top = 10;
			m_view.Left = 10;
			m_view.Anchor = AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			m_view.BackColor = Color.FromKnownColor(KnownColor.Window);
			m_view.EditingHelper.DefaultCursor = Cursors.Arrow;

			m_view.SelChanged += new EventHandler(m_view_SelChanged);
			this.Controls.Add(m_view);
		}

		/// <summary>
		/// Store data in m_cdaTemp suitable for displaying words related to m_hvoEntry
		/// </summary>
		void SetupForEntry(int[] domains, int[] lexrels)
		{
			SetupDomainsForEntry(domains);
			SetupLexRelsForEntry(lexrels);
		}

		void SetupDomainsForEntry(int[] domains)
		{
			m_cdaTemp.CacheVecProp(m_hvoEntry, RelatedWordsVc.ktagDomains,
				domains, domains.Length);

			// This produces first the Semantic domains of the senses of the entry,
			// then uses a backreference to find all senses linked to those domains.
			// Todo JohnT: this finds only entries that directly own senses linked to the relevant domains.
			// It will not find entries that only have senses with subsenses in the domain.
			// This is because we're specifically looking for MoForms with the same owner as the senses we found.
			// We'd have to do something tricky and recursive to get owning entry of a subsense.
			string sql2 = string.Format("select lscd.dst, cmls.owner$, mff.txt from LexSense ls"
				+ " join CmObject cols on ls.id = cols.id and cols.owner$ = {0}"
				+ " join LexSense_SemanticDomains lscd on lscd.src = ls.id"
				+ " join LexSense_SemanticDomains lscd2 on lscd2.dst = lscd.dst and lscd2.src != ls.id"
				+ " join CmObject cmls on lscd2.src = cmls.id and cmls.owner$ != {0}"
				+ " join MoForm_ mf on mf.owner$ = cmls.owner$ and mf.OwnFlid$ = {1}"
				+ " join MoForm_Form mff on mff.obj = mf.id and mff.ws = {2}"
				+ " group by lscd.dst, cmls.owner$, mff.txt"
				+ " order by lscd.dst, mff.txt",
				m_hvoEntry, (int)LexEntry.LexEntryTags.kflidLexemeForm, m_cache.DefaultVernWs);

			IOleDbCommand odc = DbOps.MakeRowSet(m_cache, sql2, null);
			try
			{
				bool fMoreRows;
				List<int> words = new List<int>();
				int hvoOldDomain = 0;  // to trigger change of domain on first iteration
				for (odc.NextRow(out fMoreRows); fMoreRows; odc.NextRow(out fMoreRows))
				{
					int hvoNewDomain = DbOps.ReadInt(odc, 0);
					if (hvoNewDomain != hvoOldDomain)
					{
						if (hvoOldDomain != 0)
						{
							m_cdaTemp.CacheVecProp(hvoOldDomain, RelatedWordsVc.ktagWords,
								DbOps.ListToIntArray(words), words.Count);
							words.Clear();
						}
						hvoOldDomain = hvoNewDomain;
					}
					int hvoWord = DbOps.ReadInt(odc, 1);
					// JohnT: if I was better at sql, I could no doubt figure out how to prevent
					// duplicates in the query above, which are caused by having two or more senses of the
					// same entry in the same domain. But it's easier to just eliminate them here (and maybe
					// even faster).
					if (!words.Contains(hvoWord))
					{
						m_cdaTemp.CacheStringProp(hvoWord, RelatedWordsVc.ktagName,
							DbOps.ReadTss(odc, 2, m_cache.DefaultVernWs));
						words.Add(hvoWord);
					}
				}
				if (hvoOldDomain != 0)
				{
					// Cache words of last domain.
					m_cdaTemp.CacheVecProp(hvoOldDomain, RelatedWordsVc.ktagWords,
						DbOps.ListToIntArray(words), words.Count);
				}
				int hvoLf = m_cache.MainCacheAccessor.get_ObjectProp(m_hvoEntry, (int)LexEntry.LexEntryTags.kflidLexemeForm);
				if (hvoLf != 0)
				{
					ITsString tssCf = m_cache.MainCacheAccessor.get_MultiStringAlt(hvoLf,
						(int)MoForm.MoFormTags.kflidForm, m_cache.DefaultVernWs);
					m_cdaTemp.CacheStringProp(m_hvoEntry, RelatedWordsVc.ktagCf, tssCf);
				}
			}
			finally
			{
				DbOps.ShutdownODC(ref odc);
			}
		}

		private void SetupLexRelsForEntry(int[] lexrels)
		{
			m_cdaTemp.CacheVecProp(m_hvoEntry, RelatedWordsVc.ktagLexRels,
				lexrels, lexrels.Length);
			// This query does not find subsenses.
			string sSql = String.Format(
				"SELECT tar.Src, tar.Dst, tar.Ord, tar2.Ord, lrt.MappingType, cf.Txt, mff.Txt, le.HomographNumber"
				+ " FROM LexRefType lrt"
				+ " JOIN LexRefType_Members mem ON mem.Src=lrt.Id"
				+ " JOIN LexReference_Targets tar ON tar.Src=mem.Dst"
				+ " JOIN LexReference_Targets tar2 ON tar2.Src=tar.Src AND (tar2.Dst={0} OR tar2.Dst IN (SELECT Dst FROM LexEntry_Senses WHERE Src={0}))"
				+ " JOIN CmObject co ON co.Id=tar.Dst AND co.Id != {0} AND co.Id NOT IN (SELECT Dst FROM LexEntry_Senses WHERE Src={0})"
				+ " JOIN LexEntry le ON le.Id IN (co.Id, co.Owner$)"
				+ " LEFT OUTER JOIN LexEntry_CitationForm cf ON cf.Obj=le.Id"
				+ " LEFT OUTER JOIN MoForm_ mf ON mf.Owner$=le.Id AND mf.OwnFlid$={1}"
				+ " LEFT OUTER JOIN MoForm_Form mff ON mff.Obj=mf.Id AND mff.Ws={2}"
				+ " ORDER BY tar.Src, tar.Ord",
				m_hvoEntry, (int)LexEntry.LexEntryTags.kflidLexemeForm, m_cache.DefaultVernWs);
			IOleDbCommand odc = DbOps.MakeRowSet(m_cache, sSql, null);
			try
			{
				bool fMoreRows;
				int hvoOldLexRef = 0;  // to trigger change of lexical relation on first iteration
				List<int> refs = new List<int>();
				for (odc.NextRow(out fMoreRows); fMoreRows; odc.NextRow(out fMoreRows))
				{
					int hvoLexRef = DbOps.ReadInt(odc, 0);
					if (hvoLexRef != hvoOldLexRef)
					{
						if (hvoOldLexRef != 0)
						{
							m_cdaTemp.CacheVecProp(hvoOldLexRef, RelatedWordsVc.ktagWords,
								refs.ToArray(), refs.Count);
							refs.Clear();
						}
						hvoOldLexRef = hvoLexRef;
					}
					int hvoRef = DbOps.ReadInt(odc, 1);
					if (refs.Contains(hvoRef))
						continue;
					refs.Add(hvoRef);
					int ordRef = DbOps.ReadInt(odc, 2);
					int ordEntry = DbOps.ReadInt(odc, 3);
					int type = DbOps.ReadInt(odc, 4);
					if (type == (int)LexRefType.MappingTypes.kmtEntryOrSenseTree ||
						type == (int)LexRefType.MappingTypes.kmtEntryTree ||
						type == (int)LexRefType.MappingTypes.kmtSenseTree)
					{
						if (ordRef != 0 && ordEntry != 0)
							continue;		// one of them has to be the root of the tree!
					}
					if (type == (int)LexRefType.MappingTypes.kmtEntryOrSenseSequence ||
						type == (int)LexRefType.MappingTypes.kmtEntrySequence ||
						type == (int)LexRefType.MappingTypes.kmtSenseSequence)
					{
						// Do we need to include the word itself in a sequence type relation for
						// this dialog?
					}
					ITsString tss = DbOps.ReadTss(odc, 5, m_cache.DefaultVernWs);
					if (tss == null || tss.Length == 0)
						tss = DbOps.ReadTss(odc, 6, m_cache.DefaultVernWs);
					if (tss == null || tss.Length == 0)
						continue;
					int homograph = DbOps.ReadInt(odc, 7);
					if (homograph > 0)
					{
						ITsIncStrBldr tisb = tss.GetIncBldr();
						tisb.SetIntPropValues((int)FwTextPropType.ktptSuperscript,
							(int)FwTextPropVar.ktpvEnum, (int)FwSuperscriptVal.kssvSub);
						tisb.SetIntPropValues((int)FwTextPropType.ktptBold,
							(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs,
							(int)FwTextPropVar.ktpvDefault, m_cache.DefaultUserWs);
						tisb.Append(homograph.ToString());
						tss = tisb.GetString();
					}
					m_cdaTemp.CacheStringProp(hvoRef, RelatedWordsVc.ktagName, tss);
				}
				if (hvoOldLexRef != 0)
				{
					m_cdaTemp.CacheVecProp(hvoOldLexRef, RelatedWordsVc.ktagWords,
						refs.ToArray(), refs.Count);
					refs.Clear();
				}
			}
			finally
			{
				DbOps.ShutdownODC(ref odc);
			}

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
				if (m_view != null && !Controls.Contains(m_view))
					m_view.Dispose();
				if (m_detailView != null && !Controls.Contains(m_detailView))
					m_detailView.Dispose();
			}
			m_sel = null;
			m_cache = null;
			m_view = null;
			m_detailView = null;
			if (m_cdaTemp != null)
			{
				m_cdaTemp.ClearAllData();
				Marshal.ReleaseComObject(m_cdaTemp);
				m_cdaTemp = null;
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RelatedWords));
			this.m_btnInsert = new System.Windows.Forms.Button();
			this.m_btnClose = new System.Windows.Forms.Button();
			this.m_btnLookup = new System.Windows.Forms.Button();
			this.m_btnCopy = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// m_btnInsert
			//
			resources.ApplyResources(this.m_btnInsert, "m_btnInsert");
			this.m_btnInsert.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btnInsert.Name = "m_btnInsert";
			this.m_btnInsert.Click += new System.EventHandler(this.m_btnInsert_Click);
			//
			// m_btnClose
			//
			resources.ApplyResources(this.m_btnClose, "m_btnClose");
			this.m_btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnClose.Name = "m_btnClose";
			//
			// m_btnLookup
			//
			resources.ApplyResources(this.m_btnLookup, "m_btnLookup");
			this.m_btnLookup.Name = "m_btnLookup";
			this.m_btnLookup.Click += new System.EventHandler(this.m_btnLookup_Click);
			//
			// m_btnCopy
			//
			resources.ApplyResources(this.m_btnCopy, "m_btnCopy");
			this.m_btnCopy.Name = "m_btnCopy";
			this.m_btnCopy.Click += new System.EventHandler(this.m_btnCopy_Click);
			//
			// RelatedWords
			//
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.m_btnClose;
			this.Controls.Add(this.m_btnCopy);
			this.Controls.Add(this.m_btnLookup);
			this.Controls.Add(this.m_btnClose);
			this.Controls.Add(this.m_btnInsert);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.MinimizeBox = false;
			this.Name = "RelatedWords";
			this.ResumeLayout(false);

		}
		#endregion

		private void m_btnInsert_Click(object sender, System.EventArgs e)
		{
			IVwSelection sel = m_view.RootBox.Selection;
			if (sel == null)
				return;

			string undo;
			string redo;
			ResourceHelper.MakeUndoRedoLabels("kstidUndoRedoInsertRelatedWord", out undo, out redo);
			using (UndoTaskHelper undoTaskHelper = new UndoTaskHelper(m_cache.MainCacheAccessor,
				m_view.RootBox.Site != null ? m_view.RootBox.Site : null, undo, redo, true))
			{
				ITsString tss;
				sel.GetSelectionString(out tss, "");
				m_sel.ReplaceWithTsString(tss);
				// TE-5754: The selection is not the installed selection so the commits that happen
				// as part of the data monitoring process are not committed to the database. We have
				// to perform an explicit commit on the selection.
				m_sel.Commit();
			}
		}

		private LexEntryUi GetSelWord()
		{
			IVwSelection sel = m_view.RootBox.Selection;
			if (sel == null)
				return null;
			IVwSelection sel2 = sel.EndPoint(false);
			if (sel2 == null)
				return null;
			IVwSelection sel3 = sel2.GrowToWord();
			if (sel3 == null)
				return null;
			ITsString tss;
			int ichMin, ichLim, hvo, tag, ws;
			bool fAssocPrev;
			sel3.TextSelInfo(false, out tss, out ichMin, out fAssocPrev, out hvo, out tag, out ws);
			sel3.TextSelInfo(true, out tss, out ichLim, out fAssocPrev, out hvo, out tag, out ws);

			ITsString tssWf = (m_cdaTemp as ISilDataAccess).get_StringProp(hvo, tag);
			if (tssWf == null || tssWf.Length == 0)
				return null;

			// Ignore what part of it is selected...we want the entry whose whole citation form
			// the selection is part of.
			//string wf = tssWf.Text.Substring(ichMin, ichLim - ichMin);
			return LexEntryUi.FindEntryForWordform(m_cache, tssWf);
		}

		private void m_btnLookup_Click(object sender, System.EventArgs e)
		{
			LexEntryUi leui = GetSelWord();
			if (leui == null)
			{
				ShowNotInDictMessage(this);
				return;
			}

			int[] domains;
			int[] lexrels;
			IVwCacheDa cdaTemp;
			if (!LoadDomainAndRelationInfo(m_cache, leui.Object.Hvo, out domains, out lexrels, out cdaTemp, this))
				return;
			m_cdaTemp.ClearAllData();
			// copy the names loaded into the even more temporary cda to the main one.
			foreach (int hvoDomain in domains)
				m_cdaTemp.CacheStringProp(hvoDomain, RelatedWordsVc.ktagName,
					(cdaTemp as ISilDataAccess).get_StringProp(hvoDomain,  RelatedWordsVc.ktagName));
			foreach (int hvoLexRel in lexrels)
				m_cdaTemp.CacheStringProp(hvoLexRel, RelatedWordsVc.ktagName,
					(cdaTemp as ISilDataAccess).get_StringProp(hvoLexRel, RelatedWordsVc.ktagName));
			m_hvoEntry = leui.Object.Hvo;
			SetupForEntry(domains, lexrels);
			m_view.SetEntry(m_hvoEntry);
		}

		private void m_btnCopy_Click(object sender, System.EventArgs e)
		{
			m_view.EditingHelper.CopySelection();
		}

		private void m_view_SelChanged(object sender, EventArgs e)
		{
			bool fEnable = m_view.GotRangeSelection;
			m_btnCopy.Enabled = fEnable;
			m_btnInsert.Enabled = fEnable;
			m_btnLookup.Enabled = fEnable;
			// Todo: create or update XmlView of selected word if any.
			LexEntryUi leui = GetSelWord();
			if (leui == null)
				return;
			if (m_detailView == null)
			{
				// Give the detailView the bottom 1/3 of the available height.
				this.SuspendLayout();
				int totalHeight = m_view.Height;
				m_view.Height = totalHeight * 2 / 3;
				m_detailView = MakeSummaryView(leui.Object.Hvo, m_cache, m_styleSheet);
				m_detailView.Left = m_view.Left;
				m_detailView.Width = m_view.Width;
				m_detailView.Top = m_view.Bottom + 5;
				m_detailView.Height = totalHeight / 3;
				m_detailView.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
				m_detailView.EditingHelper.DefaultCursor = Cursors.Arrow;
				this.Controls.Add(m_detailView);
				this.ResumeLayout();
				// JohnT: I'm not sure why this is needed here and not
				// elsewhere, but without it, the root box somehow never
				// receives an OnSizeChanged call and never actually
				// constructs.
				m_detailView.RootBox.Reconstruct();
			}
			else
			{
				m_detailView.RootObjectHvo = leui.Object.Hvo;
			}
		}
	}

	/// <summary>
	/// View showing related words. The provided data access should contain the needed data.
	/// object hvoEntry has a sequence (ktagDomains) of domains.
	/// each domain has a string ktagName and a sequence (ktagWords) of words.
	/// each word has a ktagName.
	/// </summary>
	internal class RelatedWordsView : SimpleRootSite
	{
		int m_hvoRoot;
		ISilDataAccess m_sda;
		int m_wsUser;
		RelatedWordsVc m_vc;
		bool m_fInSelChange = false;

		public event EventHandler SelChanged;

		public RelatedWordsView(int hvoRoot, ISilDataAccess sda, int wsUser)
		{
			m_hvoRoot = hvoRoot;
			m_sda = sda;
			m_wsUser = wsUser;
			m_wsf = sda.WritingSystemFactory;
		}

		#region IDisposable override

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
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_vc != null)
					m_vc.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_vc = null;
			m_sda = null;
		}

		#endregion IDisposable override

		/// <summary>
		/// Make the root box and initialize it.
		/// </summary>
		public override void MakeRoot()
		{
			CheckDisposed();

			base.MakeRoot();

			IVwRootBox rootb = VwRootBoxClass.Create();
			rootb.SetSite(this);

			m_vc = new RelatedWordsVc(m_wsUser);

			rootb.DataAccess = m_sda;

			m_rootb = rootb;
			m_rootb.SetRootObject(m_hvoRoot, m_vc, RelatedWordsVc.kfragRoot, m_styleSheet);
			m_fRootboxMade = true;
		}
		internal void SetEntry(int hvoEntry)
		{
			CheckDisposed();

			m_hvoRoot = hvoEntry;
			m_rootb.SetRootObject(m_hvoRoot, m_vc, RelatedWordsVc.kfragRoot, m_styleSheet);
		}

		/// <summary>
		/// Handle a selection change by growing it to a word (unless the new selection IS
		/// the one we're growing to a word).
		/// </summary>
		/// <param name="prootb"></param>
		/// <param name="vwselNew"></param>
		public override void SelectionChanged(IVwRootBox prootb, IVwSelection vwselNew)
		{
			CheckDisposed();

			base.SelectionChanged (prootb, vwselNew);
			if (!m_fInSelChange)
			{
				m_fInSelChange = true;
				try
				{
					if (!vwselNew.IsRange)
					{
						vwselNew.GrowToWord().Install();
					}
				}
				finally
				{
					m_fInSelChange = false;
				}
			}
			if (SelChanged != null)
				SelChanged(this, new EventArgs());
		}

		internal bool GotRangeSelection
		{
			get
			{
				CheckDisposed();

				IVwSelection sel = RootBox.Selection;
				return sel != null && sel.IsRange;
			}
		}

		public override Cursor Cursor
		{
			get
			{
				CheckDisposed();

				return base.Cursor;
			}
			set
			{
				CheckDisposed();

				base.Cursor = value;
			}
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class RelatedWordsVc : VwBaseVc
	{
		public const int ktagDomains = 45671;
		public const int ktagName = 45672;
		public const int ktagWords = 45673;
		public const int ktagCf = 45674;
		public const int ktagLexRels = 45675;

		public const int kfragRoot = 333331;
		public const int kfragEntryList = 3333332;
		public const int kfragWords = 3333333;
		public const int kfragName = 3333334;

		private ITsString m_tssColon;
		private ITsString m_tssComma;
		private ITsString m_tssSdRelation;
		private ITsString m_tssLexRelation;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:RelatedWordsVc"/> class.
		/// </summary>
		/// <param name="wsUser">The ws user.</param>
		/// ------------------------------------------------------------------------------------
		public RelatedWordsVc(int wsUser)
		{
			m_wsDefault = wsUser;
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			m_tssColon = tsf.MakeString(": ", wsUser);
			m_tssComma = tsf.MakeString(", ", wsUser);
			m_tssSdRelation = tsf.MakeString(FdoUiStrings.ksWordsRelatedBySemanticDomain, wsUser);
			m_tssLexRelation = tsf.MakeString(FdoUiStrings.ksLexicallyRelatedWords, wsUser);
		}

		#region IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes in two distinct scenarios.
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
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
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			Marshal.ReleaseComObject(m_tssColon);
			m_tssColon = null;
			Marshal.ReleaseComObject(m_tssComma);
			m_tssComma = null;
			Marshal.ReleaseComObject(m_tssSdRelation);
			m_tssSdRelation = null;
			Marshal.ReleaseComObject(m_tssLexRelation);
			m_tssLexRelation = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// This is the main interesting method of displaying objects and fragments of them.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// -----------------------------------------------------------------------------------
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			CheckDisposed();

			switch(frag)
			{
				case kfragRoot:
					ITsString tssWord = vwenv.DataAccess.get_StringProp(hvo, ktagCf);
					ITsStrBldr tsbSdRelation = m_tssSdRelation.GetBldr();
					ITsStrBldr tsbLexRel = m_tssLexRelation.GetBldr();
					if (tssWord != null && tssWord.Length > 0)
					{
						int ich = tsbSdRelation.Text.IndexOf("{0}");
						if (ich >= 0)
							tsbSdRelation.ReplaceTsString(ich, ich + 3, tssWord);
						ich = tsbLexRel.Text.IndexOf("{0}");
						if (ich >= 0)
							tsbLexRel.ReplaceTsString(ich, ich + 3, tssWord);
					}
					int cDomains = vwenv.DataAccess.get_VecSize(hvo, ktagDomains);
					int cLexRels = vwenv.DataAccess.get_VecSize(hvo, ktagLexRels);
					Debug.Assert(cDomains > 0 || cLexRels > 0);
					if (cDomains > 0)
					{
						vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
							(int)FwTextPropVar.ktpvEnum,
							(int)TptEditable.ktptNotEditable);
						vwenv.set_IntProperty((int)FwTextPropType.ktptMarginBottom,
							(int)FwTextPropVar.ktpvMilliPoint,
							6000);
						vwenv.OpenParagraph();
						vwenv.AddString(tsbSdRelation.GetString());
						vwenv.CloseParagraph();
						vwenv.AddLazyVecItems(ktagDomains, this, kfragEntryList);
					}
					if (cLexRels > 0)
					{
						vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
							(int)FwTextPropVar.ktpvEnum,
							(int)TptEditable.ktptNotEditable);
						vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTop,
							(int)FwTextPropVar.ktpvMilliPoint, 6000);
						vwenv.set_IntProperty((int)FwTextPropType.ktptMarginBottom,
							(int)FwTextPropVar.ktpvMilliPoint, 6000);
						vwenv.OpenParagraph();
						vwenv.AddString(tsbLexRel.GetString());
						vwenv.CloseParagraph();
						vwenv.AddLazyVecItems(ktagLexRels, this, kfragEntryList);
					}
					break;
				case kfragEntryList:
					vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
						(int)FwTextPropVar.ktpvEnum,
						(int)TptEditable.ktptNotEditable);
					vwenv.OpenParagraph();
					vwenv.set_IntProperty((int)FwTextPropType.ktptBold,
						(int)FwTextPropVar.ktpvEnum,
						(int)FwTextToggleVal.kttvForceOn);
					vwenv.AddStringProp(ktagName, this);
					vwenv.AddString (m_tssColon);
					vwenv.AddObjVec(ktagWords, this, kfragWords);
					vwenv.CloseParagraph();
					break;
				case kfragName:
					vwenv.AddStringProp(ktagName, this);
					break;
				default:
					throw new Exception("bad case in RelatedWordsVc.Display");
			}
		}
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Handles displaying the vector of words with commas except after the last
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="frag"></param>
		/// -----------------------------------------------------------------------------------
		public override void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
		{
			CheckDisposed();

			Debug.Assert(frag == kfragWords);
			ISilDataAccess sda = vwenv.DataAccess;
			int cwords = sda.get_VecSize(hvo, ktagWords);
			for (int i = 0; i < cwords; i++)
			{
				vwenv.AddObj(sda.get_VecItem(hvo, ktagWords, i), this, kfragName);
				if (i != cwords - 1)
					vwenv.AddString(m_tssComma);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Estimate the height in points of one domain.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// <param name="dxAvailWidth"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override int EstimateHeight(int hvo, int frag, int dxAvailWidth)
		{
			CheckDisposed();

			return 20; // a domain typically isn't very high.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// pre-load any required data about a particular domain.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="rghvo"></param>
		/// <param name="chvo"></param>
		/// <param name="hvoParent"></param>
		/// <param name="tag"></param>
		/// <param name="frag"></param>
		/// <param name="ihvoMin"></param>
		/// ------------------------------------------------------------------------------------
		public override void LoadDataFor(IVwEnv vwenv, int[] rghvo, int chvo, int hvoParent,
			int tag, int frag, int ihvoMin)
		{
			CheckDisposed();

			// Nothing to do, all data already loaded.
		}
	}
}
