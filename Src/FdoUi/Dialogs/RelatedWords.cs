using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Resources;
using SIL.Utils;

namespace SIL.FieldWorks.FdoUi.Dialogs
{
	/// <summary>
	/// Summary description for RelatedWords.
	/// </summary>
	public class RelatedWords : Form, IFWDisposable
	{
		private Button m_btnInsert;
		private Button m_btnClose;
		private Button m_btnLookup;
		private Button m_btnCopy;
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
			bool fHaveSemDomains = LoadDomainInfo(cache, hvoEntry, out domainsOut, out cdaTemp);
			bool fHaveLexRels = LoadLexicalRelationInfo(cache, hvoEntry, out lexrelsOut, cdaTemp);
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
		/// <param name="hvoEntry">int ID of the lexical entry</param>
		/// <param name="hvoSemanticDomainsOut">A list of int IDs of the semantic domains of the lexical entry</param>
		/// <param name="cdaTemp"></param>
		/// <returns></returns>
		static private bool LoadDomainInfo(FdoCache cache, int hvoEntry, out int[] hvoSemanticDomainsOut, out IVwCacheDa cdaTemp)
		{
			// REVIEW (SteveMiller): The LINQ below runs slow the first time its run. We should try to
			// optimize it if possible.
			var entryRepo = cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var lexEntry = entryRepo.GetObject(hvoEntry);
			var domains =
				(from sense in lexEntry.AllSenses
				from sd in sense.SemanticDomainsRC
				where (from incoming in sd.ReferringObjects
					   where incoming is ILexSense && incoming.OwnerOfClass<ILexEntry>() != lexEntry
					   select incoming).FirstOrDefault() != null
				select sd).Distinct().ToArray();
			hvoSemanticDomainsOut = (
				from sd in domains
				select sd.Hvo).ToArray();

			cdaTemp = VwCacheDaClass.Create();
			foreach (var sd in domains)
			{
				cdaTemp.CacheStringProp(sd.Hvo, RelatedWordsVc.ktagName,
										sd.Name.BestVernacularAnalysisAlternative);
			}
			cdaTemp.CacheVecProp(hvoEntry, RelatedWordsVc.ktagDomains, hvoSemanticDomainsOut, hvoSemanticDomainsOut.Length);

			return hvoSemanticDomainsOut.Length > 0;
		}

		/// <summary>
		/// Load the information about the lexical relations that link to hvoEntry. Specifically, we want LexReferences
		/// that refer to the target Entry (hvoEntry) and also some other lexical entry.
		/// For each such thing, we store in cdaTemp the name (or, if appropriate, the reverse name) of the
		/// relationship that hvoEntry has to the other entry(s) in the lex reference, as property RelatedWordsVc.ktagName of
		/// the LexReference Hvo. Return through relsOut the list of LexReferences that are thus related to hvoEntry.
		/// Return true if there are any.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoEntry">ID of the lexical entry we're working with</param>
		/// <param name="relsOut">an array of IDs (HVOs) for related objects</param>
		/// <param name="cdaTemp"></param>
		/// <returns>false if the entry has no associated lexical relations, or none of them are linked to any other entries.</returns>
		//
		static private bool LoadLexicalRelationInfo(FdoCache cache, int hvoEntry, out int[] relsOut, IVwCacheDa cdaTemp)
		{
			var relatedObjectIds = new List<int>();
			var entryRepository = cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var lexEntry = entryRepository.GetObject(hvoEntry);
			var targets = new HashSet<ICmObject>(lexEntry.AllSenses.Cast<ICmObject>()) {lexEntry};

			foreach (ILexRefType lexRefType in cache.LanguageProject.LexDbOA.ReferencesOA.ReallyReallyAllPossibilities)
			{
				foreach (var lexReference in lexRefType.MembersOC)
				{
					// If at least one target is the lex entry or one of its senses.
					if ((from target in lexReference.TargetsRS where targets.Contains(target) select target).FirstOrDefault() != null &&
						(from target in lexReference.TargetsRS where !targets.Contains(target) select target).FirstOrDefault() != null)
					{

						// The name we want to use for our lex reference is either the name or the reverse name
						// (depending on the direction of the relationship, if relevant) of the owning lex ref type.
						var lexReferenceName = lexRefType.Name.BestVernacularAnalysisAlternative;

						if (lexRefType.MappingType == (int)MappingTypes.kmtEntryAsymmetricPair ||
							lexRefType.MappingType == (int)MappingTypes.kmtEntryOrSenseAsymmetricPair ||
							lexRefType.MappingType == (int)MappingTypes.kmtSenseAsymmetricPair ||
							lexRefType.MappingType == (int)MappingTypes.kmtEntryTree ||
							lexRefType.MappingType == (int)MappingTypes.kmtEntryOrSenseTree ||
							lexRefType.MappingType == (int)MappingTypes.kmtSenseTree)
						{
							if (lexEntry.OwnOrd == 0 && lexRefType.Name != null) // the original code had a check for name length as well.
								lexReferenceName = lexRefType.ReverseName.BestVernacularAnalysisAlternative;
						}

						cdaTemp.CacheStringProp(lexReference.Hvo, RelatedWordsVc.ktagName, lexReferenceName);
						relatedObjectIds.Add(lexReference.Hvo);
					}
				}
			}

			relsOut = relatedObjectIds.ToArray();
			return relsOut.Length > 0;
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
			XmlView xv = new XmlView(hvoEntry, "publishStem", false);
			xv.Cache = cache;
			xv.StyleSheet = styleSheet;
			return xv;
		}


		public RelatedWords(FdoCache cache, IVwSelection sel, int hvoEntry, int[] domains, int[] lexrels,
			IVwCacheDa cdaTemp, IVwStylesheet styleSheet, bool hideInsertButton)
		{
			m_cache = cache;
			m_sel = sel;
			m_hvoEntry = hvoEntry;
			m_styleSheet = styleSheet;
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = GetType().Name;
			m_btnInsert.Visible = !hideInsertButton;

			m_cdaTemp = cdaTemp;
			ISilDataAccess sda = m_cdaTemp as ISilDataAccess;
			sda.WritingSystemFactory = cache.WritingSystemFactory;

			SetupForEntry(domains, lexrels);

			var entry = cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetObject(m_hvoEntry);
			m_view = new RelatedWordsView(m_cache, m_hvoEntry, entry.HeadWord,
				m_cdaTemp as ISilDataAccess,
				cache.ServiceLocator.WritingSystemManager.UserWs);
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

		/// <summary>
		/// Set up the referring semantic domains for the domains found of an entry
		/// </summary>
		/// <param name="semanticDomainHvos">an array of semantic domain HVOs</param>
		void SetupDomainsForEntry(int[] semanticDomainHvos)
		{
			m_cdaTemp.CacheVecProp(m_hvoEntry, RelatedWordsVc.ktagDomains, semanticDomainHvos, semanticDomainHvos.Length);

			var entries = new List<int>();
			var semanticDomainRepository = m_cache.ServiceLocator.GetInstance<ICmSemanticDomainRepository>();

			foreach (var semanticDomainhvo in semanticDomainHvos)
			{
				var semanticDomain = semanticDomainRepository.GetObject(semanticDomainhvo);
				foreach (ICmObject obj in semanticDomain.ReferringObjects)
				{
					if (obj is ILexSense && (obj as ILexSense).SemanticDomainsRC.Contains(semanticDomain))
					{
						var entry = obj.OwnerOfClass(LexEntryTags.kClassId) as ILexEntry;
						if (entry != null && entry.LexemeFormOA != null && entry.LexemeFormOA.Form != null)
						{
							entries.Add(entry.Hvo);
							m_cdaTemp.CacheStringProp(entry.Hvo, RelatedWordsVc.ktagName,
								entry.LexemeFormOA.Form.VernacularDefaultWritingSystem);
						}
					}
				}
				if (entries.Count > 0)
				{
					m_cdaTemp.CacheVecProp(semanticDomainhvo, RelatedWordsVc.ktagWords, entries.ToArray(), entries.Count);
					entries.Clear();
				}
			}
		}

		/// <summary>
		/// Set up the referring lexical entries of an entry
		/// </summary>
		/// <param name="lexicalRelationHvos">an array of lexical relation HVOs</param>
		private void SetupLexRelsForEntry(int[] lexicalRelationHvos)
		{
			m_cdaTemp.CacheVecProp(m_hvoEntry, RelatedWordsVc.ktagLexRels, lexicalRelationHvos, lexicalRelationHvos.Length);

			var references = new List<int>();
			var lexRefRepository = m_cache.ServiceLocator.GetInstance<ILexReferenceRepository>();
			var lexEntry = m_cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetObject(m_hvoEntry);
			var targets = new HashSet<ICmObject>(lexEntry.AllSenses.Cast<ICmObject>()) { lexEntry };

			foreach (var hvoLexRel in lexicalRelationHvos)
			{
				var lexReference = lexRefRepository.GetObject(hvoLexRel);
				foreach (ICmObject target in lexReference.TargetsRS)
				{
					// If at least one target is the lex entry or one of its senses.
					if ((from t in lexReference.TargetsRS where targets.Contains(t) select t).FirstOrDefault() != null &&
						(from t in lexReference.TargetsRS where !targets.Contains(t) select t).FirstOrDefault() != null)
					{
						ILexEntry targetEntry = target is ILexSense
							? target.OwnerOfClass(LexEntryTags.kClassId) as ILexEntry
							: target as ILexEntry;
						if (targetEntry != null && targetEntry.Hvo != m_hvoEntry && targetEntry.LexemeFormOA != null && targetEntry.LexemeFormOA.Form != null)
						{
							references.Add(targetEntry.Hvo);
							m_cdaTemp.CacheStringProp(targetEntry.Hvo, RelatedWordsVc.ktagName, targetEntry.HeadWord);
						}
					}
				}

				if (references.Count > 0)
				{
					m_cdaTemp.CacheVecProp(hvoLexRel, RelatedWordsVc.ktagWords, references.ToArray(), references.Count);
					references.Clear();
				}
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
			using (UndoTaskHelper undoTaskHelper = new UndoTaskHelper(m_cache.ActionHandlerAccessor,
				m_view.RootBox.Site, undo, redo))
			{
				ITsString tss;
				sel.GetSelectionString(out tss, "");
				m_sel.ReplaceWithTsString(tss);
				undoTaskHelper.RollBack = false;
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
			using (LexEntryUi leui = GetSelWord())
			{
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
					(cdaTemp as ISilDataAccess).get_StringProp(hvoDomain, RelatedWordsVc.ktagName));
				foreach (int hvoLexRel in lexrels)
					m_cdaTemp.CacheStringProp(hvoLexRel, RelatedWordsVc.ktagName,
					(cdaTemp as ISilDataAccess).get_StringProp(hvoLexRel, RelatedWordsVc.ktagName));
				m_hvoEntry = leui.Object.Hvo;
				SetupForEntry(domains, lexrels);
				m_view.SetEntry(m_hvoEntry);
			}
		}

		private void m_btnCopy_Click(object sender, System.EventArgs e)
		{
			m_view.EditingHelper.CopySelection();
		}

		private void m_view_SelChanged(object sender, EventArgs e)
		{
			// Todo: create or update XmlView of selected word if any.
			using (LexEntryUi leui = GetSelWord())
			{
				bool fEnable = m_view.GotRangeSelection && leui != null;
				m_btnCopy.Enabled = fEnable;
				m_btnInsert.Enabled = fEnable;
				m_btnLookup.Enabled = fEnable;

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
		FdoCache m_cache;
		int m_wsUser;
		RelatedWordsVc m_vc;
		bool m_fInSelChange = false;
		private ITsString m_headword;

		public event EventHandler SelChanged;

		public RelatedWordsView(FdoCache cache, int hvoRoot, ITsString headword, ISilDataAccess sda, int wsUser)
		{
			m_cache = cache;
			m_hvoRoot = hvoRoot;
			m_headword = headword;
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
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			if (disposing)
			{
				// Dispose managed resources here.
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

			m_vc = new RelatedWordsVc(m_wsUser, m_headword);

			rootb.DataAccess = m_sda;

			m_rootb = rootb;
			m_rootb.SetRootObject(m_hvoRoot, m_vc, RelatedWordsVc.kfragRoot, m_styleSheet);
			m_fRootboxMade = true;
		}
		internal void SetEntry(int hvoEntry)
		{
			CheckDisposed();
			var entry = m_cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetObject(hvoEntry);
			m_headword = entry.HeadWord;
			m_hvoRoot = hvoEntry;
			m_vc = new RelatedWordsVc(m_wsUser, m_headword);
			m_rootb.SetRootObject(m_hvoRoot, m_vc, RelatedWordsVc.kfragRoot, m_styleSheet);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the editing helper is created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnEditingHelperCreated()
		{
			m_editingHelper.VwSelectionChanged += HandleSelectionChange;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a selection change by growing it to a word (unless the new selection IS
		/// the one we're growing to a word).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleSelectionChange(object sender, VwSelectionArgs args)
		{
			CheckDisposed();

			IVwRootBox rootb = args.RootBox;
			IVwSelection vwselNew = args.Selection;
			Debug.Assert(vwselNew != null);

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
	internal class RelatedWordsVc : FwBaseVc
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
		/// Initializes a new instance of the RelatedWordsVc class.
		/// </summary>
		/// <param name="wsUser">The ws user.</param>
		/// <param name="headword">Headword of the target lexical entry.</param>
		/// ------------------------------------------------------------------------------------
		public RelatedWordsVc(int wsUser, ITsString headword)
		{
			m_wsDefault = wsUser;
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			m_tssColon = tsf.MakeString(": ", wsUser);
			m_tssComma = tsf.MakeString(", ", wsUser);
			m_tssSdRelation = tsf.MakeString(FdoUiStrings.ksWordsRelatedBySemanticDomain, wsUser);
			m_tssLexRelation = tsf.MakeString(FdoUiStrings.ksLexicallyRelatedWords, wsUser);

			var semanticDomainStrBuilder = m_tssSdRelation.GetBldr();
			var index = semanticDomainStrBuilder.Text.IndexOf("{0}");
			if (index > 0)
				semanticDomainStrBuilder.ReplaceTsString(index, index + "{0}".Length, headword);
			m_tssSdRelation = semanticDomainStrBuilder.GetString();

			var lexStrBuilder = m_tssLexRelation.GetBldr();
			index = lexStrBuilder.Text.IndexOf("{0}");
			if (index > 0)
				lexStrBuilder.ReplaceTsString(index, index + "{0}".Length, headword);
			m_tssLexRelation = lexStrBuilder.GetString();
		}

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
			// Nothing to do, all data already loaded.
		}
	}
}
