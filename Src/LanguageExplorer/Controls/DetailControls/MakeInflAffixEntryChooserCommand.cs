// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls.Resources;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// Command for creating a new inflectional affix lexical entry
	/// </summary>
	public class MakeInflAffixEntryChooserCommand : ChooserCommand
	{
		protected bool m_fPrefix;
		protected IMoInflAffixSlot m_slot;

		public MakeInflAffixEntryChooserCommand(LcmCache cache, bool fCloseBeforeExecuting, string sLabel, bool fPrefix, IMoInflAffixSlot slot, IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
			: base(cache, fCloseBeforeExecuting, sLabel, propertyTable, publisher, subscriber)
		{
			m_fPrefix = fPrefix;
			m_slot = slot;
		}

		public override ObjectLabel Execute()
		{
			// Make create lex entry dialog and invoke it.
			ObjectLabel result = null;
			using (var dlg = new InsertEntryDlg())
			{
				dlg.InitializeFlexComponent(new FlexComponentParameters(m_propertyTable, m_publisher, m_subscriber));
				var morphType = GetMorphType();
				dlg.SetDlgInfo(Cache, morphType, MsaType.kInfl, m_slot, m_fPrefix ? MorphTypeFilterType.Prefix : MorphTypeFilterType.Suffix);
				dlg.DisableAffixTypeMainPosAndSlot();
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					bool fCreated;
					ILexEntry entry;
					dlg.GetDialogInfo(out entry, out fCreated);
					if (entry == null)
					{
						throw new ArgumentNullException("Expected entry cannot be null", "entry");
					}
					// TODO: what do to make sure it has an infl affix msa?
					// this just assumes it will
					var fInflAffix = entry.MorphoSyntaxAnalysesOC.OfType<IMoInflAffMsa>().Any();
					if (!fInflAffix)
					{
						UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(DetailControlsStrings.ksUndoCreatingInflectionalAffixCategoryItem, DetailControlsStrings.ksRedoCreatingInflectionalAffixCategoryItem,
							Cache.ActionHandlerAccessor, () =>
							{
								var newby = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
								entry.MorphoSyntaxAnalysesOC.Add(newby);
							});
					}
					if (entry.MorphoSyntaxAnalysesOC.Count > 0)
					{
						result = ObjectLabel.CreateObjectLabel(Cache, entry.MorphoSyntaxAnalysesOC.First(), string.Empty);
					}
				}
			}
			return result;
		}

		private IMoMorphType GetMorphType()
		{
			var sMorphTypeName = (m_fPrefix ? "prefix" : "suffix");
			var iEnglishWs = WritingSystemServices.FallbackUserWs(Cache);
			return Cache.LanguageProject.LexDbOA.MorphTypesOA.ReallyReallyAllPossibilities.Where(type => sMorphTypeName == type.Name.get_String(iEnglishWs).Text).Select(type => type as IMoMorphType).FirstOrDefault();
		}
	}
}