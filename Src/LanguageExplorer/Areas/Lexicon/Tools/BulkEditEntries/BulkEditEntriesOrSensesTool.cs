// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.XPath;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.Filters;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.DomainImpl;
using SIL.LCModel.DomainServices;
using SIL.ObjectModel;
using SIL.Xml;

namespace LanguageExplorer.Areas.Lexicon.Tools.BulkEditEntries
{
	/// <summary>
	/// ITool implementation for the "bulkEditEntriesOrSenses" tool in the "lexicon" area.
	/// </summary>
	[Export(LanguageExplorerConstants.LexiconAreaMachineName, typeof(ITool))]
	internal sealed class BulkEditEntriesOrSensesTool : ITool
	{
		private BulkEditEntriesOrSensesMenuHelper _toolMenuHelper;
		private PaneBarContainer _paneBarContainer;
		private RecordBrowseView _recordBrowseView;
		private IRecordList _recordList;

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to a component.
		/// </remarks>
		public void Deactivate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			// This will also remove any event handlers set up by the tool's UserControl instances that may have registered event handlers.
			majorFlexComponentParameters.UiWidgetController.RemoveToolHandlers();
			PaneBarContainerFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _paneBarContainer);

			// Dispose these after the main UI stuff.
			_toolMenuHelper.Dispose();

			_toolMenuHelper = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			// Crashes in RecordList "CheckExpectedListItemsClassInSync" with: for some reason BulkEditBar.ExpectedListItemsClassId({0}) does not match SortItemProvider.ListItemsClass({1}).
			// BulkEditBar expected 5002, but
			// SortItemProvider was: 5035
			if (_recordList == null)
			{
				_recordList = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(AreaServices.EntriesOrChildren, majorFlexComponentParameters.StatusBar, FactoryMethod);
			}
			var root = XDocument.Parse(LexiconResources.BulkEditEntriesOrSensesToolParameters).Root;
			var parametersElement = root.Element("parameters");
			parametersElement.Element("includeColumns").ReplaceWith(XElement.Parse(LexiconResources.LexiconBrowseDialogColumnDefinitions));
			OverrideServices.OverrideVisibiltyAttributes(parametersElement.Element("columns"), root.Element("overrides"));
			_recordBrowseView = new RecordBrowseView(parametersElement, majorFlexComponentParameters.LcmCache, _recordList, majorFlexComponentParameters.UiWidgetController);
			_toolMenuHelper = new BulkEditEntriesOrSensesMenuHelper(majorFlexComponentParameters, this, _recordBrowseView, _recordList);
			_paneBarContainer = PaneBarContainerFactory.Create(majorFlexComponentParameters.FlexComponentParameters, majorFlexComponentParameters.MainCollapsingSplitContainer, _recordBrowseView);
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
			_recordBrowseView.BrowseViewer.BrowseView.PrepareToRefresh();
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		public void FinishRefresh()
		{
			_recordList.ReloadIfNeeded();
			((DomainDataByFlidDecoratorBase)_recordList.VirtualListPublisher).Refresh();
		}

		/// <summary>
		/// The properties are about to be saved, so make sure they are all current.
		/// Add new ones, as needed.
		/// </summary>
		public void EnsurePropertiesAreCurrent()
		{
		}

		#endregion

		#region Implementation of IMajorFlexUiComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName => LanguageExplorerConstants.BulkEditEntriesOrSensesMachineName;

		/// <summary>
		/// User-visible localized component name.
		/// </summary>
		public string UiName => StringTable.Table.LocalizeLiteralValue(LanguageExplorerConstants.BulkEditEntriesOrSensesUiName);

		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area for the tool.
		/// </summary>
		[field: Import(LanguageExplorerConstants.LexiconAreaMachineName)]
		public IArea Area { get; private set; }

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.BrowseView.SetBackgroundColor(Color.Magenta);

		#endregion

		internal static IRecordList FactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Guard.AssertThat(recordListId == AreaServices.EntriesOrChildren, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create one with an id of '{AreaServices.EntriesOrChildren}'.");
			/*
            <clerk id="entriesOrChildren">
              <recordList owner="LexDb" property="Entries">
                <!-- by default load for Entries but can be for AllSenses too -->
                <dynamicloaderinfo assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.EntriesOrChildClassesRecordList" />
                <PartOwnershipTree>
                  <!-- the ClassOwnershipTree describes the relative relationship between the target classes in the possible source properties
								 loaded by this list. This especially helps in maintaining the CurrentIndex when switching from one property to the next. -->
                  <ClassOwnershipTree>
                    <LexEntry sourceField="Entries">
                      <LexEntryRef sourceField="AllEntryRefs" altSourceField="ComplexEntryTypes:AllComplexEntryRefPropertyTargets;VariantEntryTypes:AllVariantEntryRefPropertyTargets" />
                      <LexPronunciation sourceField="AllPossiblePronunciations" />
                      <LexEtymology sourceField="AllPossibleEtymologies" />
                      <MoForm sourceField="AllPossibleAllomorphs" />
                      <LexSense sourceField="AllSenses">
                        <LexExampleSentence sourceField="AllExampleSentenceTargets">
                          <CmTranslation sourceField="AllExampleTranslationTargets" />
                        </LexExampleSentence>
                        <LexExtendedNote sourceField="AllExtendedNoteTargets" />
                        <CmPicture sourceField="AllPossiblePictures" />
                      </LexSense>
                    </LexEntry>
                  </ClassOwnershipTree>
                  <ParentClassPathsToChildren>
                    <!-- ClassOwnershipPaths describes how to get from the parent ListItemsClass to the destinationClass objects
									 of the list properties -->
                    <part id="LexEntry-Jt-AllPossiblePronunciations" type="jtview">
                      <seq class="LexEntry" field="Pronunciations" firstOnly="true" layout="empty">
                        <int class="LexPronunciation" field="Self" />
                      </seq>
                      <!-- NOTE: AllPossiblePronunciations can also have LexEntry items, since it is a ghost field -->
                    </part>
                    <part id="LexEntry-Jt-AllPossibleEtymologies" type="jtview">
                      <seq class="LexEntry" field="Etymology" firstOnly="true" layout="empty">
                        <int class="LexEtymology" field="Self" />
                      </seq>
                      <!-- NOTE: AllPossibleEtymologies can also have LexEntry items, since it is a ghost field -->
                    </part>
                    <part id="LexEntry-Jt-AllComplexEntryRefPropertyTargets" type="jtview">
                      <seq class="LexEntry" field="EntryRefs" firstOnly="true" layout="empty">
                        <int class="LexEntryRef" field="Self" />
                      </seq>
                      <!-- NOTE: AllComplexEntryRefPropertyTargets can also have LexEntry items, since it is a ghost field -->
                    </part>
                    <part id="LexEntry-Jt-AllVariantEntryRefPropertyTargets" type="jtview">
                      <seq class="LexEntry" field="EntryRefs" firstOnly="true" layout="empty">
                        <int class="LexEntryRef" field="Self" />
                      </seq>
                      <!-- NOTE: AllVariantEntryRefPropertyTargets can also have LexEntry items, since it is a ghost field -->
                    </part>
                    <part id="LexEntry-Jt-AllPossibleAllomorphs" type="jtview">
                      <seq class="LexEntry" field="AlternateForms" firstOnly="true" layout="empty">
                        <int class="MoForm" field="Self" />
                      </seq>
                      <!-- NOTE: AllPossibleAllomorphs can also have LexEntry items, since it is a ghost field -->
                    </part>
                    <part id="LexEntry-Jt-AllEntryRefs" type="jtview">
                      <seq class="LexEntry" field="EntryRefs" firstOnly="true" layout="empty">
                        <int class="LexEntryRef" field="Self" />
                      </seq>
                    </part>
                    <part id="LexEntry-Jt-AllSenses" type="jtview">
                      <seq class="LexEntry" field="AllSenses" firstOnly="true" layout="empty">
                        <int class="LexSense" field="Self" />
                      </seq>
                    </part>
                    <!-- the next item is needed to prevent a crash -->
                    <part id="LexSense-Jt-AllSenses" type="jtview">
                      <obj class="LexSense" field="Self" firstOnly="true" layout="empty" />
                    </part>
                    <part id="LexEntry-Jt-AllExampleSentenceTargets" type="jtview">
                      <seq class="LexEntry" field="AllSenses" firstOnly="true" layout="empty">
                        <seq class="LexSense" field="Examples" firstOnly="true" layout="empty">
                          <int class="LexExampleSentence" field="Self" />
                        </seq>
                      </seq>
                    </part>
                    <part id="LexSense-Jt-AllExampleSentenceTargets" type="jtview">
                      <seq class="LexSense" field="Examples" firstOnly="true" layout="empty">
                        <int class="LexExampleSentence" field="Self" />
                      </seq>
                    </part>
                    <part id="LexEntry-Jt-AllPossiblePictures" type="jtview">
                      <seq class="LexEntry" field="AllSenses" firstOnly="true" layout="empty">
                        <seq class="LexSense" field="Pictures" firstOnly="true" layout="empty">
                          <int class="CmPicture" field="Self" />
                        </seq>
                      </seq>
                    </part>
                    <part id="LexSense-Jt-AllPossiblePictures" type="jtview">
                      <seq class="LexSense" field="Pictures" firstOnly="true" layout="empty">
                        <int class="CmPicture" field="Self" />
                      </seq>
                    </part>
                    <part id="LexEntry-Jt-AllExampleTranslationTargets" type="jtview">
                      <seq class="LexEntry" field="AllSenses" firstOnly="true" layout="empty">
                        <seq class="LexSense" field="Examples" firstOnly="true" layout="empty">
                          <seq class="LexExampleSentence" field="Translations" firstOnly="true" layout="empty">
                            <int class="CmTranslation" field="Self" />
                          </seq>
                        </seq>
                      </seq>
                    </part>
                    <part id="LexSense-Jt-AllExampleTranslationTargets" type="jtview">
                      <seq class="LexSense" field="Examples" firstOnly="true" layout="empty">
                        <seq class="LexExampleSentence" field="Translations" firstOnly="true" layout="empty">
                          <int class="CmTranslation" field="Self" />
                        </seq>
                      </seq>
                    </part>
                    <part id="LexExampleSentence-Jt-AllExampleTranslationTargets" type="jtview">
                      <seq class="LexExampleSentence" field="Translations" firstOnly="true" layout="empty">
                        <int class="CmTranslation" field="Self" />
                      </seq>
                    </part>
                    <part id="LexEntry-Jt-AllExtendedNoteTargets" type="jtview">
                      <seq class="LexEntry" field="AllSenses" firstOnly="true" layout="empty">
                        <seq class="LexSense" field="ExtendedNote" firstOnly="true" layout="empty">
                          <int class="LexExtendedNote" field="Self" />
                        </seq>
                      </seq>
                    </part>
                    <part id="LexSense-Jt-AllExtendedNoteTargets" type="jtview">
                      <seq class="LexSense" field="ExtendedNote" firstOnly="true" layout="empty">
                        <int class="LexExtendedNote" field="Self" />
                      </seq>
                    </part>
                  </ParentClassPathsToChildren>
                </PartOwnershipTree>
              </recordList>
              <filters />
              <!-- only the default sortMethod is needed -->
              <sortMethods>
                <sortMethod label="Default" assemblyPath="Filters.dll" class="SIL.FieldWorks.Filters.PropertyRecordSorter" sortProperty="ShortName" />
                <sortMethod label="Primary Gloss" assemblyPath="Filters.dll" class="SIL.FieldWorks.Filters.PropertyRecordSorter" sortProperty="PrimaryGloss" />
              </sortMethods>
            </clerk>
			 */
			return new EntriesOrChildClassesRecordList(recordListId, statusBar, cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), cache.LanguageProject.LexDbOA);
		}

		/// <summary>
		/// Record list that can be used to bulk edit Entries or its child classes (e.g. Senses or Pronunciations).
		/// The class owning relationship between these properties can be defined by the destinationClass of the
		/// properties listed as 'sourceField' in the RecordList xml definition:
		/// <code>
		///		<ClassOwnershipTree>
		///			<LexEntry sourceField="Entries">
		///				<LexEntryRef sourceField="AllEntryRefs" altSourceField="ComplexEntryTypes:AllComplexEntryRefPropertyTargets;VariantEntryTypes:AllVariantEntryRefPropertyTargets" />
		///				<LexPronunciation sourceField="AllPossiblePronunciations"/>
		///				<LexEtymology sourceField="AllPossibleEtymologies" />
		///				<MoForm sourceField="AllPossibleAllomorphs" />
		///				<LexSense sourceField = "AllSenses" >
		///					<LexExampleSentence sourceField="AllExampleSentenceTargets">
		///						<CmTranslation sourceField = "AllExampleTranslationTargets" />
		///					</LexExampleSentence>
		///					<LexExtendedNote sourceField="AllExtendedNoteTargets" />
		///					<CmPicture sourceField = "AllPossiblePictures" />
		///				</LexSense>
		///			</LexEntry>
		///		</ClassOwnershipTree>
		/// </code>
		/// </summary>
		private sealed class EntriesOrChildClassesRecordList : RecordList, IMultiListSortItemProvider
		{
			private int m_flidEntries;
			private int m_prevFlid;
			private readonly IDictionary<int, bool> m_reloadNeededForProperty = new Dictionary<int, bool>();
			PartOwnershipTree _partOwnershipTree;
			// suspend loading the property until given a class by RecordBrowseView via
			// RecordList.OnChangeListItemsClass();
			private bool m_suspendReloadUntilOnChangeListItemsClass = true;

			/// <summary />
			internal EntriesOrChildClassesRecordList(string id, StatusBar statusBar, ISilDataAccessManaged decorator, ILexDb owner)
				: base(id, statusBar, decorator, false, new VectorPropertyParameterObject(owner, "Entries", decorator.MetaDataCache.GetFieldId(LexDbTags.kClassName, "Entries", false)), new Dictionary<string, PropertyRecordSorter>
					{
					{ AreaServices.Default, new PropertyRecordSorter(AreaServices.ShortName) },
					{ "PrimaryGloss", new PropertyRecordSorter("PrimaryGloss") }
					})
			{
			}

			#region Overrides of RecordList

			/// <summary>
			/// If m_flid is one of the special classes where we need to modify the expected destination class, do so.
			/// </summary>
			public override int ListItemsClass
			{
				get
				{
					var result = base.ListItemsClass;
					return result == 0 ? GhostParentHelper.GetBulkEditDestinationClass(m_cache, m_flid) : result;
				}
			}

			/// <summary>
			/// Initialize a FLEx component with the basic interfaces.
			/// </summary>
			/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
			public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
			{
				base.InitializeFlexComponent(flexComponentParameters);

				// suspend loading the property until given a class by RecordBrowseView via RecordList.OnChangeListItemsClass();
				m_suspendReloadUntilOnChangeListItemsClass = true;

				// Used for finding first relative of corresponding current object
				_partOwnershipTree = new PartOwnershipTree(m_cache, true);
				if (!m_reloadNeededForProperty.ContainsKey(m_flid))
				{
					m_reloadNeededForProperty.Add(m_flid, false);
				}
				m_flidEntries = m_flid;
			}

			protected override void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
				if (IsDisposed)
				{
					// No need to run it more than once.
					return;
				}

				if (disposing)
				{
					_partOwnershipTree?.Dispose();
				}
				_partOwnershipTree = null;

				base.Dispose(disposing);
			}

			public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
			{
				if (UpdatingList || m_reloadingList)
				{
					return; // we're already in the process of changing our list.
				}
				var fLoadSuppressed = m_requestedLoadWhileSuppressed;
				using (var luh = new ListUpdateHelper(new ListUpdateHelperParameterObject { MyRecordList = this }))
				{
					// don't reload the entire list via propchanges.  just indicate we need to reload.
					luh.TriggerPendingReloadOnDispose = false;
					base.PropChanged(hvo, tag, ivMin, cvIns, cvDel);

					// even if RecordList is in m_fUpdatingList mode, we still want to make sure
					// our alternate list figures out whether it needs to reload as well.
					if (UpdatingList)
					{
						TryHandleUpdateOrMarkPendingReload(hvo, tag, ivMin, cvIns, cvDel);
					}
					// If we edited the list of entries, all our properties are in doubt.
					if (tag == m_cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries)
					{
						foreach (var key in m_reloadNeededForProperty.Keys.ToArray())
						{
							m_reloadNeededForProperty[key] = true;
						}
					}
				}
				// The ListUpdateHelper doesn't always reload the list when it needs it.  See the
				// second bug listed in FWR-1081.
				if (m_requestedLoadWhileSuppressed && !fLoadSuppressed && !UpdatingList)
				{
					ReloadList();
				}
			}

			protected override void MarkEntriesForReload()
			{
				m_reloadNeededForProperty[m_flidEntries] = true;
			}

			protected override void FinishedReloadList()
			{
				m_reloadNeededForProperty[m_flid] = false;
			}

			public override bool RequestedLoadWhileSuppressed
			{
				get => base.RequestedLoadWhileSuppressed || (m_reloadNeededForProperty.ContainsKey(m_flid) && m_reloadNeededForProperty[m_flid]);
				set => base.RequestedLoadWhileSuppressed = value;
			}

			protected override bool NeedToReloadList()
			{
				return RequestedLoadWhileSuppressed;
			}

			public override bool RestoreListFrom(string pathname)
			{
				// If we are restoring, presumably the 'previous flid' (which should be the flid from the last complete
				// ReloadList) should match the flid that is current, which in turn should correspond to the saved list
				// of sorted objects.
				m_prevFlid = m_flid;
				return base.RestoreListFrom(pathname);
			}

			protected override void ReloadList(int newListItemsClass, int newTargetFlid, bool force)
			{
				// reload list if it differs from current target class (or if forcing a reload anyway).
				if (newListItemsClass != ListItemsClass || newTargetFlid != TargetFlid || force)
				{
					m_prevFlid = m_flid;
					TargetFlid = newTargetFlid;
					// m_owningObject is *always* the LexDB. All of the properties in PartOwnershipTree/ClassOwnershipTree
					// are virtual properties of the LexDb.
					m_flid = newTargetFlid;
					if (!m_reloadNeededForProperty.ContainsKey(m_flid))
					{
						m_reloadNeededForProperty.Add(m_flid, false);
					}
					if (m_flidEntries == 0 && newListItemsClass == LexEntryTags.kClassId)
					{
						m_flidEntries = m_flid;
					}
					CheckExpectedListItemsClassInSync(newListItemsClass, ListItemsClass);
					ReloadList();
				}
				// wait until afterwards, so that the dispose will reload the list for the first time
				// whether or not we've loaded yet.
				if (m_suspendReloadUntilOnChangeListItemsClass)
				{
					m_suspendReloadUntilOnChangeListItemsClass = false;
					ReloadList();
				}
				// otherwise, we'll assume there isn't anything to load.
			}

			protected override void ReloadList()
			{
				if (m_suspendReloadUntilOnChangeListItemsClass)
				{
					m_requestedLoadWhileSuppressed = true;
					return;
				}
				base.ReloadList();
			}

			protected override int GetNewCurrentIndex(List<IManyOnePathSortItem> newSortedObjects, int hvoCurrentBeforeGetObjectSet)
			{
				if (hvoCurrentBeforeGetObjectSet == 0)
				{
					return -1;
				}
				var repo = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
				if (!repo.IsValidObjectId(hvoCurrentBeforeGetObjectSet))
				{
					return -1;
				}

				// first lookup the old object in the new list, just in case it's there.
				var newIndex = base.GetNewCurrentIndex(newSortedObjects, hvoCurrentBeforeGetObjectSet);
				if (newIndex != -1)
				{
					return newIndex;
				}
				var newListItemsClass = ListItemsClass;
				// NOTE: the class of hvoBeforeListChange could be different then prevListItemsClass, if the item is a ghost (owner).
				var classOfObsoleteCurrentObj = repo.GetObject(hvoCurrentBeforeGetObjectSet).ClassID;
				if (m_prevFlid == m_flid || DomainObjectServices.IsSameOrSubclassOf(VirtualListPublisher.MetaDataCache, classOfObsoleteCurrentObj, newListItemsClass))
				{
					// nothing else we can do, since we've already looked that class of object up
					// in our newSortedObjects.
					return -1;
				}
				// we've changed list items class, so find the corresponding new object.
				var relatives = _partOwnershipTree.FindCorrespondingItemsInCurrentList(m_prevFlid, new HashSet<int>(new[] { hvoCurrentBeforeGetObjectSet }), m_flid, out var commonAncestors);
				var newHvoRoot = relatives.Count > 0 ? relatives.ToArray()[0] : 0;
				var hvoCommonAncestor = commonAncestors.Count > 0 ? commonAncestors.ToArray()[0] : 0;
				if (newHvoRoot == 0 && hvoCommonAncestor != 0)
				{
					// see if we can find the parent in the list (i.e. it might be in a ghost list)
					newIndex = base.GetNewCurrentIndex(newSortedObjects, hvoCommonAncestor);
					if (newIndex != -1)
					{
						return newIndex;
					}
				}
				return base.GetNewCurrentIndex(newSortedObjects, newHvoRoot);
			}

			/// <summary>
			/// these bulk edit column filters/sorters can be considered Entries based, so that they can possibly
			/// be reusable in other Entries record lists (e.g. the one used by Lexicon Edit, Browse, Dictionary).
			/// </summary>
			/// <param name="sorterOrFilter"></param>
			/// <returns></returns>
			protected override string PropertyTableId(string sorterOrFilter)
			{
				return $"LexDb.Entries_{sorterOrFilter}";
			}

			#endregion

			/// <summary>
			/// Stores the target flid (that is, typically the field we want to bulk edit) most recently passed to ReloadList.
			/// </summary>
			private int TargetFlid { get; set; }

			#region IMultiListSortItemProvider Members

			/// <summary>
			/// See documentation for IMultiListSortItemProvider
			/// </summary>
			public object ListSourceToken => m_flid;

			/// <summary>
			/// See documentation for IMultiListSortItemProvider
			/// </summary>
			public void ConvertItemsToRelativesThatApplyToCurrentList(ref IDictionary<int, object> oldItems)
			{
				var oldItemsToRemove = new HashSet<int>();
				var itemsToAdd = new HashSet<int>();
				// Create a PartOwnershipTree in a mode that can return more than one descendent relative.
				using (var partOwnershipTree = new PartOwnershipTree(m_cache, false))
				{
					foreach (var oldItem in oldItems)
					{
						var dictOneOldItem = new Dictionary<int, object>
					{
						{
							oldItem.Key,
							oldItem.Value
						}
					};
						var relatives = FindCorrespondingItemsInCurrentList(dictOneOldItem, partOwnershipTree);
						// remove the old item if we found relatives we could convert over to.
						if (relatives.Count > 0)
						{
							itemsToAdd.UnionWith(relatives);
							oldItemsToRemove.Add(oldItem.Key);
						}
					}
				}
				foreach (var itemToRemove in oldItemsToRemove)
				{
					oldItems.Remove(itemToRemove);
				}
				// complete any conversions by adding its relatives.
				var sourceTag = ListSourceToken;
				foreach (var relativeToAdd in itemsToAdd)
				{
					if (!oldItems.ContainsKey(relativeToAdd))
					{
						oldItems.Add(relativeToAdd, sourceTag);
					}
				}
			}

			#endregion

			private HashSet<int> FindCorrespondingItemsInCurrentList(IDictionary<int, object> itemAndListSourceTokenPairs, PartOwnershipTree pot)
			{
				// create a reverse index of classes to a list of items
				var sourceFlidsToItems = MapSourceFlidsToItems(itemAndListSourceTokenPairs);
				var relativesInCurrentList = new HashSet<int>();
				foreach (var sourceFlidToItems in sourceFlidsToItems)
				{
					relativesInCurrentList.UnionWith(pot.FindCorrespondingItemsInCurrentList(sourceFlidToItems.Key, sourceFlidToItems.Value, m_flid, out _));
				}
				return relativesInCurrentList;
			}

			private IDictionary<int, HashSet<int>> MapSourceFlidsToItems(IDictionary<int, object> itemAndListSourceTokenPairs)
			{
				var sourceFlidsToItems = new Dictionary<int, HashSet<int>>();
				foreach (var itemAndSourceTag in itemAndListSourceTokenPairs)
				{
					if ((int)itemAndSourceTag.Value == m_flid)
					{
						// skip items in the current list
						// (we're trying to lookup relatives to items that are a part of
						// a previous list, not the current one)
						continue;
					}
					if (!sourceFlidsToItems.TryGetValue((int)itemAndSourceTag.Value, out var itemsInSourceFlid))
					{
						itemsInSourceFlid = new HashSet<int>();
						sourceFlidsToItems.Add((int)itemAndSourceTag.Value, itemsInSourceFlid);
					}
					itemsInSourceFlid.Add(itemAndSourceTag.Key);
				}
				return sourceFlidsToItems;
			}

			/// <summary>
			/// Helper for handling switching between related (ListItemClass) lists.
			/// </summary>
			private sealed class PartOwnershipTree : DisposableBase
			{
				private XElement _classOwnershipTreeElement;
				private XElement _parentClassPathsToChildrenElementClone;
				private LcmCache Cache { get; set; }

				/// <summary />
				internal PartOwnershipTree(LcmCache cache, bool returnFirstDescendentOnly)
				{
					var returnFirstDescendentOnlyAsString = returnFirstDescendentOnly.ToString().ToLowerInvariant();
					var partOwnershipTreeSpec = XDocument.Parse(LexiconResources.EntriesOrChildrenClerkPartOwnershipTree).Root;
					Cache = cache;
					_classOwnershipTreeElement = partOwnershipTreeSpec.Element("ClassOwnershipTree");
					_parentClassPathsToChildrenElementClone = partOwnershipTreeSpec.Element("ParentClassPathsToChildren").Clone();
					// now go through the seq specs and set the "firstOnly" to the requested value.
					foreach (var seqElement in _parentClassPathsToChildrenElementClone.Elements("part").Elements("seq"))
					{
						var firstOnlyAttribute = seqElement.Attribute("firstOnly");
						if (firstOnlyAttribute == null)
						{
							// Create the first only attribute, with no value (reset soon).
							firstOnlyAttribute = new XAttribute("firstOnly", string.Empty);
							seqElement.Add(firstOnlyAttribute);
						}
						firstOnlyAttribute.Value = returnFirstDescendentOnlyAsString;
					}
				}

				#region DisposableBase overrides

				/// <summary />
				protected override void DisposeUnmanagedResources()
				{
					Cache = null;
					_classOwnershipTreeElement = null;
					_parentClassPathsToChildrenElementClone = null;
				}

				/// <inheritdoc />
				protected override void Dispose(bool disposing)
				{
					Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + " ******");
					base.Dispose(disposing);
				}

				#endregion DisposableBase overrides

				/// <summary>
				/// Map itemsBeforeListChange (associated with flidForItemsBeforeListChange)
				/// to those in the current list (associated with flidForCurrentList)
				/// and provide a set of common ancestors.
				/// </summary>
				internal ISet<int> FindCorrespondingItemsInCurrentList(int flidForItemsBeforeListChange, ISet<int> itemsBeforeListChange, int flidForCurrentList, out ISet<int> commonAncestors)
				{
					var relatives = new HashSet<int>();
					commonAncestors = new HashSet<int>();
					var newListItemsClass = GhostParentHelper.GetBulkEditDestinationClass(Cache, flidForCurrentList);
					var prevListItemsClass = GhostParentHelper.GetBulkEditDestinationClass(Cache, flidForItemsBeforeListChange);
					var relationshipOfTarget = FindTreeRelationship(prevListItemsClass, newListItemsClass);
					// if new listListItemsClass is same as the given object, there's nothing more we have to do.
					switch (relationshipOfTarget)
					{
						case RelationshipOfRelatives.Sibling:
							{
								Debug.Fail("Sibling relationships are not supported.");
								// no use for this currently.
								break;
							}
						case RelationshipOfRelatives.Ancestor:
							{
								var gph = GetGhostParentHelper(flidForItemsBeforeListChange);
								// the items (e.g. senses) are owned by the new class (e.g. entry),
								// so find the (new class) ancestor for each item.
								foreach (var hvoBeforeListChange in itemsBeforeListChange)
								{
									int hvoAncestorOfItem;
									if (gph != null && gph.GhostOwnerClass == newListItemsClass && gph.IsGhostOwnerClass(hvoBeforeListChange))
									{
										// just add the ghost owner, as the ancestor relative,
										// since it's already in the newListItemsClass
										hvoAncestorOfItem = hvoBeforeListChange;
									}
									else
									{
										var obj = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoBeforeListChange);
										hvoAncestorOfItem = obj.OwnerOfClass(newListItemsClass).Hvo;
									}
									relatives.Add(hvoAncestorOfItem);
								}
								commonAncestors = relatives;
								break;
							}
						case RelationshipOfRelatives.Descendent:
						case RelationshipOfRelatives.Cousin:
							{
								var newClasses = new HashSet<int>(Cache.GetManagedMetaDataCache().GetAllSubclasses(newListItemsClass));
								foreach (var hvoBeforeListChange in itemsBeforeListChange)
								{
									if (!Cache.ServiceLocator.IsValidObjectId(hvoBeforeListChange))
									{
										continue; // skip this one.
									}
									if (newClasses.Contains(Cache.ServiceLocator.GetObject(hvoBeforeListChange).ClassID))
									{
										// strangely, the 'before' object is ALREADY one that is valid for, and presumably in,
										// the destination property. One way this happens is at startup, when switching to
										// the saved target column, but we have also saved the list of objects.
										relatives.Add(hvoBeforeListChange);
										continue;
									}
									int hvoCommonAncestor;
									if (relationshipOfTarget == RelationshipOfRelatives.Descendent)
									{
										// the item is the ancestor
										hvoCommonAncestor = hvoBeforeListChange;
									}
									else
									{
										// the item and its cousins have a common ancestor.
										hvoCommonAncestor = GetHvoCommonAncestor(hvoBeforeListChange, prevListItemsClass, newListItemsClass);
									}
									// only add the descendants/cousins if we haven't already processed the ancestor.
									if (!commonAncestors.Contains(hvoCommonAncestor))
									{
										var gph = GetGhostParentHelper(flidForCurrentList);
										var descendents = GetDescendents(hvoCommonAncestor, flidForCurrentList);
										if (descendents.Count > 0)
										{
											relatives.UnionWith(descendents);
										}
										else if (gph != null && gph.IsGhostOwnerClass(hvoCommonAncestor))
										{
											relatives.Add(hvoCommonAncestor);
										}
										commonAncestors.Add(hvoCommonAncestor);
									}
								}
								break;
							}
					}
					return relatives;
				}

				private GhostParentHelper GetGhostParentHelper(int flidToTry)
				{
					return GhostParentHelper.CreateIfPossible(Cache.ServiceLocator, flidToTry);
				}

				private ISet<int> GetDescendents(int hvoCommonAncestor, int relativesFlid)
				{
					var listPropertyName = Cache.MetaDataCacheAccessor.GetFieldName(relativesFlid);
					var parentObjName = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoCommonAncestor).ClassName;
					var xpathToPart = "./part[@id='" + parentObjName + "-Jt-" + listPropertyName + "']";
					var pathSpec = _parentClassPathsToChildrenElementClone.XPathSelectElement(xpathToPart);
					Debug.Assert(pathSpec != null, $"You are experiencing a rare and difficult-to-reproduce error (LT- 11443 and linked issues). If you can add any information to the issue or fix it please do. If JohnT is available please call him over. Expected to find part ({xpathToPart}) in ParentClassPathsToChildren");
					if (pathSpec == null)
					{
						return new HashSet<int>(); // This just means we don't find a related object. Better than crashing, but not what we intend.
					}
					// get the part spec that gives us the path from obsolete current (parent) list item object
					// to the new one.
					var vc = new XmlBrowseViewVc(Cache, null);
					var parentItem = new ManyOnePathSortItem(hvoCommonAncestor, null, null);
					var collector = new ItemsCollectorEnv(null, Cache.MainCacheAccessor, hvoCommonAncestor);
					var doc = XDocument.Load(pathSpec.ToString());
					vc.DisplayCell(parentItem, doc.Root.Elements().First(), hvoCommonAncestor, collector);
					if (collector.HvosCollectedInCell != null && collector.HvosCollectedInCell.Count > 0)
					{
						return new HashSet<int>(collector.HvosCollectedInCell);
					}
					return new HashSet<int>();
				}

				private RelationshipOfRelatives FindTreeRelationship(int prevListItemsClass, int newListItemsClass)
				{
					RelationshipOfRelatives relationshipOfTarget;
					if (DomainObjectServices.IsSameOrSubclassOf(Cache.DomainDataByFlid.MetaDataCache, prevListItemsClass, newListItemsClass))
					{
						relationshipOfTarget = RelationshipOfRelatives.Sibling;
					}
					else
					{
						// lookup new class in ownership tree and decide how to select the most related object
						var newClassName = Cache.DomainDataByFlid.MetaDataCache.GetClassName(newListItemsClass);
						var prevClassName = Cache.DomainDataByFlid.MetaDataCache.GetClassName(prevListItemsClass);
						var prevClassNode = _classOwnershipTreeElement.XPathSelectElement(".//" + prevClassName);
						var newClassNode = _classOwnershipTreeElement.XPathSelectElement(".//" + newClassName);
						// determine if prevClassName is owned (has ancestor) by the new.
						var fNewIsAncestorOfPrev = prevClassNode.XPathSelectElement("ancestor::" + newClassName) != null;
						if (fNewIsAncestorOfPrev)
						{
							relationshipOfTarget = RelationshipOfRelatives.Ancestor;
						}
						else
						{
							// okay, now find most related object in new items list.
							var fNewIsChildOfPrev = newClassNode.XPathSelectElement("ancestor::" + prevClassName) != null;
							relationshipOfTarget = fNewIsChildOfPrev ? RelationshipOfRelatives.Descendent : RelationshipOfRelatives.Cousin;
						}
					}
					return relationshipOfTarget;
				}

				private XElement GetTreeNode(int classId)
				{
					return _classOwnershipTreeElement.XPathSelectElement($".//{Cache.DomainDataByFlid.MetaDataCache.GetClassName(classId)}");
				}

				private int GetHvoCommonAncestor(int hvoBeforeListChange, int prevListItemsClass, int newListItemsClass)
				{
					var prevClassNode = GetTreeNode(prevListItemsClass);
					var newClassNode = GetTreeNode(newListItemsClass);
					var hvoCommonAncestor = 0;
					// NOTE: the class of hvoBeforeListChange could be different then prevListItemsClass, if the item is a ghost (owner).
					var classOfHvoBeforeListChange = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoBeforeListChange).ClassID;
					// so go up the parent of the previous one until it's an ancestor of the newClass.
					var ancestorOfPrev = prevClassNode.Parent;
					while (ancestorOfPrev != null)
					{
						if (newClassNode.XPathSelectElement("ancestor::" + ancestorOfPrev.Name) != null)
						{
							var commonAncestor = ancestorOfPrev;
							var classCommonAncestor = Cache.MetaDataCacheAccessor.GetClassId(commonAncestor.Name.ToString());
							if (DomainObjectServices.IsSameOrSubclassOf(Cache.DomainDataByFlid.MetaDataCache, classOfHvoBeforeListChange, classCommonAncestor))
							{
								hvoCommonAncestor = hvoBeforeListChange;
							}
							else
							{
								var obj = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoBeforeListChange);
								hvoCommonAncestor = obj.OwnerOfClass(classCommonAncestor).Hvo;
							}
							break;
						}
						ancestorOfPrev = ancestorOfPrev.Parent;
					}
					return hvoCommonAncestor;
				}

				/// <summary>
				/// Describes the relationship between list classes in a PartOwnershipTree
				/// </summary>
				private enum RelationshipOfRelatives
				{
					/// <summary>
					/// Items (e.g. Entries) in the same ItemsListClass (LexEntry) are siblings.
					/// </summary>
					Sibling,
					/// <summary>
					/// (Includes Parent relationship)
					/// </summary>
					Ancestor,
					/// <summary>
					/// (Includes Child relationship)
					/// </summary>
					Descendent,
					/// <summary>
					/// Entries.Allomorphs and Entries.Senses are cousins.
					/// </summary>
					Cousin
				}
			}
		}

		private sealed class BulkEditEntriesOrSensesMenuHelper : IDisposable
		{
			private MajorFlexComponentParameters _majorFlexComponentParameters;
			private RecordBrowseView _recordBrowseView;
			private IRecordList _recordList;
			private List<ToolStripMenuItem> _jumpMenus;
			private PartiallySharedForToolsWideMenuHelper _partiallySharedForToolsWideMenuHelper;
			private SharedLexiconToolsUiWidgetHelper _sharedLexiconToolsUiWidgetHelper;

			internal BulkEditEntriesOrSensesMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool tool, RecordBrowseView recordBrowseView, IRecordList recordList)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
				Guard.AgainstNull(tool, nameof(tool));
				Guard.AgainstNull(recordBrowseView, nameof(recordBrowseView));
				Guard.AgainstNull(recordList, nameof(recordList));

				_majorFlexComponentParameters = majorFlexComponentParameters;
				_recordBrowseView = recordBrowseView;
				_recordList = recordList;
				_partiallySharedForToolsWideMenuHelper = new PartiallySharedForToolsWideMenuHelper(majorFlexComponentParameters, recordList);
				_sharedLexiconToolsUiWidgetHelper = new SharedLexiconToolsUiWidgetHelper(_majorFlexComponentParameters, _recordList);
				_jumpMenus = new List<ToolStripMenuItem>(3);
				var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(tool);
				SetupUiWidgets(toolUiWidgetParameterObject);
				majorFlexComponentParameters.UiWidgetController.AddHandlers(toolUiWidgetParameterObject);
				CreateBrowseViewContextMenu();
			}

			private void SetupUiWidgets(ToolUiWidgetParameterObject toolUiWidgetParameterObject)
			{
				_sharedLexiconToolsUiWidgetHelper.SetupToolUiWidgets(toolUiWidgetParameterObject, new HashSet<Command> { Command.CmdGoToEntry, Command.CmdInsertLexEntry });
			}

			private void CreateBrowseViewContextMenu()
			{
				// The actual menu declaration has a gazillion menu items, but only two of them are seen in this tool (plus the separator).
				// Start: <menu id="mnuBrowseView" (partial) >
				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuBrowseView.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(3);

				var publisher = _majorFlexComponentParameters.FlexComponentParameters.Publisher;
				var jumpEventHandler = _majorFlexComponentParameters.SharedEventHandlers.GetEventHandler(Command.CmdJumpToTool);
				// Show Entry in Lexicon: AreaResources.ksShowEntryInLexicon
				// <command id="CmdEntryJumpToDefault" label="Show Entry in Lexicon" message="JumpToTool">
				var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, jumpEventHandler, LanguageExplorerResources.ksShowEntryInLexicon);
				menu.Tag = new List<object> { publisher, LanguageExplorerConstants.LexiconEditMachineName, _recordList };
				_jumpMenus.Add(menu);

				// Show Entry in Concordance: AreaResources.Show_Entry_In_Concordance
				// <item command="CmdEntryJumpToConcordance"/>
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, jumpEventHandler, AreaResources.Show_Entry_In_Concordance);
				menu.Tag = new List<object> { publisher, LanguageExplorerConstants.ConcordanceMachineName, _recordList };
				_jumpMenus.Add(menu);

				// <command id="CmdSenseJumpToConcordance" label="Show Sense in Concordance" message="JumpToTool">
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, jumpEventHandler, AreaResources.Show_Sense_in_Concordance);
				menu.Tag = new List<object> { publisher, LanguageExplorerConstants.ConcordanceMachineName, _recordList };
				_jumpMenus.Add(menu);

				// End: <menu id="mnuBrowseView" (partial) >
				_recordBrowseView.ContextMenuStrip = contextMenuStrip;
			}

			#region Implementation of IDisposable
			private bool _isDisposed;

			~BulkEditEntriesOrSensesMenuHelper()
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
					// No need to run it more than once.
					return;
				}
				if (disposing)
				{
					_sharedLexiconToolsUiWidgetHelper.Dispose();
					var jumpEventHandler = _majorFlexComponentParameters.SharedEventHandlers.GetEventHandler(Command.CmdJumpToTool);
					foreach (var menu in _jumpMenus)
					{
						menu.Click -= jumpEventHandler;
						menu.Dispose();
					}
					_jumpMenus.Clear();
					_recordBrowseView.ContextMenuStrip.Dispose();
					_recordBrowseView.ContextMenuStrip = null;
					_partiallySharedForToolsWideMenuHelper.Dispose();
				}
				_majorFlexComponentParameters = null;
				_recordBrowseView = null;
				_recordList = null;
				_jumpMenus = null;
				_partiallySharedForToolsWideMenuHelper = null;
				_sharedLexiconToolsUiWidgetHelper = null;

				_isDisposed = true;
			}
			#endregion
		}
	}
}
