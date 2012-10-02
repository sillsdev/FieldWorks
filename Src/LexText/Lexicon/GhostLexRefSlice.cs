using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.LexText.Controls;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// A slice that is used for the ghost "Components" and "Variant of" fields of LexEntry.
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
			(Control as ButtonLauncher).Initialize(m_cache, m_obj, 1, "nonsence", null, Mediator, null, null);
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
			Debug.Assert(m_obj.ClassID == LexEntry.kclsidLexEntry);
			using (LinkEntryOrSenseDlg dlg = new LinkEntryOrSenseDlg())
			{
				ILexEntry le = m_obj as ILexEntry;
				dlg.SetDlgInfo(m_obj.Cache, m_mediator, le);
				String str = ShowHelp.RemoveSpaces(this.Slice.Label);
				dlg.SetHelpTopic("khtpChooseLexicalEntryOrSense-" + str);
				if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
					AddItem(dlg.SelectedID);

			}
		}

		/// <summary>
		/// The user selected an item; now we actually need a LexEntryRef.
		/// </summary>
		/// <param name="hvoNew"></param>
		private void AddItem(int hvoNew)
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
			using (new UndoRedoTaskHelper(m_obj.Cache, sUndo, sRedo))
			{
				ILexEntry ent = m_obj as ILexEntry;
				// The slice this is part of should only be displayed for lex entries with no EntryRefs.
				Debug.Assert(ent.EntryRefsOS.Count == 0);

				// Adapted from part of DtMenuHandler.AddNewLexEntryRef.
				ILexEntryRef ler = new LexEntryRef();
				ent.EntryRefsOS.Append(ler);
				if (fForVariant)
				{
					ler.VariantEntryTypesRS.Append(ent.Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS[0].Hvo);
					ler.RefType = LexEntryRef.krtVariant;
					ler.HideMinorEntry = 0;
				}
				else
				{
					//ler.ComplexEntryTypesRS.Append(ent.Cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS[0].Hvo);
					ler.RefType = LexEntryRef.krtComplexForm;
					ler.HideMinorEntry = 1;
					ler.PrimaryLexemesRS.Append(hvoNew);
					ent.ChangeRootToStem();
				}
				ler.ComponentLexemesRS.Append(hvoNew);
				// No automatic propchanged for new objects, need to let the view see it.
				// At that point our slice will be disposed, so don't do anything after this.
				ent.Cache.PropChanged(ent.Hvo, (int)LexEntry.LexEntryTags.kflidEntryRefs, 0, 1, 0);
			}
		}
	}
}
