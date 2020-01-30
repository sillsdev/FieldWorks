// Copyright (c) 2018-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.DictionaryConfiguration;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.Xml;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// Implementation that supports the addition(s) to FLEx's right-click context menus for use by any tool.
	/// </summary>
	internal sealed class RightClickContextMenuManager : IDisposable
	{
		private ITool _currentTool;
		private DataTree _dataTree;
		private FlexComponentParameters _flexComponentParameters;
		private ISharedEventHandlers _sharedEventHandlers;
		private IRecordList _recordList;
		private LcmCache _cache;

		internal RightClickContextMenuManager(MajorFlexComponentParameters majorFlexComponentParameters, ITool currentTool, DataTree dataTree, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(currentTool, nameof(currentTool));
			Guard.AgainstNull(dataTree, nameof(dataTree));
			Guard.AgainstNull(recordList, nameof(recordList));

			_currentTool = currentTool;
			_dataTree = dataTree;
			_flexComponentParameters = majorFlexComponentParameters.FlexComponentParameters;
			_sharedEventHandlers = majorFlexComponentParameters.SharedEventHandlers;
			_recordList = recordList;
			_cache = majorFlexComponentParameters.LcmCache;

			var rightClickPopupMenuFactory = _dataTree.DataTreeSliceContextMenuParameterObject.RightClickPopupMenuFactory;
			rightClickPopupMenuFactory.RegisterPopupContextCreatorMethod(ContextMenuName.mnuReferenceChoices, PopupContextMenuCreatorMethod_mnuReferenceChoices);
			rightClickPopupMenuFactory.RegisterPopupContextCreatorMethod(ContextMenuName.mnuEnvReferenceChoices, PopupContextMenuCreatorMethod_mnuEnvReferenceChoices);
		}

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
			// Therefore, you should call GC.SuppressFinalize to
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
			_currentTool = null;
			_dataTree = null;
			_flexComponentParameters = null;
			_sharedEventHandlers = null;
			_recordList = null;
			_cache = null;

			 _isDisposed = true;
		}

		#endregion

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> PopupContextMenuCreatorMethod_mnuReferenceChoices(Slice slice, ContextMenuName contextMenuId)
		{
			Require.That(contextMenuId == ContextMenuName.mnuReferenceChoices, $"Expected argument value of '{ContextMenuName.mnuReferenceChoices.ToString()}', but got '{contextMenuId.ToString()}' instead.");

			// Start: <menu id="mnuReferenceChoices">

			var contextMenuStrip = new ContextMenuStrip
			{
				Name = ContextMenuName.mnuReferenceChoices.ToString()
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(42);
			/*
		    <!-- The following commands are invoked/displayed on a right click on a slice on a Possibility list item.

			 In the C# code see the following  classes (ReferenceBaseUi and ReferenceCollectionUi) where ContextMenuId returns  "mnuReferenceChoices".

			 Search in the xml files for the particular command (for example CmdJumpToAnthroList and CmdJumpToAnthroList2)
			 See how the command has the following parameters
				 className="CmAnthroItem" ownerClass="LangProject" ownerField="AnthroList"
			 These parameters must be used to determine that this command is only shown on slices which contain
			 Anthropology Categories.  The message is the command that is executed.-->
			*/

			/*
		      <item command="CmdEntryJumpToDefault" />
				    <command id="CmdEntryJumpToDefault" label="Show Entry in Lexicon" message="JumpToTool">
				      <parameters tool="lexiconEdit" className="LexEntry" />
				    </command>
			*/
			var wantSeparator = false;
			ConditionallyAddJumpToToolMenuItem_Overload_Also_Rans(contextMenuStrip, menuItems, slice, AreaServices.LexiconEditMachineName, ref wantSeparator, LexEntryTags.kClassName, AreaResources.ksShowEntryInLexicon);

			/*
		      <item command="CmdRecordJumpToDefault" />
				    <command id="CmdRecordJumpToDefault" label="Show Record in Notebook" message="JumpToTool">
				      <parameters tool="notebookEdit" className="RnGenericRec" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Also_Rans(contextMenuStrip, menuItems, slice, AreaServices.NotebookEditToolMachineName, ref wantSeparator, RnGenericRecTags.kClassName, AreaResources.Show_Record_in_Notebook);

			/*
		      <item command="CmdAnalysisJumpToConcordance" />
				    <command id="CmdAnalysisJumpToConcordance" label="Show Analysis in Concordance" message="JumpToTool">
				      <parameters tool="concordance" className="WfiAnalysis" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Also_Rans(contextMenuStrip, menuItems, slice, AreaServices.ConcordanceMachineName, ref wantSeparator, WfiAnalysisTags.kClassName, AreaResources.Show_Analysis_in_Concordance);

			/*
		      <item label="-" translate="do not translate" />
			*/
			var separatorInsertLocation = menuItems.Count - 1;
			wantSeparator = separatorInsertLocation > 0;

			/*
			  <item command="CmdLexemeFormJumpToConcordance" />
				<command id="CmdLexemeFormJumpToConcordance" label="Show Lexeme Form in Concordance" message="JumpToTool">
				  <parameters tool="concordance" className="MoForm" />
				</command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Also_Rans(contextMenuStrip, menuItems, slice, AreaServices.ConcordanceMachineName, ref wantSeparator, MoFormTags.kClassName, AreaResources.Show_Lexeme_Form_in_Concordance, separatorInsertLocation);

			/*
		      <item command="CmdEntryJumpToConcordance" />
			    <command id="CmdEntryJumpToConcordance" label="Show Entry in Concordance" message="JumpToTool">
			      <parameters tool="concordance" className="LexEntry" />
			    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Also_Rans(contextMenuStrip, menuItems, slice, AreaServices.ConcordanceMachineName, ref wantSeparator, LexEntryTags.kClassName, AreaResources.Show_Entry_In_Concordance, separatorInsertLocation);

			/*
		      <item command="CmdSenseJumpToConcordance" />
				    <command id="CmdSenseJumpToConcordance" label="Show Sense in Concordance" message="JumpToTool">
				      <parameters tool="concordance" className="LexSense" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Also_Rans(contextMenuStrip, menuItems, slice, AreaServices.ConcordanceMachineName, ref wantSeparator, LexSenseTags.kClassName, AreaResources.Show_Sense_in_Concordance, separatorInsertLocation);

			/*
		      <item command="CmdJumpToAcademicDomainList" />
				    <command id="CmdJumpToAcademicDomainList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="domainTypeEdit" className="CmPossibility" ownerClass="LexDb" ownerField="DomainTypes" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Jump_To_PossibilityList(contextMenuStrip, menuItems, _cache.LanguageProject.LexDbOA.DomainTypesOA, slice, AreaServices.DomainTypeEditMachineName, ref wantSeparator, separatorInsertLocation: separatorInsertLocation);

			/*
		      <item command="CmdJumpToAnthroList" />
				    <command id="CmdJumpToAnthroList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="anthroEdit" className="CmAnthroItem" ownerClass="LangProject" ownerField="AnthroList" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Jump_To_PossibilityList(contextMenuStrip, menuItems, _cache.LanguageProject.AnthroListOA, slice, AreaServices.AnthroEditMachineName, ref wantSeparator, CmAnthroItemTags.kClassName, separatorInsertLocation: separatorInsertLocation);

			ToolStripMenuItem menu;
			if (CanJumpToToolAndFilterAnthroItem)
			{
				/*
				  <item command="CmdJumpToLexiconEditWithFilter" />
						<command id="CmdJumpToLexiconEditWithFilter" label="Filter for Lexical Entries with this category" message="JumpToLexiconEditFilterAnthroItems">
						  <parameters tool=c className="CmAnthroItem" ownerClass="LangProject" ownerField="AnthroList" />
						</command>
				*/
				if (wantSeparator)
				{
					ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip, separatorInsertLocation);
					wantSeparator = false;
				}
				if (_currentTool.MachineName != AreaServices.LexiconEditMachineName)
				{
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, JumpToToolAndFilterAnthroItem, AreaResources.Filter_for_Lexical_Entries_with_this_category);
					menu.Tag = AreaServices.LexiconEditMachineName;
				}

				/*
				  <item command="CmdJumpToNotebookEditWithFilter" />
						<command id="CmdJumpToNotebookEditWithFilter" label="Filter for Notebook Records with this category" message="JumpToNotebookEditFilterAnthroItems">
						  <parameters tool="notebookEdit" className="CmAnthroItem" ownerClass="LangProject" ownerField="AnthroList" />
						</command>
				*/
				if (_currentTool.MachineName != AreaServices.NotebookEditToolMachineName)
				{
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, JumpToToolAndFilterAnthroItem, AreaResources.Filter_for_Notebook_Records_with_this_category);
					menu.Tag = AreaServices.NotebookEditToolMachineName;
				}
			}

			/*
		      <item command="CmdJumpToConfidenceList" />
				    <command id="CmdJumpToConfidenceList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="confidenceEdit" className="CmPossibility" ownerClass="LangProject" ownerField="ConfidenceLevels" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Jump_To_PossibilityList(contextMenuStrip, menuItems, _cache.LanguageProject.ConfidenceLevelsOA, slice, AreaServices.ConfidenceEditMachineName, ref wantSeparator, separatorInsertLocation: separatorInsertLocation);

			/*
		      <item command="CmdJumpToDialectLabelsList" />
				    <command id="CmdJumpToDialectLabelsList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="dialectsListEdit" className="CmPossibility" ownerClass="LexDb" ownerField="DialectLabels" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Jump_To_PossibilityList(contextMenuStrip, menuItems, _cache.LanguageProject.LexDbOA.DialectLabelsOA, slice, AreaServices.DialectsListEditMachineName, ref wantSeparator, separatorInsertLocation: separatorInsertLocation);

			/*
		      <item command="CmdJumpToDiscChartMarkerList" />
				    <command id="CmdJumpToDiscChartMarkerList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="chartmarkEdit" className="CmPossibility" ownerClass="DsDiscourseData" ownerField="ChartMarkers" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Jump_To_PossibilityList(contextMenuStrip, menuItems, _cache.LanguageProject.DiscourseDataOA.ChartMarkersOA, slice, AreaServices.ChartmarkEditMachineName, ref wantSeparator, separatorInsertLocation: separatorInsertLocation);

			/*
		      <item command="CmdJumpToDiscChartTemplateList" />
				    <command id="CmdJumpToDiscChartTemplateList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="charttempEdit" className="CmPossibility" ownerClass="DsDiscourseData" ownerField="ConstChartTempl" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Jump_To_PossibilityList(contextMenuStrip, menuItems, _cache.LanguageProject.DiscourseDataOA.ConstChartTemplOA, slice, AreaServices.CharttempEditMachineName, ref wantSeparator, separatorInsertLocation: separatorInsertLocation);

			/*
		      <item command="CmdJumpToEducationList" />
				    <command id="CmdJumpToEducationList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="educationEdit" className="CmPossibility" ownerClass="LangProject" ownerField="Education" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Jump_To_PossibilityList(contextMenuStrip, menuItems, _cache.LanguageProject.EducationOA, slice, AreaServices.EducationEditMachineName, ref wantSeparator, separatorInsertLocation: separatorInsertLocation);

			/*
		      <item command="CmdJumpToRoleList" />
				    <command id="CmdJumpToRoleList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="roleEdit" className="CmPossibility" ownerClass="LangProject" ownerField="Roles" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Jump_To_PossibilityList(contextMenuStrip, menuItems, _cache.LanguageProject.RolesOA, slice, AreaServices.RoleEditMachineName, ref wantSeparator, separatorInsertLocation: separatorInsertLocation);

			/*
		      <item command="CmdJumpToExtNoteTypeList" />
				    <command id="CmdJumpToExtNoteTypeList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="extNoteTypeEdit" className="CmPossibility" ownerClass="LexDb" ownerField="ExtendedNoteTypes" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Jump_To_PossibilityList(contextMenuStrip, menuItems, _cache.LanguageProject.LexDbOA.ExtendedNoteTypesOA, slice, AreaServices.ExtNoteTypeEditMachineName, ref wantSeparator, separatorInsertLocation: separatorInsertLocation);

			/*
		      <item command="CmdJumpToComplexEntryTypeList" />
				    <command id="CmdJumpToComplexEntryTypeList" label="Show in Complex Form Types list" message="JumpToTool">
				      <parameters tool="complexEntryTypeEdit" className="LexEntryType" ownerClass="LexDb" ownerField="ComplexEntryTypes" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Jump_To_PossibilityList(contextMenuStrip, menuItems, _cache.LanguageProject.LexDbOA.ComplexEntryTypesOA, slice, AreaServices.ComplexEntryTypeEditMachineName, ref wantSeparator, LexEntryTypeTags.kClassName, AreaResources.Show_in_Complex_Form_Types_list, separatorInsertLocation);

			/*
		      <item command="CmdJumpToVariantEntryTypeList" />
				    <command id="CmdJumpToVariantEntryTypeList" label="Show in Variant Types list" message="JumpToTool">
				      <parameters tool="variantEntryTypeEdit" className="LexEntryType" ownerClass="LexDb" ownerField="VariantEntryTypes" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Jump_To_PossibilityList(contextMenuStrip, menuItems, _cache.LanguageProject.LexDbOA.VariantEntryTypesOA, slice, AreaServices.VariantEntryTypeEditMachineName, ref wantSeparator, LexEntryTypeTags.kClassName, AreaResources.Show_in_Variant_Types_list, separatorInsertLocation);

			/*
		      <item command="CmdJumpToTextMarkupTagsList" />
				    <command id="CmdJumpToTextMarkupTagsList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="textMarkupTagsEdit" className="CmPossibility" ownerClass="LangProject" ownerField="TextMarkupTags" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Jump_To_PossibilityList(contextMenuStrip, menuItems, _cache.LanguageProject.TextMarkupTagsOA, slice, AreaServices.TextMarkupTagsEditMachineName, ref wantSeparator, separatorInsertLocation: separatorInsertLocation);

			/*
		      <item command="CmdJumpToLexRefTypeList" />
				    <command id="CmdJumpToLexRefTypeList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="lexRefEdit" className="LexRefType" ownerClass="LexDb" ownerField="References" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Jump_To_PossibilityList(contextMenuStrip, menuItems, _cache.LanguageProject.LexDbOA.ReferencesOA, slice, AreaServices.LexRefEditMachineName, ref wantSeparator, LexRefTypeTags.kClassName, separatorInsertLocation: separatorInsertLocation);

			/*
		      <item command="CmdJumpToLanguagesList" />
				    <command id="CmdJumpToLanguagesList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="languagesListEdit" className="CmPossibility" ownerClass="LexDb" ownerField="Languages" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Jump_To_PossibilityList(contextMenuStrip, menuItems, _cache.LanguageProject.LexDbOA.LanguagesOA, slice, AreaServices.LanguagesListEditMachineName, ref wantSeparator, separatorInsertLocation: separatorInsertLocation);

			/*
		      <item command="CmdJumpToLocationList" />
				    <command id="CmdJumpToLocationList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="locationsEdit" className="CmLocation" ownerClass="LangProject" ownerField="Locations" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Jump_To_PossibilityList(contextMenuStrip, menuItems, _cache.LanguageProject.LocationsOA, slice, AreaServices.LocationsEditMachineName, ref wantSeparator, CmLocationTags.kClassName, separatorInsertLocation: separatorInsertLocation);

			/*
		      <item command="CmdJumpToPublicationList" />
				    <command id="CmdJumpToPublicationList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="publicationsEdit" className="CmPossibility" ownerClass="LexDb" ownerField="PublicationTypes" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Jump_To_PossibilityList(contextMenuStrip, menuItems, _cache.LanguageProject.LexDbOA.PublicationTypesOA, slice, AreaServices.PublicationsEditMachineName, ref wantSeparator, separatorInsertLocation: separatorInsertLocation);

			/*
		      <item command="CmdJumpToMorphTypeList" />
				    <command id="CmdJumpToMorphTypeList" label="Show in Morpheme Types list" message="JumpToTool">
				      <parameters tool="morphTypeEdit" className="MoMorphType" ownerClass="LexDb" ownerField="MorphTypes" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Jump_To_PossibilityList(contextMenuStrip, menuItems, _cache.LanguageProject.LexDbOA.MorphTypesOA, slice, AreaServices.MorphTypeEditMachineName, ref wantSeparator, MoMorphTypeTags.kClassName, AreaResources.Show_in_Morpheme_Types_list, separatorInsertLocation);

			/*
		      <item command="CmdJumpToPeopleList" />
				    <command id="CmdJumpToPeopleList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="peopleEdit" className="CmPerson" ownerClass="LangProject" ownerField="People" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Jump_To_PossibilityList(contextMenuStrip, menuItems, _cache.LanguageProject.PeopleOA, slice, AreaServices.PeopleEditMachineName, ref wantSeparator, CmPersonTags.kClassName, separatorInsertLocation: separatorInsertLocation);

			/*
		      <item command="CmdJumpToPositionList" />
				    <command id="CmdJumpToPositionList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="positionsEdit" className="CmPossibility" ownerClass="LangProject" ownerField="Positions" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Jump_To_PossibilityList(contextMenuStrip, menuItems, _cache.LanguageProject.PositionsOA, slice, AreaServices.PositionsEditMachineName, ref wantSeparator, separatorInsertLocation: separatorInsertLocation);

			/*
		      <item command="CmdJumpToRestrictionsList" />
				    <command id="CmdJumpToRestrictionsList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="restrictionsEdit" className="CmPossibility" ownerClass="LangProject" ownerField="Restrictions" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Jump_To_PossibilityList(contextMenuStrip, menuItems, _cache.LanguageProject.RestrictionsOA, slice, AreaServices.RestrictionsEditMachineName, ref wantSeparator, separatorInsertLocation: separatorInsertLocation);

			/*
		      <item command="CmdJumpToSemanticDomainList" />
				    <command id="CmdJumpToSemanticDomainList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="semanticDomainEdit" className="CmSemanticDomain" ownerClass="LangProject" ownerField="SemanticDomainList" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Jump_To_PossibilityList(contextMenuStrip, menuItems, _cache.LanguageProject.SemanticDomainListOA, slice, AreaServices.SemanticDomainEditMachineName, ref wantSeparator, CmSemanticDomainTags.kClassName, separatorInsertLocation: separatorInsertLocation);

			/*
		      <item command="CmdJumpToGenreList" />
				    <command id="CmdJumpToGenreList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="genresEdit" className="CmPossibility" ownerClass="LangProject" ownerField="GenreList" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Jump_To_PossibilityList(contextMenuStrip, menuItems, _cache.LanguageProject.GenreListOA, slice, AreaServices.GenresEditMachineName, ref wantSeparator, separatorInsertLocation: separatorInsertLocation);

			/*
		      <item command="CmdJumpToSenseTypeList" />
				    <command id="CmdJumpToSenseTypeList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="senseTypeEdit" className="CmPossibility" ownerClass="LexDb" ownerField="SenseTypes" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Jump_To_PossibilityList(contextMenuStrip, menuItems, _cache.LanguageProject.LexDbOA.SenseTypesOA, slice, AreaServices.SenseTypeEditMachineName, ref wantSeparator, separatorInsertLocation: separatorInsertLocation);

			/*
		      <item command="CmdJumpToStatusList" />
				    <command id="CmdJumpToStatusList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="statusEdit" className="CmPossibility" ownerClass="LangProject" ownerField="Status" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Jump_To_PossibilityList(contextMenuStrip, menuItems, _cache.LanguageProject.StatusOA, slice, AreaServices.StatusEditMachineName, ref wantSeparator, separatorInsertLocation: separatorInsertLocation);

			/*
		      <item command="CmdJumpToTranslationTypeList" />
				    <command id="CmdJumpToTranslationTypeList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="translationTypeEdit" className="CmPossibility" ownerClass="LangProject" ownerField="TranslationTags" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Jump_To_PossibilityList(contextMenuStrip, menuItems, _cache.LanguageProject.TranslationTagsOA, slice, AreaServices.TranslationTypeEditMachineName, ref wantSeparator, separatorInsertLocation: separatorInsertLocation);

			/*
		      <item command="CmdJumpToUsageTypeList" />
				    <command id="CmdJumpToUsageTypeList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="usageTypeEdit" className="CmPossibility" ownerClass="LexDb" ownerField="UsageTypes" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Jump_To_PossibilityList(contextMenuStrip, menuItems, _cache.LanguageProject.LexDbOA.UsageTypesOA, slice, AreaServices.UsageTypeEditMachineName, ref wantSeparator, separatorInsertLocation: separatorInsertLocation);

			/*
		      <item command="CmdJumpToRecordTypeList" />
				    <command id="CmdJumpToRecordTypeList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="recTypeEdit" className="CmPossibility" ownerClass="RnResearchNbk" ownerField="RecTypes" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Jump_To_PossibilityList(contextMenuStrip, menuItems, _cache.LanguageProject.ResearchNotebookOA.RecTypesOA, slice, AreaServices.RecTypeEditMachineName, ref wantSeparator, separatorInsertLocation: separatorInsertLocation);

			/*
		      <item command="CmdJumpToTimeOfDayList" />
				    <command id="CmdJumpToTimeOfDayList" label="Show in {0} list" message="JumpToTool">
				      <parameters tool="timeOfDayEdit" className="CmPossibility" ownerClass="LangProject" ownerField="TimeOfDay" />
				    </command>
			*/
			ConditionallyAddJumpToToolMenuItem_Overload_Jump_To_PossibilityList(contextMenuStrip, menuItems, _cache.LanguageProject.TimeOfDayOA, slice, AreaServices.TimeOfDayEditMachineName, ref wantSeparator, separatorInsertLocation: separatorInsertLocation);

			/*
		      <item label="-" translate="do not translate" />
			*/
			separatorInsertLocation = menuItems.Count - 1;
			wantSeparator = separatorInsertLocation > 0;

			/*
		      <item command="CmdShowSubentryUnderComponent" />
				    <command id="CmdShowSubentryUnderComponent" label="Show Subentry under this Component" message="AddComponentToPrimary">
				      <parameters tool="lexiconEdit" className="LexEntryRef" />
				    </command>
			*/
			var ler = _dataTree.CurrentSlice.MyCmObject as ILexEntryRef;
			ICmObject target = null;
			var selectedComponentHvo = slice.Flid != LexEntryRefTags.kflidComponentLexemes || ler == null ? 0 : slice.GetSelectionHvoFromControls();
			var menuIsChecked = false;
			var visibleAndEnabled = false;
			if (selectedComponentHvo != 0)
			{
				target = _cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(selectedComponentHvo);
				if (ler != null && ler.RefType == LexEntryRefTags.krtComplexForm && (target is ILexEntry || target is ILexSense))
				{
					visibleAndEnabled = true;
					menuIsChecked = ler.PrimaryLexemesRS.Contains(target); // LT-11292
				}
			}
			visibleAndEnabled = visibleAndEnabled && _currentTool.MachineName == AreaServices.LexiconEditMachineName;
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
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.CmdMoveTargetToPreviousInSequence), AreaResources.Move_Left);
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
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.CmdMoveTargetToNextInSequence), AreaResources.Move_Right);
					menu.Enabled = enabled;
				}
			}

			// End: <menu id="mnuReferenceChoices">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private bool CanJumpToToolAndFilterAnthroItem
		{
			get
			{
				var fieldName = XmlUtils.GetOptionalAttributeValue(_dataTree.CurrentSlice.ConfigurationNode, "field");
				return !string.IsNullOrEmpty(fieldName) && fieldName.Equals("AnthroCodes");
			}
		}

		private void JumpToToolAndFilterAnthroItem(object sender, EventArgs e)
		{
			var obj = ((VectorReferenceView)((VectorReferenceLauncher)_dataTree.CurrentSlice.Control).MainControl).SelectedObject;
			if (obj == null)
			{
				return;
			}
			var hvo = obj.Hvo;

			FwLinkArgs link = new FwAppArgs(_cache.ProjectId.Handle, (string)((ToolStripMenuItem)sender).Tag, Guid.Empty);
			var additionalProps = link.LinkProperties;
			additionalProps.Add(new LinkProperty(LanguageExplorerConstants.SuspendLoadListUntilOnChangeFilter, link.ToolName));
			additionalProps.Add(new LinkProperty("LinkSetupInfo", "FilterAnthroItems"));
			additionalProps.Add(new LinkProperty("HvoOfAnthroItem", hvo.ToString(CultureInfo.InvariantCulture)));
			LinkHandler.PublishFollowLinkMessage(_flexComponentParameters.Publisher, link);
		}

		private void VisibleComplexForm_Clicked(object sender, EventArgs e)
		{
			var currentSlice = _dataTree.CurrentSlice;
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
			var currentSlice = _dataTree.CurrentSlice;
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

		private void ConditionallyAddJumpToToolMenuItem_Overload_Also_Rans(ContextMenuStrip contextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>> menuItems, Slice slice, string targetToolName, ref bool wantSeparator, string className, string menuLabel, int separatorInsertLocation = 0)
		{
			// 6 users in PopupContextMenuCreatorMethod_mnuReferenceChoices
			var selectedObject = _recordList.CurrentObject;
			var asReferenceVectorSlice = slice as ReferenceVectorSlice; // May be null.
			if (asReferenceVectorSlice != null)
			{
				// Dig out selected item in slice.
				var vectorReferenceLauncher = (VectorReferenceLauncher)asReferenceVectorSlice.Control;
				var vectorReferenceView = (VectorReferenceView)vectorReferenceLauncher.MainControl;
				selectedObject = vectorReferenceView.SelectedObject;
			}
			else
			{
				var asAtomicReferenceSlice = slice as AtomicReferenceSlice; // May be null.
				if (asAtomicReferenceSlice != null)
				{
					// Dig out selected item in slice.
					var atomicReferenceLauncher = (AtomicReferenceLauncher)asAtomicReferenceSlice.Control;
					var atomicReferenceView = (AtomicReferenceView)atomicReferenceLauncher.MainControl;
					selectedObject = _cache.ServiceLocator.GetObject(_cache.GetManagedSilDataAccess().get_ObjectProp(atomicReferenceView.Object.Hvo, slice.Flid));
				}
			}
			if (selectedObject is ICmPossibility)
			{
				// These are handled by a dedicated method.
				return;
			}

			AreaServices.ConditionallyAddJumpToToolMenuItem(contextMenuStrip, menuItems, _cache, _flexComponentParameters.Publisher,
				_recordList.CurrentObject, selectedObject, _sharedEventHandlers.Get(AreaServices.JumpToTool),
				_currentTool.MachineName, targetToolName, ref wantSeparator, className, menuLabel, separatorInsertLocation);
		}

		/// <summary>
		/// Used only for possibility lists and possibilities.
		/// </summary>
		private void ConditionallyAddJumpToToolMenuItem_Overload_Jump_To_PossibilityList(ContextMenuStrip contextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>> menuItems, ICmPossibilityList listToJumpTo, Slice slice, string targetToolName, ref bool wantSeparator, string className = CmPossibilityTags.kClassName, string menuLabel = null, int separatorInsertLocation = 0)
		{
			// 28 users in PopupContextMenuCreatorMethod_mnuReferenceChoices
			Guard.AgainstNull(listToJumpTo, nameof(listToJumpTo));

			if (menuLabel == null)
			{
				menuLabel = string.Format(AreaResources.Show_in_0_list, listToJumpTo.ShortName);
			}
			var selectedObject = _recordList.CurrentObject as ICmPossibility; // Most likely it is null.
			var asReferenceVectorSlice = slice as ReferenceVectorSlice; // May be null.
			if (asReferenceVectorSlice != null)
			{
				// Dig out selected item in slice.
				var vectorReferenceLauncher = (VectorReferenceLauncher)asReferenceVectorSlice.Control;
				var vectorReferenceView = (VectorReferenceView)vectorReferenceLauncher.MainControl;
				selectedObject = vectorReferenceView.SelectedObject as ICmPossibility;
			}
			var asAtomicReferenceSlice = slice as AtomicReferenceSlice; // May be null.
			if (asAtomicReferenceSlice != null)
			{
				// Dig out selected item in slice.
				var atomicReferenceLauncher = (AtomicReferenceLauncher)asAtomicReferenceSlice.Control;
				var atomicReferenceView = (AtomicReferenceView)atomicReferenceLauncher.MainControl;
				selectedObject = _cache.ServiceLocator.GetObject(_cache.GetManagedSilDataAccess().get_ObjectProp(atomicReferenceView.Object.Hvo, slice.Flid)) as ICmPossibility;
			}
			Debug.Assert(selectedObject != null, "Found another path that isn't returning a possibility. Find it and make it work here.");
			if (listToJumpTo != selectedObject.OwningList)
			{
				return;
			}

			if (wantSeparator)
			{
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip, separatorInsertLocation);
				wantSeparator = false;
			}
			var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.JumpToTool), menuLabel);
			menu.Tag = new List<object> { _flexComponentParameters.Publisher, targetToolName, selectedObject };
		}

		#region "mnuEnvReferenceChoices"

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> PopupContextMenuCreatorMethod_mnuEnvReferenceChoices(Slice slice, ContextMenuName contextMenuId)
		{
			Require.That(contextMenuId == ContextMenuName.mnuEnvReferenceChoices, $"Expected argument value of '{ContextMenuName.mnuEnvReferenceChoices.ToString()}', but got '{contextMenuId.ToString()}' instead.");

			// Start: <menu id="mnuEnvReferenceChoices">

			var contextMenuStrip = new ContextMenuStrip
			{
				Name = ContextMenuName.mnuEnvReferenceChoices.ToString()
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(8);

			if (CanJumpToEnvironmentList)
			{
				/*
				  <item command="CmdJumpToEnvironmentList" />
						<command id="CmdJumpToEnvironmentList" label="Show in Environments list" message="JumpToTool">
						  <parameters tool="EnvironmentEdit" className="PhEnvironment" ownerClass="PhPhonData" ownerField="Environments" />
						</command>
				*/
				var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.JumpToTool), AreaResources.Show_in_Environments_list);
				menu.Tag = new List<object> { _flexComponentParameters.Publisher, AreaServices.EnvironmentEditMachineName, _dataTree };
			}

			PartiallySharedForToolsWideMenuHelper.CreateShowEnvironmentErrorMessageContextMenuStripMenus(slice, menuItems, contextMenuStrip);

			PartiallySharedForToolsWideMenuHelper.CreateCommonEnvironmentContextMenuStripMenus(slice, menuItems, contextMenuStrip);

			// End: <menu id="mnuEnvReferenceChoices">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private bool CanJumpToEnvironmentList
		{
			get
			{
				var currentSlice = _dataTree.CurrentSlice;
				if (_currentTool.MachineName == AreaServices.EnvironmentEditMachineName && currentSlice.MyCmObject == _recordList.CurrentObject || currentSlice.MyCmObject.IsOwnedBy(_recordList.CurrentObject))
				{
					return false;
				}
				if (currentSlice.MyCmObject.ClassID == PhEnvironmentTags.kClassId)
				{
					return true;
				}
				return _dataTree.Cache.DomainDataByFlid.MetaDataCache.GetBaseClsId(currentSlice.MyCmObject.ClassID) == PhEnvironmentTags.kClassId;
			}
		}

		#endregion "mnuEnvReferenceChoices"
	}
}