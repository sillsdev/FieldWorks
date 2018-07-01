// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// Implementation that supports the addition(s) to FLEx's right-click context menus for use by any tool.
	/// </summary>
	internal sealed class RightClickContextMenuManager : IToolUiWidgetManager
	{
		private const string mnuObjectChoices = "mnuObjectChoices";
		private const string mnuReferenceChoices = "mnuReferenceChoices";
		private const string mnuEnvReferenceChoices = "mnuEnvReferenceChoices";
		private const string mnuPhRegularRule = "mnuPhRegularRule";
		private const string mnuPhMetathesisRule = "mnuPhMetathesisRule";
		private const string mnuMoAffixProcess = "mnuMoAffixProcess";

		private ITool _currentTool;
		private DataTree MyDataTree { get; set; }
		private FlexComponentParameters _flexComponentParameters;
		private ISharedEventHandlers _sharedEventHandlers;
		private IRecordList MyRecordList { get; set; }

		internal RightClickContextMenuManager(ITool currentTool, DataTree dataTree)
		{
			Guard.AgainstNull(currentTool, nameof(currentTool));
			Guard.AgainstNull(dataTree, nameof(dataTree));

			_currentTool = currentTool;
			MyDataTree = dataTree;
		}

		#region Implementation of IToolUiWidgetManager

		/// <inheritdoc />
		public void Initialize(MajorFlexComponentParameters majorFlexComponentParameters, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(recordList, nameof(recordList));

			_flexComponentParameters = majorFlexComponentParameters.FlexComponentParameters;
			_sharedEventHandlers = majorFlexComponentParameters.SharedEventHandlers;
			MyRecordList = recordList;

			var rightClickPopupMenuFactory = MyDataTree.DataTreeStackContextMenuFactory.RightClickPopupMenuFactory;

			rightClickPopupMenuFactory.RegisterPopupContextCreatorMethod(mnuObjectChoices, PopupContextMenuCreatorMethod_mnuObjectChoices);
			rightClickPopupMenuFactory.RegisterPopupContextCreatorMethod(mnuReferenceChoices, PopupContextMenuCreatorMethod_mnuReferenceChoices);
			rightClickPopupMenuFactory.RegisterPopupContextCreatorMethod(mnuEnvReferenceChoices, PopupContextMenuCreatorMethod_mnuEnvReferenceChoices);
			rightClickPopupMenuFactory.RegisterPopupContextCreatorMethod(mnuPhRegularRule, PopupContextMenuCreatorMethod_mnuPhRegularRule);
			rightClickPopupMenuFactory.RegisterPopupContextCreatorMethod(mnuPhMetathesisRule, PopupContextMenuCreatorMethod_mnuPhMetathesisRule);
			rightClickPopupMenuFactory.RegisterPopupContextCreatorMethod(mnuMoAffixProcess, PopupContextMenuCreatorMethod_mnuMoAffixProcess);
		}

		#endregion

		#region Implementation of IDisposable

		private bool _isDisposed;

		~RightClickContextMenuManager()
		{
			// The base class finalizer is called automatically.
			Dispose(false);
		}


		/// <inheritdoc />
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");

			if (_isDisposed)
			{
				// No need to do it more than once.
				return;
			}

			if (disposing)
			{
			}

			_isDisposed = true;
		}

		#endregion

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> PopupContextMenuCreatorMethod_mnuObjectChoices(Slice slice, string contextMenuId)
		{
			/*
		    <menu id="mnuObjectChoices">
		      <item command="CmdEntryJumpToDefault" /> // Also in "mnuBrowseView", menu: "mnuInflAffixTemplate-TemplateTable" & LexiconEditToolDataTreeStackLexEntryFormsManager->Create_mnuDataTree_VariantForm.
		      <item command="CmdWordformJumpToAnalyses" /> // Also in "mnuBrowseView" & menu: "mnuIText-RawText".
				    <command id="CmdWordformJumpToAnalyses" label="Show in Word Analyses" message="JumpToTool">
				      <parameters tool="Analyses" className="WfiWordform" />
				    </command>
		      <menu label="Show Concordance of">
		        <item command="CmdWordformJumpToConcordance" label="Wordform" /> // Also in "mnuBrowseView" & menu: "mnuIText-RawText".
					    <command id="CmdWordformJumpToConcordance" label="Show Wordform in Concordance" message="JumpToTool">
					      <parameters tool="concordance" className="WfiWordform" />
					    </command>
		        <item command="CmdAnalysisJumpToConcordance" label="Analysis" /> // Also: menu "mnuDataTree-HumanApprovedAnalysis" & another place in this class.
		        <item command="CmdMorphJumpToConcordance" label="Morph" /> // See LexiconEditToolDataTreeStackLexEntryManager:Register_LexemeForm_Bundle for 'CmdMorphJumpToConcordance' defn.
		        <item command="CmdEntryJumpToConcordance" label="Entry" /> // Also in "mnuBrowseView" & (LexiconEditTool->).
		        <item command="CmdSenseJumpToConcordance" label="Sense" /> // Also in "mnuBrowseView" & LexiconEditToolDataTreeStackLexSenseManager->Create_mnuDataTree_Sense
		        <item command="CmdLexGramInfoJumpToConcordance" />
					    <command id="CmdLexGramInfoJumpToConcordance" label="Lex. Gram. Info." message="JumpToTool">
					      <parameters tool="concordance" concordOn="PartOfSpeechGramInfo" className="PartOfSpeechGramInfo" ownerClass="LangProject" ownerField="PartsOfSpeech" />
					    </command>
		        <item command="CmdWordGlossJumpToConcordance" label="Word Gloss" /> // Also in "mnuBrowseView".
					    <command id="CmdWordGlossJumpToConcordance" label="Show Word Gloss in Concordance" message="JumpToTool">
					      <parameters tool="concordance" className="WfiGloss" />
					    </command>
		        <item command="CmdWordPOSJumpToConcordance" label="Word Category" /> // Also in "mnuBrowseView".
					    <command id="CmdWordPOSJumpToConcordance" label="Show Word Category in Concordance" message="JumpToTool">
					      <parameters tool="concordance" concordOn="WordPartOfSpeech" className="WordPartOfSpeech" ownerClass="LangProject" ownerField="PartsOfSpeech" />
					    </command>
		      </menu>
		      <item command="CmdPOSJumpToDefault" /> // Also in "mnuBrowseView".
				    <command id="CmdPOSJumpToDefault" label="Show in Category Edit" message="JumpToTool">
				      <parameters tool="posEdit" className="PartOfSpeech" ownerClass="LangProject" ownerField="PartsOfSpeech" />
				    </command>
		      <item command="CmdWordPOSJumpToDefault" /> // Also in "mnuBrowseView".
				    <command id="CmdWordPOSJumpToDefault" label="Show Word Category in Category Edit" message="JumpToTool">
				      <parameters tool="posEdit" className="WordPartOfSpeech" ownerClass="LangProject" ownerField="PartsOfSpeech" />
				    </command>
		      <item command="CmdEndoCompoundRuleJumpToDefault" /> // Also in "mnuBrowseView".
				    <command id="CmdEndoCompoundRuleJumpToDefault" label="Show in Compound Rules Editor" message="JumpToTool">
				      <parameters tool="compoundRuleAdvancedEdit" className="MoEndoCompound" />
				    </command>
		      <item command="CmdExoCompoundRuleJumpToDefault" /> // Also in "mnuBrowseView".
				    <command id="CmdExoCompoundRuleJumpToDefault" label="Show in Compound Rules Editor" message="JumpToTool">
				      <parameters tool="compoundRuleAdvancedEdit" className="MoExoCompound" />
				    </command>
		      <item command="CmdPhonemeJumpToDefault" />
				    <command id="CmdNaturalClassJumpToDefault" label="Show in Natural Classes Editor" message="JumpToTool">
				      <parameters tool="naturalClassEdit" className="PhNCSegments" />
				    </command>
		      <item command="CmdNaturalClassJumpToDefault" /> // Also in "mnuBrowseView".
		      <item command="CmdEnvironmentsJumpToDefault" /> // Also in "mnuBrowseView".
				    <command id="CmdEnvironmentsJumpToDefault" label="Show in Environments Editor" message="JumpToTool">
				      <parameters tool="EnvironmentEdit" className="PhEnvironment" />
				    </command>
		      <item label="-" translate="do not translate" />
		      <item command="CmdDeleteSelectedObject" />
				    <!-- This is on the popup menu, and is for non-record level objects. -->
				    <command id="CmdDeleteSelectedObject" label="Delete selected {0}" message="DeleteSelectedItem" />
		    </menu>
			*/
			throw new NotImplementedException();
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> PopupContextMenuCreatorMethod_mnuReferenceChoices(Slice slice, string contextMenuId)
		{
			/*
		    <!-- The following commands are involked/displayed on a right click on a slice on a Possibility list item.

			 In the C# code see the following  class ReferenceViewBase and class ReferenceBaseUi
			 where ContextMenuId returns  "mnuReferenceChoices".

			 Search in the xml files for the particular command (for example CmdJumpToAnthroList and CmdJumpToAnthroList2)
			 See how the command has the following parameters
				 className="CmAnthroItem" ownerClass="LangProject" ownerField="AnthroList"
			 These parameters must be used to determine that this command is only shown on slices which contain
			 Anthropology Categories.  The messsage is the command that is executed.-->
		    <menu id="mnuReferenceChoices">
		      <item command="CmdEntryJumpToDefault" /> // Also in: "mnuInflAffixTemplate-TemplateTable" & "mnuBrowseView" & LexiconEditToolDataTreeStackLexEntryFormsManager->Create_mnuDataTree_VariantForm
		      <item command="CmdRecordJumpToDefault" />
				    <command id="CmdRecordJumpToDefault" label="Show Record in Notebook" message="JumpToTool">
				      <parameters tool="notebookEdit" className="RnGenericRec" />
				    </command>
		      <item command="CmdAnalysisJumpToConcordance" /> // Also in "mnuDataTree-HumanApprovedAnalysis"
				    <command id="CmdAnalysisJumpToConcordance" label="Show Analysis in Concordance" message="JumpToTool">
				      <parameters tool="concordance" className="WfiAnalysis" />
				    </command>
		      <item label="-" translate="do not translate" />
		      <item command="CmdLexemeFormJumpToConcordance" /> // See: LexiconEditToolDataTreeStackLexEntryManager:Register_LexemeForm_Bundle for 'CmdLexemeFormJumpToConcordance' defn.
		      <item command="CmdEntryJumpToConcordance" /> // Used in mnuBrowseView (in "lexiconEdit" tool).
		      <item command="CmdSenseJumpToConcordance" /> // Used in mnuBrowseView
				    <command id="CmdSenseJumpToConcordance" label="Show Sense in Concordance" message="JumpToTool">
				      <parameters tool="concordance" className="LexSense" />
				    </command>
		      <item command="CmdJumpToAcademicDomainList" />
				    <command id="CmdJumpToAcademicDomainList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="domainTypeEdit" className="CmPossibility" ownerClass="LexDb" ownerField="DomainTypes" />
				    </command>
		      <item command="CmdJumpToAnthroList" />
				    <command id="CmdJumpToAnthroList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="anthroEdit" className="CmAnthroItem" ownerClass="LangProject" ownerField="AnthroList" />
				    </command>
		      <item command="CmdJumpToLexiconEditWithFilter" />
				    <command id="CmdJumpToLexiconEditWithFilter" label="Filter for Lexical Entries with this category" message="JumpToLexiconEditFilterAnthroItems">
				      <parameters tool="lexiconEdit" className="CmAnthroItem" ownerClass="LangProject" ownerField="AnthroList" />
				    </command>
		      <item command="CmdJumpToNotebookEditWithFilter" />
				    <command id="CmdJumpToNotebookEditWithFilter" label="Filter for Notebook Records with this category" message="JumpToNotebookEditFilterAnthroItems">
				      <parameters tool="notebookEdit" className="CmAnthroItem" ownerClass="LangProject" ownerField="AnthroList" />
				    </command>
		      <item command="CmdJumpToConfidenceList" />
				    <command id="CmdJumpToConfidenceList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="confidenceEdit" className="CmPossibility" ownerClass="LangProject" ownerField="ConfidenceLevels" />
				    </command>
		      <item command="CmdJumpToDialectLabelsList" />
				    <command id="CmdJumpToDialectLabelsList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="dialectsListEdit" className="CmPossibility" ownerClass="LexDb" ownerField="DialectLabels" />
				    </command>
		      <item command="CmdJumpToDiscChartMarkerList" />
				    <command id="CmdJumpToDiscChartMarkerList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="chartmarkEdit" className="CmPossibility" ownerClass="DsDiscourseData" ownerField="ChartMarkers" />
				    </command>
		      <item command="CmdJumpToDiscChartTemplateList" />
				    <command id="CmdJumpToDiscChartTemplateList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="charttempEdit" className="CmPossibility" ownerClass="DsDiscourseData" ownerField="ConstChartTempl" />
				    </command>
		      <item command="CmdJumpToEducationList" />
				    <command id="CmdJumpToEducationList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="educationEdit" className="CmPossibility" ownerClass="LangProject" ownerField="Education" />
				    </command>
		      <item command="CmdJumpToRoleList" />
				    <command id="CmdJumpToRoleList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="roleEdit" className="CmPossibility" ownerClass="LangProject" ownerField="Roles" />
				    </command>
		      <item command="CmdJumpToExtNoteTypeList" />
				    <command id="CmdJumpToExtNoteTypeList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="extNoteTypeEdit" className="CmPossibility" ownerClass="LexDb" ownerField="ExtendedNoteTypes" />
				    </command>
		      <item command="CmdJumpToComplexEntryTypeList" />
				    <command id="CmdJumpToComplexEntryTypeList" label="Show in Complex Form Types list" message="JumpToTool">
				      <parameters tool="complexEntryTypeEdit" className="LexEntryType" ownerClass="LexDb" ownerField="ComplexEntryTypes" />
				    </command>
		      <item command="CmdJumpToVariantEntryTypeList" />
				    <command id="CmdJumpToVariantEntryTypeList" label="Show in Variant Types list" message="JumpToTool">
				      <parameters tool="variantEntryTypeEdit" className="LexEntryType" ownerClass="LexDb" ownerField="VariantEntryTypes" />
				    </command>
		      <item command="CmdJumpToTextMarkupTagsList" />
				    <command id="CmdJumpToTextMarkupTagsList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="textMarkupTagsEdit" className="CmPossibility" ownerClass="LangProject" ownerField="TextMarkupTags" />
				    </command>
		      <item command="CmdJumpToLexRefTypeList" />
				    <command id="CmdJumpToLexRefTypeList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="lexRefEdit" className="LexRefType" ownerClass="LexDb" ownerField="References" />
				    </command>
		      <item command="CmdJumpToLanguagesList" />
				    <command id="CmdJumpToLanguagesList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="languagesListEdit" className="CmPossibility" ownerClass="LexDb" ownerField="Languages" />
				    </command>
		      <item command="CmdJumpToLocationList" />
				    <command id="CmdJumpToLocationList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="locationsEdit" className="CmLocation" ownerClass="LangProject" ownerField="Locations" />
				    </command>
		      <item command="CmdJumpToPublicationList" />
				    <command id="CmdJumpToPublicationList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="publicationsEdit" className="CmPossibility" ownerClass="LexDb" ownerField="PublicationTypes" />
				    </command>
		      <item command="CmdJumpToMorphTypeList" />
				    <command id="CmdJumpToMorphTypeList" label="Show in Morpheme Types list" message="JumpToTool">
				      <parameters tool="morphTypeEdit" className="MoMorphType" ownerClass="LexDb" ownerField="MorphTypes" />
				    </command>
		      <item command="CmdJumpToPeopleList" />
				    <command id="CmdJumpToPeopleList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="peopleEdit" className="CmPerson" ownerClass="LangProject" ownerField="People" />
				    </command>
		      <item command="CmdJumpToPositionList" />
				    <command id="CmdJumpToPositionList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="positionsEdit" className="CmPossibility" ownerClass="LangProject" ownerField="Positions" />
				    </command>
		      <item command="CmdJumpToRestrictionsList" />
				    <command id="CmdJumpToRestrictionsList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="restrictionsEdit" className="CmPossibility" ownerClass="LangProject" ownerField="Restrictions" />
				    </command>
		      <item command="CmdJumpToSemanticDomainList" />
				    <command id="CmdJumpToSemanticDomainList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="semanticDomainEdit" className="CmSemanticDomain" ownerClass="LangProject" ownerField="SemanticDomainList" />
				    </command>
		      <item command="CmdJumpToGenreList" />
				    <command id="CmdJumpToGenreList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="genresEdit" className="CmPossibility" ownerClass="LangProject" ownerField="GenreList" />
				    </command>
		      <item command="CmdJumpToSenseTypeList" />
				    <command id="CmdJumpToSenseTypeList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="senseTypeEdit" className="CmPossibility" ownerClass="LexDb" ownerField="SenseTypes" />
				    </command>
		      <item command="CmdJumpToStatusList" />
				    <command id="CmdJumpToStatusList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="statusEdit" className="CmPossibility" ownerClass="LangProject" ownerField="Status" />
				    </command>
		      <item command="CmdJumpToTranslationTypeList" />
				    <command id="CmdJumpToTranslationTypeList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="translationTypeEdit" className="CmPossibility" ownerClass="LangProject" ownerField="TranslationTags" />
				    </command>
		      <item command="CmdJumpToUsageTypeList" />
				    <command id="CmdJumpToUsageTypeList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="usageTypeEdit" className="CmPossibility" ownerClass="LexDb" ownerField="UsageTypes" />
				    </command>
		      <item command="CmdJumpToRecordTypeList" />
				    <command id="CmdJumpToRecordTypeList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="recTypeEdit" className="CmPossibility" ownerClass="RnResearchNbk" ownerField="RecTypes" />
				    </command>
		      <item command="CmdJumpToTimeOfDayList" />
				    <command id="CmdJumpToTimeOfDayList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="timeOfDayEdit" className="CmPossibility" ownerClass="LangProject" ownerField="TimeOfDay" />
				    </command>
		      <item label="-" translate="do not translate" />
		      <item command="CmdShowSubentryUnderComponent" />
				    <command id="CmdShowSubentryUnderComponent" label="Show Subentry under this Component" message="AddComponentToPrimary">
				      <parameters tool="lexiconEdit" className="LexEntryRef" />
				    </command>
		      <item command="CmdVisibleComplexForm" />
				    <command id="CmdVisibleComplexForm" label="Referenced Complex Form" message="VisibleComplexForm">
				      <parameters tool="lexiconEdit" className="LexEntryOrLexSense" />
				    </command>
		      <item command="CmdMoveTargetToPreviousInSequence" /> // Use shared?
		      <item command="CmdMoveTargetToNextInSequence" /> // Use shared?
		    </menu>
			*/
			throw new NotImplementedException();
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> PopupContextMenuCreatorMethod_mnuEnvReferenceChoices(Slice slice, string contextMenuId)
		{
			/* Grammar
		    <menu id="mnuEnvReferenceChoices">
		      <item command="CmdJumpToEnvironmentList" />
				    <command id="CmdJumpToEnvironmentList" label="Show in Environments list" message="JumpToTool">
				      <parameters tool="EnvironmentEdit" className="PhEnvironment" ownerClass="PhPhonData" ownerField="Environments" />
				    </command>
		      <item command="CmdShowEnvironmentErrorMessage" />
					<command id="CmdShowEnvironmentErrorMessage" label="_Describe Error in Environment" message="ShowEnvironmentError" />
		      <item label="-" translate="do not translate" />
		      <item command="CmdInsertEnvSlash" />
					<command id="CmdInsertEnvSlash" label="Insert Environment _slash" message="InsertSlash" />
		      <item command="CmdInsertEnvUnderscore" />
					<command id="CmdInsertEnvUnderscore" label="Insert Environment _bar" message="InsertEnvironmentBar" />
		      <item command="CmdInsertEnvNaturalClass" />
					<command id="CmdInsertEnvNaturalClass" label="Insert _Natural Class" message="InsertNaturalClass" />
		      <item command="CmdInsertEnvOptionalItem" />
					<command id="CmdInsertEnvOptionalItem" label="Insert _Optional Item" message="InsertOptionalItem" />
		      <item command="CmdInsertEnvHashMark" />
					<command id="CmdInsertEnvHashMark" label="Insert _Word Boundary" message="InsertHashMark" />
		    </menu>
			*/
			throw new NotImplementedException();
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> PopupContextMenuCreatorMethod_mnuPhRegularRule(Slice slice, string contextMenuId)
		{
			/* Grammar
		    <menu id="mnuPhRegularRule">
		      <item command="CmdCtxtOccurOnce" />
				    <command id="CmdCtxtOccurOnce" label="Occurs exactly once" message="ContextSetOccurrence"> mnuPhRegularRule (x4)
				      <parameters min="1" max="1" />
				    </command>
		      <item command="CmdCtxtOccurZeroMore" />
				    <command id="CmdCtxtOccurZeroMore" label="Occurs zero or more times" message="ContextSetOccurrence"> mnuPhRegularRule (x4)
				      <parameters min="0" max="-1" />
				    </command>
		      <item command="CmdCtxtOccurOneMore" />
				    <command id="CmdCtxtOccurOneMore" label="Occurs one or more times" message="ContextSetOccurrence"> mnuPhRegularRule (x4)
				      <parameters min="1" max="-1" />
				    </command>
		      <item command="CmdCtxtSetOccur" />
					<command id="CmdCtxtSetOccur" label="Set occurrence (min. and max.)..." message="ContextSetOccurrence" /> mnuPhRegularRule (x4)
		      <item label="-" translate="do not translate" />
		      <item command="CmdCtxtSetFeatures" />
					<command id="CmdCtxtSetFeatures" label="Set Phonological Features..." message="ContextSetFeatures" /> mnuPhRegularRule, mnuPhMetathesisRule, and mnuMoAffixProcess
		      <item label="-" translate="do not translate" />
		      <item command="CmdCtxtJumpToNC" />
					<command id="CmdCtxtJumpToNC" label="Show in Natural Classes list" message="ContextJumpToNaturalClass" /> mnuPhRegularRule, mnuPhMetathesisRule, and mnuMoAffixProcess
		      <item command="CmdCtxtJumpToPhoneme" />
					<command id="CmdCtxtJumpToPhoneme" label="Show in Phonemes list" message="ContextJumpToPhoneme" /> mnuPhRegularRule, mnuPhMetathesisRule, and mnuMoAffixProcess
		    </menu>
			*/
			throw new NotImplementedException();
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> PopupContextMenuCreatorMethod_mnuPhMetathesisRule(Slice slice, string contextMenuId)
		{
			/*
		    <menu id="mnuPhMetathesisRule">
		      <item command="CmdCtxtSetFeatures" />
					<command id="CmdCtxtSetFeatures" label="Set Phonological Features..." message="ContextSetFeatures" /> mnuPhRegularRule, mnuPhMetathesisRule, and mnuMoAffixProcess
		      <item label="-" translate="do not translate" />
		      <item command="CmdCtxtJumpToNC" />
					<command id="CmdCtxtJumpToNC" label="Show in Natural Classes list" message="ContextJumpToNaturalClass" /> mnuPhRegularRule, mnuPhMetathesisRule, and mnuMoAffixProcess
		      <item command="CmdCtxtJumpToPhoneme" />
					<command id="CmdCtxtJumpToPhoneme" label="Show in Phonemes list" message="ContextJumpToPhoneme" /> mnuPhRegularRule, mnuPhMetathesisRule, and mnuMoAffixProcess
		    </menu>
			*/
			throw new NotImplementedException();
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> PopupContextMenuCreatorMethod_mnuMoAffixProcess(Slice slice, string contextMenuId)
		{
			/*
			 RuleFormulaSlice (Yes)
				AffixRuleFormulaSlice (Yes)
				RegRuleFormulaSlice (Yes)
				MetaRuleFormulaSlice (no)
		    <menu id="mnuMoAffixProcess">
		      <item command="CmdCtxtSetFeatures" />
					<command id="CmdCtxtSetFeatures" label="Set Phonological Features..." message="ContextSetFeatures" /> mnuPhRegularRule, mnuPhMetathesisRule, and mnuMoAffixProcess
		      <item command="CmdMappingSetFeatures" />
					<command id="CmdMappingSetFeatures" label="Set Phonological Features..." message="MappingSetFeatures" /> mnuMoAffixProcess
		      <item command="CmdMappingSetNC" />
					<command id="CmdMappingSetNC" label="Set Natural Class..." message="MappingSetNaturalClass" /> mnuMoAffixProcess
		      <item label="-" translate="do not translate" />
		      <item command="CmdCtxtJumpToNC" />
					<command id="CmdCtxtJumpToNC" label="Show in Natural Classes list" message="ContextJumpToNaturalClass" /> mnuPhRegularRule, mnuPhMetathesisRule, and mnuMoAffixProcess
		      <item command="CmdCtxtJumpToPhoneme" />
					<command id="CmdCtxtJumpToPhoneme" label="Show in Phonemes list" message="ContextJumpToPhoneme" /> mnuPhRegularRule, mnuPhMetathesisRule, and mnuMoAffixProcess
		      <item label="-" translate="do not translate" />
		      <item command="CmdMappingJumpToNC" />
					<command id="CmdMappingJumpToNC" label="Show in Natural Classes list" message="MappingJumpToNaturalClass" /> AffixRuleFormulaSlice
		      <item command="CmdMappingJumpToPhoneme" />
					<command id="CmdMappingJumpToPhoneme" label="Show in Phonemes list" message="MappingJumpToPhoneme" /> AffixRuleFormulaSlice
		    </menu>
			*/
			throw new NotImplementedException();
		}
	}
}
