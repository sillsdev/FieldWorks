// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Controls.DetailControls.Resources;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Controls.DetailControls
{
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
					var sNewSlotName = StringTable.Table.GetString("NewSlotName", "Linguistics/Morphology/TemplateTable");
					var defAnalWs = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
					slot.Name.set_String(defAnalWs, TsStringUtils.MakeString(sNewSlotName, defAnalWs));
					slot.Optional = m_fOptional;
				});
			// Enhance JohnT: usually the newly created slot will also get inserted into a template.
			// Ideally we would make both part of the same UOW. However the code is in two distinct DLLs (see MorphologyEditor.dll).
			return ObjectLabel.CreateObjectLabel(m_cache, slot, "");
		}
	}
}