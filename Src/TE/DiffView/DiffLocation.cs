// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DiffLocation.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.TE
{
	#region Cluster class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A DiffLocation holds information about a place (character position or range) in a
	/// paragraph.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public sealed class DiffLocation
	{
		/// <summary>The paragraph</summary>
		internal readonly IScrTxtPara Para;
		/// <summary>The character offset to the start of the location in the paragraph</summary>
		internal readonly int IchMin;
		/// <summary>The limit of the location in the paragraph</summary>
		internal readonly int IchLim;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DiffLocation"/> class for the end of
		/// a paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal DiffLocation(IScrTxtPara para) : this(para, para.Contents.Length)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DiffLocation"/> class for a point.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal DiffLocation(IScrTxtPara para, int ich) : this (para, ich, ich)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DiffLocation"/> class that covers an
		/// entire Scripture verse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal DiffLocation(ScrVerse scrVerse) : this(scrVerse.Para, scrVerse.VerseStartIndex)
		{
			if (scrVerse.Text != null)
				IchLim = IchMin + scrVerse.TextLength;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DiffLocation"/> class for a range of
		/// text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal DiffLocation(IScrTxtPara para, int ichMin, int ichLim)
		{
			Para = para;
			IchMin = ichMin;
			IchLim = ichLim;
		}
	}
	#endregion
}
