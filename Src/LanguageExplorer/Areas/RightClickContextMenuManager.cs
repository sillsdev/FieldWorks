// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Areas.Lexicon;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.DictionaryConfiguration;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// Implementation that supports the addition(s) to FLEx's right-click context menus for use by any tool.
	/// </summary>
	internal sealed class RightClickContextMenuManager : IToolUiWidgetManager
	{
		private ITool _currentTool;
		private DataTree MyDataTree { get; }
		private FlexComponentParameters _flexComponentParameters;
		private ISharedEventHandlers _sharedEventHandlers;
		private IRecordList MyRecordList { get; set; }
		private LcmCache _cache;

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
			_cache = majorFlexComponentParameters.LcmCache;

			var rightClickPopupMenuFactory = MyDataTree.DataTreeStackContextMenuFactory.RightClickPopupMenuFactory;

			rightClickPopupMenuFactory.RegisterPopupContextCreatorMethod(AreaServices.mnuObjectChoices, PopupContextMenuCreatorMethod_mnuObjectChoices);
			rightClickPopupMenuFactory.RegisterPopupContextCreatorMethod(AreaServices.mnuReferenceChoices, PopupContextMenuCreatorMethod_mnuReferenceChoices);
			rightClickPopupMenuFactory.RegisterPopupContextCreatorMethod(AreaServices.mnuEnvReferenceChoices, PopupContextMenuCreatorMethod_mnuEnvReferenceChoices);
		}

		/// <inheritdoc />
		void IToolUiWidgetManager.UnwireSharedEventHandlers()
		{
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
			_cache = null;

			_isDisposed = true;
		}

		#endregion

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> PopupContextMenuCreatorMethod_mnuObjectChoices(Slice slice, string contextMenuId)
		{
			/* "mnuObjectChoices" is used by ContextMenuId in CmObjectUi->HandleRightClick, but it is overridden by two subclasses:
					ReferenceBaseUi ("mnuReferenceChoices")
					ReferenceCollectionUi : VectorReferenceUi : ReferenceBaseUi ("mnuReferenceChoices" OR "mnuEnvReferenceChoices" (if flid is on a IPhEnvironment))

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
			if (contextMenuId != AreaServices.mnuReferenceChoices)
			{
				throw new ArgumentException($"Expected argument value of '{AreaServices.mnuReferenceChoices}', but got '{contextMenuId}' instead.");
			}

			// Start: <menu id="mnuReferenceChoices">

			var contextMenuStrip = new ContextMenuStrip
			{
				Name = AreaServices.mnuReferenceChoices
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(42);
			/*
		    <!-- The following commands are involked/displayed on a right click on a slice on a Possibility list item.

			 In the C# code see the following  classes (ReferenceBaseUi and ReferenceCollectionUi) where ContextMenuId returns  "mnuReferenceChoices".

			 Search in the xml files for the particular command (for example CmdJumpToAnthroList and CmdJumpToAnthroList2)
			 See how the command has the following parameters
				 className="CmAnthroItem" ownerClass="LangProject" ownerField="AnthroList"
			 These parameters must be used to determine that this command is only shown on slices which contain
			 Anthropology Categories.  The messsage is the command that is executed.-->
			*/

			/*
		      <item command="CmdEntryJumpToDefault" /> // Also in: "mnuInflAffixTemplate-TemplateTable" & "mnuBrowseView" & "mnuDataTree-VariantForm" (in LexiconEditToolDataTreeStackLexEntryFormsManager->Create_mnuDataTree_VariantForm)
				    <command id="CmdEntryJumpToDefault" label="Show Entry in Lexicon" message="JumpToTool">
				      <parameters tool="lexiconEdit" className="LexEntry" />
				    </command>
			*/
			var wantSeparator = false;
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, LexEntryTags.kClassName, AreaResources.ksShowEntryInLexicon, AreaServices.LexiconEditMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator);

			/*
		      <item command="CmdRecordJumpToDefault" />
				    <command id="CmdRecordJumpToDefault" label="Show Record in Notebook" message="JumpToTool">
				      <parameters tool="notebookEdit" className="RnGenericRec" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, RnGenericRecTags.kClassName, AreaResources.Show_Record_in_Notebook, AreaServices.NotebookEditToolMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator);

			/*
		      <item command="CmdAnalysisJumpToConcordance" /> // Also in "mnuDataTree-HumanApprovedAnalysis"
				    <command id="CmdAnalysisJumpToConcordance" label="Show Analysis in Concordance" message="JumpToTool">
				      <parameters tool="concordance" className="WfiAnalysis" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, WfiAnalysisTags.kClassName, AreaResources.Show_Analysis_in_Concordance, AreaServices.ConcordanceMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator);

			/*
		      <item label="-" translate="do not translate" />
			*/
			var separatorInsertLocation = menuItems.Count - 1;
			wantSeparator = separatorInsertLocation > 0;

			/*
			  <item command="CmdLexemeFormJumpToConcordance" /> // See: LexiconEditToolDataTreeStackLexEntryManager:Register_LexemeForm_Bundle for 'CmdLexemeFormJumpToConcordance' defn.
				<command id="CmdLexemeFormJumpToConcordance" label="Show Lexeme Form in Concordance" message="JumpToTool">
				  <parameters tool="concordance" className="MoForm" />
				</command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, MoFormTags.kClassName, AreaResources.Show_Lexeme_Form_in_Concordance, AreaServices.ConcordanceMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item command="CmdEntryJumpToConcordance" /> // Used in mnuBrowseView (in "lexiconEdit" tool).
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, MoFormTags.kClassName, AreaResources.Show_Entry_In_Concordance, AreaServices.ConcordanceMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item command="CmdSenseJumpToConcordance" /> // Used in mnuBrowseView
				    <command id="CmdSenseJumpToConcordance" label="Show Sense in Concordance" message="JumpToTool">
				      <parameters tool="concordance" className="LexSense" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, LexSenseTags.kClassName, AreaResources.Show_Sense_in_Concordance, AreaServices.ConcordanceMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item command="CmdJumpToAcademicDomainList" />
				    <command id="CmdJumpToAcademicDomainList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="domainTypeEdit" className="CmPossibility" ownerClass="LexDb" ownerField="DomainTypes" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, CmPossibilityTags.kClassName, string.Format(AreaResources.Show_in_0_list, _cache.LanguageProject.LexDbOA.DomainTypesOA.ShortName), AreaServices.DomainTypeEditMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item command="CmdJumpToAnthroList" />
				    <command id="CmdJumpToAnthroList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="anthroEdit" className="CmAnthroItem" ownerClass="LangProject" ownerField="AnthroList" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, CmAnthroItemTags.kClassName, string.Format(AreaResources.Show_in_0_list, _cache.LanguageProject.AnthroListOA.ShortName), AreaServices.DomainTypeEditMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item command="CmdJumpToLexiconEditWithFilter" />
				    <command id="CmdJumpToLexiconEditWithFilter" label="Filter for Lexical Entries with this category" message="JumpToLexiconEditFilterAnthroItems">
				      <parameters tool="lexiconEdit" className="CmAnthroItem" ownerClass="LangProject" ownerField="AnthroList" />
				    </command>
			*/
			ToolStripMenuItem menu;
			var visibleAndEnabled = MyDataTree.CanJumpToToolAndFilterAnthroItem;
			if (visibleAndEnabled)
			{
				if (wantSeparator)
				{
					ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip, separatorInsertLocation);
					wantSeparator = false;
				}
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MyDataTree.JumpToToolAndFilterAnthroItem, AreaResources.Filter_for_Lexical_Entries_with_this_category);
				menu.Tag = AreaServices.LexiconEditMachineName;
			}

			/*
		      <item command="CmdJumpToNotebookEditWithFilter" />
				    <command id="CmdJumpToNotebookEditWithFilter" label="Filter for Notebook Records with this category" message="JumpToNotebookEditFilterAnthroItems">
				      <parameters tool="notebookEdit" className="CmAnthroItem" ownerClass="LangProject" ownerField="AnthroList" />
				    </command>
			*/
			visibleAndEnabled = MyDataTree.CanJumpToToolAndFilterAnthroItem;
			if (visibleAndEnabled)
			{
				if (wantSeparator)
				{
					ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip, separatorInsertLocation);
					wantSeparator = false;
				}
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MyDataTree.JumpToToolAndFilterAnthroItem, AreaResources.Filter_for_Notebook_Records_with_this_category);
				menu.Tag = AreaServices.NotebookEditToolMachineName;
			}

			/*
		      <item command="CmdJumpToConfidenceList" />
				    <command id="CmdJumpToConfidenceList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="confidenceEdit" className="CmPossibility" ownerClass="LangProject" ownerField="ConfidenceLevels" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, CmPossibilityTags.kClassName, string.Format(AreaResources.Show_in_0_list, _cache.LanguageProject.ConfidenceLevelsOA.ShortName), AreaServices.DomainTypeEditMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item command="CmdJumpToDialectLabelsList" />
				    <command id="CmdJumpToDialectLabelsList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="dialectsListEdit" className="CmPossibility" ownerClass="LexDb" ownerField="DialectLabels" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, CmPossibilityTags.kClassName, string.Format(AreaResources.Show_in_0_list, _cache.LanguageProject.LexDbOA.DialectLabelsOA.ShortName), AreaServices.DialectsListEditMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item command="CmdJumpToDiscChartMarkerList" />
				    <command id="CmdJumpToDiscChartMarkerList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="chartmarkEdit" className="CmPossibility" ownerClass="DsDiscourseData" ownerField="ChartMarkers" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, CmPossibilityTags.kClassName, string.Format(AreaResources.Show_in_0_list, _cache.LanguageProject.DiscourseDataOA.ChartMarkersOA.ShortName), AreaServices.ChartmarkEditMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item command="CmdJumpToDiscChartTemplateList" />
				    <command id="CmdJumpToDiscChartTemplateList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="charttempEdit" className="CmPossibility" ownerClass="DsDiscourseData" ownerField="ConstChartTempl" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, CmPossibilityTags.kClassName, string.Format(AreaResources.Show_in_0_list, _cache.LanguageProject.DiscourseDataOA.ChartMarkersOA.ShortName), AreaServices.ChartmarkEditMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item command="CmdJumpToEducationList" />
				    <command id="CmdJumpToEducationList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="educationEdit" className="CmPossibility" ownerClass="LangProject" ownerField="Education" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, CmPossibilityTags.kClassName, string.Format(AreaResources.Show_in_0_list, _cache.LanguageProject.EducationOA.ShortName), AreaServices.EducationEditMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item command="CmdJumpToRoleList" />
				    <command id="CmdJumpToRoleList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="roleEdit" className="CmPossibility" ownerClass="LangProject" ownerField="Roles" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, CmPossibilityTags.kClassName, string.Format(AreaResources.Show_in_0_list, _cache.LanguageProject.RolesOA.ShortName), AreaServices.RoleEditMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item command="CmdJumpToExtNoteTypeList" />
				    <command id="CmdJumpToExtNoteTypeList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="extNoteTypeEdit" className="CmPossibility" ownerClass="LexDb" ownerField="ExtendedNoteTypes" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, CmPossibilityTags.kClassName, string.Format(AreaResources.Show_in_0_list, _cache.LanguageProject.LexDbOA.ExtendedNoteTypesOA.ShortName), AreaServices.ExtNoteTypeEditMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item command="CmdJumpToComplexEntryTypeList" />
				    <command id="CmdJumpToComplexEntryTypeList" label="Show in Complex Form Types list" message="JumpToTool">
				      <parameters tool="complexEntryTypeEdit" className="LexEntryType" ownerClass="LexDb" ownerField="ComplexEntryTypes" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, LexEntryTypeTags.kClassName, AreaResources.Show_in_Complex_Form_Types_list, AreaServices.ComplexEntryTypeEditMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item command="CmdJumpToVariantEntryTypeList" />
				    <command id="CmdJumpToVariantEntryTypeList" label="Show in Variant Types list" message="JumpToTool">
				      <parameters tool="variantEntryTypeEdit" className="LexEntryType" ownerClass="LexDb" ownerField="VariantEntryTypes" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, LexEntryTypeTags.kClassName, AreaResources.Show_in_Variant_Types_list, AreaServices.VariantEntryTypeEditMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item command="CmdJumpToTextMarkupTagsList" />
				    <command id="CmdJumpToTextMarkupTagsList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="textMarkupTagsEdit" className="CmPossibility" ownerClass="LangProject" ownerField="TextMarkupTags" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, CmPossibilityTags.kClassName, string.Format(AreaResources.Show_in_0_list, _cache.LanguageProject.TextMarkupTagsOA.ShortName), AreaServices.TextMarkupTagsEditMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item command="CmdJumpToLexRefTypeList" />
				    <command id="CmdJumpToLexRefTypeList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="lexRefEdit" className="LexRefType" ownerClass="LexDb" ownerField="References" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, LexRefTypeTags.kClassName, string.Format(AreaResources.Show_in_0_list, _cache.LanguageProject.LexDbOA.ReferencesOA.ShortName), AreaServices.LexRefEditMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item command="CmdJumpToLanguagesList" />
				    <command id="CmdJumpToLanguagesList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="languagesListEdit" className="CmPossibility" ownerClass="LexDb" ownerField="Languages" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, CmPossibilityTags.kClassName, string.Format(AreaResources.Show_in_0_list, _cache.LanguageProject.LexDbOA.LanguagesOA.ShortName), AreaServices.LanguagesListEditMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item command="CmdJumpToLocationList" />
				    <command id="CmdJumpToLocationList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="locationsEdit" className="CmLocation" ownerClass="LangProject" ownerField="Locations" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, CmLocationTags.kClassName, string.Format(AreaResources.Show_in_0_list, _cache.LanguageProject.LocationsOA.ShortName), AreaServices.LocationsEditMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item command="CmdJumpToPublicationList" />
				    <command id="CmdJumpToPublicationList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="publicationsEdit" className="CmPossibility" ownerClass="LexDb" ownerField="PublicationTypes" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, CmPossibilityTags.kClassName, string.Format(AreaResources.Show_in_0_list, _cache.LanguageProject.LexDbOA.PublicationTypesOA.ShortName), AreaServices.PublicationsEditMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item command="CmdJumpToMorphTypeList" />
				    <command id="CmdJumpToMorphTypeList" label="Show in Morpheme Types list" message="JumpToTool">
				      <parameters tool="morphTypeEdit" className="MoMorphType" ownerClass="LexDb" ownerField="MorphTypes" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, MoMorphTypeTags.kClassName, AreaResources.Show_in_Morpheme_Types_list, AreaServices.MorphTypeEditMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item command="CmdJumpToPeopleList" />
				    <command id="CmdJumpToPeopleList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="peopleEdit" className="CmPerson" ownerClass="LangProject" ownerField="People" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, CmPersonTags.kClassName, string.Format(AreaResources.Show_in_0_list, _cache.LanguageProject.PeopleOA.ShortName), AreaServices.PeopleEditMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item command="CmdJumpToPositionList" />
				    <command id="CmdJumpToPositionList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="positionsEdit" className="CmPossibility" ownerClass="LangProject" ownerField="Positions" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, CmPossibilityTags.kClassName, string.Format(AreaResources.Show_in_0_list, _cache.LanguageProject.PositionsOA.ShortName), AreaServices.PositionsEditMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item command="CmdJumpToRestrictionsList" />
				    <command id="CmdJumpToRestrictionsList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="restrictionsEdit" className="CmPossibility" ownerClass="LangProject" ownerField="Restrictions" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, CmPossibilityTags.kClassName, string.Format(AreaResources.Show_in_0_list, _cache.LanguageProject.RestrictionsOA.ShortName), AreaServices.RestrictionsEditMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item command="CmdJumpToSemanticDomainList" />
				    <command id="CmdJumpToSemanticDomainList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="semanticDomainEdit" className="CmSemanticDomain" ownerClass="LangProject" ownerField="SemanticDomainList" />
				    </command>
			*/

			/*
		      <item command="CmdJumpToGenreList" />
				    <command id="CmdJumpToGenreList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="genresEdit" className="CmPossibility" ownerClass="LangProject" ownerField="GenreList" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, CmPossibilityTags.kClassName, string.Format(AreaResources.Show_in_0_list, _cache.LanguageProject.GenreListOA.ShortName), AreaServices.GenresEditMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item command="CmdJumpToSenseTypeList" />
				    <command id="CmdJumpToSenseTypeList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="senseTypeEdit" className="CmPossibility" ownerClass="LexDb" ownerField="SenseTypes" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, CmPossibilityTags.kClassName, string.Format(AreaResources.Show_in_0_list, _cache.LanguageProject.LexDbOA.SenseTypesOA.ShortName), AreaServices.SenseTypeEditMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item command="CmdJumpToStatusList" />
				    <command id="CmdJumpToStatusList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="statusEdit" className="CmPossibility" ownerClass="LangProject" ownerField="Status" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, CmPossibilityTags.kClassName, string.Format(AreaResources.Show_in_0_list, _cache.LanguageProject.StatusOA.ShortName), AreaServices.StatusEditMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item command="CmdJumpToTranslationTypeList" />
				    <command id="CmdJumpToTranslationTypeList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="translationTypeEdit" className="CmPossibility" ownerClass="LangProject" ownerField="TranslationTags" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, CmPossibilityTags.kClassName, string.Format(AreaResources.Show_in_0_list, _cache.LanguageProject.TranslationTagsOA.ShortName), AreaServices.TranslationTypeEditMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item command="CmdJumpToUsageTypeList" />
				    <command id="CmdJumpToUsageTypeList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="usageTypeEdit" className="CmPossibility" ownerClass="LexDb" ownerField="UsageTypes" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, CmPossibilityTags.kClassName, string.Format(AreaResources.Show_in_0_list, _cache.LanguageProject.LexDbOA.UsageTypesOA.ShortName), AreaServices.UsageTypeEditMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item command="CmdJumpToRecordTypeList" />
				    <command id="CmdJumpToRecordTypeList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="recTypeEdit" className="CmPossibility" ownerClass="RnResearchNbk" ownerField="RecTypes" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, CmPossibilityTags.kClassName, string.Format(AreaResources.Show_in_0_list, _cache.LanguageProject.ResearchNotebookOA.RecTypesOA.ShortName), AreaServices.RecTypeEditMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item command="CmdJumpToTimeOfDayList" />
				    <command id="CmdJumpToTimeOfDayList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="timeOfDayEdit" className="CmPossibility" ownerClass="LangProject" ownerField="TimeOfDay" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, slice, CmPossibilityTags.kClassName, string.Format(AreaResources.Show_in_0_list, _cache.LanguageProject.TimeOfDayOA.ShortName), AreaServices.TimeOfDayEditMachineName, MyRecordList.CurrentObject.Guid, ref wantSeparator, separatorInsertLocation);

			/*
		      <item label="-" translate="do not translate" />
			*/
			separatorInsertLocation = menuItems.Count - 1;
			wantSeparator = separatorInsertLocation > 0;

			/* **************
		      <item command="CmdShowSubentryUnderComponent" />
				    <command id="CmdShowSubentryUnderComponent" label="Show Subentry under this Component" message="AddComponentToPrimary">
				      <parameters tool="lexiconEdit" className="LexEntryRef" />
				    </command>
			*/
			var ler = MyDataTree.CurrentSlice.MyCmObject as ILexEntryRef;
			ICmObject target = null;
			var selectedComponentHvo = slice.Flid != LexEntryRefTags.kflidComponentLexemes || ler == null ? 0 : slice.GetSelectionHvoFromControls();
			var menuIsChecked = false;
			if (selectedComponentHvo != 0)
			{
				target = _cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(selectedComponentHvo);
				if (ler != null && ler.RefType == LexEntryRefTags.krtComplexForm && (target is ILexEntry || target is ILexSense))
				{
					visibleAndEnabled = true;
					menuIsChecked = ler.PrimaryLexemesRS.Contains(target); // LT-11292
				}
			}
			visibleAndEnabled = visibleAndEnabled && _currentTool.MachineName == AreaServices.LexiconEditMachineName && ler != null;
			if (visibleAndEnabled)
			{
				if (wantSeparator)
				{
					ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip, separatorInsertLocation);
					wantSeparator = false;
				}
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, AddComponentToPrimary_Clicked, AreaResources.Show_Subentry_under_this_Component);
				menu.Tag = target;
				menu.Checked = menuIsChecked;
			}

			/*
		      <item command="CmdVisibleComplexForm" />
				    <command id="CmdVisibleComplexForm" label="Referenced Complex Form" message="VisibleComplexForm">
				      <parameters tool="lexiconEdit" className="LexEntryOrLexSense" />
				    </command>
			*/
			Can_Do_VisibleComplexForm(slice, (ILexEntry)target, out visibleAndEnabled, out menuIsChecked);
			if (visibleAndEnabled)
			{
				if (wantSeparator)
				{
					ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip, separatorInsertLocation);
					wantSeparator = false;
				}
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, VisibleComplexForm_Clicked, AreaResources.ksReferencedComplexForm);
				menu.Tag = target;
				menu.Checked = menuIsChecked;
			}

			if (slice is ReferenceVectorSlice)
			{
				/*
				  <item command="CmdMoveTargetToPreviousInSequence" />
				  <command id="CmdMoveTargetToPreviousInSequence" label="Move Left" message="MoveTargetDownInSequence" />
				*/
				var referenceVectorSlice = (ReferenceVectorSlice)slice;
				bool visible;
				var enabled = referenceVectorSlice.CanDisplayMoveTargetDownInSequence(out visible);
				if (visible)
				{
					if (wantSeparator)
					{
						ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip, separatorInsertLocation);
						wantSeparator = false;
					}
					// <command id="CmdMoveTargetToPreviousInSequence" label="Move Left" message="MoveTargetDownInSequence"/>
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconAreaConstants.CmdMoveTargetToPreviousInSequence), AreaResources.Move_Left);
					menu.Enabled = enabled;
				}

				/*
				  <item command="CmdMoveTargetToNextInSequence" />
				  <command id="CmdMoveTargetToNextInSequence" label="Move Right" message="MoveTargetUpInSequence" />
				*/
				enabled = referenceVectorSlice.CanDisplayMoveTargetUpInSequence(out visible);
				if (visible)
				{
					if (wantSeparator)
					{
						ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip, separatorInsertLocation);
					}
					// <command id="CmdMoveTargetToNextInSequence" label="Move Right" message="MoveTargetUpInSequence"/>
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconAreaConstants.CmdMoveTargetToNextInSequence), AreaResources.Move_Right);
					menu.Enabled = enabled;
				}
			}

			// End: <menu id="mnuReferenceChoices">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private void VisibleComplexForm_Clicked(object sender, EventArgs e)
		{
			var currentSlice = MyDataTree.CurrentSlice;
			var entryOrSense = currentSlice.MyCmObject;
			var entry = _cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetObject(currentSlice.GetSelectionHvoFromControls());
			ILexEntryRef lexEntryRef;
			if (ComponentShowsComplexForm(entryOrSense, entry, out lexEntryRef))
			{
				// Remove from visibility array
				using (var helper = new UndoableUnitOfWorkHelper(_cache.ActionHandlerAccessor, DictionaryConfigurationStrings.ksUndoVisibleComplexForm, DictionaryConfigurationStrings.ksRedoVisibleComplexForm))
				{
					lexEntryRef.ShowComplexFormsInRS.Remove(entryOrSense);
					helper.RollBack = false;
				}
				return;
			}
			// Otherwise, continue and add it
			var idx = 0;
			foreach (var obj in lexEntryRef.ComponentLexemesRS)
			{
				// looping preserves the order of the components
				if (obj == entryOrSense)
				{
					using (var helper = new UndoableUnitOfWorkHelper(_cache.ActionHandlerAccessor, DictionaryConfigurationStrings.ksUndoVisibleComplexForm, DictionaryConfigurationStrings.ksRedoVisibleComplexForm))
					{
						lexEntryRef.ShowComplexFormsInRS.Insert(idx, entryOrSense);
						helper.RollBack = false;
					}
					break;
				}
				if (lexEntryRef.ShowComplexFormsInRS.Contains(obj))
				{
					++idx;
				}
			}
		}

		private void Can_Do_VisibleComplexForm(Slice slice, ILexEntry complexFormEntry, out bool visibleAndEnabled, out bool menuIsChecked)
		{
			visibleAndEnabled = false;
			menuIsChecked = false;
			if (complexFormEntry == null)
			{
				// no selection
				return;
			}
			var className = "LexEntryOrLexSense";
			var lexOrSenseComponent = slice.MyCmObject;
			var currentSliceObjectClassName = lexOrSenseComponent.ClassName;
			if ("LexEntry" != currentSliceObjectClassName && "LexSense" != currentSliceObjectClassName)
			{
				return; // not the right message target
			}
			// The complex form slice is in both entriy and sense layouts.
			if (slice.Flid != _cache.MetaDataCacheAccessor.GetFieldId2(LexEntryTags.kClassId, "ComplexFormEntries", false) &&
				slice.Flid != _cache.MetaDataCacheAccessor.GetFieldId2(LexSenseTags.kClassId, "ComplexFormEntries", false))
			{
				return; // Not the right slice for this command
			}
			visibleAndEnabled = true;
			ILexEntryRef lexEntryRef;
			menuIsChecked = ComponentShowsComplexForm(lexOrSenseComponent, complexFormEntry, out lexEntryRef);
		}

		private bool ComponentShowsComplexForm(ICmObject lexOrSenseComponent, ILexEntry complexFormEntry, out ILexEntryRef complexFormReference)
		{
			complexFormReference = complexFormEntry.EntryRefsOS.FirstOrDefault(item => item.RefType == LexEntryRefTags.krtComplexForm);
			return complexFormReference.ShowComplexFormsInRS.Contains(lexOrSenseComponent);
		}

		private void AddComponentToPrimary_Clicked(object sender, EventArgs e)
		{
			var currentSlice = MyDataTree.CurrentSlice;
			var ler = (ILexEntryRef)currentSlice.MyCmObject;
			var target = (ICmObject)((ToolStripMenuItem)sender).Tag;
			if (ler.PrimaryLexemesRS.Contains(target))
			{
				// Remove from visibility array
				using (var helper = new UndoableUnitOfWorkHelper(_cache.ActionHandlerAccessor, DictionaryConfigurationStrings.ksUndoShowSubentryForComponent, DictionaryConfigurationStrings.ksRedoShowSubentryForComponent))
				{
					ler.PrimaryLexemesRS.Remove(target);
					helper.RollBack = false;
				}
			}
			else
			{
				var idx = 0;
				foreach (var obj in ler.ComponentLexemesRS)
				{
					// looping preserves the order of the components
					if (obj == target)
					{
						using (var helper = new UndoableUnitOfWorkHelper(_cache.ActionHandlerAccessor, DictionaryConfigurationStrings.ksUndoShowSubentryForComponent, DictionaryConfigurationStrings.ksRedoShowSubentryForComponent))
						{
							ler.PrimaryLexemesRS.Insert(idx, target);
							helper.RollBack = false;
						}
						break;
					}
					if (ler.PrimaryLexemesRS.Contains(obj))
					{
						++idx;
					}
				}
			}
		}

		private void ConditionallyAddJumpToToolMenuItem(ContextMenuStrip contextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>> menuItems, Slice slice, string className, string menuLabel, string targetToolName, Guid targetGuid, ref bool wantSeparator, int separatorInsertLocation = 0)
		{
			var visibleAndEnabled = AreaWideMenuHelper.CanJumpToTool(_currentTool.MachineName, targetToolName, _cache, MyRecordList.CurrentObject, slice.MyCmObject, className);
			if (visibleAndEnabled)
			{
				if (wantSeparator)
				{
					ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip, separatorInsertLocation);
					wantSeparator = false;
				}
				var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.JumpToTool), menuLabel);
				menu.Tag = new List<object> { _flexComponentParameters.Publisher, targetToolName, targetGuid };
			}
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> PopupContextMenuCreatorMethod_mnuEnvReferenceChoices(Slice slice, string contextMenuId)
		{
			if (contextMenuId != AreaServices.mnuEnvReferenceChoices)
			{
				throw new ArgumentException($"Expected argument value of '{AreaServices.mnuEnvReferenceChoices}', but got '{contextMenuId}' instead.");
			}

			// Start: <menu id="mnuEnvReferenceChoices">

			var contextMenuStrip = new ContextMenuStrip
			{
				Name = AreaServices.mnuEnvReferenceChoices
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(8);

			var visible = CanJumpToEnvironmentList;
			if (visible)
			{
				/*
				  <item command="CmdJumpToEnvironmentList" />
						<command id="CmdJumpToEnvironmentList" label="Show in Environments list" message="JumpToTool">
						  <parameters tool="EnvironmentEdit" className="PhEnvironment" ownerClass="PhPhonData" ownerField="Environments" />
						</command>
				*/
				var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.JumpToTool), AreaResources.Show_in_Environments_list);
				menu.Tag = new List<object> { _flexComponentParameters.Publisher, AreaServices.EnvironmentEditMachineName, MyDataTree.CurrentSlice.MyCmObject.Guid };
			}

			AreaWideMenuHelper.CreateShowEnvironmentErrorMessageMenus(_sharedEventHandlers, slice, menuItems, contextMenuStrip);

			AreaWideMenuHelper.CreateCommonEnvironmentMenus(_sharedEventHandlers, slice, menuItems, contextMenuStrip);

			// End: <menu id="mnuEnvReferenceChoices">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private bool CanJumpToEnvironmentList
		{
			get
			{
				var currentSlice = MyDataTree.CurrentSlice;
				if (_currentTool.MachineName == AreaServices.EnvironmentEditMachineName && currentSlice.MyCmObject == MyRecordList.CurrentObject || currentSlice.MyCmObject.IsOwnedBy(MyRecordList.CurrentObject))
				{
					return false;
				}
				if (currentSlice.MyCmObject.ClassID == PhEnvironmentTags.kClassId)
				{
					return true;
				}
				return MyDataTree.Cache.DomainDataByFlid.MetaDataCache.GetBaseClsId(currentSlice.MyCmObject.ClassID) == PhEnvironmentTags.kClassId;
			}
		}
	}
}
