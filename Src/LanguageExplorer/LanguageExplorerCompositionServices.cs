// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using LanguageExplorer.Areas.Grammar;
using LanguageExplorer.Areas.Grammar.Tools;
using LanguageExplorer.Areas.Lexicon;
using LanguageExplorer.Areas.Lexicon.Tools;
using LanguageExplorer.Areas.Lists;
using LanguageExplorer.Areas.Lists.Tools;
using LanguageExplorer.Areas.Notebook;
using LanguageExplorer.Areas.Notebook.Tools;
using LanguageExplorer.Areas.TextsAndWords;
using LanguageExplorer.Areas.TextsAndWords.Tools.Analyses;
using LanguageExplorer.Areas.TextsAndWords.Tools.BulkEditWordforms;
using LanguageExplorer.Areas.TextsAndWords.Tools.ComplexConcordance;
using LanguageExplorer.Areas.TextsAndWords.Tools.Concordance;
using LanguageExplorer.Areas.TextsAndWords.Tools.CorpusStatistics;
using LanguageExplorer.Areas.TextsAndWords.Tools.InterlinearEdit;
using LanguageExplorer.Areas.TextsAndWords.Tools.WordListConcordance;
using LanguageExplorer.Impls;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer
{
	/// <summary>
	/// Get types in this assembly that are globally and windows level scoped.
	/// </summary>
	internal static class LanguageExplorerCompositionServices
	{
		/// <summary>
		/// Get globally scoped types in this assembly.
		/// </summary>
		internal static IList<Type> GetGloballyAvailableTypes()
		{
			return new List<Type>
			{
				typeof(FlexApp),
				typeof(FlexHelpTopicProvider)
			};
		}

		/// <summary>
		/// Get types in this assembly that are created once per window instance (including the window).
		/// </summary>
		internal static IList<Type> GetWindowScopedTypes()
		{
			return new List<Type>
			{
				typeof(FwMainWnd),
				typeof(Publisher),
				typeof(Subscriber),
				typeof(PropertyTable),
				typeof(IdleQueue),
				typeof(MacroMenuHandler),
				// Areas & their respective tools.
				typeof(AreaRepository),
				typeof(LexiconArea),
					typeof(LexiconEditTool),
					typeof(LexiconBrowseTool),
					typeof(LexiconDictionaryTool),
					typeof(RapidDataEntryTool),
					typeof(LexiconClassifiedDictionaryTool),
					typeof(BulkEditEntriesOrSensesTool),
					typeof(ReversalEditCompleteTool),
					typeof(ReversalBulkEditReversalEntriesTool),
				typeof(TextAndWordsArea),
					typeof(InterlinearEditTool),
					typeof(ConcordanceTool),
					typeof(ComplexConcordanceTool),
					typeof(WordListConcordanceTool),
					typeof(AnalysesTool),
					typeof(BulkEditWordformsTool),
					typeof(CorpusStatisticsTool),
				typeof(GrammarArea),
					typeof(PosEditTool),
					typeof(CategoryBrowseTool),
					typeof(CompoundRuleAdvancedEditTool),
					typeof(PhonemeEditTool),
					typeof(PhonologicalFeaturesAdvancedEditTool),
					typeof(BulkEditPhonemesTool),
					typeof(NaturalClassEditTool),
					typeof(EnvironmentEditTool),
					typeof(PhonologicalRuleEditTool),
					typeof(AdhocCoprohibitionRuleEditTool),
					typeof(FeaturesAdvancedEditTool),
					typeof(ProdRestrictEditTool),
					typeof(GrammarSketchTool),
					typeof(LexiconProblemsTool),
				typeof(NotebookArea),
					typeof(NotebookBrowseTool),
					typeof(NotebookDocumentTool),
					typeof(NotebookEditTool),
				typeof(ListsArea),
					typeof(DomainTypeEditTool),
					typeof(AnthroEditTool),
					typeof(ComplexEntryTypeEditTool),
					typeof(ConfidenceEditTool),
					typeof(DialectsListEditTool),
					typeof(ChartmarkEditTool),
					typeof(CharttempEditTool),
					typeof(EducationEditTool),
					typeof(RoleEditTool),
					typeof(ExtNoteTypeEditTool),
					typeof(FeatureTypesAdvancedEditTool),
					typeof(GenresEditTool),
					typeof(LanguagesListEditTool),
					typeof(LexRefEditTool),
					typeof(LocationsEditTool),
					typeof(PublicationsEditTool),
					typeof(MorphTypeEditTool),
					typeof(PeopleEditTool),
					typeof(PositionsEditTool),
					typeof(RestrictionsEditTool),
					typeof(SemanticDomainEditTool),
					typeof(SenseTypeEditTool),
					typeof(StatusEditTool),
					typeof(TextMarkupTagsEditTool),
					typeof(TranslationTypeEditTool),
					typeof(UsageTypeEditTool),
					typeof(VariantEntryTypeEditTool),
					typeof(RecTypeEditTool),
					typeof(TimeOfDayEditTool),
					typeof(ReversalIndexPosTool)
			};
		}
	}
}
