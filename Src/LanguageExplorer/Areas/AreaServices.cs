// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.DictionaryConfiguration;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// Area level services
	/// </summary>
	internal static class AreaServices
	{
		#region General area/tool
		internal const string AreaChoice = "areaChoice";
		internal const string ToolChoice = "toolChoice";
		internal const string ToolForAreaNamed_ = "ToolForAreaNamed_";
		internal const string InitialArea = "InitialArea";
		internal const string InitialAreaMachineName = LexiconAreaMachineName;
		#endregion General area/tool

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
		#endregion Lexicon area

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
		#endregion Text and Words area

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
		#endregion Grammar area

		#region Notebook area
		internal const string NotebookAreaMachineName = "notebook";
		internal const string NotebookAreaDefaultToolMachineName = NotebookEditToolMachineName;
			internal const string NotebookEditToolMachineName = "notebookEdit";
			internal const string NotebookBrowseToolMachineName = "notebookBrowse";
			internal const string NotebookDocumentToolMachineName = "notebookDocument";
		#endregion Notebook area

		#region Lists area
		internal const string ListsAreaMachineName = "lists";
		internal const string ListsAreaDefaultToolMachineName = DomainTypeEditMachineName;
			internal const string DomainTypeEditMachineName = "domainTypeEdit";
			internal const string AnthroEditMachineName = "anthroEdit";
			internal const string ComplexEntryTypeEditMachineName = "complexEntryTypeEdit";
			internal const string ConfidenceEditMachineName = "confidenceEdit";
			internal const string DialectsListEditMachineName = "dialectsListEdit";
			internal const string ChartmarkEditMachineName = "chartmarkEdit";
			internal const string CharttempEditMachineName = "charttempEdit";
			internal const string EducationEditMachineName = "educationEdit";
			internal const string RoleEditMachineName = "roleEdit";
			internal const string ExtNoteTypeEditMachineName = "extNoteTypeEdit";
			internal const string FeatureTypesAdvancedEditMachineName = "featureTypesAdvancedEdit";
			internal const string GenresEditMachineName = "genresEdit";
			internal const string LanguagesListEditMachineName = "languagesListEdit";
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
		#endregion Lists area

		#region menus
		internal const string mnuBrowseView = "mnuBrowseView";
		internal const string mnuEnvChoices = "mnuEnvChoices";
		internal const string mnuObjectChoices = "mnuObjectChoices";
		internal const string mnuReferenceChoices = "mnuReferenceChoices";
		internal const string mnuEnvReferenceChoices = "mnuEnvReferenceChoices";
		#endregion menus

		#region commands
		internal const string JumpToTool = "JumpToTool";
		internal const string InsertCategory = "InsertCategory";
		internal const string DataTreeDelete = "DataTreeDelete";
		internal const string CmdDeleteSelectedObject = "CmdDeleteSelectedObject";
		internal const string DeleteSelectedBrowseViewObject = "DeleteSelectedBrowseViewObject";
		internal const string LexiconLookup = "LexiconLookup";
		#endregion commands

		#region LanguageExplorer.DictionaryConfiguration.ImageHolder smallCommandImages image constants
		internal const int MoveUp = 12;
		internal const int MoveRight = 13;
		internal const int MoveDown = 14;
		internal const int MoveLeft = 15;
		#endregion LanguageExplorer.DictionaryConfiguration.ImageHolder smallCommandImages image constants

		#region Random strings
		internal const string Default = "Default";
		internal const string ShortName = "ShortName";
		internal const string PartOfSpeechGramInfo = "PartOfSpeechGramInfo";
		internal const string WordPartOfSpeech = "WordPartOfSpeech";
		internal const string OwningField = "field";
		internal const string ClassName = "className";
		internal const string OwnerClassName = "ownerClassName";
		internal const string BaseUowMessage = "baseUowMessage";
		internal const string PanelMenuId = "left";
		internal const string MainItem = "MainItem";
		internal const string SubItem = "Subitem";
		#endregion Random strings

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
			if (importDlg is IFormReplacementNeeded && oldWsUser != cache.WritingSystemFactory.UserWs)
			{
				flexApp.ReplaceMainWindow(mainWindow);
			}
			else if (importDlg is IImportForm)
			{
				// Make everything we've imported visible.
				mainWindow.RefreshAllViews();
			}
		}

		public static bool UpdateCachedObjects(LcmCache cache, FieldDescription fd)
		{
			// We need to find every instance of a reference from this flid to that custom list and delete it!
			// I can't figure out any other way of ensuring that EnsureCompleteIncomingRefs doesn't try to refer
			// to a non-existent flid at some point.
			var owningListGuid = fd.ListRootId;
			if (owningListGuid == Guid.Empty)
			{
				return false;
			}
			var list = cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>().GetObject(owningListGuid);
			// This is only a problem for fields referencing a custom list
			if (list.Owner != null)
			{
				// Not a custom list.
				return false;
			}
			bool fchanged;
			var type = fd.Type;
			var objRepo = cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			var objClass = fd.Class;
			var flid = fd.Id;
			var ddbf = cache.DomainDataByFlid;
			switch (type)
			{
				case CellarPropertyType.ReferenceSequence: // drop through
				case CellarPropertyType.ReferenceCollection:
					// Handle multiple reference fields
					// Is there a way to do this in LINQ without repeating the get_VecSize call?
					var tupleList = new List<Tuple<int, int>>();
					tupleList.AddRange(objRepo.AllInstances(objClass).Where(obj => ddbf.get_VecSize(obj.Hvo, flid) > 0)
						.Select(obj => new Tuple<int, int>(obj.Hvo, ddbf.get_VecSize(obj.Hvo, flid))));
					NonUndoableUnitOfWorkHelper.Do(cache.ActionHandlerAccessor, () =>
					{
						foreach (var partResult in tupleList)
						{
							ddbf.Replace(partResult.Item1, flid, 0, partResult.Item2, null, 0);
						}
					});
					fchanged = tupleList.Any();
					break;
				case CellarPropertyType.ReferenceAtomic:
					// Handle atomic reference fields
					// If there's a value for (Hvo, flid), nullify it!
					var objsWithDataThisFlid = new List<int>();
					objsWithDataThisFlid.AddRange(objRepo.AllInstances(objClass).Where(obj => ddbf.get_ObjectProp(obj.Hvo, flid) > 0).Select(obj => obj.Hvo));
					// Delete these references
					NonUndoableUnitOfWorkHelper.Do(cache.ActionHandlerAccessor, () =>
					{
						foreach (var hvo in objsWithDataThisFlid)
						{
							ddbf.SetObjProp(hvo, flid, LcmCache.kNullHvo);
						}
					});
					fchanged = objsWithDataThisFlid.Any();
					break;
				default:
					fchanged = false;
					break;
			}
			return fchanged;
		}

		private static bool IsCustomList(LcmCache cache, Guid owningListGuid)
		{
			// Custom lists are unowned.
			var list = cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>().GetObject(owningListGuid);
			return list.Owner == null;
		}

		/// <summary>
		/// Tell the user why we aren't jumping to his record
		/// </summary>
		internal static void GiveSimpleWarning(Form form, string helpFile, ExclusionReasonCode xrc)
		{
			string caption;
			string reason;
			string shlpTopic;
			switch (xrc)
			{
				case ExclusionReasonCode.NotInPublication:
					caption = AreaResources.ksEntryNotPublished;
					reason = AreaResources.ksEntryNotPublishedReason;
					shlpTopic = "User_Interface/Menus/Edit/Find_a_lexical_entry.htm";
					break;
				case ExclusionReasonCode.ExcludedHeadword:
					caption = AreaResources.ksMainNotShown;
					reason = AreaResources.ksMainNotShownReason;
					shlpTopic = "khtpMainEntryNotShown";
					break;
				case ExclusionReasonCode.ExcludedMinorEntry:
					caption = AreaResources.ksMinorNotShown;
					reason = AreaResources.ksMinorNotShownReason;
					shlpTopic = "khtpMinorEntryNotShown";
					break;
				default:
					throw new ArgumentException("Unknown ExclusionReasonCode");
			}
			// TODO-Linux: Help is not implemented on Mono
			MessageBox.Show(form, string.Format(AreaResources.ksSelectedEntryNotInDict, reason), caption, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, 0, helpFile, HelpNavigator.Topic, shlpTopic);
		}

		/// <summary>
		/// See if a menu is visible/enabled that moves items down in an owning property.
		/// </summary>
		internal static bool CanMoveDownObjectInOwningSequence(DataTree dataTree, LcmCache cache, out bool visible)
		{
			visible = false;
			bool enabled;
			var type = CellarPropertyType.ReferenceAtomic;
			var sliceObject = dataTree.CurrentSlice.MyCmObject;
			var owningFlid = sliceObject.OwningFlid;
			if (owningFlid > 0)
			{
				type = (CellarPropertyType)cache.DomainDataByFlid.MetaDataCache.GetFieldType(owningFlid);
			}
			if (type != CellarPropertyType.OwningSequence && type != CellarPropertyType.ReferenceSequence)
			{
				visible = false;
				return false;
			}
			var owningObject = sliceObject.Owner;
			var chvo = cache.DomainDataByFlid.get_VecSize(owningObject.Hvo, owningFlid);
			if (chvo < 2)
			{
				enabled = false;
			}
			else
			{
				var hvo = cache.DomainDataByFlid.get_VecItem(owningObject.Hvo, owningFlid, 0);
				enabled = sliceObject.Hvo != hvo;
				// if the first LexEntryRef in LexEntry.EntryRefs is a complex form, and the
				// slice displays the second LexEntryRef in the sequence, then we can't move it
				// up, since the first slot is reserved for the complex form.
				if (enabled && owningFlid == LexEntryTags.kflidEntryRefs && cache.DomainDataByFlid.get_VecSize(hvo, LexEntryRefTags.kflidComplexEntryTypes) > 0)
				{
					enabled = sliceObject.Hvo != cache.DomainDataByFlid.get_VecItem(owningObject.Hvo, owningFlid, 1);
				}
				else
				{
					var sliceObjIdx = cache.DomainDataByFlid.GetObjIndex(owningObject.Hvo, owningFlid, sliceObject.Hvo);
					enabled = sliceObjIdx < chvo - 1;
				}
			}
			visible = true;
			return enabled;
		}

		/// <summary>
		/// See if a menu is visible/enabled that moves items up in an owning property.
		/// </summary>
		internal static bool CanMoveUpObjectInOwningSequence(DataTree dataTree, LcmCache cache, out bool visible)
		{
			visible = false;
			bool enabled;
			var type = CellarPropertyType.ReferenceAtomic;
			var sliceObject = dataTree.CurrentSlice.MyCmObject;
			var owningFlid = sliceObject.OwningFlid;
			if (owningFlid > 0)
			{
				type = (CellarPropertyType)cache.DomainDataByFlid.MetaDataCache.GetFieldType(owningFlid);
			}
			if (type != CellarPropertyType.OwningSequence && type != CellarPropertyType.ReferenceSequence)
			{
				return false;
			}
			var owningObject = sliceObject.Owner;
			var chvo = cache.DomainDataByFlid.get_VecSize(owningObject.Hvo, owningFlid);
			if (chvo < 2)
			{
				enabled = false;
			}
			else
			{
				var hvo = cache.DomainDataByFlid.get_VecItem(owningObject.Hvo, owningFlid, 0);
				enabled = sliceObject.Hvo != hvo;
				if (enabled && owningFlid == LexEntryTags.kflidEntryRefs && cache.DomainDataByFlid.get_VecSize(hvo, LexEntryRefTags.kflidComplexEntryTypes) > 0)
				{
					// if the first LexEntryRef in LexEntry.EntryRefs is a complex form, and the
					// slice displays the second LexEntryRef in the sequence, then we can't move it
					// up, since the first slot is reserved for the complex form.
					enabled = sliceObject.Hvo != cache.DomainDataByFlid.get_VecItem(owningObject.Hvo, owningFlid, 1);
				}
				else
				{
					var sliceObjIdx = cache.DomainDataByFlid.GetObjIndex(owningObject.Hvo, owningFlid, sliceObject.Hvo);
					enabled = sliceObjIdx > 0;
				}
			}
			visible = true;

			return enabled;
		}

		internal static void CreateDeleteMenuItem(List<Tuple<ToolStripMenuItem, EventHandler>> menuItems, ContextMenuStrip contextMenuStrip, Slice slice, string menuText, EventHandler deleteEventHandler)
		{
			var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, deleteEventHandler, menuText, image: LanguageExplorerResources.Delete);
			menu.Enabled = !slice.IsGhostSlice && slice.CanDeleteNow;
			if (!menu.Enabled)
			{
				menu.Text = $"{menuText} {StringTable.Table.GetString("(cannot delete this)")}";
			}
			menu.ImageTransparentColor = Color.Magenta;
			menu.Tag = slice;
		}

		internal static Dictionary<string, string> PopulateForMainItemInsert(ICmPossibilityList owningList, ICmPossibility currentPossibility, string baseUowMessage)
		{
			Guard.AgainstNull(owningList, nameof(owningList));
			// The list may be empty, so 'currentPossibility' may be null.
			var mdc = owningList.Cache.GetManagedMetaDataCache();
			var owningPossibility = currentPossibility?.OwningPossibility;
			string className;
			string ownerClassName;
			if (owningPossibility == null)
			{
				className = owningList.ClassName;
				ownerClassName = mdc.GetFieldName(CmPossibilityListTags.kflidPossibilities);
			}
			else
			{
				className = owningPossibility.ClassName;
				ownerClassName = mdc.GetFieldName(CmPossibilityTags.kflidSubPossibilities);
			}
			// Top level newbies are of the class specified in the list,
			// even for lists that allow for certain newbies to be of some other class, such as the variant entry ref type list.
			return CreateSharedInsertDictionary(mdc.GetClassName(owningList.ItemClsid), className, ownerClassName, baseUowMessage);
		}

		internal static Dictionary<string, string> PopulateForSubitemInsert(ICmPossibilityList owningList, ICmPossibility owningPossibility, string baseUowMessage)
		{
			// There has to be a list that ultimately owns a possibility.
			Guard.AgainstNull(owningList, nameof(owningList));

			var mdc = owningList.Cache.GetManagedMetaDataCache();
			var className = owningPossibility == null ? mdc.GetClassName(owningList.ItemClsid) : owningPossibility.ClassName;
			var ownerClassName = className;
			return CreateSharedInsertDictionary(className, ownerClassName, mdc.GetFieldName(CmPossibilityTags.kflidSubPossibilities), baseUowMessage);
		}

		private static Dictionary<string, string> CreateSharedInsertDictionary(string className, string ownerClassName, string owningFieldName, string baseUowMessage)
		{
			return new Dictionary<string, string>
			{
				{ ClassName, className },
				{ OwnerClassName, ownerClassName },
				{ OwningField, owningFieldName },
				{ BaseUowMessage, baseUowMessage }
			};
		}
	}
}