// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ScriptureBookIntroVc.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// View constructor for intro sections and title for print layout
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScriptureBookIntroVc : DraftViewVc
	{
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScriptureBookIntroVc"/> class.
		/// </summary>
		/// <param name="target">target of the view (printer or draft)</param>
		/// <param name="filterInstance">Number used to make filters unique for each main
		/// window</param>
		/// <param name="styleSheet">Optional stylesheet. Null is okay if this view constructor
		/// promises never to try to display a back translation</param>
		/// <param name="displayInTable">True to display the paragraphs in a table layout,
		/// false otherwise</param>
		/// --------------------------------------------------------------------------------
		public ScriptureBookIntroVc(LayoutViewTarget target, int filterInstance,
			IVwStylesheet styleSheet, bool displayInTable)
			: base(target, filterInstance, styleSheet, displayInTable)
		{
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
					vwenv.AddObjProp(ScrBookTags.kflidTitle, this,
						(int)StTextFrags.kfrText);
					vwenv.NoteDependency(new int[] { m_cache.LanguageProject.TranslatedScriptureOA.Hvo },
						new int[] { ScriptureTags.kflidScriptureBooks }, 1);
					vwenv.NoteDependency(new int[] { hvo },
						new int[] { ScrBookTags.kflidSections }, 1);
					vwenv.AddLazyVecItems(ScrBookTags.kflidSections, this, (int)ScrFrags.kfrSection);

					// TODO (EberhardB): The gap between the intro division and the text
					// division probably needs to be configurable somewhere...
					// Add a 24 point gap at the bottom of the intro section
					vwenv.AddSimpleRect((int)ColorUtil.ConvertColorToBGR(BackColor), -1, 24000, 0);
					vwenv.CloseDiv();
					break;
				default:
					base.Display(vwenv, hvo, frag);
					break;
			}
		}
	}
}
