// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: InterlinTaggingView.cs
// Responsibility: MartinG
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Xml;
using SIL.Utils;
using SIL.FieldWorks.Common.Utils;
using System.Collections.Generic;

namespace SIL.FieldWorks.IText
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class to handle the tagging tab for marking up interlinear text with syntactic
	/// (or types of) tags.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class InterlinTaggingChild : InterlinDocChild
	{
		ContextMenuStrip m_taggingContextMenu;
		protected int m_textTagAnnDefn; // hvo of TextTag AnnotationDefn
		protected int m_twficAnnDefn; // hvo of Twfic AnnotationDefn
		int m_hvoCurSegment; // hvo of segment currently containing the selection

		// Helps determine if a rt-click is opening or closing the context menu.
		long m_ticksWhenContextMenuClosed = 0;

		// SelectionChanged updates this list
		internal List<int> m_selectedWfics;

		const int kflidAbbreviation = (int)CmPossibility.CmPossibilityTags.kflidAbbreviation;
		const int kflidName = (int)CmPossibility.CmPossibilityTags.kflidName;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:InterlinTaggingChild"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public InterlinTaggingChild()
		{
			this.BackColor = Color.FromKnownColor(KnownColor.Window);
			m_hvoCurSegment = 0;
		}

		/// <summary>
		/// Returns a list of wordforms selected in the Tagging (Text Markup) tab.
		/// The underlying member variable is updated by the SelectionChanged() override.
		/// </summary>
		public List<int> SelectedWfics
		{
			get { return m_selectedWfics; }
		}

		protected override void MakeVc()
		{
			m_vc = new InterlinTaggingVc(m_fdoCache);

			// Needed somewhere to initialize these where the cache is already created. Here looks good.
			// But test subclass doesn't DO MakeVc(), so we make them protected and load this in the
			// test subclass ctor
			m_textTagAnnDefn = CmAnnotationDefn.TextMarkupTag(Cache).Hvo;
			m_twficAnnDefn = CmAnnotationDefn.Twfic(Cache).Hvo;
		}

		#region SelectionMethods

		/// <summary>
		/// Suppress the special behavior that produces a Sandbox when a click happens.
		/// </summary>
		/// <param name="vwselNew"></param>
		/// <param name="fBundleOnly"></param>
		/// <param name="fConfirm"></param>
		protected override bool HandleClickSelection(IVwSelection vwselNew, bool fBundleOnly, bool fConfirm)
		{
			return false;
		}

		bool m_fInSelChanged;
		/// <summary>
		/// Any selection that involves a Wfic should be expanded to complete Wfic objects.
		/// This method also updates a list of selected Wfic annotations.
		/// </summary>
		/// <param name="prootb"></param>
		/// <param name="vwselNew"></param>
		public override void SelectionChanged(IVwRootBox prootb, IVwSelection vwselNew)
		{
			if (m_fInSelChanged)
				return;
			m_fInSelChanged = true;
			try
			{
				m_selectedWfics = null;
				base.SelectionChanged(prootb, vwselNew);
				if (vwselNew == null)
					return;

				SelLevInfo[] wficLevels;
				SelLevInfo[] endLevels;
				if (TryGetWficLevelsAndEndLevels(vwselNew, out wficLevels, out endLevels))
				{
					m_hvoCurSegment = wficLevels[1].hvo;
					m_selectedWfics = GetSelectedWfics(wficLevels, endLevels[0].ihvo);
					RootBox.MakeTextSelInObj(0, wficLevels.Length, wficLevels, endLevels.Length, endLevels, false, false,
											 false, true, true);
				}
			}
			finally
			{
				m_fInSelChanged = false;
			}
		}

		private bool TryGetWficLevelsAndEndLevels(IVwSelection vwselNew, out SelLevInfo[] wficLevels, out SelLevInfo[] endLevels)
		{
			endLevels = null;
			wficLevels = GetWficLevelsFromSelection(vwselNew);
			if (wficLevels == null)
				return false;

			SelLevInfo[] rgvsliEnd = GetOneEndPointOfSelection(vwselNew, true);

			// wficLevels[0] contains info about the sequence of wfics property. We want the corresponding
			// level in rgvsliEnd to have the same tag but not necessarily the same ihvo.
			// All the higher levels should be exactly the same. The loop checks this, and if successful
			// sets iend to the index of the property correponding to wficLevels[0].
			endLevels = null;
			int iend;
			if (AreHigherLevelsSameObject(wficLevels, rgvsliEnd, out iend))
			{
				endLevels = wficLevels.Clone() as SelLevInfo[];
				endLevels[0] = new SelLevInfo(); // clone is SHALLOW; don't modify element of wficLevels.
				endLevels[0].ihvo = rgvsliEnd[iend].ihvo;
				endLevels[0].tag = wficLevels[0].tag;
			}
			return endLevels != null;
		}

		/// <summary>
		/// Updates the list of selected wfics as well as the current StTxtPara.
		/// </summary>
		/// <param name="wficLevels">The wfic levels.</param>
		/// <param name="iend">The iend.</param>
		private List<int> GetSelectedWfics(SelLevInfo[] wficLevels, int iend)
		{
			int hvoSegment = wficLevels[1].hvo;
			int flidWfics = wficLevels[0].tag;

			int ianchor = wficLevels[0].ihvo;

			// These two lines are in case the user selects "backwards"
			int first = Math.Min(ianchor, iend);
			int last = Math.Max(ianchor, iend);

			List<int> selectedWfics = new List<int>();
			for (int i = first; i <= last; i++)
				selectedWfics.Add(Cache.GetVectorItem(hvoSegment, flidWfics, i));
			return selectedWfics;
		}

		private List<int> GetSelectedWfics(IVwSelection vwselNew)
		{
			if (vwselNew == null)
				return null;
			SelLevInfo[] wficLevels;
			SelLevInfo[] endLevels;
			if (TryGetWficLevelsAndEndLevels(vwselNew, out wficLevels, out endLevels))
				return GetSelectedWfics(wficLevels, endLevels[0].ihvo);
			return null;
		}

		/// <summary>
		/// Get SelLevInfo[] that starts with Wfic level. Returns null if itagAnalysis is less than zero.
		/// </summary>
		/// <param name="vwsel"></param>
		/// <returns></returns>
		private SelLevInfo[] GetWficLevelsFromSelection(IVwSelection vwsel)
		{
			return GetWficLevelsFromSelLevInfo(GetSelLevInfoFromSelection(vwsel));
		}

		private static SelLevInfo[] GetWficLevelsFromSelLevInfo(SelLevInfo[] rgvsli)
		{
			if (rgvsli == null)
				return null;
			int itagWfic = GetWfiAnalysisIndexInSelLevInfoArray(rgvsli) + 1;
			if (itagWfic <= 0)
				return null;
			SelLevInfo[] result = new SelLevInfo[rgvsli.Length - itagWfic];
			Array.Copy(rgvsli, itagWfic, result, 0, rgvsli.Length - itagWfic);
			return result;
		}

		/// <summary>
		/// Gets the sel lev info from selection.
		/// </summary>
		/// <param name="vwsel">The vwsel.</param>
		/// <returns></returns>
		private SelLevInfo[] GetSelLevInfoFromSelection(IVwSelection vwsel)
		{
			SelectionHelper helper = SelectionHelper.Create(vwsel, this);
			if (helper == null)
				return null;
			return helper.LevelInfo;
		}

		private static bool AreHigherLevelsSameObject(SelLevInfo[] wficLevels, SelLevInfo[] rgvsliEnd, out int iend)
		{
			iend = rgvsliEnd.Length - 1;
			for (int ianchor = wficLevels.Length - 1; ianchor > 0 && iend >= 0; ianchor--, iend--)
			{
				if (rgvsliEnd[iend].tag != wficLevels[ianchor].tag)
					return false;
				if (rgvsliEnd[iend].ihvo != wficLevels[ianchor].ihvo)
					return false;
			}
			if (iend < 0)
				return false; // not enough levels in the end selection, very unlikely.
			if (wficLevels[0].tag != rgvsliEnd[iend].tag)
				return false;
			return true;
		}

		/// <summary>
		/// COPIED from INTERLINPRINTVIEW temporarily to (temporarily) preserve Print View functionality.
		/// Return annotation Hvo that contains the selection.
		/// </summary>
		/// <returns></returns>
		public int AnnotationContainingSelection()
		{
			Debug.Assert(m_rootb != null);
			if (m_rootb == null)
				return 0;
			IVwSelection sel = m_rootb.Selection;
			if (sel == null)
				return 0;

			// See if our selection contains a base annotation.
			int cvsli = sel.CLevels(false);
			cvsli--; // CLevels includes the string property itself, but AllTextSelInfo doesn't need it.
			// Out variables for AllTextSelInfo.
			int ihvoRoot, tagTextProp, cpropPrevious, ichAnchor, ichEnd, ws, ihvoEnd;
			bool fAssocPrev;
			ITsTextProps ttpBogus;
			// Main array of information retrived from sel that made combo.
			SelLevInfo[] rgvsli = SelLevInfo.AllTextSelInfo(sel, cvsli,
				out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
				out ws, out fAssocPrev, out ihvoEnd, out ttpBogus);

			return FindBaseAnnInSelLevInfo(rgvsli); // returns 0 if unsuccessful
		}

		/// <summary>
		/// COPIED from INTERLINPRINTVIEW temporarily to (temporarily) preserve Print View functionality.
		/// </summary>
		/// <param name="rgvsli"></param>
		/// <returns></returns>
		private int FindBaseAnnInSelLevInfo(SelLevInfo[] rgvsli)
		{
			for (int i = 0; i < rgvsli.Length; i++)
			{
				int hvoAnn = rgvsli[i].hvo;
				if (hvoAnn > 0 && HvoIsRealBaseAnnotation(hvoAnn))
					return hvoAnn;
			}
			return 0;
		}

		/// <summary>
		/// COPIED from INTERLINPRINTVIEW temporarily to prevent Sandbox stuff.
		/// Select the word indicated by the text-wordform-in-context (twfic) annotation.
		/// This ignores the Sandbox! This is 'public' because it overrides a public method.
		/// </summary>
		/// <param name="hvoAnn"></param>
		public override void SelectAnnotation(int hvoAnn)
		{
			ISilDataAccess sda = Cache.MainCacheAccessor;
			// We should assert that ann is Twfic
			int annoType = sda.get_ObjectProp(hvoAnn, (int)CmAnnotation.CmAnnotationTags.kflidAnnotationType);
			Debug.Assert(annoType == m_twficAnnDefn, "Given annotation type should be twfic("
				+ m_twficAnnDefn + ") but was " + annoType + ".");

			// The following will select the Twfic, ... I hope!
			// Scroll to selection into view
			IVwSelection sel = SelectWficInIText(hvoAnn);
			if (sel == null)
				return;
			if (!this.Focused)
				this.Focus();
			this.ScrollSelectionIntoView(sel, VwScrollSelOpts.kssoTop);
			Update();
		}

		/// <summary>
		/// COPIED from INTERLINPRINTVIEW temporarily to prevent Sandbox stuff.
		/// Selecting an annotation has no side effects in this view. Among other things
		/// this suppresses the initial positioning and display of the sandbox.
		/// </summary>
		/// <param name="hvoAnnotation"></param>
		/// <param name="hvoAnalysis"></param>
		/// <param name="fConfirm"></param>
		/// <param name="fMakeDefaultSelection"></param>
		public override void TriggerAnnotationSelected(int hvoAnnotation, int hvoAnalysis,
			bool fConfirm, bool fMakeDefaultSelection)
		{
		}

		#endregion // SelectionMethods

		#region ContextMenuMethods

		protected override void OnMouseDown(MouseEventArgs e)
		{
			// (LT-9415) if the user has right-clicked in a selected wfic, bring
			// up context menu for those selected wfics.
			// otherwise, bring up the context menu for any newly selected wfic.
			if (m_selectedWfics != null && m_selectedWfics.Count > 0
				&& e.Button == MouseButtons.Right)
			{
				IVwSelection selTest = null;
				try
				{
					Point pt;
					Rectangle rcSrcRoot;
					Rectangle rcDstRoot;
					using (new HoldGraphics(this))
					{
						pt = PixelToView(new Point(e.X, e.Y));
						GetCoordRects(out rcSrcRoot, out rcDstRoot);
					}
					// Make an invisible selection to see if we are in editable text.
					selTest = m_rootb.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
					List<int> newSelectedWfics = GetSelectedWfics(selTest);
					// if we don't overlap with an existing wfic selection then
					// make a new wfic selection. (Otherwise, just make the context
					// menu based on the current selected wfics).
					if (newSelectedWfics == null ||
						newSelectedWfics.Count == 0 ||
						!m_selectedWfics.Contains(newSelectedWfics[0]))
					{
						// make a new (wfic) selection (via our SelectionChanged override)
						// before making the context menu.
						selTest.Install();
					}
				}
				catch
				{

				}

			}
			else
			{
				base.OnMouseDown(e);
			}
			if (e.Button == MouseButtons.Right)
			{
				// Make a context menu and show it, if I'm not just closing one!
				// This time test seems to be the only way to find out whether this click closed the last one.
				if (DateTime.Now.Ticks - m_ticksWhenContextMenuClosed > 50000) // 5ms!
				{
					m_taggingContextMenu = MakeContextMenu();
					m_taggingContextMenu.Closed += new ToolStripDropDownClosedEventHandler(m_taggingContextMenu_Closed);
					m_taggingContextMenu.Show(this, e.X, e.Y);
				}
			}

		}

		void m_taggingContextMenu_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			m_ticksWhenContextMenuClosed = DateTime.Now.Ticks;
		}

		internal ContextMenuStrip MakeContextMenu()
		{
			ContextMenuStrip menu = new ContextMenuStrip();

			// A little indirection for when we make more menu options later.
			// If we have not selected any Wfics, don't put tags in the context menu.
			if (SelectedWfics != null && SelectedWfics.Count > 0)
			{
				ICmPossibilityList tagList = GetTaggingLists();
				MakeContextMenu_AvailableTags(menu, tagList);
			}
			return menu;
		}

		protected void MakeContextMenu_AvailableTags(ContextMenuStrip menu, ICmPossibilityList list)
		{
			foreach (ICmPossibility tagList in list.PossibilitiesOS)
			{
				if (tagList.SubPossibilitiesOS.Count > 0)
					AddListToMenu(menu, tagList);
			}
		}

		private void AddListToMenu(ToolStrip menu, ICmPossibility tagList)
		{
			Debug.Assert(tagList.SubPossibilitiesOS.Count > 0, "There should be sub-possibilities here!");

			// Add the main entry first
			ToolStripMenuItem tagSubmenu = new ToolStripMenuItem(tagList.Name.BestAnalysisAlternative.Text);
			menu.Items.Add(tagSubmenu);

			foreach (ICmPossibility poss in tagList.SubPossibilitiesOS)
			{
				// Add 'tag' BestDefaultAnalWS Name to menu
				TagPossibilityMenuItem tagItem = new TagPossibilityMenuItem(poss.Hvo);
				tagItem.Click += new EventHandler(Tag_Item_Click);
				tagItem.Text = poss.Name.BestAnalysisAlternative.Text;
				tagItem.Checked = DoSelectedXficsHaveTag(poss.Hvo);
				tagSubmenu.DropDownItems.Add(tagItem);
			}
		}

		private bool DoSelectedXficsHaveTag(int hvoPoss)
		{
			ICmIndirectAnnotation tagAnn = FindFirstTagOfSpecifiedTypeOnSelection(hvoPoss);
			if (tagAnn == null)
				return false;
			return true;
		}

		/// <summary>
		/// Finds the first tag of specified possibility pointing to the selected wordforms.
		/// If successful, returns the tagging annotation. If unsuccessful, returns null.
		/// </summary>
		/// <param name="hvoPoss">The hvo of the tagging possibility item.</param>
		/// <returns></returns>
		private ICmIndirectAnnotation FindFirstTagOfSpecifiedTypeOnSelection(int hvoPoss)
		{
			List<int> hvoTagList = FindAllTagsReferencingXficList(SelectedWfics);
			foreach (int hvoTagAnnot in hvoTagList)
			{
				ICmIndirectAnnotation tagAnn = CmObject.CreateFromDBObject(Cache, hvoTagAnnot) as ICmIndirectAnnotation;
				Debug.Assert(tagAnn != null, "Tagging annotation not reconstituted from hvo ="+tagAnn);
				if (tagAnn.InstanceOfRAHvo == hvoPoss)
					return tagAnn;
			}
			return null;
		}

		internal ICmPossibilityList GetTaggingLists()
		{
			Debug.Assert(Cache != null, "No Cache available!");
			Debug.Assert(Cache.LangProject != null, "No LangProject available!");
			ICmPossibilityList result = Cache.LangProject.TextMarkupTagsOA;

			if (result == null) // Just trying to be careful.
			{
				// Create the containing object and lists.
				result = CreateDefaultListFromXML();
				Cache.LangProject.TextMarkupTagsOA = result;
			}
			return result;
		}

		#endregion // ContextMenuMethods

		#region DefaultListFromXMLMethods

		internal CmPossibility CreateTagFromXml(CmPossibility parent, XmlNode spec)
		{
			CmPossibility result = (CmPossibility)parent.SubPossibilitiesOS.Append(new CmPossibility());
			SetNameAndChildTagsFromXml(result, spec);
			return result;
		}

		private void SetNameAndChildTagsFromXml(CmPossibility parent, XmlNode spec)
		{
			parent.Name.AnalysisDefaultWritingSystem = XmlUtils.GetManditoryAttributeValue(spec, "name");
			if (spec.Name == "subpossibility")
				parent.Abbreviation.AnalysisDefaultWritingSystem = XmlUtils.GetManditoryAttributeValue(spec, "abbreviation");
			foreach (XmlNode child in spec.ChildNodes)
				CreateTagFromXml(parent, child);
		}

		#region ForTestOnlyConstants

		public const string kFTO_RRG_Semantics	= "RRG Semantics";
		public const string kFTO_Actor				= "ACTOR";
		public const string kFTO_Non_Macrorole		= "NON-MACROROLE";
		public const string kFTO_Undergoer			= "UNDERGOER";
		public const string kFTO_Syntax			= "Syntax";
		public const string kFTO_Noun_Phrase		= "Noun Phrase";
		public const string kFTO_Verb_Phrase		= "Verb Phrase";
		public const string kFTO_Adjective_Phrase	= "Adjective Phrase";

		#endregion

		public ICmPossibilityList CreateDefaultListFromXML()
		{
			// Set up default XML to load in case permanent list is empty.
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(
			  "<taglist name=\"Text Markup Tags\">"
			  + "<possibility name=\"" + kFTO_RRG_Semantics + "\">"
				+ "<subpossibility name=\"" + kFTO_Actor + "\" abbreviation=\"ACT\"/>"
				+ "<subpossibility name=\"" + kFTO_Non_Macrorole + "\" abbreviation=\"NON-MR\"/>"
				+ "<subpossibility name=\"" + kFTO_Undergoer + "\" abbreviation=\"UND\"/>"
			  + "</possibility>"
			  + "<possibility name=\"" + kFTO_Syntax + "\">"
				+ "<subpossibility name=\"" + kFTO_Noun_Phrase + "\" abbreviation=\"NP\"/>"
				+ "<subpossibility name=\"" + kFTO_Verb_Phrase + "\" abbreviation=\"VP\"/>"
				+ "<subpossibility name=\"" + kFTO_Adjective_Phrase + "\" abbreviation=\"AdjP\"/>"
			  + "</possibility>"
			+ "</taglist>");

			XmlNode spec = doc.DocumentElement;
			int hvoPoss = Cache.CreateObject(CmPossibilityList.kClassId);
			ICmPossibilityList possList = CmPossibilityList.CreateFromDBObject(Cache, hvoPoss);

			foreach (XmlNode tagListXml in spec.ChildNodes)
			{
				CmPossibility tagList = new CmPossibility();
				possList.PossibilitiesOS.Append(tagList);
				SetNameAndChildTagsFromXml(tagList, tagListXml); // One Possibility (Syntax, Semantics, etc.)
			}
			return possList;
		}

		#endregion // DefaultListFromXMLMethods

		void Tag_Item_Click(object sender, EventArgs e)
		{
			TagPossibilityMenuItem item = sender as TagPossibilityMenuItem;
			if (item == null)
				return;
			// save current selection info. (e.g. EnsureSelectedWficsAndSegAreAllReal() can destroy it).
			SelectionHelper sh = SelectionHelper.Create(this);
			if (item.Checked)
			{
				using (new UndoRedoTagTextTaskHelper(this, ITextStrings.ksUndoDeleteTextTag, ITextStrings.ksRedoDeleteTextTag, sh))
				   RemoveTextTagAnnotation(item.HvoPoss);
			}
			else
			{
				// before we try to do anything to the real wfics, make sure they're all real
				// but this needs to be outside of any Undoable task because it's tricky to Undo.
				using (new SuppressSubTasks(Cache))
					EnsureSelectedWficsAndSegAreAllReal();

				using (new UndoRedoTagTextTaskHelper(this, ITextStrings.ksUndoAddTextTag, ITextStrings.ksRedoAddTextTag, sh))
					MakeTextTagAnnotation(item.HvoPoss);
			}
			// restore current selection info, since PropChanged can destroy the selection.
			if (sh != null)
				sh.RestoreSelectionAndScrollPos();
		}

		/// <summary>
		/// this will automatically add undo and redo actions for updating tags and the selection.
		/// </summary>
		class UndoRedoTagTextTaskHelper : UndoRedoTaskHelper
		{
			private InterlinTaggingChild m_itc;
			private SelectionHelper m_originalSelectionInfo;

			internal UndoRedoTagTextTaskHelper(InterlinTaggingChild itc, string undo, string redo,
				SelectionHelper originalSelectionInfo)
				: base(itc.Cache, undo, redo)
			{
				m_itc = itc;
				m_originalSelectionInfo = originalSelectionInfo;
				// add the undo action first, since this will be the last action undone.
				AddAction(new UndoRedoTextTag(itc, false, m_originalSelectionInfo));
			}

			protected override void Dispose(bool disposing)
			{
				// Add the redo action last, since it will be the last action redone.
				if (disposing)
					AddAction(new UndoRedoTextTag(m_itc, true, m_originalSelectionInfo));
				base.Dispose(disposing);
			}
		}

		/// <summary>
		/// Removes the text tag annotation matching the hvoPoss on the selected wordforms.
		/// If there are multiples, this will remove the first one it finds.
		/// </summary>
		/// <param name="hvoPoss">The hvo of the tag list possibility item.</param>
		private void RemoveTextTagAnnotation(int hvoPoss)
		{
			if (SelectedWfics == null || SelectedWfics.Count == 0)
				return;

			int hvoTag = FindFirstTagOfSpecifiedTypeOnSelection(hvoPoss).Hvo;
			CacheNullTagString(hvoTag);
			Set<int> hvoToDelete = new Set<int>();
			hvoToDelete.Add(hvoTag);
			CmObject.DeleteObjects(hvoToDelete, Cache, false);
		}

		/// <summary>
		/// Creates a new CmIndirectAnnotation for the tag possibility (parameter)
		/// pointing to the Wfics in m_selectedWfics. If there are no Wfics selected,
		/// the method returns null.
		/// </summary>
		/// <param name="hvoTagPoss"></param>
		/// <returns>The new annotation</returns>
		protected ICmIndirectAnnotation MakeTextTagAnnotation(int hvoTagPoss)
		{
			Debug.Assert(hvoTagPoss != 0, "Expecting a possibility Hvo.");
			if (SelectedWfics == null || SelectedWfics.Count == 0)
				return null;

			// We'll try doing without passing the list of selected annotations in here
			// Just get it from m_selectedWfics.
			ICmIndirectAnnotation tagAnn;
			// This is called by a routine with an UndoRedo Helper
			// Delete ones that point at selected ones already
			// (before we add the new one or it gets deleted too!)
			Set<int> hvosToDelete = new Set<int>();
			Set<int> wficCollection = CheckForOverlappingTags(SelectedWfics, ref hvosToDelete);

			// Create and add the new one
			tagAnn = Cache.LangProject.AnnotationsOC.Add(new CmIndirectAnnotation()) as ICmIndirectAnnotation;
			tagAnn.InstanceOfRAHvo = hvoTagPoss;
			tagAnn.AnnotationTypeRAHvo = m_textTagAnnDefn;
			foreach (int hvoWfic in SelectedWfics)
			{
				tagAnn.AppliesToRS.Append(hvoWfic);
			}
			DeleteTextTagAnnotations(hvosToDelete);
			CacheTagString(tagAnn);
			UpdateAffectedBundles(wficCollection);
			return tagAnn;
		}

		protected override void UpdateDisplayAfterUndo()
		{
			// hopefully nothing to do after UndoRedoTextTag finishes.
		}

		/// <summary>
		/// before affecting the real data, we need to make sure
		/// the selected wfics and their segment exist in the database (LT-9405).
		/// </summary>
		private void EnsureSelectedWficsAndSegAreAllReal()
		{
			// first convert the current segment to real.
			if (Cache.IsDummyObject(m_hvoCurSegment))
				m_hvoCurSegment = CmBaseAnnotation.ConvertBaseAnnotationToReal(Cache, m_hvoCurSegment).Hvo;
			// next the wfics
			List<int> originalSelectedWfics = new List<int>(SelectedWfics);
			for (int i = 0; i < originalSelectedWfics.Count; i++)
			{
				int hvoWfic = SelectedWfics[i];
				if (Cache.IsDummyObject(hvoWfic))
				{
					int hvoRealWfic = CmBaseAnnotation.ConvertBaseAnnotationToReal(Cache, hvoWfic).Hvo;
					SelectedWfics.RemoveAt(i);
					SelectedWfics.Insert(i, hvoRealWfic);
				}
			}
		}

		/// <summary>
		/// NOTE: This can destroy any current selection, so remember to save and restore the selection as needed
		/// </summary>
		/// <param name="wficCollection"></param>
		protected virtual void UpdateAffectedBundles(Set<int> wficCollection)
		{
			foreach (int hvoWfic in wficCollection)
			{
				StTxtPara.TwficInfo ti = new StTxtPara.TwficInfo(Cache, hvoWfic);
				if (ti.SegmentIndex > -1)
					Cache.PropChanged(ti.SegmentHvo, m_vc.ktagSegmentForms, ti.SegmentIndex, 1, 1);
			}
		}

		/// <summary>
		/// Checks for overlapping tags.
		/// </summary>
		/// <param name="hvoWfics">The hvo wfics.</param>
		/// <param name="hvosToDelete">The hvos to delete.</param>
		/// <returns>A collection of wfic hvos that are affected by this change.</returns>
		private Set<int> CheckForOverlappingTags(List<int> hvoWfics, ref Set<int> hvosToDelete)
		{
			// Look through all existing tags pointing to this wfic.
			List<int> tags = FindAllTagsReferencingXficList(hvoWfics);
			// If we find one that AppliesTo something in hvoWfics then do:
			if (tags.Count > 0)
				hvosToDelete.AddRange(tags);
			return GetAllAffectedWficsFromTags(tags);
		}

		private List<int> FindAllTagsReferencingXficList(List<int> hvoXfics)
		{
			if (hvoXfics == null || hvoXfics.Count == 0)
				return new List<int>();

			// We're unlikely to have more than a few hundred words in a sentence.
			return GetTaggingReferencingTheseAnnotations(Cache, hvoXfics, m_textTagAnnDefn);
		}

		private Set<int> GetAllAffectedWficsFromTags(List<int> ttagAnnList)
		{
			// This sets up UpdateAffectedBundles() to do all the PropChangeds at once

			// First add the selected wfics to our collection
			Set<int> results = new Set<int>();
			results.AddRange(SelectedWfics);

			// Then add any wfics affected (deleted) by this tag addition
			foreach (int hvoTTag in ttagAnnList)
			{
				ICmIndirectAnnotation ann = CmObject.CreateFromDBObject(Cache, hvoTTag) as ICmIndirectAnnotation;
				foreach (ICmAnnotation ttagTarget in ann.AppliesToRS)
				{
					if (ttagTarget is ICmBaseAnnotation)
						results.Add(ttagTarget.Hvo);
				}
			}
			return results;
		}

		/// <summary>
		/// Given a set of wfics, return the set of xfics that may be affected
		/// by tagging or undoing the applied tag (which may overlap other xfics).
		/// </summary>
		/// <param name="hvoWfics"></param>
		/// <returns></returns>
		private Set<int> GetAllXficsPossiblyAffectedByTagging(List<int> hvoWfics)
		{
			// This is overkill, but there are too many cases to handle during undo/redo
			// to cover with just CheckForOverlappingTags().
			// For now get all the xfics for the segments owning the given wfics
			// so we can make sure the display will be properly updated.
			Set<int> segments = new Set<int>();
			foreach (int hvoWfic in hvoWfics)
			{
				// first collect a list of parent segments.
				StTxtPara.TwficInfo ti = new StTxtPara.TwficInfo(Cache, hvoWfic);
				if (ti.SegmentHvo == 0)
					continue;
				segments.Add(ti.SegmentHvo);
			}
			// now get all the xfics for those segments
			Set<int> allPossiblyAffectedXfics = new Set<int>();
			allPossiblyAffectedXfics.AddRange((m_vc as InterlinTaggingVc).CollectXficsFromSegments(segments.ToArray()));
			return allPossiblyAffectedXfics;
		}

		/// <summary>
		/// Protected virtual so the testing subclass doesn't have to know about Views.
		/// </summary>
		/// <param name="tagAnn"></param>
		protected virtual Set<int> CacheTagString(ICmIndirectAnnotation tagAnn)
		{
			// Cache the new tagging string in our dummy virtual property (TagAnnotListId)
			// and call PropChanged
			return (m_vc as InterlinTaggingVc).CacheTagString(tagAnn);
		}

		/// <summary>
		/// Protected virtual so the testing subclass doesn't have to know about Views.
		/// </summary>
		/// <param name="hvoTag"></param>
		protected virtual void CacheNullTagString(int hvoTag)
		{
			// Cache an empty tagging string in our dummy virtual property (TagAnnotListId)
			// for each xfic this tag AppliesTo and call PropChanged.
			ICmIndirectAnnotation textTag = CmObject.CreateFromDBObject(Cache, hvoTag) as ICmIndirectAnnotation;
			(m_vc as InterlinTaggingVc).CacheNullTagString(textTag.AppliesToRS.HvoArray, true);
		}

		/// <summary>
		/// Deletes the text tag annotations that a new addition overlaps (temporary).
		/// </summary>
		/// <param name="hvoTagsToDelete">The hvo tags to delete.</param>
		protected void DeleteTextTagAnnotations(Set<int> hvoTagsToDelete)
		{
			if (hvoTagsToDelete.Count == 0)
				return;
			// This is called by another method with an UndoRedoTaskHelper
			using (new UndoRedoTaskHelper(Cache, ITextStrings.ksUndoDeleteTextTag,
				ITextStrings.ksRedoDeleteTextTag))
			{
				foreach (int hvoTag in hvoTagsToDelete)
					CacheNullTagString(hvoTag);
				CmObject.DeleteObjects(hvoTagsToDelete, Cache, false);
			}
		}

		/// <summary>
		/// Gets a list of TextTags that reference the annotations whose ids are in the input paramter.
		/// </summary>
		/// <param name="xfics"></param>
		/// <returns>A list of annotation hvos.</returns>
		internal static List<int> GetTaggingReferencingTheseAnnotations(FdoCache cache, List<int> xfics, int textTagAnnDefn)
		{
			int igroup = 0;
			string xficIds = DbOps.MakePartialIdList(ref igroup, xfics.ToArray());
			if (string.IsNullOrEmpty(xficIds))
				return new List<int>(0); // no targets, makes invalid SQL.
			string sql = @"select distinct tag_at.Src from CmIndirectAnnotation_AppliesTo tag_at
				join CmAnnotation ca on ca.id = tag_at.Src and ca.AnnotationType = " + textTagAnnDefn + @"
				where tag_at.dst in (" + xficIds + @")";
			List<int> results = DbOps.ReadIntsFromCommand(cache, sql, null);
			return results;
		}

		/// <summary>
		/// This class allows smarter UndoRedo for making a text tag, so that the display and selection
		/// gets updated appropriately.
		/// </summary>
		internal class UndoRedoTextTag : UndoActionBase
		{
			FdoCache m_cache = null;
			InterlinTaggingChild m_interlinDoc = null;
			SelectionHelper m_originalSelection = null;
			Set<int> m_affectedXfics;
			bool m_fForRedo = false;

			/// <summary>
			///
			/// </summary>
			/// <param name="interlinDoc"></param>
			/// <param name="fForRedo">true for adding a redo action, false for adding undo action</param>
			internal UndoRedoTextTag(InterlinTaggingChild interlinDoc, bool fForRedo, SelectionHelper originalSelectionInfo)
			{
				m_fForRedo = fForRedo;
				m_interlinDoc = interlinDoc;
				m_cache = interlinDoc.Cache;
				m_originalSelection = originalSelectionInfo;
				m_affectedXfics = m_interlinDoc.GetAllXficsPossiblyAffectedByTagging(m_interlinDoc.SelectedWfics);
			}

			#region Overrides of UndoActionBase

			public override bool Redo(bool fRefreshPending)
			{
				if (m_fForRedo && !fRefreshPending)
					ResyncTagsForAffectedBundles();
				return true;
			}

			public override bool Undo(bool fRefreshPending)
			{
				if (!m_fForRedo && !fRefreshPending)
					ResyncTagsForAffectedBundles();
				return true;
			}

			private void ResyncTagsForAffectedBundles()
			{
				// only load on valid xfics.
				List<int> validXfics = new List<int>();
				foreach (int xfic in m_affectedXfics)
				{
					if (m_interlinDoc.Cache.IsValidObject(xfic))
						validXfics.Add(xfic);
				}
				Set<int> allAffected = new Set<int>(validXfics);
				List<int> newAllAffected = new List<int>(m_interlinDoc.GetAllXficsPossiblyAffectedByTagging(validXfics));
				// load tags for the union of both new and old, to make sure everything is covered.
				allAffected.AddRange(newAllAffected);
				List<int> xficsToLoadTagsFor = new List<int>(allAffected);
				(m_interlinDoc.m_vc as InterlinTaggingVc).LoadTextTagsForXfics(xficsToLoadTagsFor);
				m_interlinDoc.AllowLayout = true;
				(m_interlinDoc).UpdateAffectedBundles(new Set<int>(xficsToLoadTagsFor));
				if (m_originalSelection != null)
					m_originalSelection.RestoreSelectionAndScrollPos();
			}

			#endregion
		}

	} // end class InterlinTaggingChild

	/// <summary>
	/// Used for Text Tagging possibility menu items
	/// </summary>
	public class TagPossibilityMenuItem : ToolStripMenuItem
	{
		int m_hvoTagPoss;

		/// <summary>
		/// Initializes a new instance of the <see cref="TagPossibilityMenuItem"/> class
		/// used for context (right-click) menus.
		/// </summary>
		/// <param name="hvoPoss">The hvo poss.</param>
		public TagPossibilityMenuItem(int hvoPoss)
		{
			m_hvoTagPoss = hvoPoss;
		}

		public int HvoPoss
		{
			get { return m_hvoTagPoss; }
		}
	}

	/// <summary>
	/// Modifications of InterlinVc for showing TextTag possibilities.
	/// </summary>
	public class InterlinTaggingVc : InterlinVc
	{
		private int m_textTagAnnotFlid;
		private int m_textTagAnnDefn;
		private int m_lenEndTag;
		private int m_lenStartTag;
		private bool m_fAnalRtl;

		/// <summary>
		/// Initializes a new instance of the <see cref="InterlinTaggingVc"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		public InterlinTaggingVc(FdoCache cache)
			: base(cache)
		{
			m_cache = cache;
			m_textTagAnnDefn = CmAnnotationDefn.TextMarkupTag(Cache).Hvo;
			m_lenEndTag = ITextStrings.ksEndTagSymbol.Length;
			m_lenStartTag = ITextStrings.ksStartTagSymbol.Length;
			SetAnalysisRightToLeft(m_cache.DefaultAnalWs);
		}

		private void SetAnalysisRightToLeft(int wsAnal)
		{
			IWritingSystem wsObj = m_cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(wsAnal);
			if (wsObj != null)
				m_fAnalRtl = wsObj.RightToLeft;
		}

		protected FdoCache Cache
		{
			get { return m_cache; }
		}

		public const string kTagAnnotListClass = "CmBaseAnnotation";
		public const string kTagAnnotListField = "TagItem";
		/// <summary>
		/// Gets the dummy virtual property flid for cached TextTag annotations.
		/// </summary>
		public virtual int TextTagAnnotFlid
		{
			get
			{
				if (m_textTagAnnotFlid == 0)
				{
					m_textTagAnnotFlid = DummyVirtualHandler.InstallDummyHandler(Cache.VwCacheDaAccessor,
						kTagAnnotListClass, kTagAnnotListField,
						(int)FieldType.kcptReferenceAtom).Tag;
				}
				return m_textTagAnnotFlid;
			}
		}

		/// <summary>
		/// Given an array of segments (for which we have just loaded the twfics), load any associated text tagging data
		/// </summary>
		/// <param name="rghvoSegments"></param>
		private void LoadDataForTextTags(int[] rghvoSegments)
		{
			List<int> xfics = CollectXficsFromSegments(rghvoSegments);
			LoadTextTagsForXfics(xfics);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="xfics"></param>
		internal void LoadTextTagsForXfics(List<int> xfics)
		{
			List<int> textTagList = InterlinTaggingChild.GetTaggingReferencingTheseAnnotations(Cache, xfics, m_textTagAnnDefn);
			Set<int> xficsTagged = new Set<int>();
			foreach (int hvoTag in textTagList)
				xficsTagged.AddRange(CacheTagString(hvoTag)); // Preload doesn't need PropChanged() to fire. This version doesn't.
			// now go through the list of xfics that didn't have tags cached, and make sure they have empty strings cached
			Set<int> xficsWithoutTags = xficsTagged.SymmetricDifference(xfics);
			CacheNullTagString(xficsWithoutTags.ToArray(), false);
		}

		/// <summary>
		/// Gets the string of xfic ids from segments.
		/// </summary>
		/// <param name="rghvoSegments">The rghvo segments.</param>
		/// <returns></returns>
		private string GetStringOfXficIdsFromSegments(int[] rghvoSegments)
		{
			List<int> xfics = CollectXficsFromSegments(rghvoSegments);

			// We're unlikely to have more than a few hundred words in a sentence.
			int igroup = 0;
			string xficIds = DbOps.MakePartialIdList(ref igroup, xfics.ToArray());
			return xficIds;
		}

		/// <summary>
		/// Collects the xfics from segments.
		/// </summary>
		/// <param name="rghvoSegments">The array of segment hvos.</param>
		/// <returns></returns>
		internal List<int> CollectXficsFromSegments(int[] rghvoSegments)
		{
			List<int> xfics = new List<int>();
			foreach (int hvoSeg in rghvoSegments)
			{
				int cwfic = Cache.GetVectorSize(hvoSeg, ktagSegmentForms);
				for (int i = 0; i < cwfic; i++)
					xfics.Add(Cache.GetVectorItem(hvoSeg, ktagSegmentForms, i));
			}
			return xfics;
		}

		/// <summary>
		/// Caches a possibility label for each Wfic that a tag appliesTo.
		/// </summary>
		/// <param name="tagAnn"></param>
		/// <returns>set of xfics for which a tag was cached.</returns>
		internal Set<int> CacheTagString(ICmIndirectAnnotation tagAnn)
		{
			Set<int> xficsTagged = new Set<int>();
			ICmPossibility tagPossibility = tagAnn.InstanceOfRA as ICmPossibility;
			ITsString label;
			if (tagPossibility == null)
				label = TsStrFactoryClass.Create().MakeString("", Cache.LangProject.DefaultAnalysisWritingSystem);
			else
				label = tagPossibility.Abbreviation.BestAnalysisAlternative;
			int chvoArray = tagAnn.AppliesToRS.Count;
			// use 'for' loop because we need to know when we're at the beginning
			// and end of the loop
			for(int i=0; i < chvoArray; i++)
			{
				int hvoXfic = tagAnn.AppliesToRS[i].Hvo;
				int clen = label.Length;
				ITsStrBldr strBldr = label.GetBldr();
				if (i == 0) // First wfic for this tag.
					clen = StartTagSetup(clen, strBldr);
				else
				{
					// Until someone has a better idea, only show the label on the first wfic.
					clen = 0;
					strBldr.Clear();
				}
				if (i == chvoArray - 1) // Last wfic for this tag.
				{
					clen = EndTagSetup(clen, strBldr);
				}
				Cache.VwCacheDaAccessor.CacheStringProp(hvoXfic, TextTagAnnotFlid, strBldr.GetString());
				xficsTagged.Add(hvoXfic);
			}
			return xficsTagged;
		}

		/// <summary>
		/// Gets the length of the previous tag string for this wfic.
		/// </summary>
		/// <param name="hvoWfic">The hvo wfic.</param>
		/// <returns></returns>
		private int GetPrevStringLength(int hvoWfic)
		{
			ITsString tempstr = Cache.GetTsStringProperty(hvoWfic, TextTagAnnotFlid);
			if (tempstr == null)
				return 0;
			string str = tempstr.Text;
			if (str == null)
				return 0;
			return str.Length;
		}

		/// <summary>
		/// Sets up the end of the tag with its (localizable) symbol.
		/// </summary>
		/// <param name="clen">The tag string length before.</param>
		/// <param name="builder">A TsString builder.</param>
		/// <returns>The tag string length after.</returns>
		private int EndTagSetup(int clen, ITsStrBldr builder)
		{
			// How this works depends on the directionality of both the vernacular and
			// analysis writing systems.  This does assume that nobody localizes [ and ]
			// to something like ] and [!  I'm not sure those strings should be localizable.
			// See LT-9551.
			if (RightToLeft)
			{
				if (m_fAnalRtl)
				{
					builder.Replace(builder.Length, builder.Length, ITextStrings.ksStartTagSymbol, null);
					clen += ITextStrings.ksStartTagSymbol.Length;
				}
				else
				{
					builder.Replace(0, 0, ITextStrings.ksStartTagSymbol, null);
					clen += ITextStrings.ksStartTagSymbol.Length;
				}
			}
			else
			{
				if (m_fAnalRtl)
				{
					builder.Replace(0, 0, ITextStrings.ksEndTagSymbol, null);
					clen += ITextStrings.ksEndTagSymbol.Length;
				}
				else
				{
					builder.Replace(builder.Length, builder.Length, ITextStrings.ksEndTagSymbol, null);
					clen += ITextStrings.ksEndTagSymbol.Length;
				}
			}
			return clen;
		}

		/// <summary>
		/// Sets up the beginning of the tag with its (localizable) symbol.
		/// </summary>
		/// <param name="clen">The tag string length before.</param>
		/// <param name="builder">A TsString builder.</param>
		/// <returns>The tag string length after.</returns>
		private int StartTagSetup(int clen, ITsStrBldr builder)
		{
			// How this works depends on the directionality of both the vernacular and
			// analysis writing systems.  This does assume that nobody localizes [ and ]
			// to something like ] and [!  I'm not sure those strings should be localizable.
			// See LT-9551.
			if (RightToLeft)
			{
				if (m_fAnalRtl)
				{
					builder.Replace(0, 0, ITextStrings.ksEndTagSymbol, null);
					clen += ITextStrings.ksEndTagSymbol.Length;
				}
				else
				{
					builder.Replace(builder.Length, builder.Length, ITextStrings.ksEndTagSymbol, null);
					clen += ITextStrings.ksEndTagSymbol.Length;
				}
			}
			else
			{
				if (m_fAnalRtl)
				{
					builder.Replace(builder.Length, builder.Length, ITextStrings.ksStartTagSymbol, null);
					clen += ITextStrings.ksStartTagSymbol.Length;
				}
				else
				{
					builder.Replace(0, 0, ITextStrings.ksStartTagSymbol, null);
					clen += ITextStrings.ksStartTagSymbol.Length;
				}
			}
			return clen;
		}

		/// <summary>
		/// Caches a possibility label for each Wfic that a tag appliesTo.
		/// This version starts from the hvo.
		/// </summary>
		/// <param name="hvoTag"></param>
		/// <returns>set of xfics for which we cached tags</returns>
		internal Set<int> CacheTagString(int hvoTag)
		{
			ICmIndirectAnnotation tagAnn = CmObject.CreateFromDBObject(Cache, hvoTag) as ICmIndirectAnnotation;
			return CacheTagString(tagAnn);
		}

		/// <summary>
		/// Caches an empty label for each Wfic that a tag appliesTo in preparation for deletion.
		/// And calls PropChanged.
		/// </summary>
		/// <param name="xficsToApply"></param>
		internal void CacheNullTagString(int[] xficsToApply, bool fPropChanged)
		{
			if (xficsToApply == null)
				return;
			ITsString tss = StringUtils.MakeTss("", Cache.LangProject.DefaultAnalysisWritingSystem);
			foreach (int hvoXfic in xficsToApply)
			{
				int clen = 0;
				if (fPropChanged)
					clen = GetPrevStringLength(hvoXfic);
				Cache.VwCacheDaAccessor.CacheStringProp(hvoXfic, TextTagAnnotFlid, tss);
				if (fPropChanged)
					Cache.PropChanged(hvoXfic, TextTagAnnotFlid, 0, 0, clen);
			}
		}

		internal override void LoadDataForSegments(int[] rghvo, int hvoPara)
		{
			base.LoadDataForSegments(rghvo, hvoPara);
			LoadDataForTextTags(rghvo);
		}

		/// <summary>
		/// The hvo is for a Wfic (CmBaseAnnotation).
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvoWfic"></param>
		internal override void AddExtraTwficRows(IVwEnv vwenv, int hvoWfic)
		{
			SetTrailingAlignmentIfNeeded(vwenv, hvoWfic);
			vwenv.AddStringProp(TextTagAnnotFlid, this);
		}

		private void SetTrailingAlignmentIfNeeded(IVwEnv vwenv, int hvoWfic)
		{
			ITsString tss = Cache.GetTsStringProperty(hvoWfic, TextTagAnnotFlid);
			Debug.Assert(tss != null, "Should get something for a TsString here!");
			string tssText = tss.Text;
			if (tssText == null || tssText.Length == 0)
				return;
			if (TagEndsWithThisWfic(tssText) && !TagStartsWithThisWfic(tssText))
			{
				// set trailing alignment
				vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum,
					(int)FwTextAlign.ktalTrailing);
			}
		}

		private bool TagStartsWithThisWfic(string ttagLabel)
		{
			if (ttagLabel.Length >= m_lenStartTag)
			{
				return ttagLabel.Substring(0, m_lenStartTag) == ITextStrings.ksStartTagSymbol;
			}
			return false;
		}

		private bool TagEndsWithThisWfic(string ttagLabel)
		{
			int clen = ttagLabel.Length;
			if (clen >= m_lenEndTag)
			{
				return ttagLabel.Substring(clen-m_lenEndTag, m_lenEndTag)
					== ITextStrings.ksEndTagSymbol;
			}
			return false;
		}

	} // end class InterlinTaggingVc
}
