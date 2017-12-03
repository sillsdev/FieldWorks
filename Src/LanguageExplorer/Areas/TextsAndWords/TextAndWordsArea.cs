// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using LanguageExplorer.Works;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Filters;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords
{
	/// <summary>
	/// IArea implementation for the area: "textAndWords".
	/// </summary>
	[Export(AreaServices.TextAndWordsAreaMachineName, typeof(IArea))]
	[Export(typeof(IArea))]
	internal sealed class TextAndWordsArea : IArea
	{
		[ImportMany(AreaServices.TextAndWordsAreaMachineName)]
		private IEnumerable<ITool> _myTools;
		private const string MyUiName = "Texts & Words";
		internal const string ConcordanceWords = "concordanceWords";
		internal const string InterlinearTexts = "interlinearTexts";
		private string PropertyNameForToolName => $"{AreaServices.ToolForAreaNamed_}{MachineName}";
		private TextAndWordsAreaMenuHelper _textAndWordsAreaMenuHelper;
		private bool _hasBeenActivated;
		[Import]
		private IPropertyTable _propertyTable;

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to a component.
		/// </remarks>
		public void Deactivate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			_textAndWordsAreaMenuHelper.Dispose();
			_textAndWordsAreaMenuHelper = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			_propertyTable.SetDefault(PropertyNameForToolName, AreaServices.TextAndWordsAreaDefaultToolMachineName, SettingsGroup.LocalSettings, true, false);
			if (!_hasBeenActivated)
			{
				// Respeller dlg uses these.
				_propertyTable.SetDefault("RemoveAnalyses", true, SettingsGroup.GlobalSettings, true, false);
				_propertyTable.SetDefault("UpdateLexiconIfPossible", true, SettingsGroup.GlobalSettings, true, false);
				_propertyTable.SetDefault("CopyAnalysesToNewSpelling", true, SettingsGroup.GlobalSettings, true, false);
				_propertyTable.SetDefault("MaintainCaseOnChangeSpelling", true, SettingsGroup.GlobalSettings, true, false);

				_propertyTable.SetDefault("ITexts_AddWordsToLexicon", false, SettingsGroup.LocalSettings, true, false);
				_propertyTable.SetDefault("ITexts_ShowAddWordsToLexiconDlg", true, SettingsGroup.LocalSettings, true, false);
				_propertyTable.SetDefault("ITexts-ScriptureIds", string.Empty, SettingsGroup.LocalSettings, true, false);
				_hasBeenActivated = true;
			}

			_textAndWordsAreaMenuHelper = new TextAndWordsAreaMenuHelper(majorFlexComponentParameters);

			_textAndWordsAreaMenuHelper.InitializeAreaWideMenus();
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
			PersistedOrDefaultTool.PrepareToRefresh();
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		public void FinishRefresh()
		{
			PersistedOrDefaultTool.FinishRefresh();
		}

		/// <summary>
		/// The properties are about to be saved, so make sure they are all current.
		/// Add new ones, as needed.
		/// </summary>
		public void EnsurePropertiesAreCurrent()
		{
			_propertyTable.SetProperty(AreaServices.InitialArea, MachineName, SettingsGroup.LocalSettings, true, false);

			PersistedOrDefaultTool.EnsurePropertiesAreCurrent();
		}

		#endregion

		#region Implementation of IMajorFlexUiComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName => AreaServices.TextAndWordsAreaMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => MyUiName;
		#endregion

		#region Implementation of IArea

		/// <summary>
		/// Get the most recently persisted tool, or the default tool if
		/// the persisted one is no longer available.
		/// </summary>
		/// <returns>The last persisted tool or the default tool for the area.</returns>
		public ITool PersistedOrDefaultTool => _myTools.First(tool => tool.MachineName == _propertyTable.GetValue(PropertyNameForToolName, AreaServices.TextAndWordsAreaDefaultToolMachineName));

		/// <summary>
		/// Get all installed tools for the area.
		/// </summary>
		public IList<ITool> AllToolsInOrder
		{
			get
			{
				var myToolsInOrder = new List<string>
				{
					AreaServices.InterlinearEditMachineName,
					AreaServices.ConcordanceMachineName,
					AreaServices.ComplexConcordanceMachineName,
					AreaServices.WordListConcordanceMachineName,
					AreaServices.AnalysesMachineName,
					AreaServices.BulkEditWordformsMachineName,
					AreaServices.CorpusStatisticsMachineName
				};
				return myToolsInOrder.Select(toolName => _myTools.First(tool => tool.MachineName == toolName)).ToList();
			}
		}

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => LanguageExplorerResources.Text_And_Words.ToBitmap();

		/// <summary>
		/// Set the active tool for the area, or null, if no tool is active.
		/// </summary>
		public ITool ActiveTool { get; set; }

		#endregion

		internal static IRecordClerk ConcordanceWordsFactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string clerkId, StatusBar statusBar)
		{
			Require.That(clerkId == ConcordanceWords, $"I don't know how to create a clerk with an ID of '{clerkId}', as I can only create on with an id of '{ConcordanceWords}'.");

			return new ConcordanceWordList(statusBar, cache.LanguageProject, new ConcDecorator(cache.ServiceLocator));
		}

		internal static IRecordClerk InterlinearTextsFactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string clerkId, StatusBar statusBar)
		{
			Require.That(clerkId == InterlinearTexts, $"I don't know how to create a clerk with an ID of '{clerkId}', as I can only create on with an id of '{InterlinearTexts}'.");

			return new InterlinearTextsRecordList(InterlinearTexts, statusBar, new PropertyRecordSorter("Title"), "Default", null, false, false, new InterestingTextsDecorator(cache.ServiceLocator, flexComponentParameters.PropertyTable), false, InterestingTextsDecorator.kflidInterestingTexts, cache.LanguageProject, "InterestingTexts");
		}
	}
}
