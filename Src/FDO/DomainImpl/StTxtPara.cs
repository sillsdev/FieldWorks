// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: StTxtPara.cs
// Responsibility: TE Team

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.DomainImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Structured Text Paragraph.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal partial class StTxtPara
	{
		#region Member variables
		internal bool m_paraCloneInProgress = false;
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the previous paragraph within the StText, else null if there is not one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IStTxtPara PreviousParagraph
		{
			get
			{
				int index = IndexInOwner;
				if (index == 0) // if this is the first paragraph
					return null;
				IStText owningText = (IStText)Owner;
				return (IStTxtPara)owningText.ParagraphsOS[index - 1];
			}
		}
		#endregion

		#region Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return a Reference (e.g., Scripture reference, or text abbreviation/para #/sentence#) for the specified character
		/// position (in the whole paragraph), which is assumed to belong to the specified segment.
		/// (For now, ich is not actually used, but it may become important if we decide not to split segements for
		/// verse numbers.)
		/// Overridden in ScrTxtPara to handle special cases for Scripture refs.
		/// </summary>
		/// <param name="seg">The segment.</param>
		/// <param name="ich">The character position.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual ITsString Reference(ISegment seg, int ich)
		{
			IStText stText = Owner as IStText;
			if (stText == null)
			{
				// Unusual case, possibly hvoPara is not actually a para at all, for example, it may
				// be a picture caption. For now we make an empty reference so at least it won't crash.
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				return tsf.MakeString("", Cache.DefaultUserWs);
			}
			ITsString tssName = null;
			bool fUsingAbbreviation = false;
			if (stText.Owner is IText)
			{
				// see if we can find an abbreviation.
				IText text = (IText)stText.Owner;
				tssName = text.Abbreviation.BestVernacularAnalysisAlternative;
				if (tssName != null && tssName.Length > 0 && tssName.Text != text.Abbreviation.NotFoundTss.Text)
					fUsingAbbreviation = true;
			}

			if (!fUsingAbbreviation)
				tssName = stText.Title.BestVernacularAnalysisAlternative;

			ITsStrBldr bldr = tssName.GetBldr();
			// If we didn't find a "best", reset to an empty string.
			if (bldr.Length > 0 && bldr.Text == stText.Title.NotFoundTss.Text)
				bldr.ReplaceTsString(0, bldr.Length, null);
			// Truncate to 8 chars, if the user hasn't specified an abbreviation.
			if (!fUsingAbbreviation && bldr.Length > 8)
				bldr.ReplaceTsString(8, bldr.Length, null);

			// Make a TsTextProps specifying just the writing system.
			ITsPropsBldr propBldr = TsPropsBldrClass.Create();
			int dummy;
			int wsActual = bldr.get_Properties(0).GetIntPropValues((int)FwTextPropType.ktptWs, out dummy);
			propBldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, wsActual);
			ITsTextProps props = propBldr.GetTextProps();

			// Insert a space (if there was any title)
			if (bldr.Length > 0)
				bldr.Replace(bldr.Length, bldr.Length, " ", props);

			// if Scripture.IsResponsibleFor(stText) we should try to get the verse number of the annotation.
			//if (stText.OwningFlid == (int)Text.TextTags.kflidContents)
			//{

			// Insert paragraph number.
			int ipara = stText.ParagraphsOS.IndexOf(this) + 1;
			bldr.Replace(bldr.Length, bldr.Length, ipara.ToString(), props);

			// And a period...
			bldr.Replace(bldr.Length, bldr.Length, ".", props);

			// And now the segment number
			int iseg = SegmentsOS.IndexOf(seg) + 1;
			bldr.Replace(bldr.Length, bldr.Length, iseg.ToString(), props);
			return bldr.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the ORC of the specified picture and deletes it from the paragraph and any
		/// back translations and deletes the object itself.
		/// </summary>
		/// <param name="hvoPic">The HVO of the picture to delete</param>
		/// <returns>The character offset of the location where the ORC was found in this
		/// paragraph for the gievn picture. If not found, returns -1.</returns>
		/// ------------------------------------------------------------------------------------
		public int DeletePicture(int hvoPic)
		{
			ITsString contents = Contents;
			Guid guidPic = Cache.ServiceLocator.GetInstance<ICmPictureRepository>().GetObject(hvoPic).Guid;
			for (int i = 0; i < contents.RunCount; i++)
			{
				string str = contents.get_Properties(i).GetStrPropValue((int)FwTextPropType.ktptObjData);

				if (str != null)
				{
					if (MiscUtils.GetGuidFromObjData(str.Substring(1)) == guidPic)
					{
						int startOfRun = contents.get_MinOfRun(i);
						int limOfRun = contents.get_LimOfRun(i);
						RemoveOwnedObjectsForString(startOfRun, limOfRun);
						ITsStrBldr bldr = contents.GetBldr();
						bldr.Replace(startOfRun, limOfRun, string.Empty, null);
						Contents = bldr.GetString();
						return startOfRun;
					}
				}
			}
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete any objects whose guids are owned by the given portion of the paragraph
		/// Contents. Use this when part of a string is about to be deleted (except when being
		/// done thru a VwSelection).
		/// </summary>
		/// <param name="ichMin">The 0-based index of the first character to be deleted</param>
		/// <param name="ichLim">The 0-based index of the character following the last character
		/// to be deleted</param>
		/// ------------------------------------------------------------------------------------
		internal void RemoveOwnedObjectsForString(int ichMin, int ichLim)
		{
			if (ichLim <= ichMin)
				return; // nothing to do

			int firstRun = Contents.get_RunAt(ichMin);
			int lastRun = Contents.get_RunAt(ichLim - 1);

			// Check each run, and delete owned objects. Make sure we go backwards since
			// deleting the footnote will cause the ORC to get removed out of this paragraph which
			// will change our run count.
			for (int iRun = lastRun; iRun >= firstRun; iRun--)
			{
				FwObjDataTypes odt;
				Guid guid = TsStringUtils.GetOwnedGuidFromRun(Contents, iRun, out odt);
				if (guid != Guid.Empty)
				{
					ICmObjectRepository repo = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
					ICmObject obj;
					if (repo.TryGetObject(guid, out obj))
					{
						if (obj is IScrFootnote)
							((IScrBook)obj.Owner).FootnotesOS.Remove((IScrFootnote)obj);
						else if (obj is ICmPicture)
							m_cache.DomainDataByFlid.DeleteObj(obj.Hvo);
					}
					DeleteAnyBtMarkersForObject(guid);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove all embedded ORCs that reference the specified object in all
		/// writing systems for the back translation of the given (vernacular) paragraph.
		/// </summary>
		/// <param name="guid">guid of object (footnote or picture)</param>
		/// ------------------------------------------------------------------------------------
		public void DeleteAnyBtMarkersForObject(Guid guid)
		{
			BackTranslationAndFreeTranslationUpdateHelper.Do(this, () => DeleteAnyBtMarkersForObjectInternal(guid));
		}

		/// <summary>
		/// Helper method so that deletion of ORCs can be wrapped within BackTranslationAndFreeTranslationUpdateHelper
		/// call.
		/// </summary>
		private void DeleteAnyBtMarkersForObjectInternal(Guid guid)
		{
			// ENHANCE: Someday may need to do this for other translations, not just BT (will
			// need to rename this method).
			ICmTranslation trans = GetBT();

			if (trans != null)
			{
				// Delete the reference ORCs for this object in the back translation
			   for (int i = 0; i < trans.Translation.StringCount; i++)
					RemoveOrc(trans.Translation, i, guid);
			}

			foreach (var segment in SegmentsOS)
			{
				for (int i = 0; i < segment.FreeTranslation.StringCount; i++)
					RemoveOrc(segment.FreeTranslation, i, guid);
			}
		}

		private void RemoveOrc(IMultiString trans, int i, Guid guid)
		{
			int ws;
			ITsString btTss = trans.GetStringFromIndex(i, out ws);
			if (btTss.RunCount > 0 && btTss.Text != null)
			{
				ITsStrBldr btTssBldr = btTss.GetBldr();
				if (TsStringUtils.DeleteOrcFromBuilder(btTssBldr, guid) >= 0)
					trans.set_String(ws, btTssBldr.GetString());
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a copy of the specified footnote.
		/// </summary>
		/// <param name="iFirstFootnote">The index of the first footnote, or -1 if not known.</param>
		/// <param name="footnoteCount">The number of footnotes added since the first one.</param>
		/// <param name="ich">Offset in this para that is used when we need to scan
		/// backwards for the previous footnote index.</param>
		/// <param name="guidOfObjToCopy">The guid of the footnote to copy.</param>
		/// <param name="iRun">The 0-based index of the run from which we will get the writing
		/// system used in a blank footnote.</param>
		/// <returns>The GUID of the new footnote copy.</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual Guid CreateFootnoteCopy(ref int iFirstFootnote, int footnoteCount, int ich,
			Guid guidOfObjToCopy, int iRun)
		{
			throw new NotImplementedException("CreateFootnoteCopy must be overridden.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a copy of the specified picture.
		/// </summary>
		/// <param name="guidOfPicToCopy">The GUID of the picture to copy.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual Guid CreatePictureCopy(Guid guidOfPicToCopy)
		{
			// Create the new copy of the picture.
			ICmPicture oldPic = Services.GetInstance<ICmPictureRepository>().GetObject(guidOfPicToCopy);
			//REVIEW: when we support BT of a caption, be sure we copy it!
			ICmPicture newPict = Services.GetInstance<ICmPictureFactory>().Create(
				oldPic.TextRepresentation, CmFolderTags.LocalPictures);
			return newPict.Guid;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a blank dummy footnote. Used when a footnote object is missing.
		/// Note: This does not insert an ORC into the paragraph. The caller is fixing the ORC
		/// with a missing object.
		/// </summary>
		/// <param name="sequence">The sequence to which we will add a footnote.</param>
		/// <param name="iFootnote">The 0-based index where the footnote will be inserted in the
		/// owner.</param>
		/// <param name="paraContents">The paragraph string where the ORC is being fixed.</param>
		/// <param name="iRun">The 0-based index of the run from which we will get the writing
		/// system used in the footnote.</param>
		/// <returns>a blank general footnote</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual IStFootnote CreateBlankDummyFootnote(IFdoOwningSequence<IStFootnote> sequence,
			int iFootnote, ITsString paraContents, int iRun)
		{
			// Make a dummy blank footnote is handled in ScrTextPara subclass. Since this isn't
			// a regular subclass but hopefully has been mapped, we use a hack: If StTxtPara is
			// mapped, CreateFromDBObject will return a ScrTextPara on which we can call
			// CreateBlankDummyFootnoteNoRecursion. Otherwise we call our implementation
			// (which currently just throws an exception).
			// If "this" is already a ScrTxtPara, we override CreateBlankDummyFootnote and
			// don't come here.
			return CreateBlankDummyFootnoteNoRecursion(sequence, iFootnote, paraContents, iRun);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a blank dummy footnote. Used when a footnote object is missing.
		/// Note: This does not insert an ORC into the paragraph. The caller is fixing the ORC
		/// with a missing object.
		/// </summary>
		/// <param name="sequence">The sequence to which we will add a footnote.</param>
		/// <param name="iFootnote">The 0-based index where the footnote will be inserted in the
		/// owner.</param>
		/// <param name="paraContents">The paragraph string where the ORC is being fixed.</param>
		/// <param name="iRun">The 0-based index of the run from which we will get the writing
		/// system used in the footnote.</param>
		/// <returns>a blank general footnote</returns>
		/// <remarks>NOTE: Don't call this version directly - always call
		/// CreateBlankDummyFootnote!</remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual IStFootnote CreateBlankDummyFootnoteNoRecursion(IFdoOwningSequence<IStFootnote> sequence,
			int iFootnote, ITsString paraContents, int iRun)
		{
			throw new NotImplementedException("Not yet implemented for non-ScrBook owners");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the footnote sequence.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual IFdoOwningSequence<IStFootnote> FootnoteSequence
		{
			get { throw new NotImplementedException("Footnotes for non-scripture paragraphs are not supported"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the ORCs (Object Replacement Characters) in translations of this paragraph.
		/// </summary>
		/// <param name="guidOfOldObj">The GUID of the old object</param>
		/// <param name="guidOfNewObj">The GUID of the new object</param>
		/// ------------------------------------------------------------------------------------
		protected void UpdateOrcsInTranslations(Guid guidOfOldObj, Guid guidOfNewObj)
		{

			// Look in each writing system of every translation for ORCs that need to be updated
			// from the GUID of the original object to the GUID of the new copy.
			FwObjDataTypes odt;
			ITsTextProps ttp;
			TsRunInfo tri;
			foreach (ICmTranslation trans in TranslationsOC)
			{
				HashSet<WritingSystem> transWs = trans.AvailableWritingSystems;
				foreach (WritingSystem ws in transWs)
				{
					ITsString tss = trans.Translation.get_String(ws.Handle);
					if (tss != null)
					{
						// Scan through ITsString for reference ORC to the specified old object.
						// Check each run
						for (int iRun = 0; iRun < tss.RunCount; iRun++)
						{
							// Attempt to find the old GUID in the current run.
							Guid guidInTrans = TsStringUtils.GetGuidFromRun(tss, iRun, out odt,
								out tri, out ttp, null);

							// If we found the GUID of the old object, we need to update this
							//  ORC to the new GUID.
							if (guidInTrans == guidOfOldObj)
							{
								ITsStrBldr strBldr = tss.GetBldr();
								UpdateORCforNewObjData(strBldr, ttp, tri, odt, guidOfNewObj);
								// Set the translation alt string to the new value
								trans.Translation.set_String(ws.Handle, strBldr.GetString());
								break; // updated matching ORC, check next writing system
							}
						}
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// In a string builder, updates the given ORC (Object Replacement Character) run
		/// with new object data.
		/// </summary>
		/// <param name="bldr">tss builder holding the ORC run</param>
		/// <param name="ttp">text properties of the ORC run</param>
		/// <param name="tri">The info for the ORC run, including min/lim character indices.</param>
		/// <param name="odt">object data type indicating to which type of object ORC points</param>
		/// <param name="guidOfNewObj">The GUID of new object, to update the ORC</param>
		/// ------------------------------------------------------------------------------------
		protected static void UpdateORCforNewObjData(ITsStrBldr bldr, ITsTextProps ttp,
			TsRunInfo tri, FwObjDataTypes odt, Guid guidOfNewObj)
		{
			// build new ObjData properties of the ORC for the new object
			byte[] objData = TsStringUtils.GetObjData(guidOfNewObj, (byte)odt);
			ITsPropsBldr propsBldr = ttp.GetBldr();
			propsBldr.SetStrPropValueRgch((int)FwTextPropType.ktptObjData,
				objData, objData.Length);

			// update the run props in the string builder
			bldr.SetProperties(tri.ichMin, tri.ichLim, propsBldr.GetTextProps());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Looks to see if we currently have a back translation, returning it if we do.  If no
		/// BT exists, a new one is created and returned.
		/// </summary>
		/// <returns>CmTranslation for the BT</returns>
		/// ------------------------------------------------------------------------------------
		public ICmTranslation GetOrCreateBT()
		{
			ICmTranslation trans = GetBT();
			if (trans == null)
			{
				// We don't want to create an undo task if there is one already open.
				IActionHandler ah = Cache.ServiceLocator.GetInstance<IActionHandler>();
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(ah, () =>
				{
					// we need to create an empty translation if one does not exist.
					trans = new CmTranslation();
					TranslationsOC.Add(trans);
					trans.TypeRA = Services.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranBackTranslation);
				});
			}
			return trans;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Looks to see if we currently have a back translation, returning it if we do.
		/// </summary>
		/// <returns>CmTranslation for the BT or null if none exists</returns>
		/// ------------------------------------------------------------------------------------
		public ICmTranslation GetBT()
		{
			ICmTranslation trans = TranslationsOC.FirstOrDefault();
			if (Cache.FullyInitializedAndReadyToRock && trans != null)
			{
				if (trans.TypeRA == null || trans.TypeRA.Guid != CmPossibilityTags.kguidTranBackTranslation)
					throw new InvalidOperationException("Translation must have Type set before it can be retrieved.");
			}
			return trans;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a List of all pictures "owned" by this paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<ICmPicture> GetPictures()
		{
			ITsString contents = Contents;
			List<ICmPicture> pictures = new List<ICmPicture>();

			for (int iRun = 0; iRun < contents.RunCount; iRun++)
			{
				Guid guidOfObj = TsStringUtils.GetGuidFromRun(contents, iRun,
					FwObjDataTypes.kodtGuidMoveableObjDisp);
				if (guidOfObj != Guid.Empty)
				{
					try
					{
						ICmPicture picture = Services.GetInstance<ICmPictureRepository>().GetObject(guidOfObj);
						pictures.Add(picture);
					}
					catch (KeyNotFoundException) { }
				}
			}

			return pictures;
		}
		#endregion

		#region ICloneableCmObject Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clones this instance.
		/// </summary>
		/// <param name="clone">Destination object for clone operation</param>
		/// ------------------------------------------------------------------------------------
		public void SetCloneProperties(ICmObject clone)
		{
			StTxtPara clonedPara = (StTxtPara)clone;
			clonedPara.m_paraCloneInProgress = true;
			clonedPara.Contents = Contents;
			clonedPara.Label = Label;
			clonedPara.StyleRules = StyleRules;
			clonedPara.ParseIsCurrent = ParseIsCurrent;

			// This is to ensure that we don't end up with two translations of the same type in
			// the cloned paragraph - this should only be needed for tests.
			clonedPara.TranslationsOC.Clear();

			CopyObject<ISegment>.CloneFdoObjects(SegmentsOS, x => clonedPara.SegmentsOS.Add(x));
			CopyObject<ICmTranslation>.CloneFdoObjects(TranslationsOC, x => clonedPara.TranslationsOC.Add(x));
			CopyObject<ICmObject>.CloneFdoObjects(AnalyzedTextObjectsOS, x => clonedPara.AnalyzedTextObjectsOS.Add(x));
			TextObjectsRS.CopyTo(clonedPara.TextObjectsRS, 0);

			clonedPara.m_paraCloneInProgress = false;
		}

		#endregion

		/// <summary>
		/// Get set of unique wordforms in this paragraph
		/// </summary>
		/// <returns>A List that contains zero, or more, integers (hvos) for the unique wordforms occurring in this paragraph.</returns>
		public void CollectUniqueWordforms(HashSet<IWfiWordform> wordforms)
		{
			if (ParseIsCurrent)
			{
				foreach (ISegment segment in SegmentsOS)
				{
					segment.CollectUniqueWordforms(wordforms);
				}
			}
			return;
		}

		#region Internal event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the side effects of setting the paragraph contents
		/// </summary>
		/// <param name="originalValue">The original value.</param>
		/// <param name="newValue">The new value.</param>
		/// ------------------------------------------------------------------------------------
		partial void ContentsSideEffects(ITsString originalValue, ITsString newValue)
		{
			if (m_paraCloneInProgress)
				return;
			if (originalValue == null && String.IsNullOrEmpty(newValue.Text))
				return; // no point in doing AnalysisAdjuster stuff if we're just creating an empty paragraph.
			TsStringDiffInfo diffInfo = TsStringUtils.GetDiffsInTsStrings(originalValue, newValue);
			Debug.Assert(diffInfo != null, "We shouldn't get called if there is no difference");
			if (diffInfo != null)
				OnContentsChanged(originalValue, newValue, diffInfo, true);
			// NOTE: Try not add anything else in this method. It probably should go in
			// OnContentsChanged.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Virtual method to allow subclasses to handle the side effects of setting the
		/// paragraph contents
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnContentsChanged(ITsString originalValue, ITsString newValue,
			TsStringDiffInfo diffInfo, bool fAdjustAnalyses)
		{
			if (Owner is StText)
				((StText)Owner).OnParagraphContentsChanged(this, originalValue, newValue);

			int oldSegmentCount = SegmentsOS.Count;
			if (fAdjustAnalyses)
				AnalysisAdjuster.AdjustAnalysis(this, originalValue, diffInfo);

			MarkCharStylesInUse(diffInfo.IchFirstDiff, diffInfo.CchInsert);
			if (SegmentsOS.Count < oldSegmentCount)
			{
				// A segment was removed. The removed segment could have some translated
				// material that is now gone. This will mean that the CmTranslation and
				// segmented translation are now out of sync. We can't do this code in
				// RemoveObjectSideEffectsInternal() because at that point the segments
				// are in an inconsistent state.
				ICmTranslation bt = GetBT();
				if (bt != null)
				{
					BackTranslationAndFreeTranslationUpdateHelper.Do(this,
						() => ScriptureServices.UpdateMainTransFromSegmented(this,
						bt.Translation.AvailableWritingSystemIds));
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Marks the char styles within the specified range as being InUse.
		/// </summary>
		/// <param name="ichFirstDiff">The character index of the first diff.</param>
		/// <param name="cchInsert">The number of characters inserted.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void MarkCharStylesInUse(int ichFirstDiff, int cchInsert)
		{
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a List of all TextTags that only reference Segments in this paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual List<ITextTag> GetTags()
		{
			var txtTags = ((IStText)Owner).TagsOC;
			return txtTags.Where(ttag => SegmentsOS.Contains(ttag.BeginSegmentRA)
				&& SegmentsOS.Contains(ttag.EndSegmentRA)).ToList();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a List of all ConstChartWordGroups that only reference Segments in this paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual List<IConstChartWordGroup> GetChartCellRefs()
		{
			var stText = (IStText)Owner;
			var wordGrpRepo = Cache.ServiceLocator.GetInstance<IConstChartWordGroupRepository>();
			// At least one bug involved cells whose Owner was null!
			var cells = wordGrpRepo.AllInstances().Where(
				cellPart => (cellPart.Owner != null && cellPart.Owner.Owner != null &&
					((IDsConstChart)cellPart.Owner.Owner).BasedOnRA == stText));
			// Several bugs, e.g., LT-11418, have occurred where one of the segmentRa properties is null.
			// So we allow a match if one of them is null, but not if both are.
			return cells.Where(cellPart =>
				(cellPart.BeginSegmentRA != null || cellPart.EndSegmentRA != null)
				&& (cellPart.BeginSegmentRA == null || SegmentsOS.Contains(cellPart.BeginSegmentRA))
				&& (cellPart.EndSegmentRA == null || SegmentsOS.Contains(cellPart.EndSegmentRA))).ToList();
		}

		#region Methods for moving contents from one para to another
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Splits the paragraph at the specified character index.
		/// </summary>
		/// <param name="ich">The character index where a split will be inserted.</param>
		/// <returns>the new paragraph inserted at the character index</returns>
		/// ------------------------------------------------------------------------------------
		public virtual IStTxtPara SplitParaAt(int ich)
		{
			Debug.Assert(Owner is IStText);
			Debug.Assert(ich > 0 && ich < Contents.Length);

			IStTxtPara newPara = ((IStText)Owner).InsertNewTextPara(IndexInOwner + 1, StyleName);
			newPara.ReplaceTextRange(0, 0, this, ich, Contents.Length);

			// remove the moved contents from this paragraph
			ITsStrBldr bldr = Contents.GetBldr();
			bldr.Replace(ich, Contents.Length, string.Empty, null);
			Contents = bldr.GetString();

			// Make sure the CmTanslation gets updated with the changes.
			ICmTranslation trans = GetBT();
			if (trans != null)
			{
				BackTranslationAndFreeTranslationUpdateHelper.Do(this, () =>
					ScriptureServices.UpdateMainTransFromSegmented(this, trans.Translation.AvailableWritingSystemIds));
			}

			return newPara;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Replaces the specified range of text with the specified range in the fromPara to
		/// this paragraph.
		/// </summary>
		/// <param name="ichMinDest">The starting character index where the text will be replaced.</param>
		/// <param name="ichLimDest">The ending character index where the text will be replaced.</param>
		/// <param name="fromPara">The source para for the text.</param>
		/// <param name="ichMinSrc">The starting character index to copy from.</param>
		/// <param name="ichLimSrc">The ending character index to copy from.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void ReplaceTextRange(int ichMinDest, int ichLimDest, IStTxtPara fromPara,
			int ichMinSrc, int ichLimSrc)
		{
			if (ichMinDest == ichLimDest && ichMinSrc == ichLimSrc)
				return; // Nothing is actually being replaced.

			Debug.Assert(fromPara != null || ichLimSrc - ichMinSrc == 0);

			ITsStrBldr tssBldr = Contents.GetBldr();
			ILgWritingSystemFactory wsf = Cache.WritingSystemFactory;
			int oldSegmentCount = tssBldr.GetString().GetSegments(wsf).Count;
			ITsString oldContents = Contents;

			// Delete specified range of text from this paragraph.
			tssBldr.ReplaceRgch(ichMinDest, ichLimDest, string.Empty, 0, null);
			SetContentsInternal(tssBldr.GetString(), new TsStringDiffInfo(ichMinDest, 0, ichLimDest - ichMinDest), true);

			// Copy the specified range of text (if any) from the source paragraph.
			ITsString tssSrc = null;
			if (ichLimSrc - ichMinSrc > 0 && fromPara != null)
			{
				tssSrc = fromPara.Contents.GetSubstring(ichMinSrc, ichLimSrc);
				tssBldr.ReplaceTsString(ichMinDest, ichMinDest, tssSrc);
			}

			// Fix the segments/analyses for the moved text by using the information from
			// the source paragraph.
			int cchInsert = ichLimSrc - ichMinSrc;
			if (cchInsert == 0)
				return; // Just deleting text, no insertion, so nothing left to do.

			if (fromPara != null && ichMinDest == tssBldr.Length - cchInsert && ichMinSrc + cchInsert == fromPara.Contents.Length)
			{
				// This is a special case that we know how to handle: appending one para to another.
				// ENHANCE: If we ever change the SetContentsForMoveDest method to handle other cases, we should use
				// it instead of the code in the else below since the SetContentsForMoveDest can theoretically do a
				// better job then UpdateSegsForReplace.
				SetContentsForMoveDest(tssBldr.GetString(), ichMinDest, cchInsert, fromPara, ichMinSrc, false);
			}
			else
			{
				ITsString oldContentsAfterDelete = Contents;
				SetContentsInternal(tssBldr.GetString(), new TsStringDiffInfo(ichMinDest, cchInsert, 0), true);

				if (fromPara != null)
				{
					bool fMergeFirstChangedSeg = (ichMinSrc == 0 &&
						ichLimDest == oldContents.Length &&
						SegmentsOS.Count == oldSegmentCount + tssSrc.GetSegments(wsf).Count - 1);
					UpdateSegsForReplace(ichMinDest, ichLimDest, fromPara, ichMinSrc, ichLimSrc, fMergeFirstChangedSeg);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends the para contents, BT, etc. from the following paragraph to this paragraph
		/// and removes the following paragraph afterwords.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void MergeParaWithNext()
		{
			IStText owningText = ((IStText)Owner);
			if (IndexInOwner == owningText.ParagraphsOS.Count - 1)
				throw new InvalidOperationException("Can't merge the last paragraph");

			IStTxtPara nextPara = owningText[IndexInOwner + 1];

			ReplaceTextRange(Contents.Length, Contents.Length, nextPara, 0, nextPara.Contents.Length);

			// Now that the copying is finished, we can safely delete the following paragraph
			owningText.ParagraphsOS.RemoveAt(IndexInOwner + 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moves the specified portion of the contents from source to dest.
		/// </summary>
		/// <param name="source">The source containing the string to move.</param>
		/// <param name="ichMin">The character offset in the source from which we will copy.</param>
		/// <param name="ichLim">The source ich lim.</param>
		/// <param name="dest">The destination paragraph for the moved characters.</param>
		/// <param name="ichDest">The character index in the destination paragraph where the
		/// moved characters are to be inserted.</param>
		/// <param name="fDstIsNew"><c>true</c> if the destination paragraph (i.e. this paragraph)
		/// is brand new; <c>false</c> otherwise.</param>
		/// ------------------------------------------------------------------------------------
		internal static void MoveContents(StTxtPara source, int ichMin, int ichLim,
			StTxtPara dest, int ichDest, bool fDstIsNew)
		{
			int cchMove = ichLim - ichMin;
			ITsString originalDestContents = dest.Contents;
			ITsString moved = source.Contents.Substring(ichMin, cchMove);
			ITsString newSource = source.Contents.Remove(ichMin, cchMove);
			ITsString newDest = dest.Contents.Insert(ichDest, moved);
			dest.SetContentsForMoveDest(newDest, ichDest, moved.Length, source, ichMin, fDstIsNew);
			source.SetContentsForMoveSource(newSource, ichMin, cchMove);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This implements one half of a move. The text newValue is being moved from position
		/// ichSource in paragraph source to ichInsert in the current paragraph. Run an analysis
		/// adjuster to move analysis information that should survive.
		/// </summary>
		/// <param name="newValue">The new string to insert into the paragraph.</param>
		/// <param name="ichInsert">The character index to insert newValue.</param>
		/// <param name="cchInsert">The count of characters insert.</param>
		/// <param name="source">The source containing the string to move.</param>
		/// <param name="ichSource">The character offset in the source from which we will copy.</param>
		/// <param name="fDstIsNew"><c>true</c> if the destination paragraph (i.e. this paragraph)
		/// is brand new; <c>false</c> otherwise.</param>
		/// ------------------------------------------------------------------------------------
		private void SetContentsForMoveDest(ITsString newValue, int ichInsert, int cchInsert,
			IStTxtPara source, int ichSource, bool fDstIsNew)
		{
			ITsString originalValue = m_Contents;
			SetContentsInternal(newValue, new TsStringDiffInfo(ichInsert, cchInsert, 0), false);
			AnalysisAdjuster.HandleMoveDest(this, originalValue, ichInsert, cchInsert, source, ichSource, fDstIsNew);

			// We need to update the CmTranslation of the destination paragraph (this) to match
			// what happened to the segments. (FWR-2224)
			// We use the available writing systems of the source paragraph since that is where the
			// segments in the destination paragraph originated.
			ICmTranslation trans = source.GetBT();
			if (trans != null) // Should only be null for non-Scripture paragraphs
			{
				int[] availableWss = trans.Translation.AvailableWritingSystemIds;
				BackTranslationAndFreeTranslationUpdateHelper.Do(this, () =>
					ScriptureServices.UpdateMainTransFromSegmented(this, availableWss));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This implements the other half of a move. This paragraph's contents are being set
		/// to the new paragraph contents.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetContentsForMoveSource(ITsString newValue, int ichDelete, int cchDelete)
		{
			ITsString originalValue = m_Contents;
			SetContentsInternal(newValue, new TsStringDiffInfo(ichDelete, 0, cchDelete), true);

			// We need to update the CmTranslation of source paragraph (this) to match what
			// happened to the segments. (FWR-2224)
			ICmTranslation trans = GetBT();
			if (trans != null)
			{
				BackTranslationAndFreeTranslationUpdateHelper.Do(this, () =>
					ScriptureServices.UpdateMainTransFromSegmented(this, trans.Translation.AvailableWritingSystemIds));
			}
		}
		#endregion

		#region Private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the contents of this StTxtPara for a specific change where we can determine the
		/// difference better than the automated way.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetContentsInternal(ITsString newContents, TsStringDiffInfo diffInfo,
			bool fAdjustAnalyses)
		{
			ITsString originalValue = m_Contents;
			m_Contents = newContents;
			((IServiceLocatorInternal)Services).UnitOfWorkService.RegisterObjectAsModified(this,
				StTxtParaTags.kflidContents, originalValue, newContents);

			OnContentsChanged(originalValue, m_Contents, diffInfo, fAdjustAnalyses);
		}
		#endregion

		#region Segment handling code
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the character offset in the free translation (CmTranslation) corresponding to
		/// the start of the given segment.
		/// </summary>
		/// <param name="segment">The character offset in the free translation.</param>
		/// <param name="ws">The writing system HVO.</param>
		/// ------------------------------------------------------------------------------------
		public int GetOffsetInFreeTranslationForStartOfSegment(ISegment segment, int ws)
		{
			int cumulativeLengthOfBt = 0;

			int space = 0;
			foreach (ISegment seg in SegmentsOS.TakeWhile(s => s != segment))
			{
				int cchSegTrans = 0;
				if (seg.IsLabel)
				{
					cchSegTrans = seg.Length + space;
					space = 1;
				}
				else
				{
					ITsString tss = seg.FreeTranslation.get_String(ws);
					if (tss != null && tss.Length > 0)
					{
						cchSegTrans = tss.Length;
						space = 0;
					}
				}
				cumulativeLengthOfBt += cchSegTrans;
			}
			return cumulativeLengthOfBt;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the segment corresponding to the given character offset in the specified free
		/// translation.
		/// </summary>
		/// <param name="ich">The character offset in the free translation.</param>
		/// <param name="ws">The writing system HVO.</param>
		/// ------------------------------------------------------------------------------------
		public ISegment GetSegmentForOffsetInFreeTranslation(int ich, int ws)
		{
			int cumulativeLengthOfBt = 0;

			int space = 0;
			foreach (ISegment segment in SegmentsOS)
			{
				int cchSegTrans = 0;
				if (segment.IsLabel)
				{
					cchSegTrans = segment.Length + space;
					space = 1;
				}
				else
				{
					ITsString tss = segment.FreeTranslation.get_String(ws);
					if (tss != null && tss.Length > 0)
					{
						cchSegTrans = tss.Length;
						space = 0;
					}
				}
				if (ich >= cumulativeLengthOfBt && ich < cumulativeLengthOfBt + cchSegTrans)
					return segment;
				cumulativeLengthOfBt += cchSegTrans;
			}
			return SegmentsOS.Last();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a character position in the contents of this paragraph, return a character
		/// offset to the start of the free translation for the corresponding segment.
		/// </summary>
		/// <param name="ich">The ich main position.</param>
		/// <param name="btWs">The back translation writing system HVO</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int GetBtPosition(int ich, int btWs)
		{
			int cumulativeLengthOfBt = 0;

			foreach (ISegment segment in SegmentsOS)
			{
				if (ich >= segment.BeginOffset && ich < segment.EndOffset)
					return cumulativeLengthOfBt;
				int cchSeg;
				if (segment.IsLabel)
					cchSeg = segment.Length;
				else
				{
					ITsString tss = segment.FreeTranslation.get_String(btWs);
					cchSeg = (tss == null ? 0 : tss.Length);
				}
				cumulativeLengthOfBt += cchSeg;
			}
			throw new ArgumentOutOfRangeException("ich", ich, "No segment found for requested position.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the segments that are affected by the replacement of the characters in this
		/// paragraph by the characters in the specified paragraph.
		/// </summary>
		/// <param name="ichMinDest">The character index of the start of the affected characters
		/// in this paragraph.</param>
		/// <param name="ichLimDest">The character lim of the end of the affected characters
		/// in this paragraph.</param>
		/// <param name="fromPara">The paragraph where the replacement characters are coming
		/// from.</param>
		/// <param name="ichMinSrc">The character index of the start of the affected characters
		/// in the source paragraph.</param>
		/// <param name="ichLimSrc">The character lim of the end of the affected characters
		/// in the source paragraph.</param>
		/// <param name="fMergeFirstChangedSeg">if set to <c>true</c> the first segment that is
		/// affected by the replace in this paragraph (which would be the last surviving segment
		/// from the original paragraph) will be merged with the first affected segment from the
		/// source paragraph.</param>
		/// ------------------------------------------------------------------------------------
		private void UpdateSegsForReplace(int ichMinDest, int ichLimDest, IStTxtPara fromPara,
			int ichMinSrc, int ichLimSrc, bool fMergeFirstChangedSeg)
		{
			// Update the segments for the copied text from the segments of the source text
			// First we need to get the segments for the source and destination paragraphs that
			// will be copied.
			int isegMinDest, isegLimDest, isegMinLowDest;
			int isegMinSrc, isegLimSrc, isegMinLowSrc;
			GetSegmentsOverlapping(this, ichMinDest, ichLimDest, out isegMinLowDest, out isegMinDest, out isegLimDest);
			GetSegmentsOverlapping(fromPara, ichMinSrc, ichLimSrc, out isegMinLowSrc, out isegMinSrc, out isegLimSrc);

			FixBtSegsForReplace(isegMinDest, fromPara, isegMinSrc, isegLimSrc, fMergeFirstChangedSeg);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the range of segments which are treated as a single unit for diffing based on a
		/// character range. The segments in question are from the end of a previous to the start
		/// of a following label segment, or to the start or end of the paragraph.
		/// A null paragraph is treated as having no BT segments
		/// (return isegMinLow = isegMinHigh = isegLim = 0). If the character range is exactly
		/// at the end of a verse, it is ambiguous whether to include that verse. Accordingly we
		/// return two isegMin values, a low one including the ambiguous verse, and a high one not
		/// including it. If ichMin is not ambiguous they are the same.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected static void GetSegmentsOverlapping(IStTxtPara para, int ichMin, int ichLim,
			out int isegMinLow, out int isegMinHigh, out int isegLim)
		{
			if (para == null)
			{
				isegMinLow = isegLim = isegMinHigh = 0;
				return;
			}
			isegMinLow = isegMinHigh = 0; // by default we include all segments (e.g., for introduction or heading para).
			isegLim = para.SegmentsOS.Count;

			for (int i = 0; i < para.SegmentsOS.Count; i++)
			{
				ISegment seg = para.SegmentsOS[i];
				ISegment prevSeg = i > 0 ? para.SegmentsOS[i - 1] : null;
				// TODO (FWR-1124): Originally (when this code was in the BookMerger), we treated a whole
				// verse as one segment group. However, moving the code to FDO complicates that
				// behavior to the point that its actually much easier to deal with each
				// individual segment instead of a whole verse.
				if (true /*HasChapVerseStyle(seg)*/)
				{
					int segBeginOffset = seg.BeginOffset;
					string prevSegBaseLine = (prevSeg != null) ? prevSeg.BaselineText.Text : null;
					if (prevSegBaseLine != null && prevSegBaseLine[prevSegBaseLine.Length - 1] == ' ')
					{
						// If the previous segment ends with white space, we want to adjust our
						// begin offset to include that whitespace. The reason for this is that
						// moved text that starts with whitespace doesn't create a new segment
						// for the whitespace so when getting included segments around the specified
						// character range near a segment boundary containing whitespace, we can't
						// include trailing whitespace in our calculations. (FWR-1727)
						while (segBeginOffset > 0 && para.Contents.Text[segBeginOffset - 1] == ' ')
							segBeginOffset--;
					}

					if (seg.EndOffset <= ichMin)
						isegMinLow = isegMinHigh = i + 1; // found a preceding label
					else if (ichMin >= segBeginOffset)
					{
						isegMinHigh = i; // make sure we don't include any segments from a verse entirely before the start of the change.
						// Except if we're exactly at the end of a verse we may still want to include them in the 'low' range.
						if (ichMin != segBeginOffset)
							isegMinLow = i;
					}
					if (seg.BeginOffset >= ichLim)
					{
						isegLim = i;
						break;
					}
				}
			}
			// End of paragraph is like finding a verse boundary: if ichMin is exactly there, we need
			// to make sure isegMinHigh is at the end of the last segment.
			if (ichMin == para.Contents.Length)
				isegMinHigh = para.SegmentsOS.Count;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Replace the segments from iSegMinDest to isegLimDest in this paragraph with the
		/// ones from isegMinSrc to isegLimSrc in paraSrc.
		/// </summary>
		/// <param name="isegMinDest">The minimum segment that was replaced in the destination.</param>
		/// <param name="paraSrc">The source para.</param>
		/// <param name="isegMinSrc">The minimum segment from which .</param>
		/// <param name="isegLimSrc">The iseg lim SRC.</param>
		/// <param name="fMergeFirstChangedSeg">if set to <c>true</c> the first segment that is
		/// affected by the replace in this paragraph (which would be the last surviving segment
		/// from the original paragraph) will be merged with the first affected segment from the
		/// source paragraph.</param>
		/// ------------------------------------------------------------------------------------
		private void FixBtSegsForReplace(int isegMinDest, IStTxtPara paraSrc, int isegMinSrc,
			int isegLimSrc, bool fMergeFirstChangedSeg)
		{
			int iSegDest = Math.Min(isegLimSrc - isegMinSrc + isegMinDest - 1, SegmentsOS.Count - 1);
			for (int i = isegLimSrc - 1; i >= isegMinSrc && iSegDest >= isegMinDest; i--)
			{
				ISegment segSrc = paraSrc.SegmentsOS[i];
				ISegment segDest = SegmentsOS[iSegDest];
				bool fMerging = fMergeFirstChangedSeg && i == isegMinSrc;
				// In this range the text should be identical so we can just copy the segments.
				((Segment)segDest).AssimilateSegment(segSrc, fMerging);
				iSegDest--;
			}
		}
		#endregion

		/// <summary>
		/// A convenient shortcut for setting the analysis at a particular point in a particular
		/// segment. The paragraph must already have sufficient segments and analyses, except that
		/// the routine may be used to add one more analysis to a list that is too short.
		/// </summary>
		/// <param name="iSegment"></param>
		/// <param name="iAnalysis"></param>
		/// <param name="analysis"></param>
		internal void SetAnalysis(int iSegment, int iAnalysis, IAnalysis analysis)
		{
			var seg = SegmentsOS[iSegment];
			if (iAnalysis == seg.AnalysesRS.Count)
				seg.AnalysesRS.Add(analysis);
			seg.AnalysesRS.Replace(iAnalysis, 1, new ICmObject[] {analysis});
		}

		/// <summary>
		/// Get all the analyses in the paragraph.
		/// </summary>
		public IEnumerable<IAnalysis> Analyses
		{
			get { return SegmentsOS.SelectMany(seg => seg.AnalysesRS); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Need this version, because the ICmObjectInternal.ValidateAddObject version
		/// can't be virtual, and we want subclasses to be able to override the method.
		/// </summary>
		/// <exception cref="InvalidOperationException">The addition is not valid</exception>
		/// ------------------------------------------------------------------------------------
		protected override void ValidateAddObjectInternal(AddObjectEventArgs e)
		{
			base.ValidateAddObjectInternal(e);

			if (e.Flid == StTxtParaTags.kflidTranslations)
			{
				if (Cache.FullyInitializedAndReadyToRock && TranslationsOC.Count > 0)
					throw new InvalidOperationException("Can not have more than one CmTranslation for a paragraph (kind of makes you wonder why we have a collection)");

				ICmTranslation trans = (ICmTranslation)e.ObjectAdded;
				if (trans.TypeRA != null && trans.TypeRA.Guid != LangProjectTags.kguidTranBackTranslation)
					throw new ArgumentException("Back translations are the only type of translation allowed for paragraphs");
			}
		}
	}
}
