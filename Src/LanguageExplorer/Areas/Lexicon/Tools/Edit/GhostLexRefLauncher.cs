// Copyright (c) 2014-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.LexText.Controls;
using SIL.Utils;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary />
	internal sealed class GhostLexRefLauncher : ButtonLauncher
	{
		/// <summary />
		public GhostLexRefLauncher(ICmObject obj, XmlNode configNode)
		{
			m_obj = obj;
			m_configurationNode = configNode;
			// Makes the rest of the control look like content, though empty.
			BackColor = System.Drawing.SystemColors.Window;
		}

		/// <summary />
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="FindForm() returns a reference")]
		protected override void HandleChooser()
		{
			using (LinkEntryOrSenseDlg dlg = new LinkEntryOrSenseDlg())
			{
				var le = m_obj as ILexEntry;
				dlg.SetDlgInfo(m_obj.Cache, PropertyTable, Publisher, le);
				var str = ShowHelp.RemoveSpaces(Slice.Label);
				dlg.SetHelpTopic("khtpChooseLexicalEntryOrSense-" + str);
				if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
					AddItem(dlg.SelectedObject);

			}
		}

		/// <summary>
		/// The user selected an item; now we actually need a LexEntryRef.
		/// </summary>
		/// <param name="newObj"></param>
		private void AddItem(ICmObject newObj)
		{
			CheckDisposed();

			bool fForVariant = XmlUtils.GetOptionalBooleanAttributeValue(m_configurationNode, "forVariant", false);
			string sUndo, sRedo;
			if (fForVariant)
			{
				sUndo = LanguageExplorerResources.ksUndoVariantOf;
				sRedo = LanguageExplorerResources.ksRedoVariantOf;
			}
			else
			{
				sUndo = LanguageExplorerResources.ksUndoAddComponent;
				sRedo = LanguageExplorerResources.ksRedoAddComponent;
			}
			try
			{
				UndoableUnitOfWorkHelper.Do(sUndo, sRedo, m_obj,
					() =>
					{
						ILexEntry ent = m_obj as ILexEntry;

						// Adapted from part of DtMenuHandler.AddNewLexEntryRef.
						ILexEntryRef ler = ent.Services.GetInstance<ILexEntryRefFactory>().Create();
						ent.EntryRefsOS.Add(ler);
						if (fForVariant)
						{
							// The slice this is part of should only be displayed for lex entries with no VariantEntryRefs.
							Debug.Assert(ent.VariantEntryRefs.Count() == 0);
							ler.VariantEntryTypesRS.Add(ent.Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS[0] as ILexEntryType);
							ler.RefType = LexEntryRefTags.krtVariant;
							ler.HideMinorEntry = 0;
						}
						else
						{
							// The slice this is part of should only be displayed for lex entries with no ComplexEntryRefs.
							Debug.Assert(ent.ComplexFormEntryRefs.Count() == 0);
							//ler.ComplexEntryTypesRS.Append(ent.Cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS[0].Hvo);
							ler.RefType = LexEntryRefTags.krtComplexForm;
							ler.HideMinorEntry = 0; // LT-10928
							// Logic similar to this is in EntrySequenceReferenceLauncher.AddNewObjectsToProperty()
							// (when LER already exists so slice is not ghost)
							ler.PrimaryLexemesRS.Add(newObj);
							// Since it's a new LER, we can't know it to be a derivative, so by default it is visible.
							// but do NOT do that here, it's now built into the process of adding it to PrimaryLexemes,
							// and we don't want to do it twice.
							// ler.ShowComplexFormsInRS.Add(newObj);
							ent.ChangeRootToStem();
						}
						// Must do this AFTER setting the RefType (so dependent virtual properties can be updated properly)
						ler.ComponentLexemesRS.Add(newObj);
					});
			}
			catch (ArgumentException)
			{
				MessageBoxes.ReportLexEntryCircularReference((ILexEntry)m_obj, newObj, true);
			}
		}

#if __MonoCS__
	/// <summary>
	/// Activate menu only if Alt key is being pressed.  See FWNX-1353.
	/// </summary>
	/// <remarks>TODO: Getting here without the Alt key may be considered a Mono bug.</remarks>
		protected override bool ProcessDialogChar(char charCode)
		{
			if (Control.ModifierKeys == Keys.Alt)
				return base.ProcessDialogChar(charCode);
			return false;
		}
#endif
	}
}