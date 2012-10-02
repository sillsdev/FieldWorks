// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2003' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ChooserCommand.cs
// Responsibility: Andy Black
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Controls;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	// Class ChooserCommand has been moved to XmlViews DLL (file ChooserCommandBase).

	/// <summary>
	/// Command for creating a new inflectional affix lexical entry
	/// </summary>
	public class MakeInflAffixEntryChooserCommand : ChooserCommand
	{
		protected bool m_fPrefix;
		protected IMoInflAffixSlot m_slot;

		public MakeInflAffixEntryChooserCommand(FdoCache cache, bool fCloseBeforeExecuting,
			string sLabel, bool fPrefix, IMoInflAffixSlot slot, XCore.Mediator mediator)
			: base(cache, fCloseBeforeExecuting, sLabel, mediator)
		{
			m_fPrefix = fPrefix;
			m_slot = slot;
		}

		//methods

		public override ObjectLabel Execute()
		{
			// Make create lex entry dialog and invoke it.
			ObjectLabel result = null;
			using (InsertEntryDlg dlg = new InsertEntryDlg())
			{
				IMoMorphType morphType = GetMorphType();
				dlg.SetDlgInfo(m_cache, morphType, MsaType.kInfl, m_slot, m_mediator,
					m_fPrefix ? InsertEntryDlg.MorphTypeFilterType.prefix : InsertEntryDlg.MorphTypeFilterType.suffix);
				dlg.DisableAffixTypeMainPosAndSlot();
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					bool fCreated;
					int entryID;
					dlg.GetDialogInfo(out entryID, out fCreated);
					if (entryID <= 0)
						throw new ArgumentException("Expected entry ID to be greater than 0", "entryID");
					ILexEntry lex = LexEntry.CreateFromDBObject(m_cache, entryID);
					// TODO: what do to make sure it has an infl affix msa?
					// this just assumes it will
					bool fInflAffix = false;
					foreach (IMoMorphSynAnalysis msa in lex.MorphoSyntaxAnalysesOC)
					{
						if (msa is IMoInflAffMsa)
						{
							fInflAffix = true;
							break;
						}
					}
					if (!fInflAffix)
					{
						int hvoNew = m_cache.CreateObject((int)MoInflAffMsa.kClassId,
							lex.Hvo, (int)LexEntry.LexEntryTags.kflidMorphoSyntaxAnalyses, 0);
					}
					int[] hvos = lex.MorphoSyntaxAnalysesOC.HvoArray;
					if (hvos.Length > 0)
						result = ObjectLabel.CreateObjectLabel(m_cache, hvos[0], "");
				}
			}
			return result;
		}

		private IMoMorphType GetMorphType()
		{
			IMoMorphType morphType = null;
			string sMorphTypeName;
			sMorphTypeName = (m_fPrefix ? "prefix" : "suffix");
			int iEnglishWs = Cache.FallbackUserWs;

			foreach (ICmPossibility type in m_cache.LangProject.LexDbOA.MorphTypesOA.ReallyReallyAllPossibilities)
			{
				if (sMorphTypeName == type.Name.GetAlternative(iEnglishWs))
				{
					morphType = type as IMoMorphType;
					break;
				}
			}
			return morphType;
		}

	}
	/// <summary>
	/// Command for creating a new inflectional affix lexical entry
	/// </summary>
	public class MakeInflAffixSlotChooserCommand : ChooserCommand
	{
		protected int m_posHvo;
		protected bool m_fOptional;

		public MakeInflAffixSlotChooserCommand(FdoCache cache, bool fCloseBeforeExecuting, string sLabel, int posHvo, bool fOptional, XCore.Mediator mediator)
			: base(cache, fCloseBeforeExecuting, sLabel, mediator)
		{
			m_posHvo = posHvo;
			m_fOptional = fOptional;
		}

		//methods

		public override ObjectLabel Execute()
		{
			IMoInflAffixSlot slot = new MoInflAffixSlot();
			IPartOfSpeech pos = PartOfSpeech.CreateFromDBObject(m_cache, m_posHvo);
			pos.AffixSlotsOC.Add(slot);
			string sNewSlotName = m_mediator.StringTbl.GetString("NewSlotName", "Linguistics/Morphology/TemplateTable");
			slot.Name.AnalysisDefaultWritingSystem = sNewSlotName;
			slot.Optional = m_fOptional;
			return ObjectLabel.CreateObjectLabel(m_cache, slot.Hvo, "");
		}

	}
}
