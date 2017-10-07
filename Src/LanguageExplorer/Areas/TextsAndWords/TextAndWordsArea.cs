// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using LanguageExplorer.Works;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords
{
	/// <summary>
	/// IArea implementation for the area: "textAndWords".
	/// </summary>
	internal sealed class TextAndWordsArea : IArea
	{
		internal const string ConcordanceWords = "concordanceWords";
		private readonly IToolRepository _toolRepository;
		private ToolStrip _insertMenuItem;
		private ToolStripSeparator _separator2ToolStripMenuItem;
		private ToolStripMenuItem _importWordSetToolStripMenuItem;
		private ToolStripMenuItem _addApprovedAnalysisToolStripMenuItem;
		private LcmCache _cache;
		private IFlexApp _flexApp;
		private IFwMainWnd _fwMainWnd;
		private ParserMenuManager _parserMenuManager;

		/// <summary>
		/// Contructor used by Reflection to feed the tool repository to the area.
		/// </summary>
		/// <param name="toolRepository"></param>
		internal TextAndWordsArea(IToolRepository toolRepository)
		{
			_toolRepository = toolRepository;
		}

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		#endregion

		#region Implementation of IFlexComponent

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentCheckingService.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;

			// Respeller dlg uses these.
			PropertyTable.SetDefault("RemoveAnalyses", true, SettingsGroup.GlobalSettings, true, false);
			PropertyTable.SetDefault("UpdateLexiconIfPossible", true, SettingsGroup.GlobalSettings, true, false);
			PropertyTable.SetDefault("CopyAnalysesToNewSpelling", true, SettingsGroup.GlobalSettings, true, false);
			PropertyTable.SetDefault("MaintainCaseOnChangeSpelling", true, SettingsGroup.GlobalSettings, true, false);

			PropertyTable.SetDefault("ITexts_AddWordsToLexicon", false, SettingsGroup.LocalSettings, true, false);
			PropertyTable.SetDefault("ITexts_ShowAddWordsToLexiconDlg", true, SettingsGroup.LocalSettings, true, false);
			PropertyTable.SetDefault("ITexts-ScriptureIds", string.Empty, SettingsGroup.LocalSettings, true, false);
		}

		#endregion

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to a component.
		/// </remarks>
		public void Deactivate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			_insertMenuItem.Items.Remove(_separator2ToolStripMenuItem);
			_separator2ToolStripMenuItem.Dispose();

			_insertMenuItem.Items.Remove(_addApprovedAnalysisToolStripMenuItem);
			_addApprovedAnalysisToolStripMenuItem.Dispose();

			_insertMenuItem.Items.Remove(_importWordSetToolStripMenuItem);
			_importWordSetToolStripMenuItem.Dispose();

			_cache = null;
			_flexApp = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			/*
			These are all possible menu items (Insert menu) for the Text & Words area.
			<menu id="Insert">
						<item command="CmdInsertText" defaultVisible="false"/>
						<item label="-" translate="do not translate"/>
						<item command="CmdAddNote" defaultVisible="false"/>
						<item command="CmdAddWordGlossesToFreeTrans" defaultVisible="false"/>
						<item label="Click Inserts Invisible Space" boolProperty="ClickInvisibleSpace" defaultVisible="false" settingsGroup="local" icon="zeroWidth"/>
						<item command="CmdGuessWordBreaks" defaultVisible="false"/>
						<item label="-" translate="do not translate"/>
DONE:					<item command="CmdImportWordSet" defaultVisible="false"/>
						<item command="CmdInsertHumanApprovedAnalysis" defaultVisible="false"/>
			</menu>
			All of the above menus go above the global separator named "insertMenuLastGlobalSeparator"
			*/
			ToolStripItem insertMenuLastGlobalSeparator = majorFlexComponentParameters.MenuStrip.Items.Find("insertMenuLastGlobalSeparator", true)[0];
			_insertMenuItem = insertMenuLastGlobalSeparator.GetCurrentParent();

			// Add Approved Analysis...
			_addApprovedAnalysisToolStripMenuItem = new ToolStripMenuItem("PH: " + TextAndWordsResources.ksAddApprovedAnalysis, LanguageExplorerResources.Add_New_Analysis.ToBitmap())
			{
				Enabled = false
			};
			_insertMenuItem.Items.Insert(0, _addApprovedAnalysisToolStripMenuItem);

			_importWordSetToolStripMenuItem = new ToolStripMenuItem(FwUtils.ReplaceUnderlineWithAmpersand(TextAndWordsResources.ksImportWordSet));
			_importWordSetToolStripMenuItem.Click += ImportWordSetToolStripMenuItemOnClick;
			_insertMenuItem.Items.Insert(0, _importWordSetToolStripMenuItem);

			_separator2ToolStripMenuItem = new ToolStripSeparator();
			_insertMenuItem.Items.Insert(0, _separator2ToolStripMenuItem);

			_cache = majorFlexComponentParameters.LcmCache;
			_flexApp = majorFlexComponentParameters.FlexApp;
			_fwMainWnd = majorFlexComponentParameters.MainWindow;
		}

		private void ImportWordSetToolStripMenuItemOnClick(object sender, EventArgs eventArgs)
		{
			using (var dlg = new ImportWordSetDlg(_cache, _flexApp, RecordClerk.ActiveRecordClerkRepository.ActiveRecordClerk, _parserMenuManager))
			{
				dlg.ShowDialog((Form)_fwMainWnd);
			}
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
			_toolRepository.GetPersistedOrDefaultToolForArea(this).PrepareToRefresh();
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		public void FinishRefresh()
		{
			_toolRepository.GetPersistedOrDefaultToolForArea(this).FinishRefresh();
		}

		/// <summary>
		/// The properties are about to be saved, so make sure they are all current.
		/// Add new ones, as needed.
		/// </summary>
		public void EnsurePropertiesAreCurrent()
		{
			PropertyTable.SetProperty("InitialArea", MachineName, SettingsGroup.LocalSettings, true, false);

			var myCurrentTool = _toolRepository.GetPersistedOrDefaultToolForArea(this);
			myCurrentTool.EnsurePropertiesAreCurrent();
		}

		#endregion

		#region Implementation of IMajorFlexUiComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName => "textAndWords";

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Texts & Words";
		#endregion

		#region Implementation of IArea

		/// <summary>
		/// Get the most recently persisted tool, or the default tool if
		/// the persisted one is no longer available.
		/// </summary>
		/// <returns>The last persisted tool or the default tool for the area.</returns>
		public ITool GetPersistedOrDefaultToolForArea()
		{
			return _toolRepository.GetPersistedOrDefaultToolForArea(this);
		}

		/// <summary>
		/// Get the machine name of the area's default tool.
		/// </summary>
		public string DefaultToolMachineName => "interlinearEdit";

		/// <summary>
		/// Get all installed tools for the area.
		/// </summary>
		public IList<ITool> AllToolsInOrder
		{
			get
			{
				var myToolsInOrder = new List<string>
				{
					"interlinearEdit",
					"concordance",
					"complexConcordance",
					"wordListConcordance",
					"Analyses",
					"bulkEditWordforms",
					"corpusStatistics"
				};
				return _toolRepository.AllToolsForAreaInOrder(myToolsInOrder, MachineName);
			}
		}

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => LanguageExplorerResources.Text_And_Words.ToBitmap();
		#endregion

		internal static RecordClerk ConcordanceWordsFactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string clerkId, StatusBar statusBar)
		{
			Require.That(clerkId == ConcordanceWords, $"I don't know how to create a clerk with an ID of '{clerkId}', as I can only create on with an id of '{ConcordanceWords}'.");

			return new InterlinearTextsRecordClerk(statusBar, cache.LanguageProject, new ConcDecorator(cache.ServiceLocator));
		}
	}
}
