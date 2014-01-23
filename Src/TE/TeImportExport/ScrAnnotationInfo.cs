// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ScrAnnotationInfo.cs
// Responsibility: shaneyfelt
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Info needed for batching up Scripture annotation string builders
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScrAnnotationInfo
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
			int ichOffset, int startReference, int endReference) : this(guidAnnotationType,
			new List<StTxtParaBldr>(new[] { bldrDiscussion }), null, null, null,
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
		public ScrNoteKey Key
		{
			get
			{
				return new ScrNoteKey(guidAnnotationType,
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
	}
}
