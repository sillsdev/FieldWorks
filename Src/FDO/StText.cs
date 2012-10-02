// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2004' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: StText.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FDO.Cellar
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Added static methods to this class
	/// to support transferring of paragraphs between different instances of StText.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class StText
	{

		int m_tagParagraphsModifiedTimestamp = 0;
		int m_tagParsedTimestamp = 0;

		#region Initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the text for the paragraph with the specified builders, or create an
		/// empty paragraph if the list of builders is empty.
		/// </summary>
		/// <param name="bldrs">List of paragraph builders</param>
		/// ------------------------------------------------------------------------------------
		public void InitializeText(List<StTxtParaBldr> bldrs)
		{
			if (bldrs.Count == 0)
				return;
			foreach (StTxtParaBldr bldr in bldrs)
			{
				if (bldr != null)
					bldr.CreateParagraph(Hvo);
			}
		}
		#endregion

		#region Static methods to transfer contents between StTexts
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use this method to move the entire contents of one StText to an adjacent one.
		/// </summary>
		/// <param name="fromText">StText from which the contents is moved</param>
		/// <param name="toText">StText to which the contents is moved. This StText should be
		/// empty.</param>
		/// <param name="toIsPreceding">Should equal true if the toText preceeds the fromText.
		/// If true, the moved paragraphs will be appended to the toText.
		/// If false, they will be placed at the beginning of the toText.</param>
		/// ------------------------------------------------------------------------------------
		public static void MoveTextContents(IStText fromText, IStText toText, bool toIsPreceding)
		{
			int iLastFromPara = fromText.ParagraphsOS.Count -1;

			if(toIsPreceding)
				MoveWholeParas(fromText, 0, iLastFromPara, toText, toText.ParagraphsOS.Count);
			else
				MoveWholeParas(fromText, 0, iLastFromPara, toText, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use this method to move part of one StText to an adjacent one.
		/// </summary>
		/// <param name="fromText">StText from which the contents is moved</param>
		/// <param name="toText">StText to which the contents is moved. This StText should be
		/// empty.</param>
		/// <param name="divIndex">Index of last whole paragraph to be moved</param>
		/// <param name="toIsPreceding">Should equal true if the toText preceeds the fromText.
		/// If true, the moved paragraphs will be appended to the toText.
		/// If false, they will be placed at the beginning of the toText.</param>
		/// ------------------------------------------------------------------------------------
		public static void MoveTextParagraphs(IStText fromText, IStText toText,
			int divIndex, bool toIsPreceding)
		{
			int initParaCount = fromText.ParagraphsOS.Count;
			Debug.Assert(divIndex >= 0 && divIndex <= initParaCount - 1);

			if(toIsPreceding)
				//Move initial paragraphs up, appending
				MoveWholeParas(fromText, 0, divIndex, toText, toText.ParagraphsOS.Count);
			else
				//Move final paragraphs down, prepending
				MoveWholeParas(fromText, divIndex, initParaCount - 1, toText, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use this method to move part of one StText to an adjacent one when the fromText
		/// must be left with at least one empty paragraph.
		/// </summary>
		/// <param name="fromText">StText from which the contents is moved</param>
		/// <param name="toText">StText to which the contents is moved. This StText should be
		/// empty.</param>
		/// <param name="divIndex">Index of last whole paragraph to be moved</param>
		/// <param name="toIsPreceding">Should equal true if the toText preceeds the fromText.
		/// If true, the moved paragraphs will be appended to the toText.
		/// If false, they will be placed at the beginning of the toText.</param>
		/// ------------------------------------------------------------------------------------
		public static void MoveTextParagraphsAndFixEmpty(IStText fromText, IStText toText,
			int divIndex, bool toIsPreceding)
		{
			// Save the paragraph props. We might need it to create an empty paragraph.
			ITsTextProps paraPropsSave =
				((StTxtPara) fromText.ParagraphsOS[divIndex]).StyleRules;

			MoveTextParagraphs(fromText, toText, divIndex, toIsPreceding);

			// If fromText is now empty, create a new empty paragraph.
			if (fromText.ParagraphsOS.Count == 0)
			{
				string styleName =
					paraPropsSave.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
				StTxtParaBldr.CreateEmptyPara(fromText.Cache, fromText.Hvo,
					styleName, fromText.Cache.DefaultVernWs);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use this method to move the adjacent part of one StText to the adjacent position
		/// in another.
		/// </summary>
		/// <param name="fromText">StText from which the contents is moved</param>
		/// <param name="toText">StText to which the contents is moved</param>
		/// <param name="divIndex">Index of last partial paragraph to be moved or the first
		/// whole paragraph not moved</param>
		/// <param name="ichDiv">character offset of last character to be moved or zero if
		/// none are to be moved</param>
		/// <param name="toIsPreceding">Should equal true if the toText preceeds the fromText.
		/// If true, the moved text will be appended to the toText.
		/// If false, they will be placed at the beginning of the toText.</param>
		/// ------------------------------------------------------------------------------------
		public static void MovePartialContents(IStText fromText, IStText toText,
			int divIndex, int ichDiv, bool toIsPreceding)
		{
			int iLastFromPara = fromText.ParagraphsOS.Count - 1;
			Debug.Assert((divIndex >= 0) && (divIndex <= iLastFromPara));

			// Set up parameters for whole paragraph movement based on direction of movement
			int iStartAt, iEndAt, iInsertAt, iReferenceEdge;
			if (toIsPreceding)
			{
				//From beginning to para preceding IP, appended
				iStartAt = 0;
				iEndAt = divIndex - 1;
				iInsertAt = toText.ParagraphsOS.Count;
				iReferenceEdge = 0;
			}
			else
			{
				//From para following IP to the end, pre-pended
				iStartAt = (ichDiv > 0) ? divIndex + 1 : divIndex;
				iEndAt = iLastFromPara;
				iInsertAt = 0;
				iReferenceEdge = iLastFromPara;
			}

			// Move the whole paragraphs of fromText to empty toText
			if (divIndex != iReferenceEdge || (ichDiv == 0 && !toIsPreceding))
				MoveWholeParas(fromText, iStartAt, iEndAt, toText, iInsertAt);

			// Move partial paragraph (now in the edge paragraph of the fromText)
			//      to a new paragraph in the toText
			if (ichDiv > 0 || toIsPreceding)
				DivideParaContents(fromText, ichDiv, toText, toIsPreceding);

			if (fromText.ParagraphsOS.Count == 0)
			{
				// We moved all of the paragraphs out of the existing section so we need to
				// create a new paragraph so the user can enter text
				StTxtPara newSectionFirstPara = (StTxtPara)toText.ParagraphsOS[0];
				using (StTxtParaBldr bldr = new StTxtParaBldr(fromText.Cache))
				{
					bldr.ParaProps = newSectionFirstPara.StyleRules;
					bldr.AppendRun(string.Empty,
						StyleUtils.CharStyleTextProps(null, fromText.Cache.DefaultVernWs));
					bldr.CreateParagraph(fromText.Hvo);
				}
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use this method to transfer whole paragraphs from one StText to another.
		/// </summary>
		/// <param name="source">StText from which the paragraphs are moved</param>
		/// <param name="srcIFrst">Index of first paragraph to be moved
		/// </param>
		/// <param name="srcILst">Index of last paragraph to be moved</param>
		/// <param name="destn">StText to which the paragraphs are moved</param>
		/// <param name="dstIndex">Index of paragraph before which paragraphs are to be
		/// inserted</param>
		/// ------------------------------------------------------------------------------------
		private static void MoveWholeParas(IStText source, int srcIFrst, int srcILst,
			IStText destn, int dstIndex)
		{
			int paraFlid = (int)StText.StTextTags.kflidParagraphs;
			source.Cache.MoveOwningSequence(source.Hvo, paraFlid, srcIFrst, srcILst,
				destn.Hvo, paraFlid, dstIndex);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use this method to transfer the beginning part of the first paragraph of an StText
		/// to the end of another or the ending part of the last paragraph of an StText
		/// to the beginning of another. Does nothing if there are no characters to move.
		/// </summary>
		/// <param name="source">StText from which the paragraphs are moved</param>
		/// <param name="ichDiv">Character index of point where paragraph is to be divided
		/// </param>
		/// <param name="destn">StText to which the paragraphs are moved</param>
		/// <param name="moveInitialFragment">
		/// If True, move the paragraph fragment preceding the division point in the
		/// first paragraph of the source text to the end of the destination text;
		/// else, move the paragraph fragment following the division point in the
		/// last paragraph of the source text to the beginning of the destination text.</param>
		/// ------------------------------------------------------------------------------------
		private static void DivideParaContents(IStText source, int ichDiv, IStText destn,
			bool moveInitialFragment)
		{
			int contentFlid = (int)StTxtPara.StTxtParaTags.kflidContents;

			// Set up paragraph indices
			int srcParaIndex, dstParaIndex;
			if (moveInitialFragment)
			{
				// the paragraph fragment to be moved is in the first part of the first paragraph
				srcParaIndex = 0;
				// the moved portion goes to the end of the destination paragraph
				dstParaIndex = destn.ParagraphsOS.Count;
			}
			else
			{
				// the paragraph fragment to be moved is the last part of the last paragraph
				srcParaIndex = source.ParagraphsOS.Count - 1;
				// the moved portion goes to the beginning of the destination paragraph
				dstParaIndex = 0;
			}

			// Set the source paragraph
			IStTxtPara curPara = (IStTxtPara)source.ParagraphsOS[srcParaIndex];
			int ichEndOfPara = curPara.Contents.Length;

			// Set up parameters based on which portion of the divided paragraph must move
			int ichStartMoved, ichEndMoved, ichStartRemainder, ichEndRemainder;
			if (moveInitialFragment)
			{
				ichStartMoved = 0;
				ichEndMoved = ichDiv;
				ichStartRemainder = ichDiv;
				ichEndRemainder = ichEndOfPara;
			}
			else
			{
				ichStartMoved = ichDiv;
				ichEndMoved = ichEndOfPara;
				ichStartRemainder = 0;
				ichEndRemainder = ichDiv;
			}

			//Move partial paragraph if there are characters to move
			if (ichStartMoved < ichEndMoved)
			{
				IStTxtPara newPara = (IStTxtPara)destn.ParagraphsOS.InsertAt(new StTxtPara(), dstParaIndex);
				newPara.StyleRules = curPara.StyleRules;
				ITsString tss = curPara.Contents.UnderlyingTsString;
				ITsStrBldr bldr = tss.GetBldr();
				bldr.Replace(ichStartRemainder, ichEndRemainder, "", null);
				newPara.Contents.UnderlyingTsString = bldr.GetString();
				newPara.Cache.PropChanged(null, PropChangeType.kpctNotifyAll,
					newPara.Hvo, contentFlid, 0, ichEndMoved - ichStartMoved, 0);
				// if an empty paragraph would be left at end of a multiple
				// paragraph text, remove it.
				if (source.ParagraphsOS.Count > 1 && ichStartRemainder == ichEndRemainder)
					source.ParagraphsOS.RemoveAt(srcParaIndex);
				else
				{
					// else remove only the moved contents from the source paragraph
					bldr = tss.GetBldr();
					bldr.Replace(ichStartMoved, ichEndMoved, "", null);
					curPara.Contents.UnderlyingTsString = bldr.GetString();
					curPara.Cache.PropChanged(null, PropChangeType.kpctNotifyAll,
						curPara.Hvo, contentFlid, ichStartMoved, 0, ichEndMoved - ichStartMoved);
				}
			}
		}
		#endregion

		#region Utility Methods for StText Manipulation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use this method to remove the contents (all paragraphs) of an StText.
		/// </summary>
		/// <param name="text">StText from which the contents is removed</param>
		/// ------------------------------------------------------------------------------------
		public static void ClearContents(StText text)
		{
			for (int lastPara = text.ParagraphsOS.Count - 1; lastPara >= 0; lastPara--)
				text.ParagraphsOS.RemoveAt(lastPara);
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A dummy property (Int64) storing the maximum UpdStmp of any StTxtPara in the text, the last time
		/// we ran the parse algorithm.
		/// </summary>
		/// <value>The text parse timestamp flid.</value>
		/// ------------------------------------------------------------------------------------
		private int TextParseTimestampFlid
		{
			get
			{
				if (m_tagParsedTimestamp == 0)
					m_tagParsedTimestamp = TextParseTimestampTag(this.Cache);
				return m_tagParsedTimestamp;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the paragraphs modified timestamp flid.
		/// </summary>
		/// <value>The paragraphs modified timestamp flid.</value>
		/// ------------------------------------------------------------------------------------
		private int ParagraphsModifiedTimestampFlid
		{
			get
			{
				if (m_tagParagraphsModifiedTimestamp == 0)
					m_tagParagraphsModifiedTimestamp = ParagraphsModifiedTimestampTag(this.Cache);
				return m_tagParagraphsModifiedTimestamp;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The value of ParagraphsModifiedTimestamp as of the most recent parse of the text, or zero if it has
		/// not been parsed since startup (or Refresh). It needs to be reparsed if this is not equal to
		/// ParagraphsModifiedTimestamp.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static int TextParseTimestampTag(FdoCache cache)
		{
			return DummyVirtualHandler.InstallDummyHandler(cache.VwCacheDaAccessor,
				"StText", "ParseTimestamp", (int)CellarModuleDefns.kcptTime).Tag;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tag used for a dummy int64 property which records, for texts parsed with no dummy annotations
		/// required, the most recent modify timestamp for any paragraph in the text. This determines
		/// for such texts whether the text requires re-parsing, by comparison with TextParseTimeStamp.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static int ParagraphsModifiedTimestampTag(FdoCache cache)
		{
			return DummyVirtualHandler.InstallDummyHandler(cache.VwCacheDaAccessor,
				"StText", "ParagraphsModifiedTimestamp", (int)CellarModuleDefns.kcptTime).Tag;
		}

		///// <summary>
		///// Tag used for a dummy property of StTxtPara (note: NOT of StText!) which records the actual text of the
		///// paragraph used when it was most recently parsed.
		///// </summary>
		///// <param name="cache"></param>
		///// <returns></returns>
		//private static int ParagraphParseTextTag(FdoCache cache)
		//{
		//    return DummyVirtualHandler.InstallDummyHandler(cache.VwCacheDaAccessor,
		//        "StTxtPara", "ParagraphParseText", (int)CellarModuleDefns.kcptTime).Tag;
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Virtual Property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MultiStringAccessor Title
		{
			get
			{
				MultiStringVirtualHandler vh = BaseVirtualHandler.GetInstalledHandler(Cache, "StText", "Title") as MultiStringVirtualHandler;
				return new MultiStringAccessor(m_cache, m_hvo, vh.Tag, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Used to get Text.Abbreviation
		/// </summary>
		/// <param name="ws">The ws.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ITsString TitleAbbreviationForWs(int ws)
		{
			if (OwningFlid == (int)FDO.Ling.Text.TextTags.kflidContents)
			{
				return m_cache.GetMultiStringAlt(OwnerHVO, (int)FDO.Ling.Text.TextTags.kflidAbbreviation, ws);
			}
			else
			{
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use to get Text.IsTranslated
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsTranslation
		{
			get
			{
				// modelclass="StText" virtualfield="IsTranslation"
				IVwVirtualHandler vh = Cache.GetVirtualProperty("StText", "IsTranslation");
				return Cache.GetBoolProperty(this.Hvo, vh.Tag);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use to test if StText is empty
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsEmpty
		{
			get
			{
				if (ParagraphsOS.Count == 1)
				{
					StTxtPara para = ParagraphsOS[0] as StTxtPara;
					if (para != null)
						return para.Contents == null || para.Contents.Length == 0;
				}
				return false;
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Used to get Text.Source
		/// </summary>
		/// <param name="ws">The ws.</param>
		/// ------------------------------------------------------------------------------------
		public ITsString SourceOfTextForWs(int ws)
		{
			if (OwningFlid == (int)FDO.Ling.Text.TextTags.kflidContents)
			{
				return m_cache.GetMultiStringAlt(OwnerHVO, (int)FDO.Ling.Text.TextTags.kflidSource, ws);
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Used to get Text.Genres
		/// </summary>
		public List<int> GenreCategories
		{
			get
			{
				if (OwningFlid == (int)FDO.Ling.Text.TextTags.kflidContents)
				{
					return new List<int>((Owner as Text).GenresRC.HvoArray);
				}
				else
				{
					return new List<int>();
				}
			}
		}

		/// <summary>
		/// Used to get Text.Description
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		public ITsString CommentForWs(int ws)
		{
			if (OwningFlid == (int)FDO.Ling.Text.TextTags.kflidContents)
			{
				return m_cache.GetMultiStringAlt(OwnerHVO, (int)CmMajorObject.CmMajorObjectTags.kflidDescription, ws);
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		///
		/// </summary>
		public override string ShortName
		{
			get
			{
				return ShortNameTSS.Text;
			}
		}

		/// <summary>
		///
		/// </summary>
		public override ITsString ShortNameTSS
		{
			get
			{
				if (this.OwningFlid == (int)FDO.Ling.Text.TextTags.kflidContents)
					return (this.Owner as IText).ShortNameTSS;
				return Title.BestVernacularAnalysisAlternative;
			}
		}

		/// <summary>
		/// Currently only support deleting an StText from a Text. This prevents FLEx from possibly trying to delete scripture StText objects.
		/// </summary>
		public override bool CanDelete
		{
			get
			{
				return this.OwningFlid == (int)FDO.Ling.Text.TextTags.kflidContents;
			}
		}

		// This is an unfortunate kludge until we find a way
		// to work around the separation of ScrFdo into a DLL that references this.
		const int kflidScrSectContent = 3005002;

		/// <summary>
		/// we want to delete the owning Text if we delete its StText
		/// </summary>
		/// <param name="objectsToDeleteAlso"></param>
		/// <param name="state"></param>
		public override void DeleteObjectSideEffects(SIL.FieldWorks.Common.Utils.Set<int> objectsToDeleteAlso,
			SIL.FieldWorks.Common.Controls.ProgressState state)
		{
			if (this.OwningFlid == (int)FDO.Ling.Text.TextTags.kflidContents)
			{
					objectsToDeleteAlso.Add(this.OwnerHVO);	// delete the owning Text as well.
					DeleteCharts(this.Hvo, objectsToDeleteAlso, state);
			}
			// Wasn't sure we wanted to delete the owning ScrSection in this case.
			if (this.OwningFlid == kflidScrSectContent)
				DeleteCharts(this.Hvo, objectsToDeleteAlso, state);
			base.DeleteObjectSideEffects(objectsToDeleteAlso, state);
		}

		private void DeleteCharts(int hvoStText, Set<int> objectsToDeleteAlso,
			SIL.FieldWorks.Common.Controls.ProgressState state)
		{
			// Enhance GordonM: When we add other subclasses of DsChart, we'll need to delete them too.
			if (m_cache.LangProject.DiscourseDataOA == null)
				return;
			if (m_cache.LangProject.DiscourseDataOA.ChartsOC == null)
				return;
			if (m_cache.LangProject.DiscourseDataOA.ChartsOC.Count == 0)
				return;
			state.SetMilestone(Strings.ksDeletingCharts);
			foreach (DsConstChart chart in m_cache.LangProject.DiscourseDataOA.ChartsOC)
			{
				if (chart.BasedOnRAHvo == hvoStText)
				{
					// We've found a chart on the Text that's going away.
					foreach (CmIndirectAnnotation ccr in chart.RowsRS)
					{
						objectsToDeleteAlso.AddRange(ccr.AppliesToRS.HvoArray);
						objectsToDeleteAlso.Add(ccr.Hvo);
					}
					objectsToDeleteAlso.Add(chart.Hvo);
					state.Breath();
				}
			}
		}

		/// <summary>
		/// A text "IsUpToDate" when it has been parsed by ParagraphParser after being modified.
		/// </summary>
		/// <returns></returns>
		public bool IsUpToDate()
		{
			long lastParseTimestamp = this.LastParsedTimestamp;
			long lastModifyTimestamp = this.ParagraphsModifiedTimestamp;
			if (lastParseTimestamp >= lastModifyTimestamp)
			{
				// reset the timestamp, so that we can always test the latest state.
				ClearLastModifiedTimestamp();
				return true;
			}
			else
			{
				// keep ParagraphsModifiedTimestamp, so we can reuse it when we RecordParseTimestamp.
				// We should continue to be Out of Date, until we Parse again.
				return false;
			}

			// This old code is worth keeping, in case we ever get back to not needing to reparse
			// paragraphs that are entirely analyzed, but for now, we need the approach above (e.g.,
			// because of TwficRealForm issues).
			//string sql = "exec CountUpToDateParas " + CmAnnotationDefn.ProcessTime(this.Cache).Hvo
			//    + ", " + this.Hvo;
			//int cUpToDate;
			//DbOps.ReadOneIntFromCommand(this.Cache, sql, null, out cUpToDate);
			//return cUpToDate == this.Cache.MainCacheAccessor.get_VecSize(this.Hvo, (int)StText.StTextTags.kflidParagraphs);
		}

		/// <summary>
		/// Clearing this will force getting the latest ParagraphsModifiedTimestamp from the database.
		/// </summary>
		private void ClearLastModifiedTimestamp()
		{
			ParagraphsModifiedTimestamp = 0;
		}

		/// <summary>
		/// Gets the last modified timestamp of the paragraphs owned by the text.
		/// </summary>
		private long ParagraphsModifiedTimestamp
		{
			get
			{
				long paragraphsModifiedTimestamp = 0;
				ISilDataAccess sda = Cache.MainCacheAccessor;
				if (sda.get_IsPropInCache(this.Hvo, this.ParagraphsModifiedTimestampFlid, (int)CellarModuleDefns.kcptTime, 0))
				{
					paragraphsModifiedTimestamp = sda.get_Int64Prop(this.Hvo, this.ParagraphsModifiedTimestampFlid);
					// if it's not equal to zero, we'll return it, otherwise, we want to load it from the database.
					if (paragraphsModifiedTimestamp != 0)
						return paragraphsModifiedTimestamp;
				}
				string sql = string.Format("select max(UpdStmp) from StTxtPara_ where owner$ = {0}", this.Hvo);
				paragraphsModifiedTimestamp = DbOps.ReadOneLongFromCommand(this.Cache, sql, null);
				// Cache this for later use.
				ParagraphsModifiedTimestamp = paragraphsModifiedTimestamp;
				return paragraphsModifiedTimestamp;
			}
			set
			{
				Cache.VwCacheDaAccessor.CacheInt64Prop(this.Hvo, this.ParagraphsModifiedTimestampFlid, value);
				long paragraphsModifiedTimestamp = Cache.MainCacheAccessor.get_Int64Prop(this.Hvo, this.ParagraphsModifiedTimestampFlid);
				Debug.Assert(value == paragraphsModifiedTimestamp,
					String.Format("Set ParagraphsModifiedTimestamp to {0}, but got {1}", value, paragraphsModifiedTimestamp));
			}
		}

		/// <summary>
		/// The time stamp for the last time we parsed.
		/// Returns 0 if we haven't parsed yet.
		/// </summary>
		public long LastParsedTimestamp
		{
			get
			{
				ISilDataAccess sda = this.Cache.MainCacheAccessor;
				if (sda.get_IsPropInCache(this.Hvo, this.TextParseTimestampFlid, (int)CellarModuleDefns.kcptTime, 0))
				{
					return sda.get_Int64Prop(this.Hvo, this.TextParseTimestampFlid);
				}
				return 0;
			}
			set
			{
				this.Cache.VwCacheDaAccessor.CacheInt64Prop(this.Hvo, this.TextParseTimestampFlid, value);
			}
		}

		/// <summary>
		/// Set after parsing a text with ParagraphParser.
		/// </summary>
		public void RecordParseTimestamp()
		{
			LastParsedTimestamp = this.ParagraphsModifiedTimestamp;
			// Clear the last modified timestamp, so the next time we test IsUpToDate, it'll be from the
			// database.
			ClearLastModifiedTimestamp();
		}

		/// <summary>
		/// Record the current parse timestamp of a whole collection of StTexts
		/// Equivalent to calling RecordParseTimestamp on each of them.
		/// </summary>
		/// <param name="texts"></param>
		public static void RecordParseTimestamps(List<IStText> texts)
		{
			if (texts.Count == 0)
				return;
			FdoCache cache = texts[0].Cache;
			int[] targetHvos = new int[texts.Count];
			for (int i = 0; i < targetHvos.Length; i++)
				targetHvos[i] = texts[i].Hvo;
			int index = 0;
			string Hvos = DbOps.MakePartialIdList(ref index, targetHvos);
			string whereClause = "";
			if (index == targetHvos.Length)
			{
				// If we can make a single where clause we'll do it; otherwise do them all.
				whereClause = " where Owner$ in (" + Hvos + ")";
			}
			string sql = "select owner$, max(UpdStmp) from StTxtPara_ " + whereClause + " group by owner$";
			IDbColSpec dcs = DbColSpecClass.Create();
			dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);
			int modifyTimestampTag = ParagraphsModifiedTimestampTag(cache);
			dcs.Push((int)DbColType.koctInt64, 1, modifyTimestampTag, 0);
			cache.VwOleDbDaAccessor.Load(sql, dcs, 0, 0, null, false);

			ISilDataAccess sda = cache.MainCacheAccessor;
			foreach (StText text in texts)
			{
				// Much of the logic of RecordParseTimestamp, but can assume modify timestamp is already loaded.
				text.LastParsedTimestamp = sda.get_Int64Prop(text.Hvo, modifyTimestampTag);
				text.ClearLastModifiedTimestamp();
			}
		}

		///// <summary>
		///// Return true if it is necessary to reload the contents of the text into the cache
		///// in order to have an up-to-date parse of the text. Assumes that ParagraphsModifiedTimestamp
		///// is either not cached (and will now be loaded), or has been updated recently enough to be
		///// relied upon.
		///// </summary>
		//bool RequiresReloadForParse
		//{
		//    get
		//    {
		//        return ParagraphsModifiedTimestamp != LastParsedTimestamp;
		//    }
		//}

		///// <summary>
		///// Return true if it is necessary to reparse this text in order to have an accurate concordance
		///// of its contents. Should be kept in sync with NoteParsed.
		///// </summary>
		//bool RequiresParsing
		//{
		//    get
		//    {
		//        if (ParagraphsModifiedTimestamp == LastParsedTimestamp)
		//            return false;
		//        if (LastParsedTimestamp == 0)
		//            return true;
		//    }

		//}

		///// <summary>
		///// Note that the text has been parsed, and save whatever information is needed so we can later
		///// determine whether it need be parsed again. If no dummy annotations were used, we record
		///// a property on the text itself to indicate that we have a complete set of real annotations
		///// for the text as of its current ParagraphsModifiedTimestamp.
		///// </summary>
		///// <param name="hasDummyAnnotations"></param>
		//void NoteParsed(bool hasDummyAnnotations)
		//{
		//    LastParsedTimestamp = ParagraphsModifiedTimestamp;
		//    ClearLastModifiedTimestamp(); // so it must be reloaded for the next test of RequiresParsing
		//}

		/// <summary>
		/// Get list of unique wordforms in this text
		/// </summary>
		/// <returns>A List that contains zero, or more, integers (hvos) for the unique wordforms occurring in this text.</returns>
		public int[] UniqueWordforms()
		{
			Set<int> wordforms = new Set<int>();
			foreach (IStTxtPara para in ParagraphsOS)
			{
				para.CollectUniqueWordforms(wordforms);
			}
			return wordforms.ToArray();
			// string qry = "SELECT DISTINCT wwf.id FROM WfiWordform wwf " +
			//                "JOIN StTxtPara_TextObjects para ON para.dst=wwf.id " +
			//                "JOIN StText_paragraphs text ON text.Src=? " +
			//                "WHERE text.dst=para.src " +
			//                "ORDER BY wwf.id";
			// return DbOps.ReadIntsFromCommand(m_cache, qry, Hvo);
		}
		/// <summary>
		/// Get list of unique wordforms in this text
		/// </summary>
		/// <returns>A List that contains zero, or more, integers (hvos) for the unique wordforms occurring in this text.</returns>
		public void CreateTextObjects()
		{
			foreach (IStTxtPara para in ParagraphsOS)
			{
				para.CreateTextObjects();
			}
			return;
		}

	}

}
