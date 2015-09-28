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
	/// <summary />
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="m_parentWindow is a reference")]
	internal class AddPrimaryLexemeChooserCommand : ChooserCommand
	{
		private readonly ILexEntryRef m_lexEntryRef;
		private readonly Form m_parentWindow;

		/// <summary />
		public AddPrimaryLexemeChooserCommand(FdoCache cache, bool fCloseBeforeExecuting,
			string sLabel, IPropertyTable propertyTable, IPublisher publisher, ICmObject lexEntryRef, /* Why ICmObject? */
			Form parentWindow)
			: base(cache, fCloseBeforeExecuting, sLabel, propertyTable, publisher)
		{
			m_lexEntryRef = lexEntryRef as ILexEntryRef;
			m_parentWindow = parentWindow;
		}

		/// <summary />
		public override ObjectLabel Execute()
		{
			ObjectLabel result = null;
			if (m_lexEntryRef != null)
			{
				using (LinkEntryOrSenseDlg dlg = new LinkEntryOrSenseDlg())
				{
					ILexEntry le = null;
					// assume the owner is the entry (e.g. owner of LexEntryRef)
					le = m_lexEntryRef.OwnerOfClass<ILexEntry>();
					dlg.SetDlgInfo(m_cache, m_propertyTable, m_publisher, le);
					dlg.SetHelpTopic("khtpChooseLexicalEntryOrSense");
					if (dlg.ShowDialog(m_parentWindow) == DialogResult.OK)
					{
						ICmObject obj = dlg.SelectedObject;
						if (obj != null)
						{
							if (!m_lexEntryRef.PrimaryLexemesRS.Contains(obj))
							{
								try
								{
									UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(
										LanguageExplorerResources.ksUndoCreatingEntry,
										LanguageExplorerResources.ksRedoCreatingEntry,
										Cache.ActionHandlerAccessor,
										() =>
										{
											if (!m_lexEntryRef.ComponentLexemesRS.Contains(obj))
												m_lexEntryRef.ComponentLexemesRS.Add(obj);
											m_lexEntryRef.PrimaryLexemesRS.Add(obj);
										});
								}
								catch (ArgumentException)
								{
									MessageBoxes.ReportLexEntryCircularReference((ILexEntry) m_lexEntryRef.Owner, obj, true);
								}
							}
						}
					}
				}
			}
			return result;
		}
	}
}