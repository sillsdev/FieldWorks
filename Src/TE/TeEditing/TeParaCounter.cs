// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: HeightEstimator.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Counts Paragraphs
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TeParaCounter : IParagraphCounter
	{
		private FdoCache m_fdoCache;
		private Dictionary<int, int> m_paraCounts = new Dictionary<int, int>();

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TeParaCounter"/> class.
		/// </summary>
		/// <param name="cache"></param>
		/// -----------------------------------------------------------------------------------
		public TeParaCounter(FdoCache cache)
		{
			m_fdoCache = cache;
		}

		#region IParagraphCounter Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calculates the number of paragraphs for an hvo displayed with the specified frag
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// <returns>The number of paragraphs for an hvo displayed with the specified frag</returns>
		/// ------------------------------------------------------------------------------------
		public int GetParagraphCount(int hvo, int frag)
		{
			int nParas;
			if (!m_paraCounts.TryGetValue(hvo, out nParas))
			{
				switch (frag)
				{
					default:
						throw new ArgumentException("Trying to get the para count for an unknown frag");
					case (int)ScrFrags.kfrBook:
						{
							// The height of a book is the sum of the heights of the sections in the book
							int[] sections = m_fdoCache.GetVectorProperty(hvo,
								(int)ScrBook.ScrBookTags.kflidSections, false);
							int bookHeight = 0;
							foreach (int secHvo in sections)
								bookHeight += GetParagraphCount(secHvo, (int)ScrFrags.kfrSection);

							nParas = bookHeight;
							break;
						}
					case (int)ScrFrags.kfrSection:
						{
							// The height of a section is the number of paragraphs
							int hvoContent = m_fdoCache.GetObjProperty(hvo,
								(int)ScrSection.ScrSectionTags.kflidContent);
							if (hvoContent > 0)
							{
								int secHeight = m_fdoCache.GetVectorSize(hvoContent,
									(int)StText.StTextTags.kflidParagraphs);
								nParas = secHeight;
							}
							break;
						}
					case (int)FootnoteFrags.kfrBook:
						{
							// The height for a book is the number of footnotes.
							int footnoteCount = m_fdoCache.GetVectorSize(hvo,
								(int)ScrBook.ScrBookTags.kflidFootnotes);
							nParas = footnoteCount;
							break;
						}
					case (int)StTextFrags.kfrPara:
					case (int)ScrFrags.kfrParaStyles:
						{
							// The height of a paragraph is one paragraph :)
							nParas = 1;
							break;
						}
				}
				m_paraCounts[hvo] = nParas;
			}
			return nParas;
		}
		#endregion
	}
}
