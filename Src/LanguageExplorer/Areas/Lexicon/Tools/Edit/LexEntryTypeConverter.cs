// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using LanguageExplorer.UtilityTools;
using SIL.FieldWorks.FdoUi;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// What: This utility allows you to select which irregularly inflected form variant types should be converted
	///		to variant types (irregularly inflected form variant types are a special sub-kind of variant types).
	/// When: Run this utility when you need to convert one or more of your existing irregularly inflected form
	///		variant types to be variant types.  When a variant type is an irregularly inflected form variant type,
	///		it has extra fields such as 'Append to Gloss', 'Inflection Features', and 'Slots.'
	/// </summary>
	internal sealed class LexEntryTypeConverter : LexEntryTypeConverters
	{
		/// <summary />
		internal LexEntryTypeConverter(UtilityDlg utilityDlg)
			: base(utilityDlg)
		{
		}

		#region IUtility implementation

		/// <summary>
		/// Get the main label describing the utility.
		/// </summary>
		public override string Label => LanguageExplorerResources.ksConvertVariants;

		/// <summary>
		/// Notify the utility is has been selected in the dlg.
		/// </summary>
		public override void OnSelection()
		{
			m_dlg.WhenDescription = LanguageExplorerResources.ksWhenToConvertVariants;
			m_dlg.WhatDescription = LanguageExplorerResources.ksWhatIsConvertVariants;
			m_dlg.RedoDescription = LanguageExplorerResources.ksCannotRedoConvertVariants;
		}

		/// <summary>
		/// Have the utility do what it does.
		/// </summary>
		public override void Process()
		{
			m_cache = m_dlg.PropertyTable.GetValue<LcmCache>("cache");
			UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoConvertVariants, LanguageExplorerResources.ksRedoConvertVariants,
				m_cache.ActionHandlerAccessor,
				() => ShowDialogAndConvert(LexEntryTypeTags.kClassId));

		}

		#endregion IUtility implementation

		/// <summary />
		protected override void Convert(IEnumerable<ILexEntryType> itemsToChange)
		{
			m_cache.LanguageProject.LexDbOA.ConvertLexEntryTypes(new ProgressBarWrapper(m_dlg.ProgressBar), itemsToChange);
		}
	}
}