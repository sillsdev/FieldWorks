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
// File: FdoStructs.cs
// Responsibility: shaneyfelt
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Diagnostics;

namespace SIL.FieldWorks.FDO
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
			int startReference, int endReference)
			: this(guidAnnotationType,
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

	#region FootnoteInfo struct
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Information about a footnote including the footnote and its paragraph style.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public struct FootnoteInfo
	{
		/// <summary>footnote</summary>
		public readonly IScrFootnote footnote;
		/// <summary>paragraph style for footnote</summary>
		public readonly string paraStylename;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for the FootnoteInfo structure
		/// </summary>
		/// <param name="scrFootnote">given footnote</param>
		/// ------------------------------------------------------------------------------------
		public FootnoteInfo(IScrFootnote scrFootnote)
		{
			footnote = scrFootnote;
			IStPara para = footnote.ParagraphsOS[0];
			if (para.StyleRules != null)
			{
				paraStylename = para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			}
			else
			{
				paraStylename = null;
				Debug.Fail("StyleRules should never be null.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for the FootnoteInfo structure
		/// </summary>
		/// <param name="scrFootnote">given footnote</param>
		/// <param name="sParaStylename">paragraph style of the footnote</param>
		/// ------------------------------------------------------------------------------------
		public FootnoteInfo(IScrFootnote scrFootnote, string sParaStylename)
		{
			footnote = scrFootnote;
			paraStylename = sParaStylename;
		}
	}
	#endregion
}
