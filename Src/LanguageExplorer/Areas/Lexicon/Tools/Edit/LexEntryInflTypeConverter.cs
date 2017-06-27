// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using LanguageExplorer.UtilityTools;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// What: This utility allows you to select which variant types should be converted to irregularly inflected form variant types, which are a special sub-kind of variant types.
	/// When: Run this utility when you need to convert one or more of your existing variant types to be irregularly inflected form variant types.
	///		When a variant type is an irregularly inflected form variant type, it has extra fields such as 'Append to Gloss', 'Inflection Features', and 'Slots.'
	/// </summary>
	internal sealed class LexEntryInflTypeConverter : LexEntryTypeConverters
	{
		/// <summary />
		internal LexEntryInflTypeConverter(UtilityDlg utilityDlg)
			: base(utilityDlg)
		{
		}

		#region IUtility implementation

		/// <summary>
		/// Get the main label describing the utility.
		/// </summary>
		public override string Label
		{
			get
			{
				return LanguageExplorerResources.ksConvertIrregularlyInflectedFormVariants;
			}
		}

		/// <summary>
		/// Notify the utility is has been selected in the dlg.
		/// </summary>
		public override void OnSelection()
		{
			m_dlg.WhenDescription = LanguageExplorerResources.ksWhenToConvertIrregularlyInflectedFormVariants;
			m_dlg.WhatDescription = LanguageExplorerResources.ksWhatIsConvertIrregularlyInflectedFormVariants;
			m_dlg.RedoDescription = LanguageExplorerResources.ksCannotRedoConvertIrregularlyInflectedFormVariants;
		}

		/// <summary>
		/// Have the utility do what it does.
		/// </summary>
		public override void Process()
		{
			m_cache = m_dlg.PropertyTable.GetValue<FdoCache>("cache");
			UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoConvertIrregularlyInflectedFormVariants, LanguageExplorerResources.ksRedoConvertIrregularlyInflectedFormVariants,
										m_cache.ActionHandlerAccessor,
										() => ShowDialogAndConvert(LexEntryInflTypeTags.kClassId));
		}

		#endregion IUtility implementation

		/// <summary />
		protected override void Convert(IEnumerable<ILexEntryType> itemsToChange)
		{
			m_cache.LanguageProject.LexDbOA.ConvertLexEntryInflTypes(new ProgressBarWrapper(m_dlg.ProgressBar), itemsToChange);
		}

	}
}
