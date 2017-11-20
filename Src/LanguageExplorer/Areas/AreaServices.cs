// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using LanguageExplorer.Controls.LexText;
using LanguageExplorer.Controls.LexText.DataNotebook;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// Area level services
	/// </summary>
	internal static class AreaServices
	{
		/// <summary>
		/// Display for required default sorter.
		/// </summary>
		internal const string Default = "Default";
		internal const string ToolForAreaNamed_ = "ToolForAreaNamed_";
		internal const string InitialArea = "InitialArea";
		internal const string InitialAreaMachineName = LexiconAreaMachineName;

		#region Lexicon area
		internal const string LexiconAreaMachineName = "lexicon";
		internal const string LexiconAreaDefaultToolMachineName = LexiconEditMachineName;
			internal const string LexiconEditMachineName = "lexiconEdit";
			internal const string LexiconBrowseMachineName = "lexiconBrowse";
			internal const string LexiconDictionaryMachineName = "lexiconDictionary";
			internal const string RapidDataEntryMachineName = "rapidDataEntry";
			internal const string LexiconClassifiedDictionaryMachineName = "lexiconClassifiedDictionary";
			internal const string BulkEditEntriesOrSensesMachineName = "bulkEditEntriesOrSenses";
			internal const string ReversalEditCompleteMachineName = "reversalEditComplete";
			internal const string ReversalBulkEditReversalEntriesMachineName = "reversalBulkEditReversalEntries";
		#endregion

		#region Text and Words area
		internal const string TextAndWordsAreaMachineName = "textsWords";
		internal const string TextAndWordsAreaDefaultToolMachineName = InterlinearEditMachineName;
		internal const string InterlinearEditMachineName = "interlinearEdit";
		internal const string ConcordanceMachineName = "concordance";
		internal const string ComplexConcordanceMachineName = "complexConcordance";
		internal const string WordListConcordanceMachineName = "wordListConcordance";
		internal const string AnalysesMachineName = "Analyses";
		internal const string BulkEditWordformsMachineName = "bulkEditWordforms";
		internal const string CorpusStatisticsMachineName = "corpusStatistics";
		#endregion

		#region Grammar area
		internal const string GrammarAreaMachineName = "grammar";
		internal const string GrammarAreaDefaultToolMachineName = PosEditMachineName;
			internal const string PosEditMachineName = "posEdit";
			internal const string CategoryBrowseMachineName = "categoryBrowse";
			internal const string CompoundRuleAdvancedEditMachineName = "compoundRuleAdvancedEdit";
			internal const string PhonemeEditMachineName = "phonemeEdit";
			internal const string PhonologicalFeaturesAdvancedEditMachineName = "phonologicalFeaturesAdvancedEdit";
			internal const string BulkEditPhonemesMachineName = "bulkEditPhonemes";
			internal const string NaturalClassEditMachineName = "naturalClassEdit";
			internal const string EnvironmentEditMachineName = "EnvironmentEdit";
			internal const string PhonologicalRuleEditMachineName = "PhonologicalRuleEdit";
			internal const string AdhocCoprohibitionRuleEditMachineName = "AdhocCoprohibitionRuleEdit";
			internal const string FeaturesAdvancedEditMachineName = "featuresAdvancedEdit";
			internal const string ProdRestrictEditMachineName = "ProdRestrictEdit";
			internal const string GrammarSketchMachineName = "grammarSketch";
			internal const string LexiconProblemsMachineName = "lexiconProblems";
		#endregion

		#region Notebook area
		internal const string NotebookAreaMachineName = "notebook";
		internal const string NotebookAreaDefaultToolMachineName = NotebookEditToolMachineName;
			internal const string NotebookEditToolMachineName = "notebookEdit";
			internal const string NotebookBrowseToolMachineName = "notebookBrowse";
			internal const string NotebookDocumentToolMachineName = "notebookDocument";
		#endregion

		#region Lists area
		internal const string ListsAreaMachineName = "lists";
		internal const string ListsAreaDefaultToolMachineName = DomainTypeEditMachineName;
			internal const string DomainTypeEditMachineName = "domainTypeEdit";
			internal const string AnthroEditMachineName = "anthroEdit";
			internal const string ComplexEntryTypeEditMachineName = "complexEntryTypeEdit";
			internal const string ConfidenceEditMachineName = "confidenceEdit";
			internal const string ChartmarkEditMachineName = "chartmarkEdit";
			internal const string CharttempEditMachineName = "charttempEdit";
			internal const string EducationEditMachineName = "educationEdit";
			internal const string RoleEditMachineName = "roleEdit";
			internal const string FeatureTypesAdvancedEditMachineName = "featureTypesAdvancedEdit";
			internal const string GenresEditMachineName = "genresEdit";
			internal const string LexRefEditMachineName = "lexRefEdit";
			internal const string LocationsEditMachineName = "locationsEdit";
			internal const string PublicationsEditMachineName = "publicationsEdit";
			internal const string MorphTypeEditMachineName = "morphTypeEdit";
			internal const string PeopleEditMachineName = "peopleEdit";
			internal const string PositionsEditMachineName = "positionsEdit";
			internal const string RestrictionsEditMachineName = "restrictionsEdit";
			internal const string SemanticDomainEditMachineName = "semanticDomainEdit";
			internal const string SenseTypeEditMachineName = "senseTypeEdit";
			internal const string StatusEditMachineName = "statusEdit";
			internal const string TextMarkupTagsEditMachineName = "textMarkupTagsEdit";
			internal const string TranslationTypeEditMachineName = "translationTypeEdit";
			internal const string UsageTypeEditMachineName = "usageTypeEdit";
			internal const string VariantEntryTypeEditMachineName = "variantEntryTypeEdit";
			internal const string RecTypeEditMachineName = "recTypeEdit";
			internal const string TimeOfDayEditMachineName = "timeOfDayEdit";
			internal const string ReversalToolReversalIndexPOSMachineName = "reversalToolReversalIndexPOS";
		#endregion

		/// <summary>
		/// Handle the provided import dialog.
		/// </summary>
		internal static void HandleDlg(Form importDlg, LcmCache cache, IFlexApp flexApp, IFwMainWnd mainWindow, IPropertyTable propertyTable, IPublisher publisher)
		{
			var oldWsUser = cache.WritingSystemFactory.UserWs;
			((IFwExtension)importDlg).Init(cache, propertyTable, publisher);
			if (importDlg.ShowDialog((Form)mainWindow) != DialogResult.OK)
			{
				return;
			}
			// NB: Some clients are not any of the types that are checked below, which is fine. That means nothing else is done here.
			if (importDlg is LexOptionsDlg)
			{
				if (oldWsUser != cache.WritingSystemFactory.UserWs || ((LexOptionsDlg)importDlg).PluginsUpdated)
				{
					flexApp.ReplaceMainWindow(mainWindow);
				}
			}
			else if (importDlg is LinguaLinksImportDlg || importDlg is InterlinearImportDlg || importDlg is LexImportWizard || importDlg is NotebookImportWiz || importDlg is LiftImportDlg)
			{
				// Make everything we've imported visible.
				mainWindow.RefreshAllViews();
			}
		}
	}
}
