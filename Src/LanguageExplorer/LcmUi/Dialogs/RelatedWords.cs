// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.LcmUi.Dialogs
{
	/// <summary />
	public class RelatedWords : Form
	{
		private Button m_btnInsert;
		private Button m_btnClose;
		private Button m_btnLookup;
		private Button m_btnCopy;
		private IContainer components = null;
		private RelatedWordsView m_view;
		private LcmCache m_cache;
		private IVwSelection m_sel;
		private IVwStylesheet m_styleSheet;
		private int m_hvoEntry;
		private IVwCacheDa m_cdaTemp;
		private XmlView m_detailView;

		/// <summary>
		/// Shows the "not in dictionary" message.
		/// </summary>
		internal static void ShowNotInDictMessage(IWin32Window owner)
		{
			MessageBox.Show(owner, LcmUiStrings.kstidFindRelWordsNotInDict, LcmUiStrings.kstidFindRelWordsTitle);
		}

		/// <summary>
		/// Load both the semantic domains and the lexical relation information for hvoEntry.
		/// Returns false (after displaying a dialog) if the entry has no associated domains or
		/// lexical relations, or none of them are linked to any other entries.
		/// </summary>
		internal static bool LoadDomainAndRelationInfo(LcmCache cache, int hvoEntry, out int[] domainsOut, out int[] lexrelsOut, out IVwCacheDa cdaTemp, IWin32Window owner)
		{
			var fHaveSemDomains = LoadDomainInfo(cache, hvoEntry, out domainsOut, out cdaTemp);
			var fHaveLexRels = LoadLexicalRelationInfo(cache, hvoEntry, out lexrelsOut, cdaTemp);
			if (!fHaveSemDomains && !fHaveLexRels)
			{
				MessageBox.Show(owner, LcmUiStrings.ksNoSemanticDomainsListedForEntry, LcmUiStrings.ksFindRelatedWords);
				return false;
			}
			if (domainsOut.Length == 0 && lexrelsOut.Length == 0)
			{
				MessageBox.Show(owner, LcmUiStrings.ksNoEntriesWithSameSemanticDomain, LcmUiStrings.ksFindRelatedWords);
				return false;
			}
			return true;
		}

		/// <summary>
		/// Load the information about the domains of hvoEntry. Returns false
		/// if the entry has no associated domains or none of them are linked to any other entries.
		/// </summary>
		private static bool LoadDomainInfo(LcmCache cache, int hvoEntry, out int[] hvoSemanticDomainsOut, out IVwCacheDa cdaTemp)
		{
			// REVIEW (SteveMiller): The LINQ below runs slow the first time its run. We should try to
			// optimize it if possible.
			var entryRepo = cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var lexEntry = entryRepo.GetObject(hvoEntry);
			var domains = lexEntry.AllSenses
				.SelectMany(sense => sense.SemanticDomainsRC, (sense, sd) => new { sense, sd })
				.Where(@t => (@t.sd.ReferringObjects.Where(incoming => incoming is ILexSense && incoming.OwnerOfClass<ILexEntry >() != lexEntry)).FirstOrDefault() != null)
				.Select(@t => @t.sd).Distinct().ToArray();
			hvoSemanticDomainsOut = domains.Select(sd => sd.Hvo).ToArray();
			cdaTemp = VwCacheDaClass.Create();
			cdaTemp.TsStrFactory = TsStringUtils.TsStrFactory;
			foreach (var sd in domains)
			{
				cdaTemp.CacheStringProp(sd.Hvo, RelatedWordsVc.ktagName, sd.Name.BestVernacularAnalysisAlternative);
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
		private static bool LoadLexicalRelationInfo(LcmCache cache, int hvoEntry, out int[] relsOut, IVwCacheDa cdaTemp)
		{
			var relatedObjectIds = new List<int>();
			var entryRepository = cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var lexEntry = entryRepository.GetObject(hvoEntry);
			var targets = new HashSet<ICmObject>(lexEntry.AllSenses) { lexEntry };
			foreach (ILexRefType lexRefType in cache.LanguageProject.LexDbOA.ReferencesOA.ReallyReallyAllPossibilities)
			{
				foreach (var lexReference in lexRefType.MembersOC)
				{
					// If at least one target is the lex entry or one of its senses.
					if (lexReference.TargetsRS.FirstOrDefault(target => targets.Contains(target)) == null || lexReference.TargetsRS.FirstOrDefault(target => !targets.Contains(target)) == null)
					{
						continue;
					}
					// The name we want to use for our lex reference is either the name or the reverse name
					// (depending on the direction of the relationship, if relevant) of the owning lex ref type.
					var lexReferenceName = lexRefType.Name.BestVernacularAnalysisAlternative;
					if (lexRefType.MappingType == (int)MappingTypes.kmtEntryAsymmetricPair || lexRefType.MappingType == (int)MappingTypes.kmtEntryOrSenseAsymmetricPair
						|| lexRefType.MappingType == (int)MappingTypes.kmtSenseAsymmetricPair || lexRefType.MappingType == (int)MappingTypes.kmtEntryTree
						|| lexRefType.MappingType == (int)MappingTypes.kmtEntryOrSenseTree || lexRefType.MappingType == (int)MappingTypes.kmtSenseTree)
					{
						if (lexEntry.OwnOrd == 0 && lexRefType.Name != null) // the original code had a check for name length as well.
						{
							lexReferenceName = lexRefType.ReverseName.BestVernacularAnalysisAlternative;
						}
					}
					cdaTemp.CacheStringProp(lexReference.Hvo, RelatedWordsVc.ktagName, lexReferenceName);
					relatedObjectIds.Add(lexReference.Hvo);
				}
			}
			relsOut = relatedObjectIds.ToArray();
			return relsOut.Length > 0;
		}

		/// <summary>
		/// Create a view with a single LexEntry object.
		/// </summary>
		internal static XmlView MakeSummaryView(int hvoEntry, LcmCache cache, IVwStylesheet styleSheet)
		{
			var xv = new XmlView(hvoEntry, "publishStem", false)
			{
				Cache = cache,
				StyleSheet = styleSheet
			};
			return xv;
		}

		public RelatedWords(LcmCache cache, IVwSelection sel, int hvoEntry, int[] domains, int[] lexrels, IVwCacheDa cdaTemp, IVwStylesheet styleSheet, bool hideInsertButton)
		{
			m_cache = cache;
			m_sel = sel;
			m_hvoEntry = hvoEntry;
			m_styleSheet = styleSheet;
			InitializeComponent();
			AccessibleName = GetType().Name;
			m_btnInsert.Visible = !hideInsertButton;
			m_cdaTemp = cdaTemp;
			var sda = m_cdaTemp as ISilDataAccess;
			sda.WritingSystemFactory = cache.WritingSystemFactory;
			SetupForEntry(domains, lexrels);
			var entry = cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetObject(m_hvoEntry);
			m_view = new RelatedWordsView(m_cache, m_hvoEntry, entry.HeadWord, m_cdaTemp as ISilDataAccess, cache.ServiceLocator.WritingSystemManager.UserWs)
			{
				Width = Width - 20,
				Height = m_btnClose.Top - 20,
				Top = 10,
				Left = 10,
				Anchor = AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
				BackColor = Color.FromKnownColor(KnownColor.Window)
			};
			m_view.EditingHelper.DefaultCursor = Cursors.Arrow;
			m_view.SelChanged += m_view_SelChanged;
			Controls.Add(m_view);
		}

		/// <summary>
		/// Store data in m_cdaTemp suitable for displaying words related to m_hvoEntry
		/// </summary>
		private void SetupForEntry(int[] domains, int[] lexrels)
		{
			SetupDomainsForEntry(domains);
			SetupLexRelsForEntry(lexrels);
		}

		/// <summary>
		/// Set up the referring semantic domains for the domains found of an entry
		/// </summary>
		private void SetupDomainsForEntry(int[] semanticDomainHvos)
		{
			m_cdaTemp.CacheVecProp(m_hvoEntry, RelatedWordsVc.ktagDomains, semanticDomainHvos, semanticDomainHvos.Length);
			var entries = new List<int>();
			var semanticDomainRepository = m_cache.ServiceLocator.GetInstance<ICmSemanticDomainRepository>();
			foreach (var semanticDomainhvo in semanticDomainHvos)
			{
				var semanticDomain = semanticDomainRepository.GetObject(semanticDomainhvo);
				foreach (var obj in semanticDomain.ReferringObjects)
				{
					if (!(obj is ILexSense) || !(obj as ILexSense).SemanticDomainsRC.Contains(semanticDomain))
					{
						continue;
					}
					var entry = obj.OwnerOfClass(LexEntryTags.kClassId) as ILexEntry;
					if (entry?.LexemeFormOA?.Form != null)
					{
						entries.Add(entry.Hvo);
						m_cdaTemp.CacheStringProp(entry.Hvo, RelatedWordsVc.ktagName, entry.LexemeFormOA.Form.VernacularDefaultWritingSystem);
					}
				}
				if (entries.Any())
				{
					m_cdaTemp.CacheVecProp(semanticDomainhvo, RelatedWordsVc.ktagWords, entries.ToArray(), entries.Count);
					entries.Clear();
				}
			}
		}

		/// <summary>
		/// Set up the referring lexical entries of an entry
		/// </summary>
		private void SetupLexRelsForEntry(int[] lexicalRelationHvos)
		{
			m_cdaTemp.CacheVecProp(m_hvoEntry, RelatedWordsVc.ktagLexRels, lexicalRelationHvos, lexicalRelationHvos.Length);
			var references = new List<int>();
			var lexRefRepository = m_cache.ServiceLocator.GetInstance<ILexReferenceRepository>();
			var lexEntry = m_cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetObject(m_hvoEntry);
			var targets = new HashSet<ICmObject>(lexEntry.AllSenses) { lexEntry };
			foreach (var hvoLexRel in lexicalRelationHvos)
			{
				var lexReference = lexRefRepository.GetObject(hvoLexRel);
				foreach (var target in lexReference.TargetsRS)
				{
					// If at least one target is the lex entry or one of its senses.
					if (lexReference.TargetsRS.FirstOrDefault(t => targets.Contains(t)) == null ||
						lexReference.TargetsRS.FirstOrDefault(t => !targets.Contains(t)) == null)
					{
						continue;
					}
					var targetEntry = target is ILexSense ? target.OwnerOfClass(LexEntryTags.kClassId) as ILexEntry : target as ILexEntry;
					if (targetEntry != null && targetEntry.Hvo != m_hvoEntry && targetEntry.LexemeFormOA?.Form != null)
					{
						references.Add(targetEntry.Hvo);
						m_cdaTemp.CacheStringProp(targetEntry.Hvo, RelatedWordsVc.ktagName, targetEntry.HeadWord);
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
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				components?.Dispose();
				if (m_view != null && !Controls.Contains(m_view))
				{
					m_view.Dispose();
				}

				if (m_detailView != null && !Controls.Contains(m_detailView))
				{
					m_detailView.Dispose();
				}
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

			base.Dispose(disposing);
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
			var sel = m_view.RootBox.Selection;
			if (sel == null)
			{
				return;
			}
			string undo;
			string redo;
			ResourceHelper.MakeUndoRedoLabels("kstidUndoRedoInsertRelatedWord", out undo, out redo);
			using (var undoTaskHelper = new UndoTaskHelper(m_cache.ActionHandlerAccessor, m_view.RootBox.Site, undo, redo))
			{
				ITsString tss;
				sel.GetSelectionString(out tss, string.Empty);
				m_sel.ReplaceWithTsString(tss);
				undoTaskHelper.RollBack = false;
			}
		}

		private LexEntryUi GetSelWord()
		{
			var sel = m_view.RootBox.Selection;
			var sel2 = sel?.EndPoint(false);
			var sel3 = sel2?.GrowToWord();
			if (sel3 == null)
			{
				return null;
			}
			ITsString tss;
			int ichMin, ichLim, hvo, tag, ws;
			bool fAssocPrev;
			sel3.TextSelInfo(false, out tss, out ichMin, out fAssocPrev, out hvo, out tag, out ws);
			sel3.TextSelInfo(true, out tss, out ichLim, out fAssocPrev, out hvo, out tag, out ws);
			var tssWf = (m_cdaTemp as ISilDataAccess).get_StringProp(hvo, tag);
			if (tssWf == null || tssWf.Length == 0)
			{
				return null;
			}
			// Ignore what part of it is selected...we want the entry whose whole citation form
			// the selection is part of.
			//string wf = tssWf.Text.Substring(ichMin, ichLim - ichMin);
			return LexEntryUi.FindEntryForWordform(m_cache, tssWf);
		}

		private void m_btnLookup_Click(object sender, System.EventArgs e)
		{
			using (var leui = GetSelWord())
			{
				if (leui == null)
				{
					ShowNotInDictMessage(this);
					return;
				}
				int[] domains;
				int[] lexrels;
				IVwCacheDa cdaTemp;
				if (!LoadDomainAndRelationInfo(m_cache, leui.MyCmObject.Hvo, out domains, out lexrels, out cdaTemp, this))
				{
					return;
				}
				m_cdaTemp.ClearAllData();
				// copy the names loaded into the even more temporary cda to the main one.
				foreach (var hvoDomain in domains)
				{
					m_cdaTemp.CacheStringProp(hvoDomain, RelatedWordsVc.ktagName, (cdaTemp as ISilDataAccess).get_StringProp(hvoDomain, RelatedWordsVc.ktagName));
				}
				foreach (var hvoLexRel in lexrels)
				{
					m_cdaTemp.CacheStringProp(hvoLexRel, RelatedWordsVc.ktagName, (cdaTemp as ISilDataAccess).get_StringProp(hvoLexRel, RelatedWordsVc.ktagName));
				}
				m_hvoEntry = leui.MyCmObject.Hvo;
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
			using (var leui = GetSelWord())
			{
				var fEnable = m_view.GotRangeSelection && leui != null;
				m_btnCopy.Enabled = fEnable;
				m_btnInsert.Enabled = fEnable;
				m_btnLookup.Enabled = fEnable;
				if (leui == null)
				{
					return;
				}
				if (m_detailView == null)
				{
					// Give the detailView the bottom 1/3 of the available height.
					SuspendLayout();
					var totalHeight = m_view.Height;
					m_view.Height = totalHeight * 2 / 3;
					m_detailView = MakeSummaryView(leui.MyCmObject.Hvo, m_cache, m_styleSheet);
					m_detailView.Left = m_view.Left;
					m_detailView.Width = m_view.Width;
					m_detailView.Top = m_view.Bottom + 5;
					m_detailView.Height = totalHeight / 3;
					m_detailView.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
					m_detailView.EditingHelper.DefaultCursor = Cursors.Arrow;
					Controls.Add(m_detailView);
					ResumeLayout();
					// JohnT: I'm not sure why this is needed here and not
					// elsewhere, but without it, the root box somehow never
					// receives an OnSizeChanged call and never actually
					// constructs.
					m_detailView.RootBox.Reconstruct();
				}
				else
				{
					m_detailView.RootObjectHvo = leui.MyCmObject.Hvo;
				}
			}
		}

		/// <summary>
		/// View showing related words. The provided data access should contain the needed data.
		/// object hvoEntry has a sequence (ktagDomains) of domains.
		/// each domain has a string ktagName and a sequence (ktagWords) of words.
		/// each word has a ktagName.
		/// </summary>
		private sealed class RelatedWordsView : SimpleRootSite
		{
			private int m_hvoRoot;
			private ISilDataAccess m_sda;
			private LcmCache m_cache;
			private int m_wsUser;
			private RelatedWordsVc m_vc;
			private bool m_fInSelChange;
			private ITsString m_headword;

			public event EventHandler SelChanged;

			public RelatedWordsView(LcmCache cache, int hvoRoot, ITsString headword, ISilDataAccess sda, int wsUser)
			{
				m_cache = cache;
				m_hvoRoot = hvoRoot;
				m_headword = headword;
				m_sda = sda;
				m_wsUser = wsUser;
				m_wsf = sda.WritingSystemFactory;
			}

			#region IDisposable override

			/// <inheritdoc />
			protected override void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
				if (IsDisposed)
				{
					// No need to run it more than once.
					return;
				}

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
				base.MakeRoot();
				m_vc = new RelatedWordsVc(m_wsUser, m_headword);
				RootBox.DataAccess = m_sda;
				RootBox.SetRootObject(m_hvoRoot, m_vc, RelatedWordsVc.kfragRoot, m_styleSheet);
				m_fRootboxMade = true;
			}
			internal void SetEntry(int hvoEntry)
			{
				var entry = m_cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetObject(hvoEntry);
				m_headword = entry.HeadWord;
				m_hvoRoot = hvoEntry;
				m_vc = new RelatedWordsVc(m_wsUser, m_headword);
				RootBox.SetRootObject(m_hvoRoot, m_vc, RelatedWordsVc.kfragRoot, m_styleSheet);
			}

			/// <summary>
			/// Called when the editing helper is created.
			/// </summary>
			protected override void OnEditingHelperCreated()
			{
				m_editingHelper.VwSelectionChanged += HandleSelectionChange;
			}

			/// <summary>
			/// Handle a selection change by growing it to a word (unless the new selection IS
			/// the one we're growing to a word).
			/// </summary>
			private void HandleSelectionChange(object sender, VwSelectionArgs args)
			{
				var vwselNew = args.Selection;
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
				SelChanged?.Invoke(this, new EventArgs());
			}

			internal bool GotRangeSelection
			{
				get
				{
					var sel = RootBox.Selection;
					return sel != null && sel.IsRange;
				}
			}
		}

		/// <summary />
		private sealed class RelatedWordsVc : FwBaseVc
		{
			public const int ktagDomains = 45671;
			public const int ktagName = 45672;
			public const int ktagWords = 45673;
			private const int ktagCf = 45674;
			public const int ktagLexRels = 45675;
			public const int kfragRoot = 333331;
			private const int kfragEntryList = 3333332;
			private const int kfragWords = 3333333;
			private const int kfragName = 3333334;
			private ITsString m_tssColon;
			private ITsString m_tssComma;
			private ITsString m_tssSdRelation;
			private ITsString m_tssLexRelation;

			/// <summary />
			public RelatedWordsVc(int wsUser, ITsString headword)
			{
				m_wsDefault = wsUser;
				m_tssColon = TsStringUtils.MakeString(": ", wsUser);
				m_tssComma = TsStringUtils.MakeString(", ", wsUser);
				m_tssSdRelation = TsStringUtils.MakeString(LcmUiStrings.ksWordsRelatedBySemanticDomain, wsUser);
				m_tssLexRelation = TsStringUtils.MakeString(LcmUiStrings.ksLexicallyRelatedWords, wsUser);
				var semanticDomainStrBuilder = m_tssSdRelation.GetBldr();
				var index = semanticDomainStrBuilder.Text.IndexOf("{0}");
				if (index > 0)
				{
					semanticDomainStrBuilder.ReplaceTsString(index, index + "{0}".Length, headword);
				}
				m_tssSdRelation = semanticDomainStrBuilder.GetString();
				var lexStrBuilder = m_tssLexRelation.GetBldr();
				index = lexStrBuilder.Text.IndexOf("{0}");
				if (index > 0)
				{
					lexStrBuilder.ReplaceTsString(index, index + "{0}".Length, headword);
				}
				m_tssLexRelation = lexStrBuilder.GetString();
			}

			/// <summary>
			/// This is the main interesting method of displaying objects and fragments of them.
			/// </summary>
			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				switch (frag)
				{
					case kfragRoot:
						var tssWord = vwenv.DataAccess.get_StringProp(hvo, ktagCf);
						var tsbSdRelation = m_tssSdRelation.GetBldr();
						var tsbLexRel = m_tssLexRelation.GetBldr();
						if (tssWord != null && tssWord.Length > 0)
						{
							var ich = tsbSdRelation.Text.IndexOf("{0}");
							if (ich >= 0)
							{
								tsbSdRelation.ReplaceTsString(ich, ich + 3, tssWord);
							}
							ich = tsbLexRel.Text.IndexOf("{0}");
							if (ich >= 0)
							{
								tsbLexRel.ReplaceTsString(ich, ich + 3, tssWord);
							}
						}
						var cDomains = vwenv.DataAccess.get_VecSize(hvo, ktagDomains);
						var cLexRels = vwenv.DataAccess.get_VecSize(hvo, ktagLexRels);
						Debug.Assert(cDomains > 0 || cLexRels > 0);
						if (cDomains > 0)
						{
							vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
							vwenv.set_IntProperty((int)FwTextPropType.ktptMarginBottom, (int)FwTextPropVar.ktpvMilliPoint, 6000);
							vwenv.OpenParagraph();
							vwenv.AddString(tsbSdRelation.GetString());
							vwenv.CloseParagraph();
							vwenv.AddLazyVecItems(ktagDomains, this, kfragEntryList);
						}
						if (cLexRels > 0)
						{
							vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
							vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTop, (int)FwTextPropVar.ktpvMilliPoint, 6000);
							vwenv.set_IntProperty((int)FwTextPropType.ktptMarginBottom, (int)FwTextPropVar.ktpvMilliPoint, 6000);
							vwenv.OpenParagraph();
							vwenv.AddString(tsbLexRel.GetString());
							vwenv.CloseParagraph();
							vwenv.AddLazyVecItems(ktagLexRels, this, kfragEntryList);
						}
						break;
					case kfragEntryList:
						vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
						vwenv.OpenParagraph();
						vwenv.set_IntProperty((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
						vwenv.AddStringProp(ktagName, this);
						vwenv.AddString(m_tssColon);
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

			/// <summary>
			/// Handles displaying the vector of words with commas except after the last
			/// </summary>
			public override void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
			{
				Debug.Assert(frag == kfragWords);
				var sda = vwenv.DataAccess;
				var cwords = sda.get_VecSize(hvo, ktagWords);
				for (var i = 0; i < cwords; i++)
				{
					vwenv.AddObj(sda.get_VecItem(hvo, ktagWords, i), this, kfragName);
					if (i != cwords - 1)
					{
						vwenv.AddString(m_tssComma);
					}
				}
			}

			/// <summary>
			/// Estimate the height in points of one domain.
			/// </summary>
			public override int EstimateHeight(int hvo, int frag, int dxAvailWidth)
			{
				return 20; // a domain typically isn't very high.
			}

			/// <summary>
			/// pre-load any required data about a particular domain.
			/// </summary>
			public override void LoadDataFor(IVwEnv vwenv, int[] rghvo, int chvo, int hvoParent, int tag, int frag, int ihvoMin)
			{
				// Nothing to do, all data already loaded.
			}
		}
	}
}