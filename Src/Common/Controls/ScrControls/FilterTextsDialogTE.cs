// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FilterSectionDialog.cs
// --------------------------------------------------------------------------------------------
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.TE;
using SIL.Utils;
using SIL.FieldWorks.FDO.Infrastructure;
using XCore;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// FilterTextsDialog bundles both texts and, when appropriate, Scripture for selection.
	/// This file cannot be moved to the ITextDll: ../Src/LexText/Interlinear because that
	/// dll is referenced by SIL.FieldWorks.TE and would create a circular reference.
	/// It can't be moved to FwControls either for a similar reason - ScrControls uses FwControls!
	/// This class uses TE to make sure the scriptures are properly initialized for use.
	/// </summary>
	public class FilterTextsDialogTE : FilterTextsDialog
	{
		private readonly IBookImporter m_bookImporter;

		#region Constructor/Destructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the ChooseScriptureDialog class.
		/// WARNING: this constructor is called by reflection, at least in the Interlinear
		/// Text DLL. If you change its parameters be SURE to find and fix those callers also.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="objList">A list of texts and books to check as an array of hvos</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="importer">The Paratext book importer.</param>
		/// ------------------------------------------------------------------------------------
		public FilterTextsDialogTE(FdoCache cache, IStText[] objList,
			IHelpTopicProvider helpTopicProvider, IBookImporter importer)
			: base(cache, objList, helpTopicProvider)
		{
			m_bookImporter = importer;
			using (var progressDlg = new ProgressDialogWithTask(this))
			{
				// Use FLEx's help provider. I (RBR) hope TeScrInitializer can figure out
				// what to do with it. The "SE" version used it,
				// so no ground has been lost.
				var helpProvider = (IHelpTopicProvider)DynamicLoader.CreateObject(FwDirectoryFinder.FlexDll,
						"SIL.FieldWorks.XWorks.LexText.FlexHelpTopicProvider");
				NonUndoableUnitOfWorkHelper.Do(cache.ActionHandlerAccessor, () =>
				TeScrInitializer.EnsureMinimalScriptureInitialization(cache, progressDlg,
					helpProvider));
			}
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden to add general FLEx texts and ScrSections and Title for each book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void LoadTexts()
		{
			m_treeTexts.LoadScriptureAndOtherTexts(m_cache, m_bookImporter);
		}
	}
}
