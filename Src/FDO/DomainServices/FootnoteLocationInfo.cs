// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2012, SIL International. All Rights Reserved.
// <copyright from='2012' to='2012' company='SIL International'>
//		Copyright (c) 2012, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FootnoteLocationInfo.cs
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.FDO.DomainServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Simple class to hold information about a footnote and the location of its marker in the
	/// Scripture text.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FootnoteLocationInfo
	{
		/// <summary>The footnote</summary>
		public IScrFootnote m_footnote;
		/// <summary>The index of the book containing the footnote</summary>
		public int m_iBook;
		/// <summary>The index of the section containing the footnote marker (undefined for
		/// footnotes in book titles).</summary>
		public int m_iSection;
		/// <summary>The index of the paragraph containing the footnote marker.</summary>
		public int m_iPara;
		/// <summary>The index of the character position immediately following the footnote's
		/// marker (ORC).</summary>
		public int m_ich;
		/// <summary>Indicates whether the footnote marker is in the title, section
		/// Heading or section Content</summary>
		public int m_tag;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FootnoteLocationInfo"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FootnoteLocationInfo()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FootnoteLocationInfo"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FootnoteLocationInfo(IScrFootnote footnote, int iBook, int iSection, int iPara,
			int ich, int tag)
		{
			m_footnote = footnote;
			m_iBook = iBook;
			m_iSection = iSection;
			m_iPara = iPara;
			m_ich = ich;
			m_tag = tag;
		}
	}
}
