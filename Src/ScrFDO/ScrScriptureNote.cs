// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2002' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FdoScripture.cs
// Responsibility: TE Team
//
// <remarks>
// For change history before July 2009, look in FdoScripture.cs
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.FDO.Scripture
{
	#region class ScrBookAnnotations
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// ScrBookAnnotations holds and manipulates a collection of Scripture notes.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ScrBookAnnotations
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the first annotation in the list that covers the specified reference.
		/// </summary>
		/// <param name="reference">Scripture reference to seek</param>
		/// <returns>The first note for the specified reference, if any; otherwise null</returns>
		/// ------------------------------------------------------------------------------------
		public IScrScriptureNote GetFirstNoteForReference(BCVRef reference)
		{
			foreach (IScrScriptureNote note in NotesOS)
			{
				if (note.BeginRef <= reference && note.EndRef >= reference)
					return note;
			}
			return null;
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
			return InsertNote(startRef, endRef, beginObject, endObject, checkId, -1,
				0, 0, bldrQuote, bldrDiscussion, null, null, GetInsertIndexForRef(startRef));
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
			return InsertNote(startRef, endRef, beginObject, endObject, guidNoteType, -1,
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
			FdoOwningSequence<IScrScriptureNote> notes = NotesOS;

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
			return InsertNote(startRef, endRef, beginObject, endObject, guidNoteType, -1, -1, 0,
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
			return InsertNote(startRef, endRef, beginObject, endObject, guidNoteType, -1, -1, 0,
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
			return InsertNote(startRef, endRef, beginObject, endObject, guidNoteType, -1, -1, 0,
				bldrQuote, bldrDiscussion, bldrRecommendation, bldrResolution, out insertIndex);
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
		/// <param name="wsSelector">The writing system selector, indicating which
		/// writing system the annotation refers to.</param>
		/// <param name="startOffset">The starting character offset.</param>
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
			ICmObject beginObject, ICmObject endObject, Guid guidNoteType, int wsSelector,
			int startOffset, int endOffset, StTxtParaBldr bldrQuote, StTxtParaBldr bldrDiscussion,
			StTxtParaBldr bldrRecommendation, StTxtParaBldr bldrResolution, out int insertIndex)
		{
			// Determine where to insert the new annotation.
			insertIndex = (startOffset < 0 ? GetInsertIndexForRef(startRef) :
				GetNewNotesInsertIndex(startRef, beginObject, startOffset));

			if (startOffset < 0)
				startOffset = 0;

			return InsertNote(startRef, endRef, beginObject, endObject, guidNoteType, wsSelector,
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
		/// <param name="wsSelector">The writing system selector, indicating which
		/// writing system the annotation refers to.</param>
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
			ICmObject beginObject, ICmObject endObject, Guid guidNoteType, int wsSelector,
			int startOffset, int endOffset, StTxtParaBldr bldrQuote, StTxtParaBldr bldrDiscussion,
			StTxtParaBldr bldrRecommendation, StTxtParaBldr bldrResolution, int insertIndex)
		{
			IScrScriptureNote annotation = new ScrScriptureNote();
			NotesOS.InsertAt(annotation, insertIndex);

			// Initialize the annotation.
			((ScrScriptureNote)annotation).InitializeNote(guidNoteType, startRef, endRef,
				beginObject, endObject, wsSelector, startOffset, endOffset,
				bldrQuote, bldrDiscussion, bldrRecommendation, bldrResolution);

			annotation.SourceRA = annotation.AnnotationType == NoteType.CheckingError ?
				m_cache.LangProject.DefaultComputerAgent : m_cache.LangProject.DefaultUserAgent;

			// Notify all windows that the new annotation exists
			m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, Hvo,
				(int)ScrBookAnnotationsTags.kflidNotes, insertIndex, 1, 0);

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

			FdoOwningSequence<IScrScriptureNote> notes = NotesOS;
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
				if (note.BeginRef > startRef || iSect > iNewNoteSection ||
					(iSect == iNewNoteSection && iPara > iNewNotePara) ||
					(iSect == iNewNoteSection && iPara == iNewNotePara && note.BeginOffset > begOffset))
				{
					insertIndex--;
				}
			}

			return insertIndex;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the section index and paragraph index for the specified object, when this
		/// method assumes is an StTxtPara.
		/// </summary>
		/// <param name="obj">An ICmObject associated with an annotation (i.e. the annotation's
		/// BeginObjectRA) which is assumed to be an StTxtPara.</param>
		/// <param name="iNewNoteSection">The index of the section, or -1 if the index cannot
		/// be determined.</param>
		/// <param name="iNewNotePara">The index of paragraph, or int.MaxValue if the index
		/// cannot be determined.</param>
		/// ------------------------------------------------------------------------------------
		private void GetLocationInfoForObj(ICmObject obj, out int iNewNoteSection, out int iNewNotePara)
		{
			iNewNoteSection = -1;
			iNewNotePara = int.MaxValue;

			if (obj is StTxtPara)
			{
				StTxtPara para = (StTxtPara)obj;
				iNewNotePara = para.IndexInOwner;
				ScrSection section = ScrSection.GetSectionFromParagraph(para);
				if (section != null)
					iNewNoteSection = section.IndexInBook;
			}
			else if (obj is CmTranslation)
			{
				CmTranslation trans = (CmTranslation)obj;
				Debug.Assert(trans.Owner is StTxtPara);
				GetLocationInfoForObj(trans.Owner, out iNewNoteSection, out iNewNotePara);
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
	public partial class ScrScriptureNote
	{
		#region ScrNoteKey
		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// A structure which can serve as a "key" for a Scripture annotation
		/// </summary>
		/// <remarks>REVIEW: Should we just use a GUID?</remarks>
		/// ----------------------------------------------------------------------------------------
		public struct ScrNoteKey
		{
			/// <summary>Factor used to convert ticks (100 nano seconds) to seconds</summary>
			private const long TICKS_TO_SECONDS = 10000000;
			/// <summary>The hvo of the annotation type to use for this annotation</summary>
			private readonly Guid m_guidAnnotationType;
			/// <summary>The text of the first paragraph of the annotation "quote"</summary>
			private readonly string m_sQuotePara1;
			/// <summary>The text of the first paragraph of the annotation "discussion"</summary>
			private readonly string m_sDiscussionPara1;
			/// <summary>The text of the first paragraph of the annotation "recommendation"</summary>
			private readonly string m_sRecommendationPara1;
			/// <summary>The starting Scripture reference of the annotation</summary>
			private readonly int m_startReference;
			/// <summary>The ending Scripture reference of the annotation</summary>
			private readonly int m_endReference;
			/// <summary>the date/time the annotation was created</summary>
			private readonly DateTime m_dateCreated;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="ScrNoteKey"/> class based on a
			/// simple note (as would come from an import from USFM).
			/// </summary>
			/// <param name="guidAnnotationType">GUID representing the annotation type.</param>
			/// <param name="sDiscussionPara1">The text of the first paragraph of the annotation
			/// discussion</param>
			/// <param name="startReference">The starting Scripture reference of the
			/// annotation.</param>
			/// <param name="endReference">The ending Scripture reference of the annotation.</param>
			/// --------------------------------------------------------------------------------
			public ScrNoteKey(Guid guidAnnotationType, string sDiscussionPara1,
				int startReference, int endReference) : this(guidAnnotationType,
				sDiscussionPara1, null, null, startReference, endReference, DateTime.MinValue)
			{
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="ScrNoteKey"/> class.
			/// </summary>
			/// <param name="guidAnnotationType">GUID representing the annotation type.</param>
			/// <param name="sDiscussionPara1">The text of the first paragraph of the annotation
			/// discussion</param>
			/// <param name="sQuotePara1">The text of the first paragraph of the annotation
			/// resolution</param>
			/// <param name="sRecommendationPara1">The text of the first paragraph of the
			/// annotation recommendation</param>
			/// <param name="startReference">The starting Scripture reference of the
			/// annotation.</param>
			/// <param name="endReference">The ending Scripture reference of the annotation.</param>
			/// <param name="dateCreated">The date created.</param>
			/// --------------------------------------------------------------------------------
			public ScrNoteKey(Guid guidAnnotationType, string sDiscussionPara1,
				string sQuotePara1, string sRecommendationPara1, int startReference,
				int endReference, DateTime dateCreated)
			{
				m_guidAnnotationType = guidAnnotationType;
				m_sDiscussionPara1 = sDiscussionPara1 == null ? null : sDiscussionPara1.Trim();
				m_sQuotePara1 = sQuotePara1 == null ? null : sQuotePara1.Trim();
				m_sRecommendationPara1 = sRecommendationPara1 == null ? null : sRecommendationPara1.Trim();
				m_startReference = startReference;
				m_endReference = endReference;
				m_dateCreated = dateCreated;
			}

			#region Properties
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the type of the annotation represented in a GUID.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public Guid GuidAnnotationType
			{
				get { return m_guidAnnotationType; }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the start reference.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public int StartReference
			{
				get { return m_startReference; }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the end reference.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public int EndReference
			{
				get { return m_endReference; }
			}
			#endregion

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Indicates whether this instance and a specified object are equal.
			/// </summary>
			/// <param name="obj">Another object to compare to.</param>
			/// <returns>
			/// true if <paramref name="obj"/> and this instance are the same type and represent
			/// the same value; otherwise, false.
			/// </returns>
			/// --------------------------------------------------------------------------------
			public override bool Equals(object obj)
			{
				if (obj == null || !(obj is ScrNoteKey))
					return false;

				ScrNoteKey key = (ScrNoteKey)obj;
				return m_guidAnnotationType == key.m_guidAnnotationType &&
					m_sDiscussionPara1 == key.m_sDiscussionPara1 &&
					m_sQuotePara1 == key.m_sQuotePara1 &&
					m_sRecommendationPara1 == key.m_sRecommendationPara1 &&
					m_startReference == key.m_startReference &&
					m_endReference == key.m_endReference &&
					ApproximatelyEqualTimes(key);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Compares times to nearest second.
			/// </summary>
			/// --------------------------------------------------------------------------------
			private bool ApproximatelyEqualTimes(ScrNoteKey key)
			{
				if (m_dateCreated.Ticks == 0 || key.m_dateCreated.Ticks == 0)
					return true;

				return m_dateCreated.Ticks / TICKS_TO_SECONDS ==
					key.m_dateCreated.Ticks / TICKS_TO_SECONDS;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Returns the hash code for this instance.
			/// </summary>
			/// <returns>
			/// A 32-bit signed integer that is the hash code for this instance.
			/// </returns>
			/// --------------------------------------------------------------------------------
			public override int GetHashCode()
			{
				return (m_startReference + m_sQuotePara1 + m_sDiscussionPara1).GetHashCode();
			}
		}
		#endregion

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
				beginObj, endObj, -1, 0, 0, null, null, null, null);
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
		/// <param name="wsSelector">The writing system selector.</param>
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
			ICmObject beginObj, ICmObject endObj, int wsSelector, int startOffset, int endOffset,
			StTxtParaBldr bldrQuote, StTxtParaBldr bldrDiscussion, StTxtParaBldr bldrRecommendation,
			StTxtParaBldr bldrResolution)
		{
			AnnotationTypeRA = new CmAnnotationDefn(Cache, guidAnnotationType);
			BeginObjectRA = beginObj;
			EndObjectRA = endObj;
			BeginRef = startRef;
			EndRef = endRef;
			WsSelector = wsSelector;
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
				StTxtPara para = QuoteOA.ParagraphsOS.FirstItem as StTxtPara;
				if (para != null)
					return para.Contents.UnderlyingTsString;
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

		#region Overridden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// ScrScriptureNotes should not check the BeginObject in case the referenced text
		/// has been deleted.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override bool IsValidObject()
		{
			if (m_cache == null)
				return false;
			return m_cache.IsValidObject(this.Hvo, this.ClassID);
			//return base.IsValidObject();
			//if (!fIsValidObj)
			//    return false;
			// if it's real and a twfic, we should also expect it to be an InstanceOf a real object.
			//if (AnnotationTypeRAHvo == CmAnnotationDefn.Twfic(Cache).Hvo)
			//    fIsValidObj = Cache.IsValidObject(this.InstanceOfRAHvo);
			//return fIsValidObj;
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
			if (guid == LangProject.kguidAnnConsultantNote)
				return NoteType.Consultant;
			else if (guid == LangProject.kguidAnnTranslatorNote)
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
				IStTxtPara para = (StTxtPara)text.ParagraphsOS.Append(new StTxtPara());
				para.Contents.UnderlyingTsString = StringUtils.MakeTss(String.Empty, Cache.DefaultAnalWs);
				para.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.Remark);
			}
			else
			{
				bldr.CreateParagraph(text.Hvo);
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
			IStJournalText response = ResponsesOS.Append(new StJournalText());
			InitializeText(null, response);
			return response;
		}
		#endregion
	}
	#endregion

	#region ScrAnnotationInfo
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Info needed for batching up Scripture annotation string builders
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScrAnnotationInfo : IDisposable
	{
		/// <summary>The hvo of the annotation type to use for this annotation</summary>
		public readonly Guid guidAnnotationType;
		/// <summary>The builder containing the guts of the annotation "quote"</summary>
		public readonly List<StTxtParaBldr> bldrsQuote;
		/// <summary>The builder containing the guts of the annotation discussion</summary>
		public readonly List<StTxtParaBldr> bldrsDiscussion;
		/// <summary>The builder containing the guts of the annotation "recommendation"</summary>
		public readonly List<StTxtParaBldr> bldrsRecommend;
		/// <summary>The builder containing the guts of the annotation "resolution"</summary>
		public readonly List<StTxtParaBldr> bldrsResolution;
		/// <summary>The character offset where this annotation belongs in the "owning" para</summary>
		public readonly int ichOffset;
		/// <summary>The starting Scripture reference of the annotation</summary>
		public readonly int startReference;
		/// <summary>The ending Scripture reference of the annotation</summary>
		public readonly int endReference;
		/// <summary>the date/time the annotation was created</summary>
		public readonly DateTime dateCreated;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ScrAnnotationInfo"/> class.
		/// </summary>
		/// <param name="guidAnnotationType">GUID representing the annotation type.</param>
		/// <param name="bldrDiscussion">A single Tsstring builder for a one-paragraph
		/// discussion.</param>
		/// <param name="ichOffset">character offset where this annotation belongs in the
		/// "owning" para.</param>
		/// <param name="startReference">The starting Scripture reference of the annotation.</param>
		/// <param name="endReference">The ending Scripture reference of the annotation.</param>
		/// ------------------------------------------------------------------------------------
		public ScrAnnotationInfo(Guid guidAnnotationType, StTxtParaBldr bldrDiscussion,
			int ichOffset, int startReference, int endReference)
			: this(guidAnnotationType,
				new List<StTxtParaBldr>(new StTxtParaBldr[] { bldrDiscussion }), null, null, null,
				ichOffset, startReference, endReference, DateTime.MinValue)
		{
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ScrAnnotationInfo"/> class.
		/// </summary>
		/// <param name="guidAnnotationType">Type of the GUID annotation.</param>
		/// <param name="bldrsDiscussion">Collection of para builders containing the
		/// paragraph style info and string builders of the discussion paragraphs.</param>
		/// <param name="bldrsQuote">Collection of para builders containing the
		/// paragraph style info and string builders of the quote paragraphs.</param>
		/// <param name="bldrsRecommend">Collection of para builders containing the
		/// paragraph style info and string builders of the recommendation paragraphs.</param>
		/// <param name="bldrsResolution">Collection of para builders containing the
		/// paragraph style info and string builders of the resolution paragraphs.</param>
		/// <param name="ichOffset">character offset where this annotation belongs in the
		/// "owning" para.</param>
		/// <param name="startReference">The starting Scripture reference of the annotation.</param>
		/// <param name="endReference">The ending Scripture reference of the annotation.</param>
		/// <param name="dateCreated">The date created.</param>
		/// --------------------------------------------------------------------------------
		public ScrAnnotationInfo(Guid guidAnnotationType,
			List<StTxtParaBldr> bldrsDiscussion, List<StTxtParaBldr> bldrsQuote,
			List<StTxtParaBldr> bldrsRecommend, List<StTxtParaBldr> bldrsResolution,
			int ichOffset, int startReference, int endReference, DateTime dateCreated)
		{
			this.guidAnnotationType = guidAnnotationType;
			this.bldrsDiscussion = bldrsDiscussion;
			this.bldrsQuote = bldrsQuote;
			this.bldrsRecommend = bldrsRecommend;
			this.bldrsResolution = bldrsResolution;
			this.ichOffset = ichOffset;
			this.startReference = startReference;
			this.endReference = endReference;
			this.dateCreated = dateCreated;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets a key that can be used to find a matching note based on certain fields.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public ScrScriptureNote.ScrNoteKey Key
		{
			get
			{
				return new ScrScriptureNote.ScrNoteKey(guidAnnotationType,
					GetAnnotationFieldText(bldrsDiscussion),
					GetAnnotationFieldText(bldrsQuote),
					GetAnnotationFieldText(bldrsRecommend), startReference,
					endReference, dateCreated);
			}
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets the annotation field text from the given list of StTxtParaBldrs for an
		/// annotation field.
		/// </summary>
		/// <param name="bldrs">The BLDRS.</param>
		/// --------------------------------------------------------------------------------
		public string GetAnnotationFieldText(List<StTxtParaBldr> bldrs)
		{
			return (bldrs != null && bldrs.Count > 0) ? bldrs[0].StringBuilder.Text : null;
		}

		#region IDisposable Members
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or
		/// resetting unmanaged resources.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public void Dispose()
		{
			DisposeBldrs(bldrsDiscussion);
			DisposeBldrs(bldrsQuote);
			DisposeBldrs(bldrsRecommend);
			DisposeBldrs(bldrsResolution);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Disposes the given list of StTxtParaBldrs associated with each field of the
		/// annotation.
		/// </summary>
		/// <param name="bldrs">The given list of StTxtParaBldrs.</param>
		/// --------------------------------------------------------------------------------
		private void DisposeBldrs(List<StTxtParaBldr> bldrs)
		{
			if (bldrs != null)
				foreach (StTxtParaBldr bldr in bldrs)
					bldr.Dispose();
		}

		#endregion
	}

	#endregion
}
