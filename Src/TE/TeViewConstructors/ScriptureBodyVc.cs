// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScriptureBodyVc.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Scripture;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// View constructor for scripture body text in print layout
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScriptureBodyVc : DraftViewVc
	{
		private int m_sectionTag;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScriptureBodyVc"/> class.
		/// </summary>
		/// <param name="target">target of the view (printer or draft)</param>
		/// <param name="filterInstance">Number used to make filters unique for each main
		/// window</param>
		/// <param name="styleSheet">Optional stylesheet. Null is okay if this view constructor
		/// promises never to try to display a back translation</param>
		/// <param name="displayInTable">True to display the paragraphs in a table layout,
		/// false otherwise</param>
		/// <param name="sectionTag">Tag (from virtual property) to use for sections.</param>
		/// ------------------------------------------------------------------------------------
		public ScriptureBodyVc(LayoutViewTarget target, int filterInstance,
			IVwStylesheet styleSheet, bool displayInTable, int sectionTag)
			: base(target, filterInstance, styleSheet, displayInTable)
		{
			m_sectionTag = sectionTag;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the main interesting method of displaying objects and fragments of them.
		/// A Scripture is displayed by displaying its Books;
		/// and a Book is displayed by displaying its Title and Sections;
		/// and a Section is diplayed by displaying its Heading and Content;
		/// which are displayed by using the standard view constructor for StText.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// ------------------------------------------------------------------------------------
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			CheckDisposed();

			if (hvo == 0)
				return; // not much we can display without a valid object

			switch (frag)
			{
				case (int)ScrFrags.kfrBook:
					vwenv.OpenDiv();
					vwenv.NoteDependency(new int[] { m_cache.LangProject.TranslatedScriptureOAHvo },
						new int[] { (int)Scripture.ScriptureTags.kflidScriptureBooks }, 1);
					vwenv.NoteDependency(new int[] { hvo },
						new int[] { (int)ScrBook.ScrBookTags.kflidSections }, 1);
					vwenv.AddLazyVecItems(m_sectionTag, this, (int)ScrFrags.kfrSection);
					vwenv.CloseDiv();
					break;
				default:
					base.Display(vwenv, hvo, frag);
					break;
			}
		}
		/// <summary>
		/// Override to load segment info if the property is our (filtered) list of sections.
		/// </summary>
		public override void LoadDataFor(IVwEnv vwenv, int[] rghvo, int chvo, int hvoParent, int tag, int frag, int ihvoMin)
		{
			if (tag == m_sectionTag)
				LoadSegmentInfoForSection(rghvo);
			base.LoadDataFor(vwenv, rghvo, chvo, hvoParent, tag, frag, ihvoMin);
		}
	}
}
