// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FdoScripture.cs
// Responsibility: TE Team
//
// <remarks>
// For change history before July 2009, look in FdoScripture.cs
// </remarks>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.DomainImpl
{
	#region class ScrBookAnnotations
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// ScrBookAnnotations holds and manipulates a collection of Scripture notes.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal partial class ScrBookAnnotations
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Canonical Book number (1-66) corresponding to this collection of annotations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int CanonicalNum
		{
			get { return OwnOrd + 1; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a new note at its correct position in the list.
		/// </summary>
		/// <param name="startRef">beginning reference note refers to</param>
		/// <param name="endRef">ending reference note refers to</param>
		/// <param name="beginObject">id of beginning object note refers to</param>
		/// <param name="endObject">id of ending object note refers to</param>
		/// <param name="guidNoteType">The GUID representing the CmAnnotationDefn to use for
		/// the type</param>
		/// <param name="bldrDiscussion">Para builder to use to build the Discussion
		///  paragraph</param>
		/// <returns>note inserted into annotation list</returns>
		/// ------------------------------------------------------------------------------------
		public IScrScriptureNote InsertImportedNote(BCVRef startRef, BCVRef endRef,
			ICmObject beginObject, ICmObject endObject, Guid guidNoteType, StTxtParaBldr bldrDiscussion)
		{
			return InsertNote(startRef, endRef, beginObject, endObject, guidNoteType,
				0, 0, null, bldrDiscussion, null, null, GetInsertIndexForRef(startRef));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the insert index where a new annotation is inserted when the annotation's
		/// reference is that specified. The index will be at the end of those for the
		/// specified reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int GetInsertIndexForRef(BCVRef startRef)
		{
			IFdoOwningSequence<IScrScriptureNote> notes = NotesOS;

			for (int i = notes.Count - 1; i >= 0; i--)
			{
				IScrScriptureNote note = notes[i];
				if (note.BeginRef <= startRef)
					return i + 1;
			}

			return 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a new note at its correct position in the list.
		/// </summary>
		/// <param name="startRef">beginning reference note refers to</param>
		/// <param name="endRef">ending reference note refers to</param>
		/// <param name="beginObject">id of beginning object note refers to</param>
		/// <param name="endObject">id of ending object note refers to</param>
		/// <param name="guidNoteType">The GUID representing the CmAnnotationDefn to use for
		/// the type</param>
		/// <param name="insertIndex">index where note was inserted into annotation list</param>
		/// <returns>note inserted into annotation list</returns>
		/// ------------------------------------------------------------------------------------
		public IScrScriptureNote InsertNote(BCVRef startRef, BCVRef endRef,
			ICmObject beginObject, ICmObject endObject, Guid guidNoteType, out int insertIndex)
		{
			return InsertNote(startRef, endRef, beginObject, endObject, guidNoteType, -1, 0,
				null, null, null, null, out insertIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a new note at its correct position in the list.
		/// </summary>
		/// <param name="startRef">beginning reference note refers to</param>
		/// <param name="endRef">ending reference note refers to</param>
		/// <param name="beginObject">id of beginning object note refers to</param>
		/// <param name="endObject">id of ending object note refers to</param>
		/// <returns>note inserted into annotation list</returns>
		/// <param name="guidNoteType">The GUID representing the CmAnnotationDefn to use for
		/// the type</param>
		/// ------------------------------------------------------------------------------------
		public IScrScriptureNote InsertNote(BCVRef startRef, BCVRef endRef,
			ICmObject beginObject, ICmObject endObject, Guid guidNoteType)
		{
			int insertIndex;
			return InsertNote(startRef, endRef, beginObject, endObject, guidNoteType, -1, 0,
				null, null,	null, null, out insertIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a new note at its correct position in the list.
		/// </summary>
		/// <param name="startRef">beginning reference note refers to</param>
		/// <param name="endRef">ending reference note refers to</param>
		/// <param name="beginObject">id of beginning object note refers to</param>
		/// <param name="endObject">id of ending object note refers to</param>
		/// <param name="guidNoteType">The GUID representing the CmAnnotationDefn to use for
		/// the type</param>
		/// <param name="bldrQuote">Para builder to use to build the Quote paragraph</param>
		/// <param name="bldrDiscussion">Para builder to use to build the Discussion
		///  paragraph</param>
		/// <param name="bldrRecommendation">Para builder to use to build the
		///  Recommendation paragraph</param>
		/// <param name="bldrResolution">Para builder to use to build the Resolution
		///  paragraph</param>
		/// <returns>note inserted into annotation list</returns>
		/// ------------------------------------------------------------------------------------
		public IScrScriptureNote InsertNote(BCVRef startRef, BCVRef endRef,
			ICmObject beginObject, ICmObject endObject, Guid guidNoteType, StTxtParaBldr bldrQuote,
			StTxtParaBldr bldrDiscussion, StTxtParaBldr bldrRecommendation,
			StTxtParaBldr bldrResolution)
		{
			int insertIndex;
			return InsertNote(startRef, endRef, beginObject, endObject, guidNoteType, -1, 0,
				bldrQuote, bldrDiscussion, bldrRecommendation, bldrResolution, out insertIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a new check result annotation at its correct position in the list.
		/// </summary>
		/// <param name="startRef">beginning reference note refers to</param>
		/// <param name="endRef">ending reference note refers to</param>
		/// <param name="beginObject">id of beginning object note refers to</param>
		/// <param name="endObject">id of ending object note refers to</param>
		/// <param name="checkId">The check id.</param>
		/// <param name="bldrQuote">Para builder to use for the cited text paragraph</param>
		/// <param name="bldrDiscussion">Para builder to use to build the Discussion
		/// paragraph</param>
		/// <returns>note inserted into annotation list</returns>
		/// ------------------------------------------------------------------------------------
		public IScrScriptureNote InsertErrorAnnotation(BCVRef startRef, BCVRef endRef,
			ICmObject beginObject, ICmObject endObject, Guid checkId,
			StTxtParaBldr bldrQuote, StTxtParaBldr bldrDiscussion)
		{
			return InsertNote(startRef, endRef, beginObject, endObject, checkId,
				0, 0, bldrQuote, bldrDiscussion, null, null, GetInsertIndexForRef(startRef));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a new note at its correct position in the list.
		/// </summary>
		/// <param name="startRef">beginning reference note refers to</param>
		/// <param name="endRef">ending reference note refers to</param>
		/// <param name="beginObject">id of beginning object to which annotation refers</param>
		/// <param name="endObject">id of ending object to which annotation refers</param>
		/// <param name="guidNoteType">The GUID representing the CmAnnotationDefn to use for
		/// the type</param>
		/// <param name="startOffset">The starting character offset (or -1 to insert a note
		/// after all existing notes having the same start ref).</param>
		/// <param name="endOffset">The ending character offset.</param>
		/// <param name="bldrQuote">Para builder to use to build the Quote paragraph</param>
		/// <param name="bldrDiscussion">Para builder to use to build the Discussion
		/// paragraph</param>
		/// <param name="bldrRecommendation">Para builder to use to build the
		/// Recommendation paragraph</param>
		/// <param name="bldrResolution">Para builder to use to build the Resolution
		/// paragraph</param>
		/// <param name="insertIndex">out: index where annotation was inserted into
		/// annotation list</param>
		/// <returns>note inserted into annotation list</returns>
		/// ------------------------------------------------------------------------------------
		public IScrScriptureNote InsertNote(BCVRef startRef, BCVRef endRef,
			ICmObject beginObject, ICmObject endObject, Guid guidNoteType,
			int startOffset, int endOffset, StTxtParaBldr bldrQuote, StTxtParaBldr bldrDiscussion,
			StTxtParaBldr bldrRecommendation, StTxtParaBldr bldrResolution, out int insertIndex)
		{
			// Determine where to insert the new annotation.
			insertIndex = (startOffset < 0 ? GetInsertIndexForRef(startRef) :
				GetNewNotesInsertIndex(startRef, beginObject, startOffset));

			if (startOffset < 0)
				startOffset = 0;

			return InsertNote(startRef, endRef, beginObject, endObject, guidNoteType,
				startOffset, endOffset, bldrQuote, bldrDiscussion, bldrRecommendation,
				bldrResolution, insertIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a new note at its correct position in the list.
		/// </summary>
		/// <param name="startRef">beginning reference note refers to</param>
		/// <param name="endRef">ending reference note refers to</param>
		/// <param name="beginObject">id of beginning object to which annotation refers</param>
		/// <param name="endObject">id of ending object to which annotation refers</param>
		/// <param name="guidNoteType">The GUID representing the CmAnnotationDefn to use for
		/// the type</param>
		/// <param name="startOffset">The starting character offset.</param>
		/// <param name="endOffset">The ending character offset.</param>
		/// <param name="bldrQuote">Para builder to use to build the Quote paragraph</param>
		/// <param name="bldrDiscussion">Para builder to use to build the Discussion
		/// paragraph</param>
		/// <param name="bldrRecommendation">Para builder to use to build the
		/// Recommendation paragraph</param>
		/// <param name="bldrResolution">Para builder to use to build the Resolution
		/// paragraph</param>
		/// <param name="insertIndex">index where annotation is to be inserted into
		/// annotation list</param>
		/// <returns>note inserted into annotation list</returns>
		/// ------------------------------------------------------------------------------------
		public IScrScriptureNote InsertNote(BCVRef startRef, BCVRef endRef,
			ICmObject beginObject, ICmObject endObject, Guid guidNoteType,
			int startOffset, int endOffset, StTxtParaBldr bldrQuote, StTxtParaBldr bldrDiscussion,
			StTxtParaBldr bldrRecommendation, StTxtParaBldr bldrResolution, int insertIndex)
		{
			IScrScriptureNote annotation = new ScrScriptureNote();
			NotesOS.Insert(insertIndex, annotation);

			// Initialize the annotation.
			((ScrScriptureNote)annotation).InitializeNote(guidNoteType, startRef, endRef,
				beginObject, endObject, startOffset, endOffset,
				bldrQuote, bldrDiscussion, bldrRecommendation, bldrResolution);

			annotation.SourceRA = annotation.AnnotationType == NoteType.CheckingError ?
				m_cache.LangProject.DefaultComputerAgent : m_cache.LangProject.DefaultUserAgent;

			return annotation;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine where in the annotation list to insert a new annotation having the
		/// specified startRef, beginning object and beginning offset.
		/// </summary>
		/// <param name="startRef">The start ref.</param>
		/// <param name="beginObject">The begin object.</param>
		/// <param name="begOffset">The beg offset.</param>
		/// ------------------------------------------------------------------------------------
		private int GetNewNotesInsertIndex(BCVRef startRef, ICmObject beginObject, int begOffset)
		{
			// Get the index within the book of the section containing the paragraph
			// containing the reference to which the new annotation corresponds.
			int iNewNoteSection;
			int iNewNotePara;
			GetLocationInfoForObj(beginObject, out iNewNoteSection, out iNewNotePara);

			IFdoOwningSequence<IScrScriptureNote> notes = NotesOS;
			int insertIndex = notes.Count;

			// Go backward through the list of existing annotations.
			for (int i = notes.Count - 1; i >= 0; i--)
			{
				IScrScriptureNote note = notes[i];
				int iSect, iPara;
				GetLocationInfoForObj(note.BeginObjectRA, out iSect, out iPara);

				// If the annotation is for text that follows the text associated
				// with the new annotation we're adding, then decrement the index
				// of where to insert the new annotation.
				if (note.BeginRef > startRef ||
					iSect > iNewNoteSection ||
					(iSect == iNewNoteSection && iPara > iNewNotePara) ||
					(iSect == iNewNoteSection && iPara == iNewNotePara && note.BeginOffset > begOffset))
				{
					insertIndex--;
				}
				else
					break; // found first spot for note
			}

			return insertIndex;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the section index and paragraph index for the specified object, either an
		/// StTxtPara or a ICmTranslation/ISegment of an IScrTxtPara.
		/// </summary>
		/// <param name="obj">An ICmObject associated with an annotation (i.e. the annotation's
		/// BeginObjectRA) If this is not a IScrTxtPara or ICmTranslation/ISegment of a IScrTxtPara, this
		/// method can't do anything very useful.</param>
		/// <param name="iNewNoteSection">The index of the section, or -1 if the index cannot
		/// be determined.</param>
		/// <param name="iNewNotePara">The index of paragraph, or int.MaxValue if the index
		/// cannot be determined.</param>
		/// ------------------------------------------------------------------------------------
		private void GetLocationInfoForObj(ICmObject obj, out int iNewNoteSection, out int iNewNotePara)
		{
			iNewNoteSection = -1;
			iNewNotePara = int.MaxValue;

			// Don't rely on secion/paragraph information if reference object is in a saved version
			if (obj == null || obj.OwnerOfClass<IScrDraft>() != null)
				return;

			if (obj is IScrTxtPara)
			{
				IScrTxtPara para = (IScrTxtPara)obj;
				iNewNotePara = para.IndexInOwner;
				IScrSection section = para.OwningSection;
				if (section != null)
					iNewNoteSection = section.IndexInOwner;
			}
			else if (obj is ICmTranslation)
			{
				ICmTranslation trans = (ICmTranslation)obj;
				Debug.Assert(trans.Owner is IScrTxtPara);
				GetLocationInfoForObj(trans.Owner, out iNewNoteSection, out iNewNotePara);
			}
			else if (obj is ISegment)
			{
				ISegment segment = (ISegment)obj;
				Debug.Assert(segment.Owner is IScrTxtPara);
				GetLocationInfoForObj(segment.Owner, out iNewNoteSection, out iNewNotePara);
			}
		}
	}

	#endregion

	#region class ScrScriptureNote
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal partial class ScrScriptureNote
	{
		#region Construct/Initialize
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the note.
		/// </summary>
		/// <param name="info">The note key with basic info about the annotation.</param>
		/// <param name="beginObj">beginning object note refers to</param>
		/// <param name="endObj">ending object note refers to</param>
		/// ------------------------------------------------------------------------------------
		public void InitializeNote(ScrNoteKey info, ICmObject beginObj, ICmObject endObj)
		{
			InitializeNote(info.GuidAnnotationType, info.StartReference, info.EndReference,
				beginObj, endObj, 0, 0, null, null, null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize a new note.
		/// </summary>
		/// <param name="guidAnnotationType">GUID representing the type of annotation.</param>
		/// <param name="startRef">beginning reference note refers to</param>
		/// <param name="endRef">ending reference note refers to</param>
		/// <param name="beginObj">beginning object note refers to</param>
		/// <param name="endObj">ending object note refers to</param>
		/// <param name="startOffset">The starting character offset.</param>
		/// <param name="endOffset">The ending character offset.</param>
		/// <param name="bldrQuote">Para builder to use to build the Quote paragraph</param>
		/// <param name="bldrDiscussion">Para builder to use to build the Discussion
		/// paragraph</param>
		/// <param name="bldrRecommendation">Para builder to use to build the
		/// Recommendation paragraph</param>
		/// <param name="bldrResolution">Para builder to use to build the Resolution
		/// paragraph</param>
		/// ------------------------------------------------------------------------------------
		public void InitializeNote(Guid guidAnnotationType, BCVRef startRef, BCVRef endRef,
			ICmObject beginObj, ICmObject endObj, int startOffset, int endOffset,
			StTxtParaBldr bldrQuote, StTxtParaBldr bldrDiscussion, StTxtParaBldr bldrRecommendation,
			StTxtParaBldr bldrResolution)
		{
			AnnotationTypeRA = Services.GetInstance<ICmAnnotationDefnRepository>().GetObject(guidAnnotationType);
			BeginObjectRA = beginObj;
			EndObjectRA = endObj;
			BeginRef = startRef;
			EndRef = endRef;
			BeginOffset = startOffset;
			EndOffset = endOffset;

			// Now, initialize all the texts
			QuoteOA = new StJournalText();
			InitializeText(bldrQuote, QuoteOA);

			DiscussionOA = new StJournalText();
			InitializeText(bldrDiscussion, DiscussionOA);

			RecommendationOA = new StJournalText();
			InitializeText(bldrRecommendation, RecommendationOA);

			ResolutionOA = new StJournalText();
			InitializeText(bldrResolution, ResolutionOA);
		}
		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the annotation type from the Guid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public NoteType AnnotationType
		{
			get
			{
				NoteType noteType = GetAnnotationType(AnnotationTypeRA);
				if (noteType == NoteType.Unknown)
					throw new ApplicationException("Unrecognized annotation type");

				return noteType;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text of the (first paragraph) of the "quote", or cited text of the
		/// annotation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CitedText
		{
			get
			{
				ITsString tss = CitedTextTss;
				if (tss != null)
					return tss.Text;
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text of the (first paragraph) of the "quote", or cited text of the
		/// annotation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString CitedTextTss
		{
			get
			{
				Debug.Assert(QuoteOA.ParagraphsOS.Count >= 1);
				IStTxtPara para = QuoteOA[0];
				if (para != null)
					return para.Contents;
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets A key that can be used for comparison to determine whether two notes "match"
		/// (i.e., are probably just different versions of each other).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrNoteKey Key
		{
			get
			{
				string sDiscussion = null, sQuote = null, sRecommendation = null;
				// look at paragraph content
				if (DiscussionOA != null && DiscussionOA.ParagraphsOS.Count > 0)
					sDiscussion = ((IStTxtPara)DiscussionOA.ParagraphsOS[0]).Contents.Text;
				if (QuoteOA != null && QuoteOA.ParagraphsOS.Count > 0)
					sQuote = ((IStTxtPara)QuoteOA.ParagraphsOS[0]).Contents.Text;
				if (RecommendationOA != null && RecommendationOA.ParagraphsOS.Count > 0)
					sRecommendation = ((IStTxtPara)RecommendationOA.ParagraphsOS[0]).Contents.Text;

				return new ScrNoteKey(AnnotationTypeRA.Guid, sDiscussion, sQuote, sRecommendation,
					BeginRef, EndRef, DateCreated);
			}
		}
		#endregion

		#region Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the annotation type from the Guid.
		/// </summary>
		/// <param name="defn">The defn.</param>
		/// <returns>
		/// The annotation type for <paramref name="defn"/>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static NoteType GetAnnotationType(ICmAnnotationDefn defn)
		{
			Guid guid = defn.Guid;
			if (guid == CmAnnotationDefnTags.kguidAnnConsultantNote)
				return NoteType.Consultant;
			else if (guid == CmAnnotationDefnTags.kguidAnnTranslatorNote)
				return NoteType.Translator;
			else //if (guid == LangProject.kguidAnnCheckingError) -- There are multiple sub-types of checking errors
				return NoteType.CheckingError;

			//return NoteType.Unknown;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the text for the paragraph with the specified builder, or create an
		/// empty paragraph if the builder is null.
		/// </summary>
		/// <param name="bldr">paragraph builder</param>
		/// <param name="text">StText</param>
		/// ------------------------------------------------------------------------------------
		private void InitializeText(StTxtParaBldr bldr, IStText text)
		{
			if (bldr == null)
			{
				IStTxtPara para = text.AddNewTextPara(ScrStyleNames.Remark);
				para.Contents = TsStringUtils.MakeTss(String.Empty, Cache.DefaultAnalWs);
			}
			else
			{
				bldr.CreateParagraph(text);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a response to an annotation
		/// </summary>
		/// <returns>The new StJournalText that will contain the response</returns>
		/// ------------------------------------------------------------------------------------
		public IStJournalText CreateResponse()
		{
			IStJournalText response = Services.GetInstance<IStJournalTextFactory>().Create();
			ResponsesOS.Add(response);
			InitializeText(null, response);
			return response;
		}
		#endregion
	}
	#endregion
}
