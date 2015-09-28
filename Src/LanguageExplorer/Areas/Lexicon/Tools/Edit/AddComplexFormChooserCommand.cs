// Copyright (c) 2014-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.LexText.Controls;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// This is a clone of internal class AddComponentChooserCommand above.
	/// There are 2 key differences:
	/// 1) It expects an ILexEntry instead of an ILexEntryRef;
	/// 2) It displays the EntryGoDlg instead of the LinkEntryOrSenseDlg.
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="m_parentWindow is a reference")]
	internal class AddComplexFormChooserCommand : ChooserCommand
	{
		private readonly ILexEntry m_lexEntry;
		private readonly ILexSense m_lexSense;
		private readonly Form m_parentWindow;

		/// <summary />
		public AddComplexFormChooserCommand(FdoCache cache, bool fCloseBeforeExecuting,
			string sLabel, IPropertyTable propertyTable, IPublisher publisher, ICmObject lexEntry, /* Why ICmObject? */
			Form parentWindow)
			: base(cache, fCloseBeforeExecuting, sLabel, propertyTable, publisher)
		{
			m_lexEntry = lexEntry as ILexEntry;
			if (m_lexEntry == null)
			{
				m_lexSense = lexEntry as ILexSense;
				if (m_lexSense != null)
					m_lexEntry = m_lexSense.Entry;

			}
			m_parentWindow = parentWindow;
		}

		/// <summary />
		public override ObjectLabel Execute()
		{
			ObjectLabel result = null;
			if (m_lexEntry != null)
			{
				using (var dlg = new EntryGoDlg())
				{
					dlg.SetDlgInfo(m_cache, null, m_propertyTable, m_publisher);
					dlg.SetHelpTopic("khtpChooseLexicalEntryOrSense"); // TODO: When LT-11318 is fixed, use its help topic ID.
					dlg.SetOkButtonText(LanguageExplorerResources.ksMakeComponentOf);
					if (dlg.ShowDialog(m_parentWindow) == DialogResult.OK)
					{
						try
						{
							if (m_lexSense != null)
							{
								UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoAddComplexForm, LanguageExplorerResources.ksRedoAddComplexForm,
									m_lexEntry.Cache.ActionHandlerAccessor,
									() => ((ILexEntry)dlg.SelectedObject).AddComponent((ICmObject)m_lexSense ?? m_lexEntry));
							}
							else
							{
								UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoAddComplexForm, LanguageExplorerResources.ksRedoAddComplexForm,
									m_lexEntry.Cache.ActionHandlerAccessor,
									() => ((ILexEntry)dlg.SelectedObject).AddComponent(m_lexEntry));
							}
						}
						catch (ArgumentException)
						{
							MessageBoxes.ReportLexEntryCircularReference((ILexEntry)dlg.SelectedObject, m_lexEntry, false);
						}
					}
				}
			}
			return result;
		}
	}
}