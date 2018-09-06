// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.LexText.Controls;
using SIL.PlatformUtilities;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// A slice that is used for the ghost "Components" and "Variant of" fields of ILexEntry.
	/// It tries to behave like an empty list of components, but in fact the only real content is
	/// the chooser button, which behaves much like the one in an EntrySequenceReferenceSlice
	/// as used to display the ComponentLexemes of a LexEntryRef, except the list is always
	/// empty and hence not really there.
	/// </summary>
	public class GhostLexRefSlice : Slice
	{
		public GhostLexRefSlice()
		{
		}

		public override void FinishInit()
		{
			this.Control = new GhostLexRefLauncher(m_obj, m_configurationNode);
		}

		public override void Install(DataTree parent)
		{
			// JohnT: This is an awful way to make the button fit neatly, but I can't find a better one.
			Control.Height = Height;
			// It doesn't need most of the usual info, but the Mediator is important if the user
			// asks to Create a new lex entry from inside the first dialog (LT-9679).
			// We'd pass 0 and null for flid and fieldname, but there are Asserts to prevent this.
			(Control as ButtonLauncher).Initialize(m_cache, m_obj, 1, "nonsence", null, Mediator, m_propertyTable, null, null);
			base.Install(parent);
		}
	}

	public class GhostLexRefLauncher : ButtonLauncher
	{
		public GhostLexRefLauncher(ICmObject obj, XmlNode configNode)
		{
			m_obj = obj;
			m_configurationNode = configNode;
			// Makes the rest of the control look like content, though empty.
			BackColor = System.Drawing.SystemColors.Window;
		}

		protected override void HandleChooser()
		{
			Debug.Assert(m_obj.ClassID == LexEntryTags.kClassId);
			using (LinkEntryOrSenseDlg dlg = new LinkEntryOrSenseDlg())
			{
				ILexEntry le = m_obj as ILexEntry;
				dlg.SetDlgInfo(m_obj.Cache, m_mediator, m_propertyTable, le);
				String str = ShowHelp.RemoveSpaces(this.Slice.Label);
				dlg.SetHelpTopic("khtpChooseLexicalEntryOrSense-" + str);
				if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
					AddItem(dlg.SelectedObject);

			}
		}

		/// <summary>
		/// The user selected an item; now we actually need a LexEntryRef.
		/// </summary>
		/// <param name="hvoNew"></param>
		private void AddItem(ICmObject newObj)
		{
			CheckDisposed();

			bool fForVariant = XmlUtils.GetOptionalBooleanAttributeValue(m_configurationNode, "forVariant", false);
			string sUndo, sRedo;
			if (fForVariant)
			{
				sUndo = LexEdStrings.ksUndoVariantOf;
				sRedo = LexEdStrings.ksRedoVariantOf;
			}
			else
			{
				sUndo = LexEdStrings.ksUndoAddComponent;
				sRedo = LexEdStrings.ksRedoAddComponent;
			}
			try
			{
				UndoableUnitOfWorkHelper.Do(sUndo, sRedo, m_obj,
				() =>
				{
					ILexEntry ent = m_obj as ILexEntry;

					// It IS a ghost slice after all; it shouldn't already have any of whatever we're about to add.
					Debug.Assert(!(fForVariant ? ent.VariantEntryRefs.Any() : ent.ComplexFormEntryRefs.Any()));
					if (fForVariant ? ent.VariantEntryRefs.Any() : ent.ComplexFormEntryRefs.Any())
						return;

					// Adapted from part of DtMenuHandler.AddNewLexEntryRef.
					ILexEntryRef ler = ent.Services.GetInstance<ILexEntryRefFactory>().Create();
					ent.EntryRefsOS.Add(ler);
					if (fForVariant)
					{
						const string unspecVariantEntryTypeGuid = "3942addb-99fd-43e9-ab7d-99025ceb0d4e";
						var type = ent.Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS
							.First(lrt => lrt.Guid.ToString() == unspecVariantEntryTypeGuid) as ILexEntryType;
						ler.VariantEntryTypesRS.Add(type);
						ler.RefType = LexEntryRefTags.krtVariant;
						ler.HideMinorEntry = 0;
					}
					else
					{
						ler.RefType = LexEntryRefTags.krtComplexForm;
						const string unspecComplexFormEntryTypeGuid = "fec038ed-6a8c-4fa5-bc96-a4f515a98c50";
						var type = ent.Cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS
							.First(lrt => lrt.Guid.ToString() == unspecComplexFormEntryTypeGuid) as ILexEntryType;
						ler.ComplexEntryTypesRS.Add(type);
						ler.HideMinorEntry = 0; // LT-10928
						// Logic similar to this is in EntrySequenceReferenceLauncher.AddNewObjectsToProperty()
						// (when LER already exists so slice is not ghost)
						ler.PrimaryLexemesRS.Add(newObj);
						// Since it's a new LER, we can't know it to be a derivative, so by default it is visible.
						// but do NOT do that here, it's now built into the process of adding it to PrimaryLexemes,
						// and we don't want to do it twice.
						ent.ChangeRootToStem();
					}
					// Must do this AFTER setting the RefType (so dependent virtual properties can be updated properly)
					ler.ComponentLexemesRS.Add(newObj);
				});
			}
			catch (ArgumentException)
			{
				MessageBoxes.ReportLexEntryCircularReference(m_obj, newObj, true);
			}
		}

		/// <summary>
		/// Activate menu only if Alt key is being pressed.  See FWNX-1353.
		/// </summary>
		/// <remarks>TODO: Getting here without the Alt key may be considered a Mono bug.</remarks>
		protected override bool ProcessDialogChar(char charCode)
		{
			if (!Platform.IsMono || Control.ModifierKeys == Keys.Alt)
				return base.ProcessDialogChar(charCode);
			return false;
		}
	}
}
