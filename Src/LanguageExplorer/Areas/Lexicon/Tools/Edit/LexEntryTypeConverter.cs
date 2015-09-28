// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// Summary description for LexEntryTypeConverter.
	/// </summary>
	internal sealed class LexEntryTypeConverter : LexEntryTypeConverters
	{
		#region IUtility implementation

		/// <summary>
		/// Get the main label describing the utility.
		/// </summary>
		public override string Label
		{
			get
			{
				Debug.Assert(m_dlg != null);
				return LanguageExplorerResources.ksConvertVariants;
			}
		}


		/// <summary>
		/// Notify the utility is has been selected in the dlg.
		/// </summary>
		public override void OnSelection()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.WhenDescription = LanguageExplorerResources.ksWhenToConvertVariants;
			m_dlg.WhatDescription = LanguageExplorerResources.ksWhatIsConvertVariants;
			m_dlg.RedoDescription = LanguageExplorerResources.ksCannotRedoConvertVariants;
		}

		/// <summary>
		/// Have the utility do what it does.
		/// </summary>
		public override void Process()
		{
			m_cache = m_dlg.PropertyTable.GetValue<FdoCache>("cache");
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