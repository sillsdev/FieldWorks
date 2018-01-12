// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using System.Linq;
using SIL.LCModel.Core.Text;
using LanguageExplorer.Controls.DetailControls.Resources;
using LanguageExplorer.Controls.LexText;
using LanguageExplorer.Controls.XMLViews;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Controls.DetailControls
{
	// Class ChooserCommand has been moved to XmlViews DLL (file ChooserCommandBase).

	/// <summary>
	/// Command for creating a new inflectional affix lexical entry
	/// </summary>
	public class MakeInflAffixEntryChooserCommand : ChooserCommand
	{
		protected bool m_fPrefix;
		protected IMoInflAffixSlot m_slot;

		public MakeInflAffixEntryChooserCommand(LcmCache cache, bool fCloseBeforeExecuting,
			string sLabel, bool fPrefix, IMoInflAffixSlot slot, IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
			: base(cache, fCloseBeforeExecuting, sLabel, propertyTable, publisher, subscriber)
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
				dlg.InitializeFlexComponent(new FlexComponentParameters(m_propertyTable, m_publisher, m_subscriber));
				var morphType = GetMorphType();
				dlg.SetDlgInfo(m_cache, morphType, MsaType.kInfl, m_slot, m_fPrefix ? MorphTypeFilterType.Prefix : MorphTypeFilterType.Suffix);
				dlg.DisableAffixTypeMainPosAndSlot();
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					bool fCreated;
					ILexEntry entry;
					dlg.GetDialogInfo(out entry, out fCreated);
					if (entry == null)
						throw new ArgumentNullException("Expected entry cannot be null", "entry");
					// TODO: what do to make sure it has an infl affix msa?
					// this just assumes it will
					bool fInflAffix = false;
					foreach (var msa in entry.MorphoSyntaxAnalysesOC)
					{
						if (msa is IMoInflAffMsa)
						{
							fInflAffix = true;
							break;
						}
					}
					if (!fInflAffix)
					{
						UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(DetailControlsStrings.ksUndoCreatingInflectionalAffixCategoryItem,
							DetailControlsStrings.ksRedoCreatingInflectionalAffixCategoryItem,
							m_cache.ActionHandlerAccessor,
							() => {
								var newby = m_cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
								entry.MorphoSyntaxAnalysesOC.Add(newby);
							});
					}
					if (entry.MorphoSyntaxAnalysesOC.Count > 0)
						result = ObjectLabel.CreateObjectLabel(m_cache, entry.MorphoSyntaxAnalysesOC.First(), "");
				}
			}
			return result;
		}

		private IMoMorphType GetMorphType()
		{
			IMoMorphType morphType = null;
			string sMorphTypeName = (m_fPrefix ? "prefix" : "suffix");
			int iEnglishWs = WritingSystemServices.FallbackUserWs(m_cache);
			foreach (var type in m_cache.LanguageProject.LexDbOA.MorphTypesOA.ReallyReallyAllPossibilities)
			{
				if (sMorphTypeName == type.Name.get_String(iEnglishWs).Text)
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

		public MakeInflAffixSlotChooserCommand(LcmCache cache, bool fCloseBeforeExecuting, string sLabel, int posHvo, bool fOptional, IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
			: base(cache, fCloseBeforeExecuting, sLabel, propertyTable, publisher, subscriber)
		{
			m_posHvo = posHvo;
			m_fOptional = fOptional;
		}

		//methods

		public override ObjectLabel Execute()
		{
			var slot = m_cache.ServiceLocator.GetInstance<IMoInflAffixSlotFactory>().Create();
			var pos = m_cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(m_posHvo);
			UndoableUnitOfWorkHelper.Do(DetailControlsStrings.ksUndoCreateSlot, DetailControlsStrings.ksRedoCreateSlot,
				Cache.ActionHandlerAccessor,
				() =>
					{
						pos.AffixSlotsOC.Add(slot);
						string sNewSlotName = StringTable.Table.GetString("NewSlotName", "Linguistics/Morphology/TemplateTable");
						int defAnalWs = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
						slot.Name.set_String(defAnalWs, TsStringUtils.MakeString(sNewSlotName, defAnalWs));
						slot.Optional = m_fOptional;
					});
			// Enhance JohnT: usually the newly created slot will also get inserted into a template.
			// Ideally we would make both part of the same UOW. However the code is in two distinct DLLs (see MorphologyEditor.dll).
			return ObjectLabel.CreateObjectLabel(m_cache, slot, "");
		}
	}
}
