// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DbScrPassageDropDown.cs
// --------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SILUBS.SharedScrUtils;
using SILUBS.SharedScrControls;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class to extend the default behavior of the ScrPassageDropDown control to
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DbScrPassageDropDown : ScrPassageDropDown
	{
		#region Contructor and initialization
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScrPassageDropDown"/> class.
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="fBooksOnly">If true, show only books without chapter and verse</param>
		/// <param name="versification">The current versification to use when creating
		/// instances of ScrReference</param>
		/// -----------------------------------------------------------------------------------
		public DbScrPassageDropDown(Control owner, bool fBooksOnly, ScrVers versification) :
			base(owner, fBooksOnly, versification)
		{
		}
		#endregion

		#region Overrides
		///  ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of all existing chapters for the current book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override List<int> CurrentChapterList
		{
			get
			{
				List<int> chapterList = new List<int>();
				IScrBook book = ((DbScrPassageControl)ScrPassageControl).ScriptureObject.FindBook(CurrentBook);

				// Go through the sections of the book and collect the chapters for each
				// section.
				if (book != null)
				{
					foreach (IScrSection section in book.SectionsOS)
					{
						for (int chapter = (BCVRef.GetChapterFromBcv(section.VerseRefMin));
							chapter <= (BCVRef.GetChapterFromBcv(section.VerseRefMax));
							chapter++)
						{
							if (chapter != 0 && !chapterList.Contains(chapter))
								chapterList.Add(chapter);
						}
					}
				}
				return chapterList;
			}
		}
		#endregion
	}
}