// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using LanguageExplorer.Areas.Grammar;
using LanguageExplorer.Areas.Grammar.Tools.AdhocCoprohibEdit;
using LanguageExplorer.Areas.Grammar.Tools.BulkEditPhonemes;
using LanguageExplorer.Areas.Grammar.Tools.CategoryBrowse;
using LanguageExplorer.Areas.Grammar.Tools.CompoundRuleAdvancedEdit;
using LanguageExplorer.Areas.Grammar.Tools.EnvironmentEdit;
using LanguageExplorer.Areas.Grammar.Tools.FeaturesAdvancedEdit;
using LanguageExplorer.Areas.Grammar.Tools.GrammarSketch;
using LanguageExplorer.Areas.Grammar.Tools.LexiconProblems;
using LanguageExplorer.Areas.Grammar.Tools.NaturalClassEdit;
using LanguageExplorer.Areas.Grammar.Tools.PhonemeEdit;
using LanguageExplorer.Areas.Grammar.Tools.PhonologicalFeaturesAdvancedEdit;
using LanguageExplorer.Areas.Grammar.Tools.PhonologicalRuleEdit;
using LanguageExplorer.Areas.Grammar.Tools.PosEdit;
using LanguageExplorer.Areas.Grammar.Tools.ProdRestrictEdit;
using LanguageExplorer.Areas.Lexicon;
using LanguageExplorer.Areas.Lexicon.Tools.Browse;
using LanguageExplorer.Areas.Lexicon.Tools.BulkEditEntries;
using LanguageExplorer.Areas.Lexicon.Tools.BulkEditReversalEntries;
using LanguageExplorer.Areas.Lexicon.Tools.ClassifiedDictionary;
using LanguageExplorer.Areas.Lexicon.Tools.CollectWords;
using LanguageExplorer.Areas.Lexicon.Tools.Dictionary;
using LanguageExplorer.Areas.Lexicon.Tools.Edit;
using LanguageExplorer.Areas.Lexicon.Tools.ReversalIndexes;
using LanguageExplorer.Areas.Lists;
using LanguageExplorer.Areas.Lists.Tools.AnthroEdit;
using LanguageExplorer.Areas.Lists.Tools.ChartmarkEdit;
using LanguageExplorer.Areas.Lists.Tools.ChartTempEdit;
using LanguageExplorer.Areas.Lists.Tools.ComplexEntryTypeEdit;
using LanguageExplorer.Areas.Lists.Tools.ConfidenceEdit;
using LanguageExplorer.Areas.Lists.Tools.DialectsListEdit;
using LanguageExplorer.Areas.Lists.Tools.DomainTypeEdit;
using LanguageExplorer.Areas.Lists.Tools.EducationEdit;
using LanguageExplorer.Areas.Lists.Tools.ExtNoteTypeEdit;
using LanguageExplorer.Areas.Lists.Tools.FeatureTypesAdvancedEdit;
using LanguageExplorer.Areas.Lists.Tools.GenresEdit;
using LanguageExplorer.Areas.Lists.Tools.LanguagesListEdit;
using LanguageExplorer.Areas.Lists.Tools.LexRefEdit;
using LanguageExplorer.Areas.Lists.Tools.LocationsEdit;
using LanguageExplorer.Areas.Lists.Tools.MorphTypeEdit;
using LanguageExplorer.Areas.Lists.Tools.PeopleEdit;
using LanguageExplorer.Areas.Lists.Tools.PositionsEdit;
using LanguageExplorer.Areas.Lists.Tools.PublicationsEdit;
using LanguageExplorer.Areas.Lists.Tools.RecTypeEdit;
using LanguageExplorer.Areas.Lists.Tools.RestrictionsEdit;
using LanguageExplorer.Areas.Lists.Tools.ReversalIndexPOS;
using LanguageExplorer.Areas.Lists.Tools.RoleEdit;
using LanguageExplorer.Areas.Lists.Tools.SemanticDomainEdit;
using LanguageExplorer.Areas.Lists.Tools.SenseTypeEdit;
using LanguageExplorer.Areas.Lists.Tools.StatusEdit;
using LanguageExplorer.Areas.Lists.Tools.TextMarkupTagsEdit;
using LanguageExplorer.Areas.Lists.Tools.TimeOfDayEdit;
using LanguageExplorer.Areas.Lists.Tools.TranslationTypeEdit;
using LanguageExplorer.Areas.Lists.Tools.UsageTypeEdit;
using LanguageExplorer.Areas.Lists.Tools.VariantEntryTypeEdit;
using LanguageExplorer.Areas.Notebook;
using LanguageExplorer.Areas.Notebook.Tools.NotebookBrowse;
using LanguageExplorer.Areas.Notebook.Tools.NotebookDocument;
using LanguageExplorer.Areas.Notebook.Tools.NotebookEdit;
using LanguageExplorer.Areas.TextsAndWords;
using LanguageExplorer.Areas.TextsAndWords.Tools.Analyses;
using LanguageExplorer.Areas.TextsAndWords.Tools.BulkEditWordforms;
using LanguageExplorer.Areas.TextsAndWords.Tools.ComplexConcordance;
using LanguageExplorer.Areas.TextsAndWords.Tools.Concordance;
using LanguageExplorer.Areas.TextsAndWords.Tools.CorpusStatistics;
using LanguageExplorer.Areas.TextsAndWords.Tools.InterlinearEdit;
using LanguageExplorer.Areas.TextsAndWords.Tools.WordListConcordance;
using LanguageExplorer.Impls;

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
