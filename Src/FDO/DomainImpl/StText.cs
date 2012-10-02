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
using System.Linq;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.DomainImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Added static methods to this class
	/// to support transferring of paragraphs between different instances of StText.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal partial class StText
	{
		private int m_TitleFlid;
		private bool m_fIsTranslation;

		#region Overrides of CmObject
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden to allow special handling to prevent moving Scripture paragraphs to a
		/// different book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ValidateAddObjectInternal(AddObjectEventArgs e)
		{
			if (e.Flid == StTextTags.kflidParagraphs &&
				e.ObjectAdded.Hvo != (int)SpecialHVOValues.kHvoUninitializedObject)
			{
				IScrBook originalOwningBook = e.ObjectAdded.OwnerOfClass<IScrBook>();
				if (originalOwningBook != null)
				{
					// this StText is the one that is to be the new owner.
					IScrBook newOwningBook = OwnerOfClass<IScrBook>();
					if (newOwningBook != originalOwningBook)
						throw new InvalidOperationException("Scripture pargraphs cannot be moved between books.");
				}
			}
			base.ValidateAddObjectInternal(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete any discourse charts that reference the text being deleted.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnBeforeObjectDeleted()
		{
			var data = Cache.LangProject.DiscourseDataOA;
			if (data == null || data.ChartsOC == null || data.ChartsOC.Count == 0)
				return;

			// Enhance GordonM: When we add other subclasses of DsChart, we'll need to delete them too.
			var chartsToDelete = data.ChartsOC.Cast<IDsConstChart>().Where(chart => chart != null &&
				chart.BasedOnRA == this).ToList();

			foreach (var chart in chartsToDelete)
				Cache.LangProject.DiscourseDataOA.ChartsOC.Remove(chart);

			base.OnBeforeObjectDeleted();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This version overrides the CmObject version. Handles various Scripture side effects
		/// and TagsOC modifications.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void AddObjectSideEffectsInternal(AddObjectEventArgs e)
		{
			// Prevent accidentally adding a plain StTxtPara to a Scripture text...
			// the ScrTxtPara subclass should always be used.
			// But watch out for the case (FWR-3477) where paragraph is owned by a CmPossibility first!
			if (e.Flid == StTextTags.kflidParagraphs
				&& (OwnerOfClass<IScripture>() != null && !(Owner is ICmPossibility))
				&& !(e.ObjectAdded is ScrTxtPara))
				throw new ArgumentException("Can not add object of type " + e.ObjectAdded.GetType() +
					" to this StText");

			base.AddObjectSideEffectsInternal(e);

			if (e.Flid == StTextTags.kflidTags)
				TextTagCollectionChanges(true, e.ObjectAdded);

			if (e.Flid == StTextTags.kflidParagraphs)
			{
				if (e.ObjectAdded is IScrTxtPara)
				{
					IScrSection section = OwnerOfClass<IScrSection>();
					if (section != null)
						((ScrSection)section).AdjustReferences();
				}

				if (e.Index == ParagraphsOS.Count - 1 && e.Index > 0)
				{
					int flid = Services.GetInstance<Virtuals>().StParaIsFinalParaInText;
					Services.GetInstance<IUnitOfWorkService>().RegisterVirtualAsModified(
						ParagraphsOS[e.Index - 1], flid, true, false);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This version overrides the CmObject version. Handles various Scripture side effects
		/// and TagsOC modifications.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void RemoveObjectSideEffectsInternal(RemoveObjectEventArgs e)
		{
			base.RemoveObjectSideEffectsInternal(e);

			if (e.Flid == StTextTags.kflidTags)
				TextTagCollectionChanges(false, e.ObjectRemoved);

			if (e.Flid == StTextTags.kflidParagraphs)
			{
				if (e.ObjectRemoved is IScrTxtPara)
				{
					IScrSection section = OwnerOfClass<IScrSection>();
					if (section != null)
						((ScrSection)section).AdjustReferences((IScrTxtPara)e.ObjectRemoved);
				}

				if (e.Index == ParagraphsOS.Count - 1 && e.Index > 0)
				{
					int flid = Services.GetInstance<Virtuals>().StParaIsFinalParaInText;
					Services.GetInstance<IUnitOfWorkService>().RegisterVirtualAsModified(
						ParagraphsOS[e.Index - 1], flid, false, true);
				}
			}
		}

		/// <summary>
		/// Used to register changes to a Text's list of TextTags.
		/// Probably unnecessary due to StText.TagsOC being owned.
		/// </summary>
		/// <param name="fAdd">true if adding an object to AppliesTo, false if removing.</param>
		/// <param name="modObj">object being added or removed</param>
		private void TextTagCollectionChanges(bool fAdd, ICmObject modObj)
		{
			// TODO: This may be needed when we do multi-layer tagging,
			// in the case where a tag is being deleted that is pointed to by another tag.
			var ttag = modObj as ITextTag;
			var newNumberOfTags = fAdd ? 1 : 0;
			var oldNumberOfTags = fAdd ? 0 : 1;
		}
		#endregion

		#region Misc. stuff
		/// <summary>
		/// Initialize the DateModified values in the constructor.
		/// </summary>
		partial void SetDefaultValuesInConstruction()
		{
			m_DateModified = DateTime.Now;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the <see cref="T:SIL.FieldWorks.FDO.IStTxtPara"/> with the specified index.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IStTxtPara this[int i]
		{
			get	{ return (IStTxtPara)ParagraphsOS[i]; }
		}

		/// <summary>
		/// The Title for the StStext is usually the Name of its owning Text, but Scripture has a different strategy.
		/// </summary>
		[VirtualProperty(CellarPropertyType.MultiUnicode)]
		public IMultiAccessorBase Title
		{
			get
			{
				if (m_TitleFlid == 0)
					m_TitleFlid = Cache.MetaDataCache.GetFieldId("StText", "Title", false);
				return new VirtualStringAccessor(this, m_TitleFlid, TitleForWs);
			}
		}

		private ITsString TitleForWs(int ws)
		{
			ITsString tssTitle = null;
			if (ScriptureServices.ScriptureIsResponsibleFor(this))
			{
				Scripture scripture = Cache.LangProject.TranslatedScriptureOA as Scripture;
				if (scripture != null)
				{
					tssTitle = scripture.BookChapterVerseBridgeAsTss(this, ws);
					if (OwningFlid == ScrSectionTags.kflidHeading)
					{
						string sFmt = Strings.ksSectionHeading;
						int iMin = sFmt.IndexOf("{0}");
						if (iMin < 0)
						{
							tssTitle = m_cache.MakeUserTss(sFmt);
						}
						else
						{
							ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
							if (iMin > 0)
								tisb.AppendTsString(m_cache.MakeUserTss(sFmt.Substring(0, iMin)));
							tisb.AppendTsString(tssTitle);
							if (iMin + 3 < sFmt.Length)
								tisb.AppendTsString(m_cache.MakeUserTss(sFmt.Substring(iMin + 3)));
							tssTitle = tisb.GetString();
						}
					}
				}
			}
			else if (Owner is IText)
			{
				IText text = Owner as IText;
				tssTitle = text.Name.get_String(ws);
			}
			else
			{
				// throw?
			}
			if (tssTitle == null)
				tssTitle = TsStrFactoryClass.Create().EmptyString(Cache.DefaultAnalWs);
			return tssTitle;
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
					IStTxtPara para = this[0];
					if (para != null)
						return para.Contents == null || para.Contents.Length == 0;
				}
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a new paragraph to this StText. The created paragraph will be of the correct
		/// type for this StText (i.e. a ScrTxtPara or a StTxtPara)
		/// </summary>
		/// <param name="paraStyleName">The name of the paragraph style to use for the new
		/// paragraph</param>
		/// <returns>The created paragraph.</returns>
		/// ------------------------------------------------------------------------------------
		public IStTxtPara AddNewTextPara(string paraStyleName)
		{
			return CreateNewPara(ParagraphsOS.Count, paraStyleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a new paragraph into this StText at the specified position. The created
		/// paragraph will be of the correct type for this StText (i.e. a ScrTxtPara or a
		/// StTxtPara)
		/// </summary>
		/// <param name="iPos">The index to insert the paragraph.</param>
		/// <param name="paraStyleName">The name of the paragraph style to use for the new
		/// paragraph</param>
		/// <returns>The created paragraph.</returns>
		/// ------------------------------------------------------------------------------------
		public IStTxtPara InsertNewTextPara(int iPos, string paraStyleName)
		{
			return CreateNewPara(iPos, paraStyleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new paragraph of the correct type for this StText.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IStTxtPara CreateNewPara(int iPos, string paraStyleName)
		{
			int ws = MainWritingSystem;
			IStTxtPara newPara;
			// Watch out for the case (FWR-3477) where paragraph is owned by a CmPossibility first!
			if (OwnerOfClass<IScripture>() != null && !(Owner is ICmPossibility))
				newPara = Services.GetInstance<IScrTxtParaFactory>().CreateWithStyle(this, iPos, paraStyleName);
			else
			{
				newPara = Services.GetInstance<IStTxtParaFactory>().Create();
				ParagraphsOS.Insert(iPos, newPara);
				if (!string.IsNullOrEmpty(paraStyleName))
					newPara.StyleName = paraStyleName;
			}
			if (ws != 0)
				newPara.Contents = Services.GetInstance<ITsStrFactory>().EmptyString(ws);
			return newPara;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new paragraph at the end of an StText.
		/// </summary>
		/// <param name="paragraphIndex"></param>
		/// <param name="paraStyleName"></param>
		/// <param name="tss"></param>
		/// <returns>The created paragraph.</returns>
		/// ------------------------------------------------------------------------------------
		public IStTxtPara InsertNewPara(int paragraphIndex, string paraStyleName, ITsString tss)
		{
			IStTxtPara para = (paragraphIndex < 0) ? AddNewTextPara(paraStyleName) :
				InsertNewTextPara(paragraphIndex, paraStyleName);
			para.Contents = tss;
			return para;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete a paragraph from this StText.
		/// </summary>
		/// <param name="para">paragraph to delete</param>
		/// ------------------------------------------------------------------------------------
		public void DeleteParagraph(IStTxtPara para)
		{
			// delete any linked objects (footnotes or pictures)
			((StTxtPara)para).RemoveOwnedObjectsForString(0, para.Contents.Length);
			// delete the paragraph
			((IStText)para.Owner).ParagraphsOS.Remove(para);
		}
		#endregion

		#region Find-Footnote methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the next footnote starting from the given paragraph and position.
		/// </summary>
		/// <param name="iPara">Index of paragraph to start search.</param>
		/// <param name="ich">Character index to start search.</param>
		/// <param name="fSkipCurrentPosition">If <c>true</c> we search forwards starting with the
		/// run after ich, otherwise we start with the current run.</param>
		/// <returns>Next footnote in string, or <c>null</c> if footnote can't be found.</returns>
		/// ------------------------------------------------------------------------------------
		public IScrFootnote FindNextFootnote(ref int iPara, ref int ich,
			bool fSkipCurrentPosition)
		{
			if (ParagraphsOS.Count == 0)
				return null;

			IScrTxtPara para = (IScrTxtPara)ParagraphsOS[iPara];
			IScrFootnote footnote = para.FindNextFootnoteInContents(ref ich, fSkipCurrentPosition);
			while (footnote == null && iPara < ParagraphsOS.Count - 1)
			{
				iPara++;
				ich = 0;
				para = (IScrTxtPara)ParagraphsOS[iPara];
				footnote = para.FindNextFootnoteInContents(ref ich, false);
			}
			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches for first footnote reference in the StText.
		/// </summary>
		/// <param name="iPara">0-based index of paragraph where footnote was found</param>
		/// <param name="ich">0-based character offset where footnote ORC was found</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IScrFootnote FindFirstFootnote(out int iPara, out int ich)
		{
			iPara = ich = 0;
			return FindNextFootnote(ref iPara, ref ich, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches for last footnote reference in the StText.
		/// </summary>
		/// <param name="iPara">0-based index of paragraph where footnote was found</param>
		/// <param name="ich">0-based character offset where footnote ORC was found</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IScrFootnote FindLastFootnote(out int iPara, out int ich)
		{
			ich = -1;
			if (ParagraphsOS.Count == 0)
			{
				iPara = -1;
				return null;
			}

			iPara = ParagraphsOS.Count - 1;
			return FindPreviousFootnote(ref iPara, ref ich, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the previous footnote starting from the given paragraph and character position.
		/// </summary>
		/// <param name="iPara">Index of paragraph to start search, or -1 to start search in
		/// last paragraph.</param>
		/// <param name="ich">Character index to start search, or -1 to start at the end of
		/// the paragraph.</param>
		/// <param name="fSkipCurrentPosition">If <c>true</c> we search backwards starting with the
		/// run before ich, otherwise we start with the run ich is in.</param>
		/// <returns>Last footnote in string, or <c>null</c> if footnote can't be found.</returns>
		/// ------------------------------------------------------------------------------------
		public IScrFootnote FindPreviousFootnote(ref int iPara, ref int ich,
			bool fSkipCurrentPosition)
		{
			if (ParagraphsOS.Count <= iPara)
				return null;
			if (iPara == -1)
				iPara = ParagraphsOS.Count - 1;
			IScrTxtPara para = (IScrTxtPara)ParagraphsOS[iPara];
			IScrFootnote footnote = para.FindPrevFootnoteInContents(ref ich, fSkipCurrentPosition);
			while (footnote == null && iPara > 0)
			{
				para = (IScrTxtPara)ParagraphsOS[--iPara];
				ich = para.Contents.Length;
				footnote = para.FindPrevFootnoteInContents(ref ich, false);
			}
			return footnote;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows this StText to perform any side-effects when the contents of one of its
		/// paragraphs changes.
		/// </summary>
		/// <param name="stTxtPara">The changed paragraph.</param>
		/// <param name="originalValue">The original value.</param>
		/// <param name="newValue">The new value.</param>
		/// ------------------------------------------------------------------------------------
		internal virtual void OnParagraphContentsChanged(IStTxtPara stTxtPara,
			ITsString originalValue, ITsString newValue)
		{
		}

		/// <summary>
		/// The Source for a Translated StStext.
		/// </summary>
		[VirtualProperty(CellarPropertyType.MultiUnicode)]
		public IMultiAccessorBase Source
		{
			get
			{
				return new VirtualStringAccessor(this,
					Cache.MetaDataCache.GetFieldId("StText", "Source", false),
					SourceOfTextForWs);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the comment, which is the Description of the owning Text, if any. (For
		/// Scripture, the comment is always null.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[VirtualProperty(CellarPropertyType.MultiUnicode)]
		public IMultiAccessorBase Comment
		{
			get
			{
				return new VirtualStringAccessor(this,
					Cache.MetaDataCache.GetFieldId("StText", "Comment", false),
					CommentForWs);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Used to get Text.Source
		/// </summary>
		/// <param name="ws">The ws.</param>
		/// ------------------------------------------------------------------------------------
		private ITsString SourceOfTextForWs(int ws)
		{
		return OwningFlid == TextTags.kflidContents ? ((IText)Owner).Source.get_String(ws) : Cache.TsStrFactory.EmptyString(ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Used to get Text.Description
		/// </summary>
		/// <param name="ws">The ws.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ITsString CommentForWs(int ws)
		{
			return OwningFlid == TextTags.kflidContents ? ((CmMajorObject)Owner).Description.get_String(ws) : Cache.TsStrFactory.EmptyString(ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Flag whether this StText contains translated material.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[VirtualProperty(CellarPropertyType.Boolean)]
		public bool IsTranslation
		{
			get
			{
				bool fIsTranslation;
				if (ScriptureServices.ScriptureIsResponsibleFor(this))
				{
					// we'll consider everything but footnotes a translation.
					fIsTranslation = OwningFlid != ScrBookTags.kflidFootnotes;
				}
				else if (OwningFlid == TextTags.kflidContents)
				{
					var text = (IText) Owner;
					fIsTranslation = text.IsTranslated;
				}
				else
				{
					// throw?
					fIsTranslation = m_fIsTranslation;
				}
				return fIsTranslation;
			}
			set
			{
				m_fIsTranslation = value;
				if (OwningFlid == TextTags.kflidContents)
				{
					var text = (IText) Owner;
					text.IsTranslated = value;
				}
			}
		}

		private int TitleAbbreviationFlid
		{
			get
			{
				return Cache.MetaDataCache.GetFieldId("StText", "TitleAbbreviation", false);
			}
		}

		/// <summary>
		/// The TitleAbbreviation for the StStext is Abbreviation of its owning Text, if there is one.
		/// We don't yet have a strategy for Scripture sections.
		/// </summary>
		[VirtualProperty(CellarPropertyType.MultiUnicode)]
		public IMultiAccessorBase TitleAbbreviation
		{
			get
			{
				return new VirtualStringAccessor(this, TitleAbbreviationFlid, TitleAbbreviationForWs);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Used to get Text.Abbreviation
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString TitleAbbreviationForWs(int ws)
		{
			if (Owner is IText)
				return ((IText)Owner).Abbreviation.get_String(ws);
			return Cache.TsStrFactory.EmptyString(ws);
		}

		/// <summary>
		/// Used to get Text.Genres
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceSequence, "CmPossibility")]
		public List<ICmPossibility> GenreCategories
		{
			get
			{
				if (OwningFlid == TextTags.kflidContents)
				{
					return new List<ICmPossibility>(((Text)Owner).GenresRC.ToArray());
				}
				else
				{
					return new List<ICmPossibility>();
				}
			}
		}

		/// <summary>
		/// The primary writing system of the text, which we use to analyze it.
		/// Text in other writing systems is treated as punctuation.
		/// Currently this is taken from the first character of the first paragraph.
		/// We may want an explicit model property to store it.
		/// </summary>
		public int MainWritingSystem
		{
			get
			{
				if (ParagraphsOS.Count == 0)
					return 0;
				int dummy;
				return this[0].Contents.get_Properties(0).GetIntPropValues(
					(int)FwTextPropType.ktptWs, out dummy);
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the shortest, non-abbreviated label for the content of this object.
		/// This is the name that you would want to show up in a chooser list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ShortName
		{
			get { return ShortNameTSS.Text; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an ITsString that represents the shortname of this object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override ITsString ShortNameTSS
		{
			get
			{
				if (OwningFlid == TextTags.kflidContents)
					return Owner.ShortNameTSS;
				return Title.BestVernacularAnalysisAlternative;
			}
		}

		/// <summary>
		/// It's never OK to simply delete an StText. Always delete the owner (if that is legitimate in context).
		/// </summary>
		public override bool CanDelete
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// Get set of unique wordforms in this text
		/// </summary>
		/// <returns>A HashSet that contains zero, or more, unique wordforms occurring in this text.</returns>
		public HashSet<IWfiWordform> UniqueWordforms()
		{
			var wordforms = new HashSet<IWfiWordform>();
			foreach (IStTxtPara para in ParagraphsOS)
			{
				para.CollectUniqueWordforms(wordforms);
			}
			return wordforms;
		}
	}
}
