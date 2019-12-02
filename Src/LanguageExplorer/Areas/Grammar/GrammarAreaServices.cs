// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Grammar
{
	/// <summary>
	/// Handle partially shared code in the Grammar area.
	/// </summary>
	internal class GrammarAreaServices
	{
		internal const string Phonemes = "phonemes";
		private LcmCache _cache;

		internal void Setup_CmdInsertPhoneme(LcmCache cache, ToolUiWidgetParameterObject toolUiWidgetParameterObject)
		{
			_cache = cache;

			// <command id="CmdInsertPhoneme" label="Phoneme" message="InsertItemInVector" icon="phoneme" shortcut="Ctrl+I">
			UiWidgetServices.InsertPair(toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Insert], toolUiWidgetParameterObject.ToolBarItemsForTool[ToolBar.Insert],
				Command.CmdInsertPhoneme, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(InsertPhoneme_Clicked, ()=> UiWidgetServices.CanSeeAndDo));
		}

		private void InsertPhoneme_Clicked(object sender, EventArgs e)
		{
			/*
			<command id="CmdInsertPhoneme" label="Phoneme" message="InsertItemInVector" icon="phoneme" shortcut="Ctrl+I">
				<params className="PhPhoneme" />
			</command>
			*/
			UowHelpers.UndoExtension(GrammarResources.Insert_Phoneme, _cache.ActionHandlerAccessor, () =>
			{
				_cache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS[0].PhonemesOC.Add(_cache.ServiceLocator.GetInstance<IPhPhonemeFactory>().Create());
			});
		}

		internal static IRecordList PhonemesFactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Require.That(recordListId == Phonemes, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create one with an id of '{Phonemes}'.");
			/*
            <clerk id="phonemes">
              <recordList owner="MorphologicalData" property="Phonemes" />
            </clerk>
			*/
			return new RecordList(recordListId, statusBar, cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), true,
				new VectorPropertyParameterObject(cache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS[0], "Phonemes", PhPhonemeSetTags.kflidPhonemes));
		}
	}
}