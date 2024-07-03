// Copyright (c) 2009-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.DomainServices;
using SIL.FieldWorks.FwCoreDlgControls;
using SIL.LCModel.Utils;
using XCore;

namespace SIL.FieldWorks.IText
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class to handle the tagging tab for marking up interlinear text with syntactic
	/// (or types of) tags.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class InterlinTaggingChild : InterlinDocRootSiteBase
	{
		ContextMenuStrip m_taggingContextMenu;

		// Helps determine if a rt-click is opening or closing the context menu.
		long m_ticksWhenContextMenuClosed = 0;

		// SelectionChanged updates this list
		protected List<AnalysisOccurrence> m_selectedWordforms;

		// TextTag Factory
		protected ITextTagFactory m_tagFact;

		// Segment Repository
		protected ISegmentRepository m_segRepo;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:InterlinTaggingChild"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public InterlinTaggingChild()
		{
			InitializeComponent();
			BackColor = Color.FromKnownColor(KnownColor.Window);
		}

		/// <summary>
		/// Returns a list of wordforms selected in the Tagging (Text Markup) tab.
		/// The underlying member variable is updated by the SelectionChanged() override.
		/// </summary>
		public List<AnalysisOccurrence> SelectedWordforms
		{
			get { return m_selectedWordforms; }
		}

		protected override void MakeVc()
		{
			Vc = new InterlinTaggingVc(m_cache);
			m_tagFact = m_cache.ServiceLocator.GetInstance<ITextTagFactory>();
			m_segRepo = m_cache.ServiceLocator.GetInstance<ISegmentRepository>();
		}

		#region SelectionMethods

		bool m_fInSelChanged;

		/// <summary>
		/// Any selection that involves an IAnalysis should be expanded to complete IAnalysis objects.
		/// This method also updates a list of selected AnalysisOccurrences.
		/// </summary>
		/// <param name="prootb"></param>
		/// <param name="vwselNew"></param>
		protected override void HandleSelectionChange(IVwRootBox prootb, IVwSelection vwselNew)
		{
			if (m_fInSelChanged)
				return;
			m_fInSelChanged = true;
			try
			{
				m_selectedWordforms = null;
				base.HandleSelectionChange(prootb, vwselNew);
				if (vwselNew == null)
					return;

				SelLevInfo[] analysisLevels;
				SelLevInfo[] endLevels;
				if (TryGetAnalysisLevelsAndEndLevels(vwselNew, out analysisLevels, out endLevels))
				{
					m_selectedWordforms = GetSelectedOccurrences(analysisLevels, endLevels[0].ihvo);
					RootBox.MakeTextSelInObj(0, analysisLevels.Length, analysisLevels, endLevels.Length, endLevels, false, false,
											 false, true, true);
				}
				else
					RootBox.DestroySelection();
			}
			finally
			{
				m_fInSelChanged = false;
			}
		}

		private bool TryGetAnalysisLevelsAndEndLevels(IVwSelection vwselNew, out SelLevInfo[] analysisLevels, out SelLevInfo[] endLevels)
		{
			endLevels = null;
			analysisLevels = GetAnalysisLevelsFromSelection(vwselNew);
			if (analysisLevels == null)
				return false;

			var rgvsliEnd = GetOneEndPointOfSelection(vwselNew, true);

			// analysisLevels[0] contains info about the sequence of Analyses property. We want the corresponding
			// level in rgvsliEnd to have the same tag but not necessarily the same ihvo.
			// All the higher levels should be exactly the same. The loop checks this, and if successful
			// sets iend to the index of the property corresponding to analysisLevels[0].
			int iend;
			if (AreHigherLevelsSameObject(analysisLevels, rgvsliEnd, out iend))
			{
				endLevels = analysisLevels.Clone() as SelLevInfo[];
				if (endLevels == null) {}
				else
				{
					endLevels[0] = new SelLevInfo(); // clone is SHALLOW; don't modify element of analysisLevels.
					endLevels[0].ihvo = rgvsliEnd[iend].ihvo;
					endLevels[0].tag = analysisLevels[0].tag;
				}
			}
			return endLevels != null;
		}

		/// <summary>
		/// Get the array of SelLevInfo corresponding to one end point of a selection.
		/// </summary>
		/// <param name="vwselNew"></param>
		/// <param name="fEndPoint">True if we want the end of the selection. False if we want the anchor.</param>
		/// <returns></returns>
		protected static SelLevInfo[] GetOneEndPointOfSelection(IVwSelection vwselNew, bool fEndPoint)
		{
			// Get the info about the other end of the selection.
			int cvsli = vwselNew.CLevels(fEndPoint) - 1;
			SelLevInfo[] rgvsliEnd;
			using (ArrayPtr prgvsli = MarshalEx.ArrayToNative<SelLevInfo>(cvsli))
			{
				int ihvoRoot, tagTextProp, cpropPrevious, ich, ws;
				bool fAssocPrev;
				ITsTextProps ttpSelProps;
				vwselNew.AllSelEndInfo(fEndPoint, out ihvoRoot, cvsli, prgvsli,
					out tagTextProp, out cpropPrevious, out ich,
					out ws, out fAssocPrev, out ttpSelProps);
				rgvsliEnd = MarshalEx.NativeToArray<SelLevInfo>(prgvsli, cvsli);
			}
			return rgvsliEnd;
		}

		/// <summary>
		/// Updates the list of selected occurrences as well as the current StTxtPara.
		/// </summary>
		private List<AnalysisOccurrence> GetSelectedOccurrences(SelLevInfo[] analysisLevels, int iend)
		{
			var hvoSegment = analysisLevels[1].hvo;
			var ianchor = analysisLevels[0].ihvo;

			// These two lines are in case the user selects "backwards"
			var first = Math.Min(ianchor, iend);
			var last = Math.Max(ianchor, iend);

			var selectedWordforms = new List<AnalysisOccurrence>();
			ISegment seg;
			try
			{
				seg = m_segRepo.GetObject(hvoSegment);
			}
			catch (KeyNotFoundException)
			{
				return selectedWordforms; // "Selection" isn't in a TextSegment.
			}

			for (var i = first; i <= last; i++)
			{
				var point = new AnalysisOccurrence(seg, i);
				// Don't want any punctuation sneaking in!
				if (point.IsValid && point.HasWordform)
					selectedWordforms.Add(point);
			}

			return selectedWordforms;
		}

		private List<AnalysisOccurrence> GetSelectedOccurrences(IVwSelection vwselNew)
		{
			if (vwselNew == null)
				return null;
			SelLevInfo[] analysisLevels;
			SelLevInfo[] endLevels;
			if (TryGetAnalysisLevelsAndEndLevels(vwselNew, out analysisLevels, out endLevels))
				return GetSelectedOccurrences(analysisLevels, endLevels[0].ihvo);
			return null;
		}

		/// <summary>
		/// Get SelLevInfo[] that starts with WfiWordform level.
		/// Returns null if itagAnalysis is less than zero.
		/// </summary>
		/// <param name="vwsel"></param>
		/// <returns></returns>
		private SelLevInfo[] GetAnalysisLevelsFromSelection(IVwSelection vwsel)
		{
			return GetAnalysisLevelsFromSelLevInfo(GetSelLevInfoFromSelection(vwsel));
		}

		private static SelLevInfo[] GetAnalysisLevelsFromSelLevInfo(SelLevInfo[] rgvsli)
		{
			if (rgvsli == null)
				return null;
			var itagAnalysis = GetIAnalysisIndexInSelLevInfoArray(rgvsli);
			if (itagAnalysis < 0)
				return null;
			var result = new SelLevInfo[rgvsli.Length - itagAnalysis];
			Array.Copy(rgvsli, itagAnalysis, result, 0, rgvsli.Length - itagAnalysis);
			return result;
		}

		protected static int GetIAnalysisIndexInSelLevInfoArray(SelLevInfo[] rgvsli)
		{
			// Identify the IAnalysis, and the position in rgvsli of the property holding it.
			// It is also possible that the IAnalysis is the root object.
			// This is important because although we are currently displaying just an StTxtPara,
			// eventually it might be part of a higher level structure. We want to be able to
			// reproduce everything that gets us down to the IAnalysis.
			for (var i = rgvsli.Length; --i >= 0; )
			{
				if (rgvsli[i].tag == SegmentTags.kflidAnalyses)
					return i;
			}
			// Either the user didn't anchor the selection on a word or its not yet analyzed.
			// TODO: How to handle the latter situation?!
			return -1;
		}

		/// <summary>
		/// Gets the sel lev info from selection.
		/// </summary>
		/// <param name="vwsel">The vwsel.</param>
		/// <returns></returns>
		private SelLevInfo[] GetSelLevInfoFromSelection(IVwSelection vwsel)
		{
			var helper = SelectionHelper.Create(vwsel, this);
			return helper == null ? null : helper.LevelInfo;
		}

		private static bool AreHigherLevelsSameObject(SelLevInfo[] analysisLevels, SelLevInfo[] rgvsliEnd, out int iend)
		{
			iend = rgvsliEnd.Length - 1;
			for (var ianchor = analysisLevels.Length - 1; ianchor > 0 && iend >= 0; ianchor--, iend--)
			{
				if (rgvsliEnd[iend].tag != analysisLevels[ianchor].tag)
					return false;
				if (rgvsliEnd[iend].ihvo != analysisLevels[ianchor].ihvo)
					return false;
			}
			if (iend < 0)
				return false; // not enough levels in the end selection, very unlikely.
			return analysisLevels[0].tag == rgvsliEnd[iend].tag;
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayLastBundle(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = Visible;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}
		/// <summary>
		/// Move to the last bundle
		/// </summary>
		/// <param name="arg"></param>
		/// <returns></returns>
		public bool OnLastBundle(object arg)
		{
			var levels = new SelLevInfo[3];
			levels[2].tag = StTextTags.kflidParagraphs;
			levels[2].ihvo = RootStText.ParagraphsOS.Count - 1;
			var lastPara = (IStTxtPara)RootStText.ParagraphsOS.Last();
			levels[1].tag = StTxtParaTags.kflidSegments;
			levels[1].ihvo = lastPara.SegmentsOS.Count - 1;
			ISegment lastSegment = lastPara.SegmentsOS.Last();
			int i;
			for (i = lastSegment.AnalysesRS.Count - 1; i >= 0; i--)
			{
				if (!(lastSegment.AnalysesRS[i] is IPunctuationForm))
					break;
			}
			levels[0].tag = SegmentTags.kflidAnalyses;
			levels[0].ihvo = i;
			RootBox.MakeTextSelInObj(0, levels.Length, levels, 0, null, false, false, false, true, true);
			return true;
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayFirstBundle(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = Visible;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}
		/// <summary>
		/// Move to the first bundle
		/// </summary>
		/// <param name="arg"></param>
		/// <returns></returns>
		public bool OnFirstBundle(object arg)
		{
			var levels = new SelLevInfo[3];
			levels[2].tag = StTextTags.kflidParagraphs;
			levels[2].ihvo = 0;
			levels[1].tag = StTxtParaTags.kflidSegments;
			levels[1].ihvo = 0;
			levels[0].tag = SegmentTags.kflidAnalyses;
			levels[0].ihvo = 0;
			RootBox.MakeTextSelInObj(0, levels.Length, levels, 0, null, false, false, false, true, true);
			return true;
		}

		#endregion // SelectionMethods

		#region ContextMenuMethods

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				var selTest = GrabMousePtSelectionToTest(e);

				// Could be the user right-clicked on the labels?
				// If so, activate the base class method
				int dummy;
				if (UserClickedOnLabels(selTest, out dummy))
				{
					base.OnMouseDown(e);
					return;
				}

				// (LT-9415) if the user has right-clicked in a selected occurrence, bring
				// up context menu for those selected ocurrences.
				// otherwise, bring up the context menu for any newly selected occurrence.
				if (SelectedWordforms != null && SelectedWordforms.Count > 0)
				{
					var newSelectedOccurrences = GetSelectedOccurrences(selTest);
					// if we don't overlap with an existing occurrence selection then
					// make a new occurrence selection. (Otherwise, just make the context
					// menu based on the current selected occurrences).
					if (newSelectedOccurrences == null ||
						newSelectedOccurrences.Count == 0 ||
						!SelectedWordforms.Contains(newSelectedOccurrences[0]))
					{
						// make a new (occurrence) selection (via our SelectionChanged override)
						// before making the context menu.
						selTest.Install();
					}
				}
				// Make a context menu and show it, if I'm not just closing one!
				// This time test seems to be the only way to find out whether this click closed the last one.
				if (DateTime.Now.Ticks - m_ticksWhenContextMenuClosed > 50000) // 5ms!
				{
					m_taggingContextMenu = MakeContextMenu();
					m_taggingContextMenu.Closed += m_taggingContextMenu_Closed;
					m_taggingContextMenu.Show(this, e.X, e.Y);
				}
			} // end right mouse button handler
			else
			{
				base.OnMouseDown(e);
			}
		}

		void m_taggingContextMenu_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			m_ticksWhenContextMenuClosed = DateTime.Now.Ticks;
		}

		internal ContextMenuStrip MakeContextMenu()
		{
			var menu = new ContextMenuStrip();

			// A little indirection for when we make more menu options later.
			// If we have not selected any Wordforms, don't put tags in the context menu.
			if (SelectedWordforms != null && SelectedWordforms.Count > 0)
			{
				var tagList = GetTaggingLists(Cache.LangProject);
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
			var tagSubmenu = new DisposableToolStripMenuItem(tagList.Name.BestAnalysisAlternative.Text);
			menu.Items.Add(tagSubmenu);

			foreach (ICmPossibility poss in tagList.SubPossibilitiesOS)
			{
				// Add 'tag' BestDefaultAnalWS Name to menu
				var tagItem = new TagPossibilityMenuItem(poss);
				tagItem.Click += Tag_Item_Click;
				tagItem.Text = poss.Name.BestAnalysisAlternative.Text;
				tagItem.Checked = DoSelectedOccurrencesHaveTag(poss);
				tagSubmenu.DropDownItems.Add(tagItem);
			}
		}

		private bool DoSelectedOccurrencesHaveTag(ICmPossibility poss)
		{
			var dummyTag = FindFirstTagOfSpecifiedTypeOnSelection(poss);
			return dummyTag != null;
		}

		/// <summary>
		/// Finds the first tag of specified possibility pointing to the selected wordforms.
		/// If successful, returns the tagging annotation. If unsuccessful, returns null.
		/// </summary>
		/// <param name="poss">The tagging possibility item.</param>
		/// <returns></returns>
		private ITextTag FindFirstTagOfSpecifiedTypeOnSelection(ICmPossibility poss)
		{
			var tagList = FindAllTagsReferencingOccurrenceList(SelectedWordforms);
			return tagList.FirstOrDefault(textTag => textTag.TagRA == poss);
		}

		internal static ICmPossibilityList GetTaggingLists(ILangProject langProj)
		{
			Debug.Assert(langProj != null, "No LangProject available!");
			var result = langProj.TextMarkupTagsOA;

			if (result == null) // Just trying to be careful.
			{
				// Create the containing object and lists.
				result = langProj.GetDefaultTextTagList();
				langProj.TextMarkupTagsOA = result;
			}
			return result;
		}

		#endregion // ContextMenuMethods

		void Tag_Item_Click(object sender, EventArgs e)
		{
			var item = sender as TagPossibilityMenuItem;
			if (item == null)
				return;
			// save current selection info. (e.g. EnsureSelectedWordformsAndSegAreAllReal() can destroy it).
			var sh = SelectionHelper.Create(this);
			if (item.Checked)
			{
				UndoableUnitOfWorkHelper.Do(ITextStrings.ksUndoDeleteTextTag, ITextStrings.ksRedoDeleteTextTag,
											Cache.ActionHandlerAccessor, () => RemoveTextTagInstance(item.Possibility));
			}
			else
			{
				UndoableUnitOfWorkHelper.Do(ITextStrings.ksUndoAddTextTag, ITextStrings.ksRedoAddTextTag,
											Cache.ActionHandlerAccessor, () => MakeTextTagInstance(item.Possibility));
			}
			// We might try later to see if we can do without this! Nope.
			if (sh != null)
				sh.RestoreSelectionAndScrollPos();
		}

		/// <summary>
		/// Removes the TextTag matching the possibility item on the selected wordforms.
		/// If there are multiples, this will remove the first one it finds.
		/// </summary>
		/// <param name="poss">The tag list possibility item.</param>
		private void RemoveTextTagInstance(ICmPossibility poss)
		{
			if (SelectedWordforms == null || SelectedWordforms.Count == 0)
				return;

			var tag = FindFirstTagOfSpecifiedTypeOnSelection(poss);
			if (tag == null)
				return; // Shouldn't happen, but...
			CacheNullTagString(tag);
			RootStText.TagsOC.Remove(tag); // deletes tag
		}

		/// <summary>
		/// Creates a new TextTag for the tag possibility (parameter)
		/// pointing to the AnalysisOccurrences in SelectedWordforms. If there are none selected,
		/// the method returns null.
		/// </summary>
		/// <param name="tagPoss"></param>
		/// <returns>The new TextTag object</returns>
		protected ITextTag MakeTextTagInstance(ICmPossibility tagPoss)
		{
			Debug.Assert(tagPoss != null, "Expecting a valid tagging possibility.");
			if (SelectedWordforms == null || SelectedWordforms.Count == 0)
				return null;

			// TODO: Make sure SelectedWordforms only contains wordforms! No punctuation, please!!!

			// We'll try doing without passing the list of selected words in here
			// Just get it from SelectedWordforms.
			// Delete ones that point at selected ones already
			// (before we add the new one or it gets deleted too!)
			ISet<ITextTag> overlappingTags = FindAllTagsReferencingOccurrenceList(SelectedWordforms);
			ISet<ITextTag> objsToDelete = new HashSet<ITextTag>();
			int tagRow = tagPoss.Owner.IndexInOwner;
			foreach (ITextTag overlappingTag in overlappingTags)
			{
				int overlappingTagRow = overlappingTag.TagRA.Owner.IndexInOwner;
				if (tagRow == overlappingTagRow)
				{
					objsToDelete.Add(overlappingTag);
				}
			}

			// Create and add the new one
			var ttag = m_tagFact.Create();
			RootStText.TagsOC.Add(ttag);
			ttag.TagRA = tagPoss;

			// Enhance Gordon: If we allow non-contiguous selection eventually this won't work.
			var point1 = SelectedWordforms[0];
			var point2 = SelectedWordforms[SelectedWordforms.Count - 1];
			SetTagBeginPoint(ttag, point1);
			SetTagEndPoint(ttag, point2);
			DeleteTextTags(objsToDelete);
			CacheTagString(ttag);
			return ttag;
		}

		private static void SetTagEndPoint(ITextTag ttag, AnalysisOccurrence point2)
		{
			ttag.EndSegmentRA = point2.Segment;
			ttag.EndAnalysisIndex = point2.Index;
		}

		private static void SetTagBeginPoint(ITextTag ttag, AnalysisOccurrence point1)
		{
			ttag.BeginSegmentRA = point1.Segment;
			ttag.BeginAnalysisIndex = point1.Index;
		}

		private static ISet<ITextTag> FindAllTagsReferencingOccurrenceList(List<AnalysisOccurrence> occurrences)
		{
			if (occurrences == null || occurrences.Count == 0)
				return new HashSet<ITextTag>();

			// We're unlikely to have more than a few hundred words in a sentence.
			return GetTaggingReferencingTheseWords(occurrences);
		}

		/// <summary>
		/// Protected virtual so the testing subclass doesn't have to know about Views.
		/// </summary>
		/// <param name="textTag"></param>
		protected virtual void CacheTagString(ITextTag textTag)
		{
			// Cache the new tagging string and call PropChanged?
			(Vc as InterlinTaggingVc).CacheTagString(textTag);
		}

		/// <summary>
		/// Protected virtual so the testing subclass doesn't have to know about Views.
		/// </summary>
		/// <param name="textTag"></param>
		protected virtual void CacheNullTagString(ITextTag textTag)
		{
			// Cache a string for each occurrence this tag references. (PropChanged?)

			int row = textTag.TagRA.Owner.IndexInOwner;
			(Vc as InterlinTaggingVc).ClearTagStringForRow(textTag.GetOccurrences(), row);
		}

		/// <summary>
		/// Deletes the text tag annotations that a new addition overlaps (temporary).
		/// </summary>
		/// <param name="tagsToDelete">The text tags to delete.</param>
		protected void DeleteTextTags(ISet<ITextTag> tagsToDelete)
		{
			if (tagsToDelete.Count == 0)
				return;
			foreach (var tag in tagsToDelete)
			{
				CacheNullTagString(tag);
				RootStText.TagsOC.Remove(tag);
			}
		}

		/// <summary>
		/// Gets a list of TextTags that reference the analysis occurrences in the input paramter.
		/// Will need enhancement to work with multi-segment tags.
		/// </summary>
		/// <param name="occurrences"></param>
		/// <returns>A set of tags.</returns>
		internal static ISet<ITextTag> GetTaggingReferencingTheseWords(List<AnalysisOccurrence> occurrences)
		{
			var results = new HashSet<ITextTag>();
			if (occurrences.Count == 0 || !occurrences[0].IsValid)
				return results;
			var text = occurrences[0].Segment.Paragraph.Owner as IStText;
			if (text == null)
				throw new NullReferenceException("Unexpected error!");
			var tags = text.TagsOC;
			if (tags.Count == 0)
				return results;

			var occurenceSet = new HashSet<AnalysisOccurrence>(occurrences);;

			// Collect all segments referenced by these words
			var segsUsed = new HashSet<ISegment>(occurenceSet.Select(o => o.Segment));

			// Collect all tags referencing those segments
			// Enhance: This won't work for multi-segment tags where a tag can reference 3+ segments.
			// but see note on foreach below.
			var tagsRefSegs = new HashSet<ITextTag>(from ttag in tags
				where segsUsed.Contains(ttag.BeginSegmentRA) || segsUsed.Contains(ttag.EndSegmentRA)
				select ttag);

			foreach (var ttag in tagsRefSegs) // A slower, but more complete form can replace tagsRefSegs with tags here.
			{
				if (occurenceSet.Intersect(ttag.GetOccurrences()).Any())
					results.Add(ttag);
			}

			return results;
		}

		/// <summary>
		/// Activate() is disabled by default in ReadOnlyViews, but Tagging does want to show selections.
		/// </summary>
		protected override bool AllowDisplaySelection
		{
			get { return true; }
		}

		/// <summary>
		/// Used for Text Tagging possibility menu items
		/// </summary>
		private class TagPossibilityMenuItem : DisposableToolStripMenuItem
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="TagPossibilityMenuItem"/> class
			/// used for context (right-click) menus.
			/// </summary>
			/// <param name="poss">
			/// 	The possibility item.
			/// </param>
			public TagPossibilityMenuItem(ICmPossibility poss)
			{
				Possibility = poss;
			}

			public ICmPossibility Possibility { get; }
		}
	} // end class InterlinTaggingChild
}
